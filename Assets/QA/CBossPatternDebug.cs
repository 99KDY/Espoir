using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CBossPatternDebug : MonoBehaviour
{
    [System.Serializable]
    public struct SPattern
    {
        public string patternName;
        public string triggerName;
    }

    [SerializeField] private Animator _bossAnimator;
    [SerializeField] private GameObject _window;
    [SerializeField] private Transform _buttonRoot;
    [SerializeField] private Button _buttonPrefab;
    [SerializeField] private SPattern[] _patterns;
    [SerializeField] private float _repeatInterval = 5f;

    private Coroutine _repeat;

    private void Awake()
    {
        _window.SetActive(false);
    }

    public void OpenWindow()
    {
        _window.SetActive(true);
        CreateButtons();
    }

    private void CreateButtons()
    {
        for (int i = _buttonRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(_buttonRoot.GetChild(i).gameObject);
        }

        for (int i = 0; i < _patterns.Length; i++)
        {
            int index = i;
            Button button = Instantiate(_buttonPrefab, _buttonRoot);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = _patterns[i].patternName;
            button.onClick.AddListener(() => PlayOnly(index));
        }
    }

    private void PlayOnly(int index)
    {
        _window.SetActive(false);

        if (_repeat != null)
        {
            StopCoroutine(_repeat);
        }

        _repeat = StartCoroutine(RepeatPattern(index));
    }

    private IEnumerator RepeatPattern(int index)
    {
        while (true)
        {
            for (int i = 0; i < _patterns.Length; i++)
            {
                _bossAnimator.ResetTrigger(_patterns[i].triggerName);
            }

            _bossAnimator.SetTrigger(_patterns[index].triggerName);
            yield return new WaitForSeconds(_repeatInterval);
        }
    }
}