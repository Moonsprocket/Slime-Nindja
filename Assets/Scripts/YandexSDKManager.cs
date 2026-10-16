using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class YandexSDKManager : MonoBehaviour
{
    public static YandexSDKManager Instance { get; private set; }

    [System.Serializable]
    public class CloudData
    {
        public int bestScore;
        public int bestCombo;
        public int bestStreak;
        public int totalSliced;
        public int essence;
        public string unlockedBlades = "default";
        public string selectedBlade = "default";
    }

    Action<CloudData> pendingLoadCallback;
    Action onInterstitialOpen;
    Action<bool> onInterstitialClose;
    Action<string> onInterstitialError;

    Action onRewardedOpen;
    Action onRewardedReward;
    Action onRewardedClose;
    Action<string> onRewardedError;

    bool wasMutedBeforeAd;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    static extern void InitYandexSDK_JS();

    [DllImport("__Internal")]
    static extern void GameReady_JS();

    [DllImport("__Internal")]
    static extern void GameplayStart_JS();

    [DllImport("__Internal")]
    static extern void GameplayStop_JS();

    [DllImport("__Internal")]
    static extern void SaveUserData_JS(string jsonStr);

    [DllImport("__Internal")]
    static extern void LoadUserData_JS(string gameObjectName, string callbackMethodName);

    [DllImport("__Internal")]
    static extern void SubmitLeaderboardScore_JS(string leaderboardName, int score);

    [DllImport("__Internal")]
    static extern void ShowFullscreenAdv_JS(string gameObjectName, string onOpenCallback, string onCloseCallback, string onErrorCallback);

    [DllImport("__Internal")]
    static extern void ShowRewardedVideo_JS(string gameObjectName, string onOpenCallback, string onRewardedCallback, string onCloseCallback, string onErrorCallback);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInit()
    {
        if (Instance == null)
        {
            var go = new GameObject("YandexSDKManager");
            go.AddComponent<YandexSDKManager>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            InitYandexSDK_JS();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[YSDK] Init error: " + e.Message);
        }
#else
        Debug.Log("[YSDK Mock] InitYandexSDK initialized (Editor/Standalone mode)");
#endif
    }

    public void GameReady()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { GameReady_JS(); }
        catch (Exception e) { Debug.LogWarning("[YSDK] GameReady error: " + e.Message); }
#else
        Debug.Log("[YSDK Mock] GameReady() called");
#endif
    }

    public void GameplayStart()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { GameplayStart_JS(); }
        catch (Exception e) { Debug.LogWarning("[YSDK] GameplayStart error: " + e.Message); }
#else
        Debug.Log("[YSDK Mock] GameplayStart() called");
#endif
    }

    public void GameplayStop()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { GameplayStop_JS(); }
        catch (Exception e) { Debug.LogWarning("[YSDK] GameplayStop error: " + e.Message); }
#else
        Debug.Log("[YSDK Mock] GameplayStop() called");
