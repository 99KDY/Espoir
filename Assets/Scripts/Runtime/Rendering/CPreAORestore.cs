using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[DisallowMultipleComponent]
public class CPreAORestore : MonoBehaviour
{
    private class CRestoreRenderer
    {
        public Renderer renderer;
        public Material[] materials;
    }

    private const CameraEvent _COPY_EVENT = CameraEvent.AfterSkybox;
    private const CameraEvent _RESTORE_EVENT = CameraEvent.AfterImageEffectsOpaque;
    private const string _SHADER_NAME = "Hidden/Espoir/NpcPreAORestore";
    private const string _FUR_KEYWORD = "_CREATURE_FUR";

    private static readonly List<CPreAORestore> _TARGETS = new List<CPreAORestore>();
    private static readonly Dictionary<Camera, CommandBuffer> _COPY_BUFFERS = new Dictionary<Camera, CommandBuffer>();
    private static readonly Dictionary<Camera, CommandBuffer> _RESTORE_BUFFERS =
        new Dictionary<Camera, CommandBuffer>();
    private static readonly List<Camera> _UNUSED_CAMERAS = new List<Camera>();
    private static readonly int _PRE_AO_COLOR = Shader.PropertyToID("_PreAOColor");
    private static readonly int _JIG_A = Shader.PropertyToID("_JigA");
    private static readonly int _JIG_B = Shader.PropertyToID("_JigB");

    [SerializeField]
    private Renderer[] _renderers;

    private readonly List<CRestoreRenderer> _RENDERERS = new List<CRestoreRenderer>();
    private static readonly Plane[] _FRUSTUM_PLANES = new Plane[6];
    private static Shader _shader;

    private void OnEnable()
    {
        if (_shader == null)
        {
            _shader = Shader.Find(_SHADER_NAME);
        }

        if (_shader == null || _renderers == null)
        {
            return;
        }

        _RENDERERS.Clear();

        foreach (Renderer renderer in _renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Material[] sources = renderer.sharedMaterials;
            Material[] materials = new Material[sources.Length];

            for (int i = 0; i < sources.Length; i++)
            {
                materials[i] = CreateRestoreMaterial(sources[i]);
            }

            _RENDERERS.Add(new CRestoreRenderer { renderer = renderer, materials = materials });
        }

        if (_RENDERERS.Count == 0)
        {
            return;
        }

        _TARGETS.Add(this);

        if (_TARGETS.Count == 1)
        {
            Camera.onPreCull += BuildBuffers;
        }
    }

    private void OnDisable()
    {
        if (_TARGETS.Remove(this) && _TARGETS.Count == 0)
        {
            Camera.onPreCull -= BuildBuffers;
        }

        foreach (CRestoreRenderer item in _RENDERERS)
        {
            foreach (Material material in item.materials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        _RENDERERS.Clear();
        ReleaseUnusedBuffers();
    }

    private static Material CreateRestoreMaterial(Material source)
    {
        Material material = new Material(_shader)
        {
            name = "PreAORestore",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (source != null && source.IsKeywordEnabled(_FUR_KEYWORD))
        {
            material.EnableKeyword(_FUR_KEYWORD);
            material.SetVector(_JIG_A, source.GetVector(_JIG_A));
            material.SetVector(_JIG_B, source.GetVector(_JIG_B));
        }

        return material;
    }

    private static void BuildBuffers(Camera camera)
    {
        ReleaseUnusedBuffers();

        _COPY_BUFFERS.TryGetValue(camera, out CommandBuffer copy);
        _RESTORE_BUFFERS.TryGetValue(camera, out CommandBuffer restore);
        copy?.Clear();
        restore?.Clear();

        if (camera.cameraType != CameraType.Game || !camera.TryGetComponent(out PostProcessLayer layer) ||
            !layer.enabled)
        {
            return;
        }

        GeometryUtility.CalculateFrustumPlanes(camera, _FRUSTUM_PLANES);
        bool hasDraws = false;

        foreach (CPreAORestore target in _TARGETS)
        {
            foreach (CRestoreRenderer item in target._RENDERERS)
            {
                Renderer renderer = item.renderer;

                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff ||
                    !renderer.gameObject.activeInHierarchy ||
                    (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0 ||
                    !GeometryUtility.TestPlanesAABB(_FRUSTUM_PLANES, renderer.bounds))
                {
                    continue;
                }

                if (restore == null)
                {
                    copy = new CommandBuffer { name = "Espoir Pre AO Copy" };
                    restore = new CommandBuffer { name = "Espoir Pre AO Restore" };
                    camera.AddCommandBuffer(_COPY_EVENT, copy);
                    camera.AddCommandBuffer(_RESTORE_EVENT, restore);
                    _COPY_BUFFERS.Add(camera, copy);
                    _RESTORE_BUFFERS.Add(camera, restore);
                }

                if (!hasDraws)
                {
                    RenderTextureFormat format = camera.allowHDR
                        ? RenderTextureFormat.DefaultHDR
                        : RenderTextureFormat.Default;
                    copy.GetTemporaryRT(_PRE_AO_COLOR, -1, -1, 0, FilterMode.Point, format);
                    copy.Blit(BuiltinRenderTextureType.CameraTarget, _PRE_AO_COLOR);
                    copy.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                    hasDraws = true;
                }

                for (int i = 0; i < item.materials.Length; i++)
                {
                    restore.DrawRenderer(renderer, item.materials[i], i, 0);
                }
            }
        }

        if (hasDraws)
        {
            restore.ReleaseTemporaryRT(_PRE_AO_COLOR);
        }
    }

    private static void ReleaseUnusedBuffers()
    {
        foreach (KeyValuePair<Camera, CommandBuffer> pair in _RESTORE_BUFFERS)
        {
            if (pair.Key == null || !pair.Key.isActiveAndEnabled || _TARGETS.Count == 0)
            {
                _UNUSED_CAMERAS.Add(pair.Key);
            }
        }

        foreach (Camera camera in _UNUSED_CAMERAS)
        {
            CommandBuffer copy = _COPY_BUFFERS[camera];
            CommandBuffer restore = _RESTORE_BUFFERS[camera];

            if (camera != null)
            {
                camera.RemoveCommandBuffer(_COPY_EVENT, copy);
                camera.RemoveCommandBuffer(_RESTORE_EVENT, restore);
            }

            copy.Release();
            restore.Release();
            _COPY_BUFFERS.Remove(camera);
            _RESTORE_BUFFERS.Remove(camera);
        }

        _UNUSED_CAMERAS.Clear();
    }
}
