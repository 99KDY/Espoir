using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 영역의 마우스 진입과 클릭을 처리합니다.
/// </summary>
public class CCharacterInfoPointerArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler
{
    private bool _isHovered;

    /// <summary>
    /// 클릭한 UI 영역을 전달합니다.
    /// </summary>
    public event Action<CCharacterInfoPointerArea> Clicked;

    public bool IsHovered => _isHovered;

    private void OnDisable()
    {
        _isHovered = false;
    }

    /// <summary>
    /// 마우스 진입 상태를 설정합니다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
    }

    /// <summary>
    /// 마우스 진입 상태를 해제합니다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
    }

    /// <summary>
    /// 왼쪽 클릭을 전달합니다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Clicked?.Invoke(this);
    }
}
