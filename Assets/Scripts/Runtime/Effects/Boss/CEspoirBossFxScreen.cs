using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CEspoirBossFxScreen : MonoBehaviour
{
    static readonly List<CEspoirBossFxPlayer> _players = new List<CEspoirBossFxPlayer>();
    static CEspoirBossFxScreen _inst;

    public static void Register(CEspoirBossFxPlayer p)
    {
        if (!_players.Contains(p))
            _players.Add(p);
        var cam = Camera.main;
        if (cam == null)
            return;
        if (_inst == null || _inst.gameObject != cam.gameObject)
        {
            _inst = cam.GetComponent<CEspoirBossFxScreen>();
            if (_inst == null)
            {
                _inst = cam.gameObject.AddComponent<CEspoirBossFxScreen>();
                _inst.hideFlags = HideFlags.DontSave;
            }
        }
        _inst.Init(p.library);
        _inst.enabled = true;
    }

    public static void Unregister(CEspoirBossFxPlayer p)
    {
        _players.Remove(p);
        if (_players.Count == 0 && _inst != null)
            _inst.enabled = false;
    }

    Camera _cam;
    Material _mat,
        _inkMat;
    CommandBuffer _cmd;
    Matrix4x4 _view,
        _proj;

    CEspoirBossFxLibrary _lib;
    static readonly int[] Kernels = { 3, 5, 7, 9, 11 };
    readonly RenderTexture[] _blur = new RenderTexture[5];

    void Init(CEspoirBossFxLibrary lib)
    {
        if (lib != null)
            _lib = lib;
        _cam = GetComponent<Camera>();
        _cam.depthTextureMode |= DepthTextureMode.Depth;
        if (_mat == null && lib != null && lib.screenShader != null)
            _mat = new Material(lib.screenShader) { hideFlags = HideFlags.DontSave };
        if (_inkMat == null && lib != null && lib.inkMaskShader != null)
            _inkMat = new Material(lib.inkMaskShader) { hideFlags = HideFlags.DontSave };
        if (_cmd == null)
            _cmd = new CommandBuffer { name = "EspoirBossFx Ink" };
        AttachGrab();
    }

    static readonly int GrabId = Shader.PropertyToID("_EspoirBossGrab"),
        GrabOnId = Shader.PropertyToID("_EspoirGrabOn");
    CommandBuffer _grabCmd,
        _grabEnd;
    Camera _grabCam;

    void AttachGrab()
    {
        var cam = GetComponent<Camera>();
        if (cam == null || _grabCmd != null)
            return;
        _grabCam = cam;
        _grabCmd = new CommandBuffer { name = "EspoirBossFx Grab" };
        _grabCmd.GetTemporaryRT(
            GrabId,
            -1,
            -1,
            0,
            FilterMode.Bilinear,
            cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
        );
        _grabCmd.Blit(BuiltinRenderTextureType.CurrentActive, GrabId);
        _grabCmd.SetGlobalFloat(GrabOnId, 1f);
        _grabEnd = new CommandBuffer { name = "EspoirBossFx Grab End" };
        _grabEnd.SetGlobalFloat(GrabOnId, 0f);
        _grabEnd.ReleaseTemporaryRT(GrabId);
        cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, _grabCmd);
        cam.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _grabEnd);
    }

    void DetachGrab()
    {
        if (_grabCam != null)
        {
            if (_grabCmd != null)
                _grabCam.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, _grabCmd);
            if (_grabEnd != null)
                _grabCam.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _grabEnd);
        }
        _grabCmd?.Release();
        _grabEnd?.Release();
        _grabCmd = _grabEnd = null;
        _grabCam = null;
        Shader.SetGlobalFloat(GrabOnId, 0f);
    }

    void OnEnable()
    {
        if (_lib != null)
            AttachGrab();
    }

    CEspoirBossFxPlayer Active(
        out float shake,
        out float fov,
        out float ca,
        out float expo,
        out CEspoirBossFxPlayer ink
    )
    {
        shake = fov = ca = 0;
        expo = 1;
        ink = null;
        CEspoirBossFxPlayer main = null;
        for (int i = _players.Count - 1; i >= 0; i--)
        {
            var p = _players[i];
            if (p == null || p.Finished)
            {
                _players.RemoveAt(i);
                continue;
            }
            if (p.Clip == null)
                continue;
            main = p;
            float s = p.stageScale;
            shake = Mathf.Max(shake, p.G(CEspoirBossFxClipData.G_SHAKE) * s);
            fov = Mathf.Max(fov, p.G(CEspoirBossFxClipData.G_FOV));
            ca = Mathf.Max(ca, p.G(CEspoirBossFxClipData.G_CA));
            float e = p.G(CEspoirBossFxClipData.G_EXPOSURE);
            if (e > 0 && Mathf.Abs(e - 1) > Mathf.Abs(expo - 1))
                expo = e;
            if (ink == null && p.ClockAlive && p.G(CEspoirBossFxClipData.G_INK) > 0.5f)
                ink = p;
        }
        return main;
    }

    void OnPreCull()
    {
        _webList.Clear();
        if (_cam == null)
            return;
        var main = Active(out _, out _, out _, out _, out _);
        if (main == null)
            return;
        _view = _cam.worldToCameraMatrix;
        _proj = _cam.projectionMatrix;

        if (WebBlendOn)
        {
            foreach (var p in _players)
                if (p != null && !p.Finished)
                    p.GetGlowRenderers(_webList);
            foreach (var kv in _webList)
                if (kv.Key != null)
                {
                    kv.Key.forceRenderingOff = true;
                    _hidden.Add(kv.Key);
                }
        }
    }

    bool WebBlendOn =>
        _lib != null && _lib.blend != CEspoirBossFxLibrary.EEffectBlend.WithMap && _mat != null;
    readonly List<KeyValuePair<Renderer, Material>> _webList =
        new List<KeyValuePair<Renderer, Material>>();
    readonly List<Renderer> _hidden = new List<Renderer>();

    void RestoreHidden()
    {
        foreach (var r in _hidden)
            if (r != null)
                r.forceRenderingOff = false;
        _hidden.Clear();
    }

    void OnPostRender()
    {
        RestoreHidden();

    }

    Vector2 Screen01(CEspoirBossFxPlayer p, Vector3 stagePoint)
    {
        var w = p.Stage.TransformPoint(stagePoint);
        var v = _proj * (_view * new Vector4(w.x, w.y, w.z, 1));
        if (v.w <= 1e-4f)
            return new Vector2(0.5f, 0.5f);
        return new Vector2(v.x / v.w * 0.5f + 0.5f, v.y / v.w * 0.5f + 0.5f);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        var main = Active(out _, out _, out float ca, out float expo, out var ink);
        if (main == null || _mat == null)
        {
            Graphics.Blit(src, dst);
            return;
        }
        _mat.SetFloat("_Aspect", (float)src.width / Mathf.Max(1, src.height));
        if (ink != null && _inkMat != null)
        {
            var mask = RenderTexture.GetTemporary(
                src.width,
                src.height,
                24,
                RenderTextureFormat.R8
            );
            _cmd.Clear();
            _cmd.SetRenderTarget(mask);
            _cmd.ClearRenderTarget(true, true, Color.black);
            _cmd.SetViewProjectionMatrices(_view, _proj);
            if (ink.inkRenderers != null)
                foreach (var r in ink.inkRenderers)
                    Draw(r, _inkMat);
            foreach (var r in ink.InkEffectRenderers)
                Draw(r, _inkMat);
            Graphics.ExecuteCommandBuffer(_cmd);
            var pv = ink.Clip.pivot;
            _mat.SetVector("_InkCenter", Screen01(ink, new Vector3(pv.x, 4.4f, pv.z)));
            _mat.SetFloat("_InkMode", ink.G(CEspoirBossFxClipData.G_IMPMODE));
            _mat.SetFloat("_InkSeed", ink.G(CEspoirBossFxClipData.G_IMPSEED));
            _mat.SetTexture("_EspoirInkMask", mask);
            Graphics.Blit(src, dst, _mat, 1);
            RenderTexture.ReleaseTemporary(mask);
            return;
        }

        RenderTexture cur = src,
            mixed = null,
            bloomed = null;
        if (_webList.Count > 0 && WebBlendOn)
        {
            mixed = WebComposite(src);
            cur = mixed;
        }
        float strength = _lib != null ? _lib.webBloom : 0f;
        if (strength > 0f)
        {
            bloomed = WebBloom(cur, strength);
            cur = bloomed;
        }
        if (ca < 0.0002f && Mathf.Abs(expo - 1) < 0.002f)
            Graphics.Blit(cur, dst);
        else
        {
            _mat.SetFloat("_CaAmt", ca);
            _mat.SetVector("_CaCenter", Screen01(main, new Vector3(0, 4.4f, 0.6f)));
            _mat.SetFloat("_Expo", Mathf.Max(0.01f, expo));
            Graphics.Blit(cur, dst, _mat, 0);
        }
        if (bloomed != null)
            RenderTexture.ReleaseTemporary(bloomed);
        if (mixed != null)
            RenderTexture.ReleaseTemporary(mixed);
    }

    readonly List<KeyValuePair<Renderer, Material>> _sorted =
        new List<KeyValuePair<Renderer, Material>>();

    RenderTexture WebComposite(RenderTexture src)
    {
        int w = src.width,
            h = src.height;
        bool linear = _lib.blend == CEspoirBossFxLibrary.EEffectBlend.WebLinear;
        var buf = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGBHalf);
        _mat.SetFloat("_WebLinear", linear ? 1f : 0f);
        Graphics.Blit(src, buf, _mat, 5);

        Vector3 camPos = _view.inverse.GetColumn(3);
        _sorted.Clear();
        foreach (var kv in _webList)
            if (kv.Key != null && kv.Value != null)
                _sorted.Add(kv);
        _sorted.Sort(
            (a, b) =>
            {
                int qa = a.Value.renderQueue,
                    qb = b.Value.renderQueue;
                if (qa != qb)
                    return qa.CompareTo(qb);
                float da = (a.Key.bounds.center - camPos).sqrMagnitude,
                    db = (b.Key.bounds.center - camPos).sqrMagnitude;
                return qa >= 2500 ? db.CompareTo(da) : da.CompareTo(db);
            }
        );
        _cmd.Clear();
        _cmd.SetRenderTarget(buf);
        _cmd.ClearRenderTarget(true, false, Color.clear);
        _cmd.SetViewProjectionMatrices(_view, _proj);
        _cmd.SetGlobalVector("_EspoirGlowSize", new Vector4(1f / w, 1f / h, w, h));
        _cmd.SetGlobalVector("_WorldSpaceCameraPos", camPos);
        _cmd.SetGlobalFloat("_EspoirFxPostShow", linear ? 0f : 1f);

        _keyed.Clear();
        foreach (var kv in _sorted)
        {
            if (!kv.Key.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null)
                continue;
            var mat = kv.Value;
            if (!mat.IsKeywordEnabled("ESPOIR_GLOW"))
            {
                mat.EnableKeyword("ESPOIR_GLOW");
                _keyed.Add(mat);
            }
            var m = mf.sharedMesh;
            var l2w = kv.Key.localToWorldMatrix;
            bool flip = l2w.determinant < 0f;
            if (flip)
                _cmd.SetInvertCulling(true);
            for (int i = 0; i < m.subMeshCount; i++)
                _cmd.DrawMesh(m, l2w, mat, i, 0);
            if (flip)
                _cmd.SetInvertCulling(false);
        }
        _cmd.SetGlobalFloat("_EspoirFxPostShow", 0f);
        Graphics.ExecuteCommandBuffer(_cmd);
        foreach (var mat in _keyed)
            if (mat != null)
                mat.DisableKeyword("ESPOIR_GLOW");
        _keyed.Clear();
        var outRt = RenderTexture.GetTemporary(w, h, 0, src.format);
        _mat.SetTexture("_WebBuf", buf);
        _mat.SetFloat("_WebLinear", linear ? 1f : 0f);
        Graphics.Blit(src, outRt, _mat, 6);
        RenderTexture.ReleaseTemporary(buf);
        return outRt;
    }

    readonly List<Material> _keyed = new List<Material>();

    readonly List<KeyValuePair<Renderer, Material>> _glowList =
        new List<KeyValuePair<Renderer, Material>>();
    readonly Dictionary<Material, Vector3> _glowSaved = new Dictionary<Material, Vector3>();
    const float GlowBackground = 0.5f;

    RenderTexture RenderGlow(int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGBHalf);
        _glowList.Clear();
        foreach (var p in _players)
            if (p != null && !p.Finished)
                p.GetGlowRenderers(_glowList);
        _cmd.Clear();
        _cmd.SetRenderTarget(rt);
        _cmd.ClearRenderTarget(
            false,
            true,
            new Color(GlowBackground, GlowBackground, GlowBackground, 1f)
        );
        _cmd.SetViewProjectionMatrices(_view, _proj);
        _cmd.SetGlobalVector("_EspoirGlowSize", new Vector4(1f / w, 1f / h, w, h));
        _cmd.SetGlobalVector("_WorldSpaceCameraPos", (Vector3)_view.inverse.GetColumn(3));

        _glowSaved.Clear();
        foreach (var kv in _glowList)
        {
            var mat = kv.Value;
            if (!kv.Key.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null)
                continue;
            if (!_glowSaved.ContainsKey(mat))
            {
                _glowSaved[mat] = new Vector3(
                    mat.GetFloat("_ZWrite"),
                    mat.GetFloat("_ZTest"),
                    mat.IsKeywordEnabled("ESPOIR_GLOW") ? 1 : 0
                );
                mat.EnableKeyword("ESPOIR_GLOW");
                mat.SetFloat("_ZWrite", 0f);
                mat.SetFloat("_ZTest", mat.GetFloat("_ZTest") > 7.5f ? 8f : 4f);
            }
            var m = mf.sharedMesh;
            var l2w = kv.Key.localToWorldMatrix;
            bool flip = l2w.determinant < 0f;
            if (flip)
                _cmd.SetInvertCulling(true);
            for (int i = 0; i < m.subMeshCount; i++)
                _cmd.DrawMesh(m, l2w, mat, i, 0);
            if (flip)
                _cmd.SetInvertCulling(false);
        }
        Graphics.ExecuteCommandBuffer(_cmd);
        foreach (var kv in _glowSaved)
        {
            if (kv.Key == null)
                continue;
            kv.Key.SetFloat("_ZWrite", kv.Value.x);
            kv.Key.SetFloat("_ZTest", kv.Value.y);
            if (kv.Value.z < 0.5f)
                kv.Key.DisableKeyword("ESPOIR_GLOW");
        }
        _glowSaved.Clear();
        return rt;
    }

    RenderTexture WebBloom(RenderTexture src, float strength)
    {
        float aspect = (float)src.width / Mathf.Max(1, src.height);
        int h0 = 360,
            w0 = Mathf.Max(1, Mathf.RoundToInt(h0 * aspect));
        var fmt = RenderTextureFormat.ARGBHalf;
        _mat.SetFloat("_BloomFeed", _lib != null ? Mathf.Max(0.01f, _lib.bloomFeed) : 1f);
        var glow = RenderGlow(w0 * 2, h0 * 2);
        var input = RenderTexture.GetTemporary(w0, h0, 0, fmt);
        Graphics.Blit(glow, input, _mat, 2);
        RenderTexture.ReleaseTemporary(glow);
        var prev = input;
        for (int i = 0; i < 5; i++)
        {
            int w = Mathf.Max(1, w0 >> i),
                h = Mathf.Max(1, h0 >> i);
            var hor = RenderTexture.GetTemporary(w, h, 0, fmt);
            var ver = RenderTexture.GetTemporary(w, h, 0, fmt);
            _mat.SetFloat("_BlurRadius", Kernels[i]);
            _mat.SetVector("_BlurStep", new Vector4(1f / w, 0, 0, 0));
            Graphics.Blit(prev, hor, _mat, 3);
            _mat.SetVector("_BlurStep", new Vector4(0, 1f / h, 0, 0));
            Graphics.Blit(hor, ver, _mat, 3);
            RenderTexture.ReleaseTemporary(hor);
            if (_blur[i] != null)
                RenderTexture.ReleaseTemporary(_blur[i]);
            _blur[i] = ver;
            prev = ver;
        }
        RenderTexture.ReleaseTemporary(input);
        for (int i = 0; i < 5; i++)
            _mat.SetTexture("_B" + i, _blur[i]);
        _mat.SetFloat("_BloomStrength", strength);
        var outRt = RenderTexture.GetTemporary(src.width, src.height, 0, src.format);
        Graphics.Blit(src, outRt, _mat, 4);
        for (int i = 0; i < 5; i++)
        {
            RenderTexture.ReleaseTemporary(_blur[i]);
            _blur[i] = null;
        }
        return outRt;
    }

    void Draw(Renderer r, Material m)
    {
        if (m == null)
            return;
        if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
            return;
        int n = 1;
        if (r is SkinnedMeshRenderer sk && sk.sharedMesh != null)
            n = sk.sharedMesh.subMeshCount;
        else if (r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            n = mf.sharedMesh.subMeshCount;
        for (int i = 0; i < n; i++)
            _cmd.DrawRenderer(r, m, i, 0);
    }

    void OnDisable()
    {
        RestoreHidden();
        DetachGrab();
    }

    void OnDestroy()
    {
        RestoreHidden();
        DetachGrab();
        if (_mat != null)
            Destroy(_mat);
        if (_inkMat != null)
            Destroy(_inkMat);
        _cmd?.Release();
    }
}
