// QingYin SE Pro builder - generates the "青鹰SE Pro" engine part and its .pack
// Based on the project's own pipeline (SFS_PartPackModBuilder / QingYinBatchTools).
// Batch usage:  Unity.exe -batchmode -nographics -projectPath <proj> -executeMethod QingYinProBuilder.BuildAll -quit
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ModLoader;
using Newtonsoft.Json;
using SFS.Audio;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Variables;
using UnityEditor;
using UnityEngine;

public static class QingYinProBuilder
{
    // ---------------------------------------------------------------- config
    const string SePrefabPath  = "Assets/Resources/Parts/Engines/QingYin SE Engine.prefab";
    const string ProPrefabPath = "Assets/Resources/Parts/Engines/QingYin Pro Engine.prefab";
    const string PackDataPath  = "Assets/Resources/Parts/Engines/QingYinSEPro.asset";
    const string IconPath      = "Assets/Textures/Icons/Pro_Icon.png";

    const string SoundPath     = "Assets/Resources/Audio.Sound/Engine_Pro.asset";
    const string EngineClipPath = "Assets/Resources/Audio.Sound/Engine_2.wav";

    // SE palette assets (copied verbatim)
    const string ColorWhiteAsset = "Assets/Resources/Part Textures/Pattern Textures/Color_White.asset";
    const string MetalAsset      = "Assets/Resources/Part Textures/Pattern Textures/Metal.asset";
    const string Metal4Asset     = "Assets/Resources/Part Textures/Pattern Textures/Metal_4.asset";

    const string ShapeFlatSmooth        = "Assets/Resources/Part Textures/Shape Textures/Flat Smooth.asset";
    const string ShapeFlatFaces         = "Assets/Resources/Part Textures/Shape Textures/Flat Faces.asset";
    const string ShapeEdgesFaces        = "Assets/Resources/Part Textures/Shape Textures/Edges Faces.asset";
    const string ShapeEdgesFacesBottom  = "Assets/Resources/Part Textures/Shape Textures/Edges Faces Bottom.asset";

    const string BundleLabel  = "qingyingpro";
    const string PackFileName = "青鹰SEPro.pack";
    const string CacheFolder  = "Assets/ModBuilder/Cache";
    const string OutFolder    = "Assets/ModBuilder";

    // SFS installation to auto-deploy to (skipped when missing)
    const string GameModsPartsDir = @"E:\SteamLibrary\steamapps\common\Spaceflight Simulator\Spaceflight Simulator Game\Mods\Custom_Assets\Parts";

    // ---------------------------------------------------------------- data
    // !!! v4: same design language & palette as QingYin SE, but a different build:
    // !!! the nozzle is a two-piece construction (base nozzle + bolt-on extension skirt
    // !!! with a coupling collar), and the turbopumps are two-stage (slim feed elbow +
    // !!! fat volute housing) instead of SE's uniform slim tubes.

