using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

public class CPrideBoss : CEnemy
{

    public enum EPrideBossState
    {
        Idle = 0,
        NormalAttack,
        SubAttack,
        MainAttack,
        Dead
    }

    [Header("연결")]
    [SerializeField] private CPlayer _player;

    [Header("인식범위 (확인용)")]
    [SerializeField] private float _attackRange = 20f;

    [SerializeField] private CPreviewPattern _previewPattern;
    [SerializeField] private float _attackRadius = 3f;

    private Vector3 _attackPoint;


    protected override void Awake()
    {
        base.Awake();

        if (_player == null)
        {
            _player = FindFirstObjectByType<CPlayer>();
        }

        if (_player == null)
        {
            CLogger.Error("CPrideBoss : 플레이어를 찾을 수 없음");
            enabled = false;
        }
    }

    protected override void Update()
    {
        base.Update();

        // 거리 확인용 임시 입력.
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (IsPlayerInAttackRange())
            {
                ShowAttackPreview();
            }
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_previewPattern != null)
            {
                _previewPattern.ClearPreview();
            }
        }
    }

    private bool IsPlayerInAttackRange()
    {
        if (_player == null) return false;

        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;

        return toPlayer.sqrMagnitude <= _attackRange * _attackRange;
    }

    private void ShowAttackPreview()
    {
        if (_previewPattern == null)
        {
            CLogger.Error("CPrideBoss : 예고 범위를 연결해야 함");
            return;
        }

        // 플레이어 위치
        _attackPoint = _player.transform.position;
        _attackPoint.y = transform.position.y;

        _previewPattern.transform.SetPositionAndRotation(
            _attackPoint + Vector3.up * 0.05f,
            Quaternion.identity
        );

        _previewPattern.ShowCirclePattern(64, _attackRadius);
    }

    private void OnDisable()
    {
        if (_previewPattern != null)
        {
            _previewPattern.ClearPreview();
        }
    }

}