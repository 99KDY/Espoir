using System;

public enum EConsumableEffect
{
    None,
    HealConstant,
    HealRatio,
}

[Serializable]
public class CConsumableAction
{
    public EConsumableEffect effect;
    public float value;
}

public abstract class CItemConsumable : CItem
{
    Action<CPlayer> _onUse;
    private int _quantity;

    public override bool AvailableInLobby() => false;
    public override bool AvailableInBattle() => true;

    public override int Quantity
    {
        get { return _quantity; }
        set { _quantity = value; }
    }

    public CItemConsumable(CItemConsumableSO targetItem)
    {
        Setup(targetItem);
    }

    public void OnUse(CPlayer player)
    {
        if (_onUse == null) return;
        _onUse(player);
    }

    public override bool OnDiscard(CPlayer player, int quantity)
    {
        _quantity -= quantity;
        if (_quantity <= 0) return true;
        else return false;
    }

    public override bool Setup(CItemSO targetItem)
    {
        if(targetItem is CItemConsumableSO targetConsumable)
        {
            this._id = targetConsumable.Id;
            this._name = targetConsumable.Name;
            this._itemIcon = targetConsumable.ItemIcon;
            this._description = targetConsumable.Description;

            switch (targetConsumable.Action.effect)
            {
                case EConsumableEffect.None:
                    _onUse = (CPlayer player) => { };
                    break;
                case EConsumableEffect.HealConstant:
                    _onUse = (CPlayer player) => player.GetHeal(targetConsumable.Action.value);
                    break;
                case EConsumableEffect.HealRatio:
                    _onUse = (CPlayer player) => player.GetHeal(targetConsumable.Action.value, ERatioSwitch.ByMax);
                    break;
            }

            return true;
        }
        else
        {
            CLogger.Error("CItemConsumable : 소모품이 아닌 아이템이 들어옴!");
            return false;
        }
    }


}