    // Nozzle (base section: chamber, throat + upper bell; the extension takes over at y=-1.80)
    static readonly Vector4[] NozzlePoints =
    {
        new Vector4(0f, -1.7175f, 1.5055f, 0f),
        new Vector4(0f, -1.6152f, 1.4512f, 0f),
        new Vector4(0f, -1.3524f, 1.3011f, 0f),
        new Vector4(0f, -1.0896f, 1.1365f, 0f),
        new Vector4(0f, -0.8508f, 0.9762f, 0f),
        new Vector4(0f, -0.6119f, 0.8065f, 0f),
        new Vector4(0f, -0.4208f, 0.6640f, 0f),
        new Vector4(0f, -0.2775f, 0.5541f, 0f),
        new Vector4(0f, -0.1342f, 0.4410f, 0f),
        new Vector4(0f, -0.0738f, 0.4935f, 0f),
        new Vector4(0f, -0.0134f, 0.5530f, 0f),
        new Vector4(0f,  0.1074f, 0.6651f, 0f),
        new Vector4(0f,  0.4898f, 0.6651f, 0f),
        new Vector4(0f,  0.5905f, 0.5776f, 0f),
        new Vector4(0f,  0.6911f, 0.4830f, 0f),
    };
    // SE colour bands, shifted for the shortened nozzle (old heights - 0.8053)
    static readonly float[] NzColorHeights = { 0.7912f, 0.8526f, 1.5833f, 2.2f };
    static readonly Color[] NzColorValues  =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(0.55f, 0.55f, 0.55f, 1f),
        new Color(0.48f, 0.48f, 0.48f, 1f),
        new Color(1f, 1f, 1f, 1f),
    };

    // Bolt-on nozzle extension (own GameObject under "Man", scale 1,1,1).
    // NOTE: values are LOCAL to this GameObject under "Man" (Man sits at y=-0.21), and
    // "width.x" is the FULL width (visual radius = width/2). Intended world positions:
    // sleeve top -1.72, collar -1.84..-1.935, skirt -1.945..-2.345, exit -2.40 (r 0.515).
    static readonly Vector4[] ExtPoints =
    {
        new Vector4(0f, -2.190f, 1.030f, 0f),   // exit (world y -2.40)
        new Vector4(0f, -2.162f, 1.041f, 0f),   // rim lip (out)
        new Vector4(0f, -2.135f, 1.025f, 0f),   // rim lip (in)
        new Vector4(0f, -1.940f, 0.976f, 0f),   // skirt
        new Vector4(0f, -1.750f, 0.922f, 0f),   // skirt top
        new Vector4(0f, -1.735f, 0.909f, 0f),   // step in
        new Vector4(0f, -1.725f, 0.964f, 0f),   // coupling collar (proud ring)
        new Vector4(0f, -1.630f, 0.964f, 0f),
        new Vector4(0f, -1.620f, 0.904f, 0f),   // step in
        new Vector4(0f, -1.510f, 0.896f, 0f),   // sleeve overlap into the base nozzle
    };
    static readonly float[] ExtColorHeights = { 0.055f, 0.455f, 0.560f, 0.70f };
    static readonly Color[] ExtColorValues  =
    {
        new Color(0.90f, 0.90f, 0.90f, 1f),       // rim edge
        new Color(1f, 1f, 1f, 1f),                // bright skirt (SE lower-bell tone)
        new Color(0.48f, 0.48f, 0.48f, 1f),       // coupling collar (SE dark tone)
        new Color(0.85f, 0.85f, 0.85f, 1f),       // sleeve
    };
    static readonly float[] ExtTexHeights   = { 0.055f, 0.455f, 0.560f, 0.70f };

    // Top slab = SE "Base" (upper) + "Ring" band (lower) merged, spanning +0.1125 .. -0.09
    static readonly Vector4[] FlangePoints =
    {
        new Vector4(0f, -0.180f, 1.948f, 0f),
        new Vector4(0f, -0.150f, 2.000f, 0f),
        new Vector4(0f,  0.100f, 2.000f, 0f),
        new Vector4(0f,  0.225f, 1.900f, 0f),
    };
    static readonly float[] BaseColorHeights = { 0.18f, 0.415f };   // seam at y=0 like SE (Base/Ring boundary)
    static readonly Color[] BaseColorValues  =
    {
        new Color(0.45f, 0.45f, 0.45f, 1f),   // lower band (SE Ring colour)
        new Color(0.90f, 0.90f, 0.90f, 1f),   // upper slab (SE Base colour)
    };
    static readonly float[] BaseTexHeights = { 0.18f, 0.415f };     // lower: Color_White+EdgesFaces / upper: Metal+EdgesFacesBottom

    // SE ring disc, kept exactly as in the SE (hidden inside the slab, GO y = -0.04)
    static readonly Vector4[] CollarPoints =
    {
        new Vector4(0f, -0.090f, 1.320f, 0f),
        new Vector4(0f,  0.090f, 1.320f, 0f),
    };

    // Two-stage turbopump, in Nozzle-local space (GO at +-0.5863, 0.416 under Nozzle):
    // slim feed elbow (SE style) below, fat volute housing above
    static readonly Vector4[] PumpPoints =
    {
        new Vector4(-0.2625f, -0.2751f, 0.0000f, -0.0671f),
        new Vector4(-0.0788f, -0.2308f, 0.0963f, -0.0564f),
        new Vector4( 0.0000f, -0.1946f, 0.1488f,  0.0000f),
        new Vector4( 0.0000f, -0.1718f, 0.1575f,  0.0000f),
        new Vector4( 0.0000f, -0.1583f, 0.2170f,  0.0000f),
        new Vector4( 0.0000f,  0.2147f, 0.2170f,  0.0000f),
        new Vector4( 0.0000f,  0.2308f, 0.1505f,  0.0000f),
        new Vector4( 0.0000f,  0.2415f, 0.0525f,  0.0000f),
    };
    static readonly float[] PumpColorHeights = { 0.3120f, 0f };     // dark feed below, light housing above
    static readonly Color[] PumpColorValues  =
    {
        new Color(0.45f, 0.45f, 0.45f, 1f),
        new Color(1f, 1f, 1f, 1f),
    };
    static readonly float[] PumpTexHeights = { 0.3120f, 0.78f };    // Color_White / Metal, Flat Faces

    // pump pod placement in Nozzle-local space (world +-0.335, y -0.21)
    const float PumpPosX = 0.5863f;   // 0.335 / 0.57137895
    const float PumpPosY = 0.4160f;   // (0.21 + 0.31) / 0.7451939

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

    // ---------------------------------------------------------------- menu
    [MenuItem("SFS/QingYin Pro/Rebuild Part Assets Only")]
    public static void MenuRebuildAssets() { Run(buildPack: false, batch: false, regenerate: true); }

    [MenuItem("SFS/QingYin Pro/Rebuild + Build + Export Pack")]
    public static void MenuBuildPack() { Run(buildPack: true, batch: false, regenerate: true); }

    [MenuItem("SFS/QingYin Pro/Export Current Prefab as Pack")]
    public static void MenuExportCurrentPrefab() { Run(buildPack: true, batch: false, regenerate: false); }

    /// <summary>Batch-mode entry point: regenerates the part from the SE base, then builds the pack.</summary>
    public static void BuildAll() { Run(buildPack: true, batch: true, regenerate: true); }

    /// <summary>Batch-mode entry point: exports whatever prefab is currently in the project, as-is.</summary>
    public static void ExportCurrentAsPack() { Run(buildPack: true, batch: true, regenerate: false); }

    // ---------------------------------------------------------------- main
    static void Run(bool buildPack, bool batch, bool regenerate)
    {
        try
        {
            AssetDatabase.Refresh();

            GameObject prefab;
            if (regenerate)
            {
                Texture2D icon = EnsureTexture(IconPath);
                if (icon == null) Fail("Icon texture missing: " + IconPath);

                SoundEffect sound = CreateSoundEffect();
                CreatePackData(icon);

                prefab = BuildPrefab(sound);
            }
            else
            {
                // export mode: take the prefab exactly as it is (manual tweaks preserved)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProPrefabPath);
                if (prefab == null) Fail("Current prefab not found: " + ProPrefabPath);
                if (AssetDatabase.LoadAssetAtPath<PackData>(PackDataPath) == null) Fail("PackData missing: " + PackDataPath);
                if (AssetDatabase.LoadAssetAtPath<SoundEffect>(SoundPath) == null)
                    Debug.LogWarning("[QingYinPro] Sound asset missing (the prefab may reference it): " + SoundPath);
                Debug.Log("[QingYinPro] Export mode: packing the CURRENT prefab as-is (no regeneration).");
            }

            if (!Validate(prefab, strictGeometry: regenerate))
                Fail("Validation failed, see errors above.");

            if (buildPack)
            {
                if (!BuildPackInternal()) Fail("Pack build failed.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("================ [QingYinPro] DONE ================");
        }
        catch (Exception e)
        {
            Debug.LogError("[QingYinPro] EXCEPTION: " + e);
            if (batch)
                throw;   // lets batch mode exit with a non-zero code
        }
    }

    static void Fail(string msg)
    {
        Debug.LogError("[QingYinPro] " + msg);
        throw new Exception(msg);
    }

    // ---------------------------------------------------------------- textures
    static Texture2D EnsureTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[QingYinPro] No texture importer at " + path);
            return null;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
        if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
        if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
        if (dirty)
            importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------------------------------------------------------------- aux assets
    static SoundEffect CreateSoundEffect()
    {
        SoundEffect sound = AssetDatabase.LoadAssetAtPath<SoundEffect>(SoundPath);
        bool isNew = sound == null;
        if (isNew)
            sound = ScriptableObject.CreateInstance<SoundEffect>();

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EngineClipPath);
        if (clip == null)
            Debug.LogWarning("[QingYinPro] Engine clip not found at " + EngineClipPath);

        sound.sounds = new List<Sound>
        {
            new Sound { clip = clip, volume = 1f, pitch = 0.155f, delay = 0f, range = 5000 }
        };

        if (isNew)
            AssetDatabase.CreateAsset(sound, SoundPath);
        EditorUtility.SetDirty(sound);
        AssetDatabase.SaveAssets();
        Debug.Log("[QingYinPro] SoundEffect ready: " + SoundPath);
        return sound;
    }

    static PackData CreatePackData(Texture2D icon)
    {
        PackData pd = AssetDatabase.LoadAssetAtPath<PackData>(PackDataPath);
        bool isNew = pd == null;
        if (isNew)
            pd = ScriptableObject.CreateInstance<PackData>();

        pd.DisplayName = "青鹰SE Pro";
        pd.Version = "1.0";
        pd.Description = "青鹰SE 增压改进型一级发动机";
        pd.Author = "zzasza";
        pd.ShowIcon = true;
        pd.Icon = icon;

        if (isNew)
            AssetDatabase.CreateAsset(pd, PackDataPath);
        EditorUtility.SetDirty(pd);
        AssetDatabase.SaveAssets();
        Debug.Log("[QingYinPro] PackData ready: " + PackDataPath);
        return pd;
    }

    // ---------------------------------------------------------------- prefab
    static GameObject BuildPrefab(SoundEffect sound)
    {
        var se = AssetDatabase.LoadAssetAtPath<GameObject>(SePrefabPath);
        if (se == null)
        {
            Debug.LogError("[QingYinPro] Reference prefab missing: " + SePrefabPath);
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(ProPrefabPath) != null)
            AssetDatabase.DeleteAsset(ProPrefabPath);

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(se);
        inst.name = "QingYin Pro Engine";

        // Unpack first: all following edits (incl. the added extension part) are then plain
        // hierarchy operations, and SaveAsPrefabAsset writes a standalone prefab.
        PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        Transform t = inst.transform;
        Transform baseT   = t.Find("Base");
        Transform ringT   = t.Find("Ring");
        Transform manT    = t.Find("Man");
        Transform nozzleT = manT.Find("Nozzle");
        Transform pumpT   = nozzleT.Find("Pump");
        Transform pump2T  = nozzleT.Find("Pump2");

        if (baseT == null || ringT == null || manT == null || nozzleT == null || pumpT == null || pump2T == null)
        {
            Debug.LogError("[QingYinPro] Unexpected hierarchy, aborting.");
            UnityEngine.Object.DestroyImmediate(inst);
            return null;
        }

        var vars = inst.GetComponent<VariablesModule>();

        // ---- Part parameters ----
        var part = inst.GetComponent<Part>();
        part.mass.input = "3.8";
        part.centerOfMass.x.input = "0";
        part.centerOfMass.y.input = "-0.72";
        part.variants[0].variants[0].cost = 70;

        var soPart = new SerializedObject(part);
        soPart.FindProperty("displayName.TranslatableName").stringValue = "青鹰SE Pro";
        soPart.FindProperty("displayName.plainText").boolValue = true;
        soPart.FindProperty("description.TranslatableName").stringValue = "青鹰SE 增压改进型：推力与比冲更强，更具控制力，代价是重量、尺寸与成本";
        soPart.FindProperty("description.plainText").boolValue = true;
        soPart.ApplyModifiedPropertiesWithoutUndo();

        // ---- Engine parameters ----
        var eng = inst.GetComponent<EngineModule>();
        eng.thrust.input = "112";
        eng.ISP.input = "311";
        eng.thrustPosition.y.input = "-3.7";

        // ---- Deeper engine sound ----
        inst.GetComponent<AudioModule>().soundEffect = sound;

        // ---- Gimbal range +-5 -> +-7 degrees ----
        var gimbal = inst.GetComponent<MoveModule>();
        var curve = gimbal.animationElements[0].X;
        var keys = curve.keys;
        keys[0].value = -7f; keys[0].inTangent = 7f; keys[0].outTangent = 7f;
        keys[1].value = 7f;  keys[1].inTangent = 7f; keys[1].outTangent = 7f;
        curve.keys = keys;

        // ---- Geometry ----
        // CRITICAL: the SE prefab ships "Base" with a 180 deg Z rotation; reset it so the
        // top slab sits exactly where designed (the slab now spans the same on-screen area).
        baseT.localRotation = Quaternion.identity;

        SetPipePoints(nozzleT.GetComponent<CustomPipe>(), vars, NozzlePoints, false);
        SetPipePoints(baseT.GetComponent<CustomPipe>(), vars, FlangePoints, false);
        SetPipePoints(ringT.GetComponent<CustomPipe>(), vars, CollarPoints, false);

        ringT.name = "Ring";                                  // same role as in the SE
        ringT.localPosition = new Vector3(0f, -0.04f, 0f);    // SE value

        // pumps become chunky turbopump pods on the side of the powerhead
        // (kept under Nozzle as in the SE; coordinates are pre-divided by the Nozzle scale)
        pumpT.localPosition = new Vector3(PumpPosX, PumpPosY, 0f);
        pumpT.localScale = Vector3.one;
        pump2T.localPosition = new Vector3(-PumpPosX, PumpPosY, 0f);
        pump2T.localScale = Vector3.one;
        SetPipePoints(pumpT.GetComponent<CustomPipe>(), vars, PumpPoints, false);
        SetPipePoints(pump2T.GetComponent<CustomPipe>(), vars, PumpPoints, true);

        // ---- Nozzle extension: the Pro's key structural difference (bolt-on skirt) ----
        var extGo = new GameObject("Nozzle Extension");
        extGo.layer = 8;
        extGo.transform.SetParent(manT, false);               // plain op - instance was unpacked
        var extPipe = extGo.AddComponent<CustomPipe>();
        var extMesh = extGo.AddComponent<PipeMesh>();
        var extCol = extGo.AddComponent<PolygonCollider>();
        var extRenderer = extGo.AddComponent<MeshRenderer>();
        extGo.AddComponent<MeshFilter>();
        extMesh.pipeData = extPipe;
        extCol.polygon = extPipe;
        extRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Part 1.mat");
        var soExt = new SerializedObject(extPipe);
        soExt.FindProperty("physicsCollider").boolValue = false;
        soExt.FindProperty("attachmentSurfaces").boolValue = false;   // SE convention: pipes don't attach
        soExt.ApplyModifiedPropertiesWithoutUndo();
        SetPipePoints(extPipe, vars, ExtPoints, false);

        // ---- Attachment surfaces (build/launch merge logic) ----
        // SE's flange is rotated 180 deg so its PipeSurface "bottom" edge (= the profile's
        // first point) lands on the world TOP as the tank mating line. We keep the flange
        // unrotated (visual fix), so point that line at the slab TOP face instead, and move
        // the under-engine line from the (shortened) nozzle to the extension's exit plane.
        var baseSurface = baseT.GetComponent<PipeSurface>();
        if (baseSurface != null)
        {
            baseSurface.top = true;        // slab top face at world +0.1125 -> tank mating line
            baseSurface.bottom = false;
            baseSurface.left = false;
            baseSurface.right = false;
        }

        var nozzleSurface = nozzleT.GetComponent<PipeSurface>();
        if (nozzleSurface != null)
        {
            nozzleSurface.top = false;     // nozzle now ends mid-bell; its exit line moved below
            nozzleSurface.bottom = false;
            nozzleSurface.left = false;
            nozzleSurface.right = false;
        }

        var extSurface = extGo.AddComponent<PipeSurface>();
        extSurface.pipeData = extPipe;
        extSurface.top = false;
        extSurface.left = false;
        extSurface.right = false;
        extSurface.bottom = true;          // exit plane line at world -2.40 (like SE's nozzle surface)
        var soExtSurf = new SerializedObject(extSurface);
        soExtSurf.FindProperty("attachmentSurfaces").boolValue = true;
        soExtSurf.FindProperty("dragSurfaces").boolValue = false;
        soExtSurf.ApplyModifiedPropertiesWithoutUndo();

        // flame holder sits at the extension's exit plane (unchanged world position -2.4)
        Transform flameHolder = nozzleT.Find("Flame Holder");
        if (flameHolder != null)
            flameHolder.localPosition = new Vector3(0f, -2.5228f, 0f);

        // larger heat affected zone (trade-off of the higher chamber pressure)
        Transform hitbox = nozzleT.Find("Heat Holder/Heat Hitbox");
        if (hitbox != null)
        {
            hitbox.localPosition = new Vector3(0f, -1.9f, 0f);
            var box = hitbox.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(1.55f, 8.4f);
                box.offset = new Vector2(0f, -4.2f);

                // The extension's collider sits inside the flame zone (the hitbox only switches
                // on while firing). Without this link the game treats the plume as touching a
                // foreign part -> blocked exhaust -> the engine overheats and explodes.
                // The SE nozzle carries exactly this link (ownEngineNozzle); mirror it.
                if (extCol != null)
                    extCol.ownEngineNozzle = box;
            }
        }

        // ---- Materials: copied 1:1 from QingYin SE ----
        var shEdgesFaces = AssetDatabase.LoadAssetAtPath<ShapeTexture>(ShapeEdgesFaces);
        var shEdgesFacesBottom = AssetDatabase.LoadAssetAtPath<ShapeTexture>(ShapeEdgesFacesBottom);
        var shFlatSmooth = AssetDatabase.LoadAssetAtPath<ShapeTexture>(ShapeFlatSmooth);
        var shFlatFaces = AssetDatabase.LoadAssetAtPath<ShapeTexture>(ShapeFlatFaces);
        var ctColorWhite = AssetDatabase.LoadAssetAtPath<ColorTexture>(ColorWhiteAsset);
        var ctMetal = AssetDatabase.LoadAssetAtPath<ColorTexture>(MetalAsset);
        var ctMetal4 = AssetDatabase.LoadAssetAtPath<ColorTexture>(Metal4Asset);

        // top slab: SE Ring band (lower) + SE Base disc (upper) as two texture strips
        SetPipeTexturesMulti(baseT.GetComponent<PipeMesh>(),
            new[] { ctColorWhite, ctMetal },
            new[] { shEdgesFaces, shEdgesFacesBottom },
            BaseTexHeights);
        SetPipeColorsMulti(baseT.GetComponent<PipeMesh>(), BaseColorValues, BaseColorHeights);

        // ring disc, exactly as in the SE
        SetPipeTexturesSingle(ringT.GetComponent<PipeMesh>(), ctColorWhite, shEdgesFaces, new Color(0.45f, 0.45f, 0.45f, 1f));

        // nozzle: Metal_4 + Flat Smooth with the SE colour bands
        SetPipeTexturesMulti(nozzleT.GetComponent<PipeMesh>(),
            new[] { ctMetal4, ctMetal4 },
            new[] { shFlatSmooth, shFlatSmooth },
            new[] { 0f, 9.9f });
        SetPipeColorsMulti(nozzleT.GetComponent<PipeMesh>(), NzColorValues, NzColorHeights);

        // pumps: SE colours (dark feed, light housing)
        var pumpCts = new[] { ctColorWhite, ctMetal };
        var pumpSts = new[] { shFlatFaces, shFlatFaces };
        SetPipeTexturesMulti(pumpT.GetComponent<PipeMesh>(), pumpCts, pumpSts, PumpTexHeights);
        SetPipeColorsMulti(pumpT.GetComponent<PipeMesh>(), PumpColorValues, PumpColorHeights);
        SetPipeTexturesMulti(pump2T.GetComponent<PipeMesh>(), pumpCts, pumpSts, PumpTexHeights);
        SetPipeColorsMulti(pump2T.GetComponent<PipeMesh>(), PumpColorValues, PumpColorHeights);

        // nozzle extension: bright skirt, dark coupling collar, sleeve (SE palette)
        SetPipeTexturesMulti(extMesh,
            new[] { ctMetal4, ctMetal4, ctMetal, ctMetal4 },
            new[] { shFlatSmooth, shFlatSmooth, shFlatFaces, shFlatSmooth },
            ExtTexHeights);
        SetPipeColorsMulti(extMesh, ExtColorValues, ExtColorHeights);

        // ---- Save (already unpacked above; now a plain hierarchy) ----
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, ProPrefabPath);
        UnityEngine.Object.DestroyImmediate(inst);
        Debug.Log("[QingYinPro] Prefab saved: " + ProPrefabPath);
        return saved;
    }

    // ---- pipe profile writer (SerializedObject keeps everything consistent) ----
    // points: (posX, posY, widthX, widthY); the SE pump relies on a rotating width vector
    static void SetPipePoints(CustomPipe pipe, VariablesModule vars, Vector4[] pts, bool mirrorX)
    {
        var so = new SerializedObject(pipe);
        var arr = so.FindProperty("composedShape.points");
        arr.arraySize = pts.Length;

        for (int i = 0; i < pts.Length; i++)
        {
            SerializedProperty e = arr.GetArrayElementAtIndex(i);
            float s = mirrorX ? -1f : 1f;

            BindVars(e.FindPropertyRelative("variables"), vars);
            BindVars(e.FindPropertyRelative("position.variables"), vars);
            BindVars(e.FindPropertyRelative("position.x.variables"), vars);
            BindVars(e.FindPropertyRelative("position.y.variables"), vars);
            BindVars(e.FindPropertyRelative("width.variables"), vars);
            BindVars(e.FindPropertyRelative("width.x.variables"), vars);
            BindVars(e.FindPropertyRelative("width.y.variables"), vars);

            e.FindPropertyRelative("position.x.input").stringValue = F(s * pts[i].x);
            e.FindPropertyRelative("position.y.input").stringValue = F(pts[i].y);
            e.FindPropertyRelative("width.x.input").stringValue = F(s * pts[i].z);
            e.FindPropertyRelative("width.y.input").stringValue = F(pts[i].w);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BindVars(SerializedProperty p, VariablesModule vars)
    {
        if (p != null)
            p.objectReferenceValue = vars;
    }

    static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    // ---- material writers ----
    static void SetPipeTexturesSingle(PipeMesh mesh, ColorTexture ct, ShapeTexture st, Color tint)
    {
        var so = new SerializedObject(mesh);
        so.FindProperty("textures.textureMode").enumValueIndex = 0;          // Single
        so.FindProperty("textures.texture.colorTexture").objectReferenceValue = ct;
        so.FindProperty("textures.texture.shapeTexture").objectReferenceValue = st;
        so.FindProperty("colors.mode").enumValueIndex = 0;                   // Single
        so.FindProperty("colors.color.type").enumValueIndex = 0;             // Local
        so.FindProperty("colors.color.colorBasic").colorValue = tint;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetPipeTexturesMulti(PipeMesh mesh, ColorTexture[] cts, ShapeTexture[] sts, float[] heights)
    {
        var so = new SerializedObject(mesh);
        so.FindProperty("textures.textureMode").enumValueIndex = 1;          // Multiple
        var arr = so.FindProperty("textures.textures");
        arr.arraySize = cts.Length;
        for (int i = 0; i < cts.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("texture.colorTexture").objectReferenceValue = cts[i];
            e.FindPropertyRelative("texture.shapeTexture").objectReferenceValue = sts[i];
            e.FindPropertyRelative("height").floatValue = heights[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetPipeColorsMulti(PipeMesh mesh, Color[] colors, float[] heights)
    {
        var so = new SerializedObject(mesh);
        so.FindProperty("colors.mode").enumValueIndex = 1;                   // Multiple
        var arr = so.FindProperty("colors.colors");
        arr.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
        {
            var e = arr.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("color.type").enumValueIndex = 0;         // Local
            e.FindPropertyRelative("color.colorBasic").colorValue = colors[i];
            e.FindPropertyRelative("height").floatValue = heights[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- validation
    static bool Validate(GameObject root, bool strictGeometry = true)
    {
        bool ok = true;

        // must be a standalone prefab (a variant would drag the SE prefab into the pack)
        if (PrefabUtility.GetPrefabAssetType(root) != PrefabAssetType.Regular)
        {
            Debug.LogError("[QingYinPro] Prefab asset is not standalone: " + PrefabUtility.GetPrefabAssetType(root));
            ok = false;
        }

        foreach (Type type in RequiredOnRoot)
        {
            if (root.GetComponent(type) == null)
            {
                Debug.LogError("[QingYinPro] " + type.Name + " is not on the root GameObject.");
                ok = false;
            }
        }

        var engine = root.GetComponent<EngineModule>();
        if (engine != null)
        {
            if (engine.source == null) { Debug.LogError("[QingYinPro] EngineModule.source missing."); ok = false; }
            if (engine.heatHolder == null) { Debug.LogError("[QingYinPro] EngineModule.heatHolder missing."); ok = false; }
            if (engine.gimbal == null) { Debug.LogError("[QingYinPro] EngineModule.gimbal missing."); ok = false; }
        }

        // nozzle extension must exist with complete components
        var extT = root.transform.Find("Man/Nozzle Extension");
        if (extT == null)
        {
            Debug.LogError("[QingYinPro] Nozzle Extension GameObject missing.");
            ok = false;
        }
        else
        {
            var ep = extT.GetComponent<CustomPipe>();
            var em = extT.GetComponent<PipeMesh>();
            if (ep == null || em == null || em.pipeData == null || extT.GetComponent<PolygonCollider>() == null)
            {
                Debug.LogError("[QingYinPro] Nozzle Extension components incomplete.");
                ok = false;
            }

            var ec = extT.GetComponent<PolygonCollider>();
            if (ec != null && ec.ownEngineNozzle == null)
            {
                Debug.LogError("[QingYinPro] Extension collider has no ownEngineNozzle link -> engine would overheat itself.");
                ok = false;
            }

            // numeric report: read the extension's ACTUAL data (transform + profile), so this
            // log is meaningful both when regenerating and when exporting a hand-edited prefab
            var manTr = root.transform.Find("Man");
            float manY = manTr != null ? manTr.localPosition.y : 0f;
            var extPts = ep.composedShape.points;
            if (extPts.Count >= 2)
            {
                Vector2 p0 = extPts[0].Value.position;
                Vector2 pn = extPts[extPts.Count - 1].Value.position;
                float sy = extT.localScale.y;
                float sx = Mathf.Abs(extT.localScale.x);
                float extExitWorld = manY + extT.localPosition.y + p0.y * sy;
                float extTopWorld = manY + extT.localPosition.y + pn.y * sy;
                float extExitWidth = extPts[0].Value.width.magnitude * sx;
                Debug.Log($"[QingYinPro] Check: extension world span {extExitWorld:F3} .. {extTopWorld:F3}, exit width {extExitWidth:F3}" +
                          (strictGeometry ? " (design target -2.400 .. -1.720 / 1.030)" : " (as-is export)"));

                // strict geometry guard only when the prefab was generated by this script
                if (strictGeometry && (Mathf.Abs(extExitWorld + 2.40f) > 0.02f || Mathf.Abs(extTopWorld + 1.72f) > 0.02f || Mathf.Abs(extExitWidth - 1.03f) > 0.05f))
                {
                    Debug.LogError("[QingYinPro] Extension geometry does not match the design spec.");
                    ok = false;
                }
            }

            var esf = extT.GetComponent<PipeSurface>();
            if (esf == null || !esf.bottom || esf.top || esf.left || esf.right)
            {
                Debug.LogError("[QingYinPro] Extension must expose the exit-plane attachment line (bottom edge).");
                ok = false;
            }
        }

        // attachment surfaces: the tank mating line must sit on the slab's TOP face (+0.1125)
        var baseT2 = root.transform.Find("Base");
        var bsf = baseT2 != null ? baseT2.GetComponent<PipeSurface>() : null;
        if (bsf == null || !bsf.top || bsf.bottom || bsf.left || bsf.right)
        {
            Debug.LogError("[QingYinPro] Base PipeSurface must expose only the top edge (tank mating line).");
            ok = false;
        }

        var flows = root.GetComponentsInChildren<FlowModule>(true);
        if (flows.Length == 0) { Debug.LogError("[QingYinPro] No FlowModule found."); ok = false; }
        foreach (var flow in flows)
            foreach (var src in flow.sources)
            {
                if (src.surface == null) { Debug.LogError("[QingYinPro] Flow source has no surface."); ok = false; }
                if (src.resourceType == null) { Debug.LogError("[QingYinPro] Flow source has no resource."); ok = false; }
            }

        int missing = 0;
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            foreach (var c in tr.GetComponents<Component>())
                if (c == null)
                    missing++;
        if (missing > 0) { Debug.LogError("[QingYinPro] " + missing + " missing script reference(s)."); ok = false; }

        // sanity check the new parameters
        var part = root.GetComponent<Part>();
        var nozzleT = root.transform.Find("Man/Nozzle");
        var nozzle = nozzleT == null ? null : nozzleT.GetComponent<CustomPipe>();
        Debug.Log($"[QingYinPro] Check: mass={part.mass.input}, thrust={engine.thrust.input}, ISP={engine.ISP.input}, " +
                  $"nozzle points={(nozzle == null ? -1 : nozzle.composedShape.points.Count)}, cost={part.variants[0].variants[0].cost}");

        Debug.Log(ok ? "[QingYinPro] Validation passed." : "[QingYinPro] Validation FAILED.");
        return ok;
    }

    // ---------------------------------------------------------------- pack build
    static bool BuildPackInternal()
    {
        Directory.CreateDirectory(OutFolder);
        Directory.CreateDirectory(CacheFolder);

        AssetImporter.GetAtPath(ProPrefabPath).SetAssetBundleNameAndVariant(BundleLabel, "");
        AssetImporter.GetAtPath(PackDataPath).SetAssetBundleNameAndVariant(BundleLabel, "");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        byte[] win = BuildForTarget("Windows", BuildTarget.StandaloneWindows64);
        if (win == null)
        {
            Debug.LogError("[QingYinPro] Windows asset bundle build failed.");
            return false;
        }

        byte[] android = null;
        try
        {
            android = BuildForTarget("Android", BuildTarget.Android);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[QingYinPro] Android bundle skipped: " + e.Message);
        }

        var pack = new PackOut
        {
            WindowsBuild = win,
            AndroidBuild = android,
        };

        string packPath = Path.Combine(OutFolder, PackFileName);
        File.WriteAllText(packPath, JsonConvert.SerializeObject(pack));
        Debug.Log($"[QingYinPro] Pack written: {packPath}  (windows {win.Length} bytes, android {(android == null ? 0 : android.Length)} bytes)");

        // deploy straight into the game when present
        if (Directory.Exists(GameModsPartsDir))
        {
            string dest = Path.Combine(GameModsPartsDir, PackFileName);
            File.Copy(packPath, dest, true);
            Debug.Log("[QingYinPro] Deployed to game: " + dest);
        }
        else
        {
            Debug.LogWarning("[QingYinPro] Game mods folder not found, skipped deploy: " + GameModsPartsDir);
        }

        FileUtil.DeleteFileOrDirectory(CacheFolder);
        FileUtil.DeleteFileOrDirectory(CacheFolder + ".meta");
        AssetDatabase.Refresh();
        return true;
    }

    static byte[] BuildForTarget(string platformFolderName, BuildTarget target)
    {
        string relativePath = CacheFolder + "/" + platformFolderName;
        Directory.CreateDirectory(relativePath);
        BuildPipeline.BuildAssetBundles(relativePath, BuildAssetBundleOptions.None, target);

        string file = Path.Combine(relativePath, BundleLabel);
        if (File.Exists(file))
            return File.ReadAllBytes(file);

        Debug.LogError("[QingYinPro] Can't find built asset bundle at " + file);
        return null;
    }

    [Serializable]
    class PackOut
    {
        public byte[] MacBuild, WindowsBuild, AndroidBuild, IOS_Build, CodeAssembly;
    }
}
