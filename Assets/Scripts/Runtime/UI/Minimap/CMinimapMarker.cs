using System.Collections.Generic;
using UnityEngine;

/// <summary>미니맵 표식의 종류입니다.</summary>
public enum EMinimapMarkerKind
{
    Npc,
    Monster,
    Boss
}

/// <summary>
/// 미니맵에 표식을 띄울 대상. NPC · 몬스터 · 보스 오브젝트에 붙이면 켜져 있는 동안 미니맵이 실제 월드 위치를 따라 그린다.
/// 모양은 붙은 컴포넌트로 정한다: CSlothBoss → Boss, 그 밖의 CEnemy → Monster, CTalkNpc → Npc (셋 다 없으면 Inspector 값).
/// 같은 오브젝트에 CUnit 이 있으면 HP 가 0 일 때 숨긴다. 미니맵(CMinimapHud)이 새로 생긴 적 · 대화 NPC 에 자동으로 붙인다.
/// </summary>
[DisallowMultipleComponent]
public class CMinimapMarker : MonoBehaviour
{
    private static readonly List<CMinimapMarker> _ACTIVE = new List<CMinimapMarker>();

    [Tooltip("보스 · 적 · 대화 NPC 컴포넌트가 없을 때만 쓰는 모양")]
    [SerializeField] private EMinimapMarkerKind _kind = EMinimapMarkerKind.Npc;
    [Tooltip("비우면 이 오브젝트 위치를 쓴다")]
    [SerializeField] private Transform _anchor;

    private CUnit _unit;

    /// <summary>지금 켜져 있는 표식 전부. 미니맵이 매 프레임 읽는다.</summary>
    public static IReadOnlyList<CMinimapMarker> Active => _ACTIVE;

    public EMinimapMarkerKind Kind => _kind;
    public Vector3 WorldPosition => _anchor != null ? _anchor.position : transform.position;
    public bool IsShown => _unit == null || _unit.HpCurrent > 0f;

    /// <summary>표식 모양과 기준 위치를 직접 정한다 (컴포넌트로 정한 모양보다 우선).</summary>
    public void Setup(EMinimapMarkerKind kind, Transform anchor = null)
    {
        _kind = kind;
        _anchor = anchor;
    }

    private void Awake()
    {
        TryGetComponent(out _unit);
        if (TryGetComponent(out CSlothBoss _)) _kind = EMinimapMarkerKind.Boss;
        else if (TryGetComponent(out CEnemy _)) _kind = EMinimapMarkerKind.Monster;
        else if (TryGetComponent(out CTalkNpc _)) _kind = EMinimapMarkerKind.Npc;
    }

    private void OnEnable()
    {
        if (!_ACTIVE.Contains(this)) _ACTIVE.Add(this);
    }

    private void OnDisable()
    {
        _ACTIVE.Remove(this);
    }
}
