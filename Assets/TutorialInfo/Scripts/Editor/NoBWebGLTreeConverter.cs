using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class NoBWebGLTreeConverter
{
    private const string OutputRoot = "Assets/04.Asset/Mini Nature Pack/Models/WebGL Trees";
    private const string TerrainDataPath = "Assets/02.Model/New Terrain.asset";
    private const string WaterPrefabPath = "Assets/02.Model/1-3/Water.prefab";

    private static readonly string[] RockPrefabs =
    {
        "AN_Rock_1", "AN_Rock_2", "AN_Rock_3", "AN_Rock_1_Cov",
        "AN_Rock_2_Cov", "AN_Rock_3_Cov", "AN_Stones_1", "AN_Stones_1_Cov"
    };

    private static readonly (string source, string outputName)[] Trees =
    {
        ("Assets/04.Asset/Mini Nature Pack/Models/Tree 3.prefab", "Tree3_WebGL"),
        ("Assets/04.Asset/Mini Nature Pack/Models/Tree 4.prefab", "Tree4_WebGL"),
        ("Assets/04.Asset/Mini Nature Pack/Models/Tree 6.prefab", "Tree6_WebGL"),
    };

    [MenuItem("Tools/NoB/Rebuild WebGL Tree Prefabs")]
    private static void RunFromMenu()
    {
        Convert();
    }

    private static void Convert()
    {
        try
        {
            EnsureFolder(OutputRoot);
            EnsureFolder(OutputRoot + "/Materials");
            RefreshGeneratedAzureMaterials();

            var replacements = new Dictionary<GameObject, GameObject>();
            foreach (var tree in Trees)
            {
                var replacement = CreateReplacement(tree.source, tree.outputName);
                replacements.Add(replacement.Key, replacement.Value);
            }

            var terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (terrainData == null)
                throw new InvalidOperationException("TerrainData not found: " + TerrainDataPath);

            var prototypes = terrainData.treePrototypes;
            var replaced = 0;
            var azureMaterials = new Dictionary<Material, Material>();
            for (var i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i].prefab != null && replacements.TryGetValue(prototypes[i].prefab, out var replacement))
                {
                    prototypes[i].prefab = replacement;
                    replaced++;
                }
                else if (prototypes[i].prefab != null &&
                         AssetDatabase.GetAssetPath(prototypes[i].prefab).StartsWith("Assets/04.Asset/AZURE Nature/", StringComparison.Ordinal))
                {
                    prototypes[i].prefab = CreateAzureReplacement(prototypes[i].prefab, azureMaterials);
                    replaced++;
                }
            }

            terrainData.treePrototypes = prototypes;

            var detailPrototypes = terrainData.detailPrototypes;
            var replacedDetails = 0;
            for (var i = 0; i < detailPrototypes.Length; i++)
            {
                var prototype = detailPrototypes[i].prototype;
                if (prototype != null && replacements.TryGetValue(prototype, out var replacement))
                {
                    detailPrototypes[i].prototype = replacement;
                    replacedDetails++;
                }
            }

            terrainData.detailPrototypes = detailPrototypes;
            EditorUtility.SetDirty(terrainData);
            ConvertWater();
            ConvertRocks();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[NoB WebGL Trees] Completed: tree prototypes={prototypes.Length}, replaced trees={replaced}; detail prototypes={detailPrototypes.Length}, replaced details={replacedDetails}; tree instances={terrainData.treeInstanceCount}.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void RefreshGeneratedAzureMaterials()
    {
        var folder = OutputRoot + "/Azure/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
            return;

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { folder }))
        {
            var generatedPath = AssetDatabase.GUIDToAssetPath(guid);
            var generated = AssetDatabase.LoadAssetAtPath<Material>(generatedPath);
            if (generated == null || !generated.name.EndsWith("_WebGL", StringComparison.Ordinal))
                continue;

            var sourceName = generated.name.Substring(0, generated.name.Length - "_WebGL".Length);
            foreach (var sourceGuid in AssetDatabase.FindAssets(sourceName + " t:Material", new[] { "Assets/04.Asset/AZURE Nature/Materials" }))
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(sourceGuid));
                if (source != null && source.name == sourceName)
                {
                    CreateAzureMaterial(source, generatedPath);
                    break;
                }
            }
        }
    }

    private static void ConvertRocks()
    {
        var materialCache = new Dictionary<Material, Material>();
        var convertedRenderers = 0;
        foreach (var prefabName in RockPrefabs)
        {
            var path = "Assets/04.Asset/AZURE Nature/Prefabs/" + prefabName + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                    {
                        var source = materials[i];
                        if (source == null || source.shader == null || source.shader.name == "Universal Render Pipeline/Lit")
                            continue;

                        if (!materialCache.TryGetValue(source, out var replacement))
                        {
                            var output = OutputRoot + "/Azure/Materials/" + Sanitize(source.name) + "_WebGL.mat";
                            replacement = CreateAzureMaterial(source, output, false);
                            materialCache.Add(source, replacement);
                        }
                        materials[i] = replacement;
                    }
                    renderer.sharedMaterials = materials;
                    convertedRenderers++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        Debug.Log($"[NoB WebGL Rocks] Updated {RockPrefabs.Length} prefabs and {convertedRenderers} renderers.");
    }

    private static void ConvertWater()
    {
        var sourceWater = AssetDatabase.LoadAssetAtPath<Material>("Assets/04.Asset/AZURE Nature/Materials/Nature/AN_Water.mat");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (sourceWater == null || shader == null)
            throw new InvalidOperationException("Water source material or URP Lit shader was not found.");

        var materialPath = OutputRoot + "/AN_Water_WebGL.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.shader = shader;
        var waterColor = sourceWater.HasProperty("_Color2") ? sourceWater.GetColor("_Color2") : new Color(0.09f, 0.63f, 0.71f, 1f);
        waterColor.a = sourceWater.HasProperty("_Opacity") ? sourceWater.GetFloat("_Opacity") : 0.79f;
        material.SetColor("_BaseColor", waterColor);
        material.SetColor("_Color", waterColor);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cull", 2f);
        material.SetFloat("_Smoothness", sourceWater.HasProperty("_Smoothness") ? sourceWater.GetFloat("_Smoothness") : 0.88f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);

        var waterRoot = PrefabUtility.LoadPrefabContents(WaterPrefabPath);
        try
        {
            var renderer = waterRoot.GetComponentInChildren<MeshRenderer>(true);
            if (renderer == null)
                throw new InvalidOperationException("Water prefab has no MeshRenderer.");
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            PrefabUtility.SaveAsPrefabAsset(waterRoot, WaterPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(waterRoot);
        }

        Debug.Log("[NoB WebGL Water] Replaced AN_Water custom shader with transparent URP Lit material.");
    }

    private static GameObject CreateAzureReplacement(GameObject source, Dictionary<Material, Material> materialCache)
    {
        var azureRoot = OutputRoot + "/Azure";
        EnsureFolder(azureRoot);
        EnsureFolder(azureRoot + "/Materials");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        try
        {
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = source.name + "_WebGL";

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var sourceMaterial = materials[i];
                    if (sourceMaterial == null)
                        continue;

                    if (!materialCache.TryGetValue(sourceMaterial, out var replacement))
                    {
                        replacement = CreateAzureMaterial(sourceMaterial, azureRoot + "/Materials/" + Sanitize(sourceMaterial.name) + "_WebGL.mat");
                        materialCache.Add(sourceMaterial, replacement);
                    }

                    materials[i] = replacement;
                }

                renderer.sharedMaterials = materials;
            }

            var path = azureRoot + "/" + Sanitize(source.name) + "_WebGL.prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            if (saved == null)
                throw new InvalidOperationException("Failed to save AZURE replacement prefab: " + path);
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static Material CreateAzureMaterial(Material source, string path, bool? alphaClipOverride = null)
    {
        var isBark = source.name.IndexOf("Bark", StringComparison.OrdinalIgnoreCase) >= 0;
        var alphaClip = alphaClipOverride ?? !isBark;
        var material = CreateMaterial(source, path, alphaClip);
        var textureProperties = new[]
        {
            "_LeavesTexture", "_BarkAlbedo", "_RockAlbedo", "_SurfaceAlbedo", "_GroundAlbedo", "_Albedo",
            "_BaseMap", "_MainTex", "_Texture00", "_Texture0"
        };

        Texture texture = null;
        foreach (var property in textureProperties)
        {
            if (source.HasProperty(property) && source.GetTexture(property) != null)
            {
                texture = source.GetTexture(property);
                break;
            }
        }

        if (texture == null)
        {
            foreach (var property in source.GetTexturePropertyNames())
            {
                var lower = property.ToLowerInvariant();
                if (lower.Contains("normal") || lower.Contains("mask") || lower.Contains("smooth") ||
                    lower.Contains("metal") || lower.Contains("snow") || lower.Contains("bump"))
                    continue;

                texture = source.GetTexture(property);
                if (texture != null)
                    break;
            }
        }

        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        var tint = Color.white;
        if (alphaClip && source.HasProperty("_Color1"))
            tint = source.GetColor("_Color1");
        else if (source.HasProperty("_BaseColor"))
            tint = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))
            tint = source.GetColor("_Color");
        tint.a = 1f;
        material.SetColor("_BaseColor", tint);
        material.SetColor("_Color", tint);
        if (alphaClip)
        {
            var cutoff = source.HasProperty("_AlphaCutoff") ? source.GetFloat("_AlphaCutoff") : 0.35f;
            material.SetFloat("_Cutoff", cutoff);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static string Sanitize(string value)
    {
        foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace('/', '_');
    }

    private static KeyValuePair<GameObject, GameObject> CreateReplacement(string sourcePath, string outputName)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
            throw new InvalidOperationException("Source prefab not found: " + sourcePath);

        var sourceFilter = source.GetComponent<MeshFilter>();
        var sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceFilter == null || sourceRenderer == null || sourceFilter.sharedMesh == null)
            throw new InvalidOperationException("Source prefab has no usable MeshFilter/MeshRenderer: " + sourcePath);

        var sourceMaterials = sourceRenderer.sharedMaterials;
        if (sourceMaterials.Length < 2)
            throw new InvalidOperationException("Expected bark and leaf material slots: " + sourcePath);

        var bark = CreateMaterial(sourceMaterials[0], OutputRoot + "/Materials/" + outputName + "_Bark.mat", false);
        var leaves = CreateMaterial(sourceMaterials[1], OutputRoot + "/Materials/" + outputName + "_Leaves.mat", true);

        var instance = new GameObject(outputName);
        try
        {
            var filter = instance.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;

            var renderer = instance.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { bark, leaves };
            renderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
            renderer.receiveShadows = sourceRenderer.receiveShadows;
            renderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
            renderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

            var destinationPath = OutputRoot + "/" + outputName + ".prefab";
            var replacement = PrefabUtility.SaveAsPrefabAsset(instance, destinationPath);
            if (replacement == null)
                throw new InvalidOperationException("Failed to save prefab: " + destinationPath);

            return new KeyValuePair<GameObject, GameObject>(source, replacement);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static Material CreateMaterial(Material source, string path, bool alphaClip)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("Universal Render Pipeline/Lit shader was not found.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        var mainTexture = source.GetTexture("_BaseMap") ?? source.GetTexture("_MainTex");
        material.SetTexture("_BaseMap", mainTexture);
        material.SetTexture("_MainTex", mainTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_Cull", alphaClip ? 0f : 2f);
        material.SetFloat("_ZWrite", 1f);
        material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
        material.SetFloat("_AlphaToMask", alphaClip ? 1f : 0f);
        material.SetFloat("_Cutoff", source.HasProperty("_Cutoff") ? source.GetFloat("_Cutoff") : 0.5f);
        material.doubleSidedGI = alphaClip;
        material.enableInstancing = true;

        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (alphaClip)
        {
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        else
        {
            material.DisableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var slash = path.LastIndexOf('/');
        var parent = path.Substring(0, slash);
        var name = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
