using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTestEnemy : CEnemy
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        // TODO : 플레이어를 쫓아가기
        // TODO : 주기적으로 공격하기
    }
}
