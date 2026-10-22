using System;
using System.Collections.Generic;

public abstract class CItemEquipment : CItem
{
    private List<CStatus> onUse;


    public override bool AvailableInLobby() => true;
    public override bool AvailableInBattle() => false;

    public override int Quantity
    {
        get { return 1; }
        set { value = 1; }
    }

    public void OnEquip(CPlayer player)
    {
        // 장비 효과로 고쳐야됨
        for (int i = 0; i < onUse.Count; i++)
        {
            player.Status.Add(onUse[i]);
        }
    }

    public void OnUnequip(CPlayer player)
    {
        // 장비 효과로 고쳐야됨
        for (int i = 0; i < onUse.Count; i++)
        {
            player.Status.Remove(onUse[i]);
        }
    }

    public override bool OnDiscard(CPlayer player, int quantity)
    {
        OnUnequip(player);
        return true;
    }
}