#endif
    }

    public void SaveCloud(CloudData data)
    {
        string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
        try { SaveUserData_JS(json); }
        catch (Exception e) { Debug.LogWarning("[YSDK] SaveUserData error: " + e.Message); }
#else
        Debug.Log("[YSDK Mock] SaveUserData: " + json);
        PlayerPrefs.SetString("YSDK_Mock_CloudSave", json);
        PlayerPrefs.Save();
#endif
    }

    public void LoadCloud(Action<CloudData> onLoaded)
    {
        pendingLoadCallback = onLoaded;
#if UNITY_WEBGL && !UNITY_EDITOR
        try { LoadUserData_JS(gameObject.name, nameof(OnCloudDataReceived)); }
        catch (Exception e)
        {
            Debug.LogWarning("[YSDK] LoadUserData error: " + e.Message);
            OnCloudDataReceived("{}");
        }
#else
        Debug.Log("[YSDK Mock] LoadUserData called");
        string json = PlayerPrefs.GetString("YSDK_Mock_CloudSave", "{}");
        OnCloudDataReceived(json);
#endif
    }

    public void OnCloudDataReceived(string jsonStr)
    {
        CloudData data = null;
        try
        {
            if (!string.IsNullOrEmpty(jsonStr) && jsonStr != "{}")
            {
                data = JsonUtility.FromJson<CloudData>(jsonStr);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[YSDK] Failed to parse cloud save: " + e.Message);
        }

        if (data == null) data = new CloudData();
        pendingLoadCallback?.Invoke(data);
        pendingLoadCallback = null;
    }

    public void SubmitLeaderboardScore(string leaderboardName, int score)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { SubmitLeaderboardScore_JS(leaderboardName, score); }
        catch (Exception e) { Debug.LogWarning("[YSDK] SubmitLeaderboardScore error: " + e.Message); }
#else
        Debug.Log($"[YSDK Mock] SubmitLeaderboardScore: leaderboard='{leaderboardName}', score={score}");
#endif
    }

    public void ShowInterstitial(Action onOpen = null, Action<bool> onClose = null, Action<string> onError = null)
    {
        onInterstitialOpen = onOpen;
        onInterstitialClose = onClose;
        onInterstitialError = onError;

        BeforeAd();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            ShowFullscreenAdv_JS(gameObject.name, nameof(OnInterstitialOpened), nameof(OnInterstitialClosed), nameof(OnInterstitialErrored));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[YSDK] ShowFullscreenAdv error: " + e.Message);
            OnInterstitialErrored(e.Message);
        }
#else
        Debug.Log("[YSDK Mock] ShowInterstitial ad triggered");
        OnInterstitialOpened("");
        OnInterstitialClosed("true");
#endif
    }

    public void ShowRewarded(Action onOpen = null, Action onRewarded = null, Action onClose = null, Action<string> onError = null)
    {
        onRewardedOpen = onOpen;
        onRewardedReward = onRewarded;
        onRewardedClose = onClose;
        onRewardedError = onError;

        BeforeAd();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            ShowRewardedVideo_JS(gameObject.name, nameof(OnRewardedOpened), nameof(OnRewardedRewarded), nameof(OnRewardedClosed), nameof(OnRewardedErrored));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[YSDK] ShowRewardedVideo error: " + e.Message);
            OnRewardedErrored(e.Message);
        }
#else
        Debug.Log("[YSDK Mock] ShowRewarded ad triggered");
        OnRewardedOpened("");
        OnRewardedRewarded("");
        OnRewardedClosed("");
#endif
    }

    void BeforeAd()
    {
        wasMutedBeforeAd = AudioListener.pause;
        Sfx.SetMute(true);
        if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
        {
            GameManager.Instance.SetPause(true);
        }
    }

    void AfterAd()
    {
        if (!wasMutedBeforeAd)
        {
            Sfx.SetMute(false);
        }
    }

    public void OnInterstitialOpened(string unused)
    {
        onInterstitialOpen?.Invoke();
        onInterstitialOpen = null;
    }

    public void OnInterstitialClosed(string wasShownStr)
    {
        AfterAd();
        bool wasShown = wasShownStr == "true";
        onInterstitialClose?.Invoke(wasShown);
        onInterstitialClose = null;
    }

    public void OnInterstitialErrored(string error)
    {
        AfterAd();
        onInterstitialError?.Invoke(error);
        onInterstitialError = null;
    }

    public void OnRewardedOpened(string unused)
    {
        onRewardedOpen?.Invoke();
        onRewardedOpen = null;
    }

    public void OnRewardedRewarded(string unused)
    {
        onRewardedReward?.Invoke();
        onRewardedReward = null;
    }

    public void OnRewardedClosed(string unused)
    {
        AfterAd();
        onRewardedClose?.Invoke();
        onRewardedClose = null;
    }

    public void OnRewardedErrored(string error)
    {
        AfterAd();
        onRewardedError?.Invoke(error);
        onRewardedError = null;
    }
}
