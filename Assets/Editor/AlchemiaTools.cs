using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Помощники редактора: игра запускается по кнопке Play из любой открытой сцены,
// а исходные картинки и звуки создаются файлами, чтобы править их самостоятельно.
[InitializeOnLoad]
public static class AlchemiaTools
{
    public const string ArtFolder = "Assets/Resources/" + Art.Folder;
    public const string SoundFolder = "Assets/Resources/" + Sfx.CustomFolder;
    public const string MusicFolder = "Assets/Resources/Music";

    // В свежескачанном проекте Unity открывает пустую сцену. Чтобы Play всё равно запускал игру,
    // стартовой назначается первая сцена из настроек сборки.
    static AlchemiaTools()
    {
        EditorApplication.delayCall += () =>
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                if (asset != null) EditorSceneManager.playModeStartScene = asset;
                return;
            }
        };
    }

    // «Создать недостающие» не трогает существующие файлы — ваши правки в безопасности.
    // «Вернуть исходные» перезаписывает всё заново из формул.

    [MenuItem("Алхимия/Картинки: создать недостающие")]
    static void MissingArt() { ExportArt(false); }

    [MenuItem("Алхимия/Картинки: вернуть исходные (перезаписать все)")]
    static void ResetArt() { if (Confirm(ArtFolder)) ExportArt(true); }

    [MenuItem("Алхимия/Звуки: создать недостающие")]
    static void MissingSounds() { ExportSounds(false); }

    [MenuItem("Алхимия/Звуки: вернуть исходные (перезаписать все)")]
    static void ResetSounds() { if (Confirm(SoundFolder)) ExportSounds(true); }

    [MenuItem("Алхимия/Картинки: создать недостающие", true)]
    [MenuItem("Алхимия/Картинки: вернуть исходные (перезаписать все)", true)]
    [MenuItem("Алхимия/Звуки: создать недостающие", true)]
    [MenuItem("Алхимия/Звуки: вернуть исходные (перезаписать все)", true)]
    static bool NotPlaying()
    {
        return !Application.isPlaying;
    }

    static bool Confirm(string folder)
    {
        return EditorUtility.DisplayDialog("Вернуть исходные файлы?",
            "Все файлы в " + folder + " будут перезаписаны исходными. Ваши правки в них пропадут.", "Перезаписать", "Отмена");
    }

    static void ExportArt(bool overwrite)
    {
        int written = 0;
        Art.Export((name, draft) =>
        {
            string path = ArtFolder + "/" + name + ".png";
            if (!overwrite && File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Файлы вдвое чётче, чем нужно игре, а стена — втрое: есть запас для правки.
            var tex = draft.Bake(name == "Scene/Wall" ? 3 : 2, true);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            written++;
        });
        AssetDatabase.Refresh();
        Debug.Log("Картинок создано: " + written + ". Папка " + ArtFolder + ".");
    }

    static void ExportSounds(bool overwrite)
    {
        int written = 0;
        Directory.CreateDirectory(SoundFolder);
        foreach (var pair in Sfx.Render())
        {
            string path = SoundFolder + "/" + pair.Key + ".wav";
            if (!overwrite && File.Exists(path)) continue;
            WriteWav(path, pair.Value);
            written++;
        }
        AssetDatabase.Refresh();
        Debug.Log("Звуков создано: " + written + ". Папка " + SoundFolder + ".");
    }

    // Моно, 16 бит, без сжатия — такой файл откроет любой звуковой редактор.
    static void WriteWav(string path, float[] samples)
    {
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            int bytes = samples.Length * 2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + bytes);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Sfx.Rate);
            writer.Write(Sfx.Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(bytes);
            foreach (float sample in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32767f));
        }
    }
}

// Файлы в папках игры импортируются одинаково, поэтому свой файл достаточно просто положить в папку —
// настраивать импорт руками не нужно.
public class AlchemiaImport : AssetPostprocessor
{
    // Картинки: прозрачность, без сжатия, без подгонки размера.
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(AlchemiaTools.ArtFolder + "/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
    }

    // Звуки: без сжатия и сразу в памяти — короткие эффекты должны звучать без задержки, а петли без шва.
    // Музыка — длинный файл: его незачем целиком держать в памяти, он читается с диска по ходу игры.
    void OnPreprocessAudio()
    {
        var importer = (AudioImporter)assetImporter;
        if (assetPath.StartsWith(AlchemiaTools.MusicFolder + "/"))
        {
            var stream = importer.defaultSampleSettings;
            stream.loadType = AudioClipLoadType.Streaming;
            stream.compressionFormat = AudioCompressionFormat.Vorbis;
            stream.quality = 0.7f;
            importer.defaultSampleSettings = stream;
            return;
        }
        if (!assetPath.StartsWith(AlchemiaTools.SoundFolder + "/")) return;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
    }
}
