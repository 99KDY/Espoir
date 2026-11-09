using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CPlayerDashGhost : MonoBehaviour
{
    private static readonly int _DIRECTION = Shader.PropertyToID("_Dir");
    private static readonly int _GROUND_Y = Shader.PropertyToID("_GhostGroundY");
    private static readonly int _SEED = Shader.PropertyToID("_Seed");
    private static readonly int _PROGRESS = Shader.PropertyToID("_T");
    private static readonly int _ALPHA = Shader.PropertyToID("_AlphaNow");
    private static readonly int _SMEAR = Shader.PropertyToID("_SmearNow");

    internal class CSource
    {
        internal readonly GameObject _CHARACTER;
        internal readonly CharacterController _CONTROLLER;
        internal readonly SkinnedMeshRenderer[] _SKINS;
        internal readonly MeshFilter[] _SOLIDS;
        internal readonly MeshRenderer[] _RENDERERS;
        internal readonly Transform[] _BONES;
        internal readonly Quaternion[] _ROTATIONS;
        internal readonly Quaternion[] _ORIGINAL;
        internal readonly CPlayerDashGhostPose _POSE;

        /// <summary>
        /// 잔상에 사용할 렌더러와 보정 본을 한 번 찾아 보관합니다.
        /// </summary>

        public CSource(GameObject character)
        {
            _CHARACTER = character;
            _CONTROLLER = character.GetComponentInParent<CharacterController>();
            _SKINS = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _SOLIDS = character.GetComponentsInChildren<MeshFilter>(true);
            _RENDERERS = new MeshRenderer[_SOLIDS.Length];
            _POSE = Resources.Load<CPlayerDashGhostPose>("Animations/Player/PlayerDashGhostPose");

            for (int i = 0; i < _SOLIDS.Length; i++)
            {
                _SOLIDS[i].TryGetComponent(out _RENDERERS[i]);
            }

            List<Transform> bones = new List<Transform>();
            List<Quaternion> rotations = new List<Quaternion>();
            if (_POSE != null)
            {
                foreach (Transform bone in character.GetComponentsInChildren<Transform>(true))
                {
                    if (_POSE.TryGetRotation(bone.name, out Quaternion rotation))
                    {
                        bones.Add(bone);
                        rotations.Add(rotation);
                    }
                }
            }

            _BONES = bones.ToArray();
            _ROTATIONS = rotations.ToArray();
            _ORIGINAL = new Quaternion[_BONES.Length];
        }

        internal bool ShouldCapture(Renderer renderer, Mesh mesh)
        {
            return renderer != null && mesh != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
                renderer.gameObject.layer != CPlayerEffectRenderer.EFFECT_LAYER &&
                (_POSE == null || !_POSE.IsExcluded(mesh));
        }
    }

    private static Shader _depthShader;

    private readonly List<Mesh> _BAKED_MESHES = new List<Mesh>();
    private Material _colorMaterial;
    private Material _depthMaterial;
    private float _lifetime;
    private float _opacity;
    private float _smear;
    private float _hold;
    private float _age;

    public static int ActiveCount { get; private set; }

    /// <summary>
    /// 현재 자세를 복사해 이동 방향을 따라 사라지는 잔상을 생성합니다.
    /// </summary>

    internal static CPlayerDashGhost Spawn(CSource source, Material material, float life, float alpha,
        float smear, float hold, Vector3 direction, bool flare = true)
    {
        GameObject root = new GameObject("PlayerDashGhost");
        CPlayerDashGhost ghost = root.AddComponent<CPlayerDashGhost>();
        ghost._lifetime = life;
        ghost._opacity = alpha;
        ghost._smear = smear;
        ghost._hold = hold;
        ghost._colorMaterial = new Material(material);

        float ground = source._CONTROLLER != null
            ? source._CONTROLLER.bounds.min.y : source._CHARACTER.transform.position.y;
        ghost._colorMaterial.SetVector(_DIRECTION, direction);
        ghost._colorMaterial.SetFloat(_GROUND_Y, ground);
        ghost._colorMaterial.SetFloat(_SEED, UnityEngine.Random.value * 7f);

        if (_depthShader == null)
        {
            _depthShader = Resources.Load<Shader>("Shaders/Player/Effects/PlayerGhostDepth");
        }

        ghost._depthMaterial = new Material(_depthShader);
        ghost._depthMaterial.CopyPropertiesFromMaterial(ghost._colorMaterial);
        ghost._depthMaterial.renderQueue = _depthShader.renderQueue;
        ghost.Capture(source, flare);
        ghost.UpdateMaterials();
        return ghost;
    }

    private void Capture(CSource source, bool flare)
    {
        if (flare)
        {
            for (int i = 0; i < source._BONES.Length; i++)
            {
                source._ORIGINAL[i] = source._BONES[i].localRotation;
                if (source._BONES[i].gameObject.activeInHierarchy)
                {
                    source._BONES[i].localRotation = source._ROTATIONS[i];
                }
            }
        }

        try
        {
            foreach (SkinnedMeshRenderer skin in source._SKINS)
            {
                if (skin == null || !source.ShouldCapture(skin, skin.sharedMesh))
                {
                    continue;
                }

                Mesh mesh = new Mesh();
                _BAKED_MESHES.Add(mesh);
                skin.BakeMesh(mesh, true);
                AddPart(skin.transform, mesh, Vector3.one);
            }

            for (int i = 0; i < source._SOLIDS.Length; i++)
            {
                MeshFilter solid = source._SOLIDS[i];
                if (solid != null && source.ShouldCapture(source._RENDERERS[i], solid.sharedMesh))
                {
                    AddPart(solid.transform, solid.sharedMesh, solid.transform.lossyScale);
                }
            }
        }
        finally
        {
            if (flare)
            {
                for (int i = 0; i < source._BONES.Length; i++)
                {
                    source._BONES[i].localRotation = source._ORIGINAL[i];
                }
            }
        }
    }

    private void AddPart(Transform source, Mesh mesh, Vector3 scale)
    {
        AddRenderer(source, mesh, scale, _depthMaterial);
        AddRenderer(source, mesh, scale, _colorMaterial);
    }

    private void AddRenderer(Transform source, Mesh mesh, Vector3 scale, Material material)
    {
        GameObject part = new GameObject(source.name);
        part.layer = CPlayerEffectRenderer.EFFECT_LAYER;
        part.transform.SetParent(transform, false);
        part.transform.SetPositionAndRotation(source.position, source.rotation);
        part.transform.localScale = scale;
        part.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        Material[] materials = new Material[Mathf.Max(1, mesh.subMeshCount)];

        for (int i = 0; i < materials.Length; i++)
        {
            materials[i] = material;
        }

        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnEnable()
    {
        ActiveCount++;
    }

    private void OnDisable()
    {
        ActiveCount--;
    }

    private void Update()
    {
        _age += Time.unscaledDeltaTime;

        if (_age >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        UpdateMaterials();
    }

    private void UpdateMaterials()
    {
        float t = Mathf.InverseLerp(0f, _lifetime, _age);
        float progress = Mathf.InverseLerp(_hold, 1f, t);
        float opacity = _opacity * Mathf.Min(1f, _age / 0.025f);
        float smear = _smear * (1f + t * 0.6f);

        _colorMaterial.SetFloat(_PROGRESS, progress);
        _depthMaterial.SetFloat(_PROGRESS, progress);
        _colorMaterial.SetFloat(_ALPHA, opacity);
        _depthMaterial.SetFloat(_ALPHA, opacity);
        _colorMaterial.SetFloat(_SMEAR, smear);
        _depthMaterial.SetFloat(_SMEAR, smear);
    }

    private void OnDestroy()
    {
        Destroy(_colorMaterial);
        Destroy(_depthMaterial);

        foreach (Mesh mesh in _BAKED_MESHES)
        {
            Destroy(mesh);
        }
    }
}
