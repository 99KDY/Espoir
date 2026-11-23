using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD 이름표에서 캐릭터 정보창을 여는 키 안내를 표시합니다.
/// </summary>
public class CCharacterInfoKeyHint : MonoBehaviour
{
    private static readonly Vector4 _CURVE_SETTLE = new Vector4(0.2f, 0.7f, 0.2f, 1f);

    [Header("연결")]
    [Tooltip("비워 두면 시작할 때 씬에서 한 번 찾습니다.")]
    [SerializeField] private CCharacterInfoUI _window;
    [SerializeField] private CCharacterInfoUI _windowPrefab;
    [Tooltip("이름판 전체를 덮는 마우스 영역")]
    [SerializeField] private CCharacterInfoPointerArea _area;
    [SerializeField] private CHudBox _key;
    [SerializeField] private TMP_Text _keyLabel;
    [SerializeField] private RectTransform _wordsMask;
    [SerializeField] private CanvasGroup _wordsGroup;

    [Header("마우스를 올렸을 때")]
    [SerializeField] private Color _keyFill = new Color(10f / 255f, 12f / 255f, 20f / 255f, 0.18f);
    [SerializeField] private Color _keyHoverFill = new Color(1f, 240f / 255f, 220f / 255f, 1f);
    [SerializeField] private Color _labelColor = new Color(1f, 228f / 255f, 196f / 255f, 1f);
    [SerializeField] private Color _labelHoverColor = new Color(24f / 255f, 26f / 255f, 44f / 255f, 1f);
    [Tooltip("평소 글자 재질(그림자 있음)과 밝게 채워졌을 때 재질(그림자 없음)")]
    [SerializeField] private Material _labelMaterial;
    [SerializeField] private Material _labelHoverMaterial;
    [SerializeField, Min(0f)] private float _wordsWidth = 110f;
    [SerializeField, Min(0f)] private float _keyTime = 0.25f;
    [SerializeField, Min(0f)] private float _wordsWidthTime = 0.35f;
    [SerializeField, Min(0f)] private float _wordsFadeTime = 0.3f;

    private SCssTween _keyBlend;
    private SCssTween _widthBlend;
    private SCssTween _fadeBlend;
    private float _appliedKey = -1f;
    private float _appliedWidth = -1f;
    private float _appliedFade = -1f;
    private bool _isBrightLabel;
    private bool _ownsWindow;

    private void Awake()
    {
        if (_window == null) _window = FindFirstObjectByType<CCharacterInfoUI>();
        if (_window != null || _windowPrefab == null) return;

        _window = Instantiate(_windowPrefab);
        SceneManager.MoveGameObjectToScene(_window.gameObject, gameObject.scene);
        _ownsWindow = true;
    }

    private void Start()
    {
        if (_window == null)
        {
            gameObject.SetActive(false);
            return;
        }

        Apply(0f, 0f, 0f);
    }

    private void OnEnable()
    {
        if (_area != null) _area.Clicked += OnClicked;
    }

    private void OnDisable()
    {
        if (_area != null) _area.Clicked -= OnClicked;
    }

    private void OnDestroy()
    {
        if (_ownsWindow && _window != null) Destroy(_window.gameObject);
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        float target = _area != null && _area.IsHovered ? 1f : 0f;
        _keyBlend.To(target, now, _keyTime, 0f, CCssEase.EASE);
        _widthBlend.To(target, now, _wordsWidthTime, 0f, _CURVE_SETTLE);
        _fadeBlend.To(target, now, _wordsFadeTime, 0f, CCssEase.EASE);
        Apply(_keyBlend.Value(now), _widthBlend.Value(now), _fadeBlend.Value(now));
    }

    private void Apply(float key, float width, float fade)
    {
        if (!Mathf.Approximately(key, _appliedKey))
        {
            _appliedKey = key;
            _key.Fill = Color.Lerp(_keyFill, _keyHoverFill, key);
            _keyLabel.color = Color.Lerp(_labelColor, _labelHoverColor, key);
            bool isBright = key > 0.5f;
            if (isBright != _isBrightLabel)
            {
                _isBrightLabel = isBright;
                Material material = isBright ? _labelHoverMaterial : _labelMaterial;
                if (material != null) _keyLabel.fontSharedMaterial = material;
            }
        }

        if (!Mathf.Approximately(width, _appliedWidth))
        {
            _appliedWidth = width;
            _wordsMask.sizeDelta = new Vector2(_wordsWidth * width, _wordsMask.sizeDelta.y);
        }

        if (!Mathf.Approximately(fade, _appliedFade))
        {
            _appliedFade = fade;
            _wordsGroup.alpha = fade;
        }
    }

    private void OnClicked(CCharacterInfoPointerArea area)
    {
        if (_window != null) _window.Toggle();
    }
}
