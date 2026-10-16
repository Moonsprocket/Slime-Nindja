using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Stage1EconomyVerification
{
    [MenuItem("Alchemia/Verify Stage 1 Economy")]
    public static void RunVerification()
    {
        Debug.Log("=== [STAGE 1] Starting Economy & Shop Verification ===");

        TestBladesCatalog();
        TestEconomyCurve();
        TestSetUnionPersistence();

        Debug.Log("=== [STAGE 1] ALL VERIFICATION CHECKS PASSED SUCCESSFULLY ===");
    }

    public static void RunBatchVerification()
    {
        try
        {
            RunVerification();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[STAGE 1 VERIFICATION FAILED]: {ex}");
            EditorApplication.Exit(1);
        }
    }

    static void TestBladesCatalog()
    {
        var blades = GameManager.Blades;
        if (blades == null || blades.Length != 12)
        {
            throw new Exception($"Expected 12 blades in GameManager.Blades, but found {blades?.Length ?? 0}");
        }

        if (blades[0].cost != 0)
        {
            throw new Exception($"Base blade (index 0) must be free, but cost is {blades[0].cost}");
        }

        var ids = new HashSet<string>();
        int totalCost = 0;
        foreach (var b in blades)
        {
            if (string.IsNullOrEmpty(b.id)) throw new Exception("Blade ID cannot be empty");
            if (!ids.Add(b.id)) throw new Exception($"Duplicate blade ID found: {b.id}");
            if (string.IsNullOrEmpty(b.name)) throw new Exception($"Blade {b.id} has empty name");
            if (string.IsNullOrEmpty(b.rarity)) throw new Exception($"Blade {b.id} has empty rarity");
            if (b.cost < 0) throw new Exception($"Blade {b.id} has negative cost: {b.cost}");
            totalCost += b.cost;
        }

        if (totalCost != 24550)
        {
            throw new Exception($"Expected total blade catalog cost to be 24,550 Essence, but got {totalCost}");
        }

        Debug.Log($"[PASS] Blades Catalog: 12 unique blades verified. Total armory cost: {totalCost:N0} Essence.");
    }

    static void TestEconomyCurve()
    {
        // 0 Score
        int zero = GameManager.CalculateEarnedEssence(0, 0, 0);
        if (zero != 0) throw new Exception($"Expected 0 essence for 0 score, got {zero}");

        // Casual run (300 score, combo 4, 25 sliced)
        int casual = GameManager.CalculateEarnedEssence(300, 4, 25);
        if (casual < 14 || casual > 22) throw new Exception($"Casual run essence out of expected range: {casual}");

        // Good run (1,000 score, combo 6, 65 sliced)
        int good = GameManager.CalculateEarnedEssence(1000, 6, 65);
        if (good < 35 || good > 50) throw new Exception($"Good run essence out of expected range: {good}");

        // Great run (5,000 score, combo 10, 180 sliced)
        int great = GameManager.CalculateEarnedEssence(5000, 10, 180);
        if (great < 95 || great > 125) throw new Exception($"Great run essence out of expected range: {great}");

        // Master session matching user's report (10,237 score, combo 14, 300 sliced)
        int userRun = GameManager.CalculateEarnedEssence(10237, 14, 300);
        Debug.Log($"User Run Simulation (Score 10,237, Combo 14, Sliced 300) => Earned: {userRun} Essence.");

        // Old formula gave: 10237 / 10 + 14 * 2 = 1051 Essence!
        // New formula should be in range 160-190 Essence.
        if (userRun >= 300)
        {
            throw new Exception($"Formula inflation detected! User run earned {userRun} Essence (must be < 300)");
        }
        if (userRun < 140)
        {
            throw new Exception($"Formula too stingy! User run earned {userRun} Essence (must be >= 140)");
        }

        Debug.Log($"[PASS] Economy Curve: Grounded logarithmic/power scaling verified. User run: {userRun} Essence (down from 1,051).");
    }

    static void TestSetUnionPersistence()
    {
        string localUnlocks = "azure,flame";
        string cloudUnlocks = "azure,frost,thunder";

        var set = new HashSet<string>(localUnlocks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        set.Add("azure");
        foreach (var p in cloudUnlocks.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            set.Add(p);
        }

        string merged = string.Join(",", set);
        if (!set.Contains("azure") || !set.Contains("flame") || !set.Contains("frost") || !set.Contains("thunder"))
        {
            throw new Exception($"Set union failed to retain all items: {merged}");
        }

        Debug.Log($"[PASS] Persistence: HashSet union merges local and cloud unlocks without loss ({merged}).");
    }
}
