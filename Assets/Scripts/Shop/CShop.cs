using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CShop : MonoBehaviour
{
    [SerializeField] private List<CItemSO> _products = new List<CItemSO>();

    public IReadOnlyList<CItemSO> Products => _products;

    public bool TryBuy(CInventory buyer, int productIndex)
    {
        if (buyer == null || productIndex < 0|| productIndex >= _products.Count)
        {
            return false;
        }

        CItemSO product = _products[productIndex];
        if(product == null)
        {
            return false;
        }

        return buyer.TrybuyOneItem(product.Id, product.Cost);
    }
}
