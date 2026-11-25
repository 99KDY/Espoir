using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[DefaultExecutionOrder(200)]
public class CSlothLightningPillar : MonoBehaviour
{
    [SerializeField] private TextAsset _clip;
    [SerializeField] private CEspoirBossFxLibrary _library;

    private CEspoirBossFxPlayer _visual;
    private CEspoirBossFxPlayer _pattern;
    private CapsuleCollider _trigger;
    private CEspoirBossFxClipData.CFrameKey[] _keys;
    private int _nextKey;
    private int _frame;
    private float _elapsed;
    private Vector3 _center;
    private bool _visible;
    private bool _inPattern;

    /// <summary>
    /// 기둥의 재생 프레임을 패턴에 맞춥니다.
    /// </summary>
    public void Bind(CEspoirBossFxPlayer pattern, TextAsset clip)
    {
        _pattern = pattern;
        _clip = clip;
        _inPattern = true;
    }

    private void Start()
    {
        _trigger = GetComponent<CapsuleCollider>();
        _trigger.enabled = false;
        GameObject visual = new GameObject("PillarVisual");
        visual.layer = gameObject.layer;
        visual.SetActive(false);
        visual.transform.SetParent(transform, false);
        _visual = visual.AddComponent<CEspoirBossFxPlayer>();
        _visual.data = _clip;
        _visual.library = _library;
        _visual.screenEffects = false;
        visual.SetActive(true);
        _visual.enabled = false;
        _visual.clock = GetClipTime;
        _keys = _visual.Clip.objs[0].keys;

        if (!_inPattern || _pattern.screenEffects) CEspoirBossFxScreen.Register(_visual);
        Tick(0f);
    }

    private float GetClipTime()
    {
        return _frame * _visual.Clip.dt;
    }

    private void LateUpdate()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float deltaTime)
    {
        if (_visual == null || _visual.Finished || (_inPattern && (_pattern == null || _pattern.Finished)))
        {
            _trigger.enabled = false;
            Destroy(gameObject);
            return;
        }

        _elapsed += deltaTime;
        _frame = _inPattern ? _pattern.Frame : Mathf.RoundToInt(_elapsed / _visual.Clip.dt);
        while (_nextKey < _keys.Length && _keys[_nextKey].frame <= _frame)
        {
            CEspoirBossFxClipData.CFrameKey key = _keys[_nextKey++];
            if ((key.flags & CEspoirBossFxClipData.F_MAT) != 0)
            {
                _center = key.matrix.GetColumn(3);
                _center.x = -_center.x;
            }
            if ((key.flags & CEspoirBossFxClipData.F_VIS) != 0) _visible = key.vis;
            if ((key.flags & CEspoirBossFxClipData.F_GONE) != 0) _visible = false;
        }

        if (_inPattern) transform.localPosition = _center;
        _visual.transform.localPosition = -_center;
        bool show = !_inPattern || _pattern.PreviewGroup < 0 ||
            _pattern.PreviewGroup == (int)CEspoirBossFxPlayer.EEffectGroup.Beam;
        _visual.PreviewGroup = show ? -1 : (int)CEspoirBossFxPlayer.EEffectGroup.Ground;
        _trigger.enabled = show && _visible && _frame > 0;
        _visual.Advance(0f);
    }

    private void OnDisable()
    {
        if (_trigger != null) _trigger.enabled = false;
    }
}
