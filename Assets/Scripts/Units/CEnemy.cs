using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CEnemy : CUnit
{
    [Header("몬스터 정보 (테스트 전용, 우선시됨)")]
    [SerializeField] private CEnemyStatSO _targetStatSO;

    [Header("드롭하는 아이템 범위")]
    [SerializeField] private List<SDropTable> _drops;

    private bool _isDropped = false;

    public List<SDropTable> Drops
    {
        get { return _drops; }
        set { _drops = value; }
    }

    protected override void Awake()
    {
        base.Awake();
        if (_targetStatSO != null)
        {
            Setup(_targetStatSO);
        }
    }

    public void Setup(CEnemyStatSO target)
    {
        this._baseHpMax = target.Hp;
        this._baseAttack = target.Attack;
        this._baseDefence = target.Defence;

        this._hpMax = _baseHpMax;
        this._hpCurrent = _hpMax;
        this._attack = _baseAttack;
        this._defence = _baseDefence;
    }

    protected virtual void Start()
    {
        
    }

    protected override void Update()
    {
        base.Update();
    }

    // 디버그용
    public void CallDie()
    {
        this.Die();
    }

    protected override void Die()
    {
        base.Die();
        CPlayer player = FindFirstObjectByType<CPlayer>();
        if (!_isDropped)
        {
            _isDropped = true;
            if (player != null && player.Inventory != null)
            {
                if (CItemSO.TryMakeDropItem(_drops, out CItemSO targetItem)) player.Inventory.AddItem(targetItem.Id);
            }
        }
    }
}
