using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 열려 있는 씬의 맵을 위에서 곧게(직교) 찍어 미니맵 지도 이미지로 굽는다.
/// 후처리 없는 HDR 장면을 촬영하고, 실시간 미니맵과 같은 톤 곡선을 적용합니다.
/// 맵 루트(CMinimapMapSO.BakeRootName) 아래 렌더러만 찍고, 플레이어 · 몬스터 · UI 는 찍지 않는다.
/// </summary>
public static class CMinimapMapBaker
{
    private const float _TONE_KNEE = 0.5f;
    private const float _TONE_SLOPE = 2.2f;
    private const float _TONE_CLAMP = 4f;
    private const int _SHADOW_RESOLUTION = 8192;
    private const string _DEFAULT_FOLDER = "Assets/Scenes/Heyseong/Minimap";

    [MenuItem("Espoir/Minimap/Bake Selected Map Image (Open Scene)")]
    private static void BakeSelected()
    {
        CMinimapMapSO map = Selection.activeObject as CMinimapMapSO;
        if (map == null)
        {
            EditorUtility.DisplayDialog("Minimap", "Project 창에서 CMinimapMapSO 에셋을 고른 뒤 실행하세요.", "OK");
            return;
        }

        Bake(map);
    }

    /// <summary>
    /// 지금 열린 씬에서 map 의 지도 이미지를 굽고, 이미지 · 월드 범위를 map 에 저장한다. 성공하면 true.
    /// </summary>
    public static bool Bake(CMinimapMapSO map)
    {
        GameObject root = FindRoot(map.BakeRootName);
        if (root == null)
        {
            Debug.LogError($"[Minimap] 씬 루트 '{map.BakeRootName}' 를 찾지 못했습니다.");
            return false;
        }

        Bounds area;
        if (!TryGetMapBounds(root, map.BakeIgnoreLargerThan, out area))
        {
            Debug.LogError($"[Minimap] '{map.BakeRootName}' 아래에 찍을 렌더러가 없습니다.");
            return false;
        }

        float side = Mathf.Max(area.size.x, area.size.z) + map.BakeMargin * 2f;
        Vector2 center = new Vector2(area.center.x, area.center.z);
        int tiles = Mathf.Max(1, map.BakeTiles);
        int tilePixels = Mathf.Max(64, map.BakeResolution / tiles);
        int resolution = tilePixels * tiles;
        float tileSide = side / tiles;
        float top = GetTopInSquare(root, center, side) + 5f;
        float bottom = area.min.y - 5f;
        // 카메라는 가장 높은 물체 바로 위. 땅이 기본 그림자 거리(150) 안에 들어야 그림자가 찍힌다.
        // 모래 셰이더가 시선 방향(_WorldSpaceCameraPos)을 쓰므로 칸을 나누면 칸 경계에 무늬 차이가 생길 수 있어 기본은 1칸.
        float cameraY = top + 1f;
        float far = cameraY - bottom;

        List<Renderer> hidden = HideOtherRenderers(root);
        List<Light> lights = new List<Light>();
        List<int> lightResolutions = new List<int>();
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional || light.shadows == LightShadows.None) continue;
            lights.Add(light);
            lightResolutions.Add(light.shadowCustomResolution);
            light.shadowCustomResolution = _SHADOW_RESOLUTION;   // 굽는 동안만 (끝나면 되돌림)
        }

        GameObject cameraObject = new GameObject("MinimapBakeCamera") { hideFlags = HideFlags.HideAndDontSave };
        RenderTexture target = new RenderTexture(tilePixels, tilePixels, 24, RenderTextureFormat.ARGBHalf,
            RenderTextureReadWrite.Linear)
        {
            antiAliasing = 4
        };
        Texture2D readBack = new Texture2D(tilePixels, tilePixels, TextureFormat.RGBAHalf, false, true);
        Color32[] pixels = new Color32[resolution * resolution];
        try
        {
            Camera bakeCamera = cameraObject.AddComponent<Camera>();
            bakeCamera.enabled = false;
            bakeCamera.orthographic = true;
            bakeCamera.orthographicSize = tileSide * 0.5f;
            bakeCamera.aspect = 1f;
            bakeCamera.allowHDR = true;
            bakeCamera.allowMSAA = true;
            bakeCamera.clearFlags = CameraClearFlags.SolidColor;
            bakeCamera.backgroundColor = Color.black;
            bakeCamera.nearClipPlane = 0.5f;
            bakeCamera.farClipPlane = far;
            bakeCamera.targetTexture = target;
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // 아래를 봄, 화면 위 = 월드 +Z, 오른쪽 = +X

            float minX = center.x - side * 0.5f;
            float minZ = center.y - side * 0.5f;
            for (int tz = 0; tz < tiles; tz++)
            {
                for (int tx = 0; tx < tiles; tx++)
                {
                    float x = minX + (tx + 0.5f) * tileSide;
                    float z = minZ + (tz + 0.5f) * tileSide;
                    cameraObject.transform.position = new Vector3(x, cameraY, z);
                    bakeCamera.Render();
                    RenderTexture.active = target;
                    readBack.ReadPixels(new Rect(0, 0, tilePixels, tilePixels), 0, 0, false);
                    readBack.Apply(false);
                    RenderTexture.active = null;
                    WriteTile(readBack.GetPixels(), pixels, tx * tilePixels, tz * tilePixels, tilePixels, resolution);
                }
            }
        }
        finally
        {
            RenderTexture.active = null;
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] != null) lights[i].shadowCustomResolution = lightResolutions[i];
            }

            foreach (Renderer r in hidden)
            {
                if (r != null) r.forceRenderingOff = false;
            }

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(readBack);
            target.Release();
            Object.DestroyImmediate(target);
        }

        string path = map.MapTexture != null ? AssetDatabase.GetAssetPath(map.MapTexture) : GetDefaultTexturePath(map);
        SaveTexture(pixels, resolution, path);
        Texture2D saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        SerializedObject so = new SerializedObject(map);
        so.FindProperty("_mapTexture").objectReferenceValue = saved;
        so.FindProperty("_worldCenter").vector2Value = center;
        so.FindProperty("_worldSize").vector2Value = new Vector2(side, side);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Minimap] 구움: {path} {resolution}px, 가운데 {center}, 한 변 {side:F1}m, 칸 {tiles}x{tiles}, " +
                  $"높이 {bottom:F1}~{top:F1}");
        return true;
    }

    private static GameObject FindRoot(string rootName)
    {
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.name == rootName) return go;
        }

        return null;
    }

    private static bool TryGetMapBounds(GameObject root, float ignoreLargerThan, out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (!IsMapRenderer(r)) continue;
            Vector3 size = r.bounds.size;
            if (size.x > ignoreLargerThan || size.z > ignoreLargerThan) continue;
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }

            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasBounds;
    }

    private static float GetTopInSquare(GameObject root, Vector2 center, float side)
    {
        float half = side * 0.5f;
        float top = float.MinValue;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (!IsMapRenderer(r)) continue;
            Bounds b = r.bounds;
            if (b.size.y > 200f) continue;   // 하늘 구는 위를 덮지 않음 (안쪽 면만 보임)
            if (b.max.x < center.x - half || b.min.x > center.x + half) continue;
            if (b.max.z < center.y - half || b.min.z > center.y + half) continue;
            top = Mathf.Max(top, b.max.y);
        }

        return top;
    }

    private static bool IsMapRenderer(Renderer r)
    {
        if (!r.enabled || !r.gameObject.activeInHierarchy) return false;
        return !(r is ParticleSystemRenderer) && !(r is TrailRenderer) && !(r is LineRenderer);
    }

    private static List<Renderer> HideOtherRenderers(GameObject root)
    {
        // forceRenderingOff 는 저장되지 않는 값이라 씬을 바꾸지 않고 잠시 숨길 수 있음
        List<Renderer> hidden = new List<Renderer>();
        Renderer[] renderers =
            Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer r in renderers)
        {
            bool isEffect = r is ParticleSystemRenderer || r is TrailRenderer;
            bool isMap = r.transform.IsChildOf(root.transform) && !isEffect;
            if (isMap || r.forceRenderingOff) continue;
            r.forceRenderingOff = true;
            hidden.Add(r);
        }

        return hidden;
    }

    private static void WriteTile(Color[] tile, Color32[] pixels, int offsetX, int offsetY, int tilePixels,
        int resolution)
    {
        for (int y = 0; y < tilePixels; y++)
        {
            int src = y * tilePixels;
            int dst = (offsetY + y) * resolution + offsetX;
            for (int x = 0; x < tilePixels; x++)
            {
                Color c = tile[src + x];
                pixels[dst + x] = new Color32(Encode(c.r), Encode(c.g), Encode(c.b), 255);
            }
        }
    }

    // 0.5를 넘는 밝기를 부드럽게 제한한 뒤 sRGB로 저장합니다.
    private static byte Encode(float linear)
    {
        float c = Mathf.Clamp(linear, 0f, _TONE_CLAMP);
        float over = Mathf.Max(c - _TONE_KNEE, 0f);
        float toned = Mathf.Min(c, _TONE_KNEE) + over / (1f + _TONE_SLOPE * over);
        return (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.LinearToGammaSpace(toned)) * 255f);
    }

    private static string GetDefaultTexturePath(CMinimapMapSO map)
    {
        string assetPath = AssetDatabase.GetAssetPath(map);
        if (string.IsNullOrEmpty(assetPath)) return _DEFAULT_FOLDER + "/" + map.name + ".jpg";
        string folder = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        return folder + "/" + map.name + ".jpg";
    }

    private static void SaveTexture(Color32[] pixels, int resolution, string path)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGB24, false, false);
        texture.SetPixels32(pixels);
        texture.Apply(false);
        byte[] bytes = texture.EncodeToJPG(95);
        Object.DestroyImmediate(texture);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }
}
