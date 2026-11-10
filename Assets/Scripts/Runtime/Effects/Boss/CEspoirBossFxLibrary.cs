using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Espoir/Boss Fx Library", fileName = "EspoirBossFxLibrary")]
public class CEspoirBossFxLibrary : ScriptableObject
{
    public Shader[] shaders;
    public Texture2D noise;
    public bool webToneMap = true;
    public float bloomFeed = 0f;
    public float webBloom = 0.9f;

    public enum EEffectBlend
    {
        WithMap = 0,
        AfterPost = 1,
        WebLinear = 2,
    }

    public EEffectBlend blend = EEffectBlend.WebLinear;
    public Shader screenShader;
    public Shader inkMaskShader;

    private readonly Dictionary<TextAsset, CEspoirBossFxClipData> _clips =
        new Dictionary<TextAsset, CEspoirBossFxClipData>();

    /// <summary>공유 이펙트 데이터를 한 번 읽어 재사용합니다.</summary>
    public CEspoirBossFxClipData GetClip(TextAsset data)
    {
        if (!_clips.TryGetValue(data, out CEspoirBossFxClipData clip))
        {
            clip = CEspoirBossFxClipData.Parse(data.bytes);
            _clips.Add(data, clip);
        }

        return clip;
    }

    private void OnDisable()
    {
        _clips.Clear();
    }

    /// <summary>이름에 대응하는 이펙트 셰이더를 반환합니다.</summary>
    public Shader Get(string name)
    {
        Shader.SetGlobalFloat("_EspoirFxTone", webToneMap ? 1f : 0f);
        Shader.SetGlobalFloat("_EspoirFxBloomFeed", bloomFeed);
        string full = "EspoirBossFx/" + name;
        foreach (var s in shaders)
            if (s != null && s.name == full)
                return s;
        return null;
    }
}
