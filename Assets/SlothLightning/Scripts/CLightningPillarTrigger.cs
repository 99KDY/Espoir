using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CLightningPillarTrigger : MonoBehaviour
{

    private CSlothBoss _boss;
    

    private void Awake()
    {

        _boss = GetComponent<CSlothBoss>();
        if(_boss == null)
        {
            _boss = FindFirstObjectByType<CSlothBoss>();
        }

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            CLogger.Log($"플레이어의 태그를 찾지 못했습니다.");

            return;
        }
        
        
        CPlayer player = other.GetComponent<CPlayer>();
        if(player == null)
        {
            CLogger.Warn("Player를 찾지 못했당");
            return;
        }

        CLogger.Log($"플레이어의 레이어 : {other.gameObject.layer}");

        SDamageInfo damageToPlayer = new SDamageInfo(0.3f, ERatioSwitch.ByCurrent);
        
        player.OnHit(_boss, damageToPlayer);
        
        CLogger.Log($"플레이어 체력 : {player.HpCurrent}");

    }

}
