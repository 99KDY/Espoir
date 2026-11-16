using TMPro;
using UnityEngine;

/// <summary>
/// 기울임꼴이 없는 TMP 글꼴에 합성 기울임을 적용합니다.
/// Chrome(Skia)은 글자마다 기준선을 축으로 skewX −1/4 → x += 0.25·(기준선 위 높이).
/// TMP 의 Italic 은 글리프 사각형(아틀라스 여백 포함)의 가운데를 축으로 기울여서, 여백이 큰 글꼴(빛무리용 여백 56)에서는 글자가 왼쪽으로 크게 밀린다.
/// 그래서 TMP Italic 은 끄고 이 컴포넌트로 기울인다(OnPreRenderText 에서 정점만 바꿈, 글자 배치 · 진행폭은 그대로).
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class CHudObliqueText : MonoBehaviour
{
    [Tooltip("기울기 (Chrome 합성 기울임 = 0.25)")]
    [SerializeField] private float _skew = 0.25f;

    private TMP_Text _text;

    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        _text.OnPreRenderText += Shear;
        _text.SetVerticesDirty();
    }

    private void OnDisable()
    {
        if (_text == null) return;

        _text.OnPreRenderText -= Shear;
        _text.SetVerticesDirty();
    }

    private void Shear(TMP_TextInfo info)
    {
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            Vector3[] v = info.meshInfo[c.materialReferenceIndex].vertices;
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = v[c.vertexIndex + k];
                p.x += _skew * (p.y - c.baseLine);
                v[c.vertexIndex + k] = p;
            }
        }
    }
}
