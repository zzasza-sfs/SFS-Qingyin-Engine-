using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModLoader;
using Newtonsoft.Json;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Variables;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch-mode helpers for the QingYin engine part (also usable from the SFS menu in the editor).
///
/// The engine modules must live on the same GameObject as the Part component:
/// the game looks some of them up with GetComponent (EngineModule.Awake reads the
/// VariablesModule) and with GetComponentInParent (ColliderModule reads the
/// EngineModule of the part the flame hitbox touches). Keeping them on a child
/// object throws NullReferenceExceptions at runtime.
/// </summary>
public static class QingYinBatchTools
{
    const string PrefabPath = "Assets/Resources/Parts/Engines/QingYin Engine.prefab";
    const string BundleLabel = "sdsa mod";
    const string CacheFolder = "Assets/ModBuilder/Cache";
    const string DefaultPackName = "青鹰SE3.pack";

    static readonly Type[] RequiredOnRoot =
    {
        typeof(Part),
        typeof(OrientationModule),
        typeof(VariablesModule),
        typeof(MagnetModule),
        typeof(CustomSurfaces),
        typeof(FlowModule),
        typeof(EngineModule),
        typeof(TorqueModule),
        typeof(AudioModule),
        typeof(MoveModule),
    };

    [MenuItem("SFS/QingYin/Validate Prefab")]
    public static void Validate() => ValidateInternal(exitOnFailure: false);

    /// <summary>Validates the prefab and builds the pack; returns a non-zero exit code on failure.</summary>
    public static void ValidateAndBuild()
    {
        if (!ValidateInternal(exitOnFailure: true))
            return;
        BuildPack();
    }

    static bool ValidateInternal(bool exitOnFailure)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"Can't load prefab at {PrefabPath}");
            if (exitOnFailure) EditorApplication.Exit(1);
            return false;
        }

        bool ok = true;
        foreach (Type type in RequiredOnRoot)
        {
            if (root.GetComponent(type) == null)
            {
                Debug.LogError($"[QingYin] {type.Name} is not on the root GameObject.");
                ok = false;
            }
        }

        // Every module must also be reachable from the Part (children included).
        Part part = root.GetComponent<Part>();
        if (part == null)
        {
            Debug.LogError("[QingYin] Root GameObject has no Part component.");
            if (exitOnFailure) EditorApplication.Exit(1);
            return false;
        }

        EngineModule engine = root.GetComponent<EngineModule>();
        if (engine != null)
        {
            if (engine.source == null) { Debug.LogError("[QingYin] EngineModule.source (FlowModule) is not set."); ok = false; }
            if (engine.heatHolder == null) { Debug.LogError("[QingYin] EngineModule.heatHolder is not set."); ok = false; }
            if (engine.gimbal == null) { Debug.LogError("[QingYin] EngineModule.gimbal is not set."); ok = false; }
            if (engine.engineOn.referenceToVariables == null) { Debug.LogError("[QingYin] EngineModule.engineOn has no VariablesModule reference."); ok = false; }
        }

        FlowModule[] flows = root.GetComponentsInChildren<FlowModule>(true);
        if (flows.Length == 0) { Debug.LogError("[QingYin] No FlowModule found."); ok = false; }
        foreach (FlowModule flow in flows)
        {
            foreach (FlowModule.Flow source in flow.sources)
            {
                if (source.surface == null) { Debug.LogError("[QingYin] A FlowModule source has no surface."); ok = false; }
                if (source.resourceType == null) { Debug.LogError("[QingYin] A FlowModule source has no ResourceType."); ok = false; }
            }
        }

        int missingScripts = root.GetComponentsInChildren<Transform>(true)
            .SelectMany(t => t.GetComponents<Component>())
            .Count(c => c == null);
        if (missingScripts > 0) { Debug.LogError($"[QingYin] {missingScripts} missing script reference(s)."); ok = false; }

        Debug.Log(ok
            ? "[QingYin] Validation passed: all modules are on the root GameObject and all references resolve."
            : "[QingYin] Validation FAILED (see errors above).");
        if (!ok && exitOnFailure) EditorApplication.Exit(1);
        return ok;
    }

    /// <summary>Builds the pack non-interactively: used by -executeMethod in batch mode.</summary>
    public static void BuildPack() => BuildPack(DefaultPackName, null);

    public static void BuildPack(string packFileName, string outputDirectory)
    {
        if (string.IsNullOrEmpty(outputDirectory))
            outputDirectory = Path.Combine(Application.dataPath, "ModBuilder");

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(CacheFolder);

        PackData packData = AssetDatabase.LoadAssetAtPath<PackData>("Assets/Resources/Parts/Engines/SDSA.asset");
        if (packData == null)
        {
            Debug.LogError("[QingYin] Can't find the PackData asset (SDSA.asset).");
            return;
        }

        AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(packData)).SetAssetBundleNameAndVariant(BundleLabel, "");

        AssetBundlePack bundlePack = new AssetBundlePack
        {
            WindowsBuild = BuildAssetBundleForTarget("Windows", BuildTarget.StandaloneWindows64),
            AndroidBuild = BuildAssetBundleForTarget("Android", BuildTarget.Android),
        };

        if (bundlePack.WindowsBuild == null)
        {
            Debug.LogError("[QingYin] Windows asset bundle build failed; pack not written.");
            return;
        }

        string packPath = Path.Combine(outputDirectory, packFileName);
        File.WriteAllText(packPath, JsonConvert.SerializeObject(bundlePack));
        Debug.Log($"[QingYin] Pack written to {packPath} (windows {bundlePack.WindowsBuild.Length} bytes)");

        FileUtil.DeleteFileOrDirectory(CacheFolder);
        FileUtil.DeleteFileOrDirectory(CacheFolder + ".meta");
        AssetDatabase.Refresh();
        Debug.Log("[QingYin] Done.");
    }

    static byte[] BuildAssetBundleForTarget(string platformFolderName, BuildTarget target)
    {
        string relativePath = $"{CacheFolder}/{platformFolderName}";
        Directory.CreateDirectory(relativePath);
        BuildPipeline.BuildAssetBundles(relativePath, BuildAssetBundleOptions.None, target);

        string file = Path.Combine(relativePath, BundleLabel);
        if (File.Exists(file))
            return File.ReadAllBytes(file);

        Debug.LogError($"[QingYin] Can't find the built asset bundle for {target} at {file}");
        return null;
    }
}
