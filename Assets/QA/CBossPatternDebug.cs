using System.Collections;
using System.Reflection;
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
    [SerializeField] private float _repeatInterval = 7f;

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
            string trigger = _patterns[index].triggerName;

            if (trigger == "bAttack01")
            {
                PlayBossPattern("TwiceAttack");
            }

            else if (trigger == "bAttack02")
            {
                PlayBossPattern("WhirlWindAttack");
            }

            else if (trigger == "bAttack03")
            {
                SetOnAttack(true);

                CPreviewPattern preview = _bossAnimator.GetComponentInChildren<CPreviewPattern>();
                preview.ShowCirclePattern(20, 15f);
                yield return new WaitForSeconds(1.5f);
                preview.ClearPreview();

                _bossAnimator.SetTrigger(trigger);
                yield return new WaitForSeconds(3.1f);
                SetOnAttack(false);
                yield return new WaitForSeconds(Mathf.Max(0f, _repeatInterval - 4.6f));
                continue;
            }

            else if (trigger == "bAttack04")
            {
                PlayBossPattern("DonutAttack");
            }

            else
            {
                SetOnAttack(true);
                _bossAnimator.SetTrigger(trigger);
                yield return new WaitForSeconds(3f);
                SetOnAttack(false);
                yield return new WaitForSeconds(Mathf.Max(0f, _repeatInterval - 3f));
                continue;
            }

            yield return new WaitForSeconds(_repeatInterval);
        }
    }

    private void PlayBossPattern(string methodName)
    {
        CSlothBoss boss = _bossAnimator.GetComponent<CSlothBoss>();
        SetOnAttack(true);

        MethodInfo method = typeof(CSlothBoss).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Invoke(boss, null);
    }

    private void SetOnAttack(bool attacking)
    {
        CSlothBoss boss = _bossAnimator.GetComponent<CSlothBoss>();
        FieldInfo onAttack = typeof(CSlothBoss).GetField("_onAttack", BindingFlags.NonPublic | BindingFlags.Instance);
        onAttack.SetValue(boss, attacking);
    }
}