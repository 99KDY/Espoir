using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CShopUI : MonoBehaviour
{
    [SerializeField] private CShop _shop;
    [SerializeField] private CInventory _inventory;
    [SerializeField] private GameObject _shopPanel;
    [SerializeField] private RectTransform _productContent;
    [SerializeField] private GameObject _productRow;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _resultText;

    private void Awake()
    {
        _productRow.SetActive(false);

        for (int i = 0; i < _shop.Products.Count; i++)
        {
            CItemSO product = _shop.Products[i];
            if (product == null) continue;

            int productIndex = i;
            GameObject row = Instantiate(_productRow, _productContent);
            row.transform.Find("ItemIcon").GetComponent<Image>().sprite = product.ItemIcon;
            row.transform.Find("PriceText").GetComponent<TMP_Text>().text =
                $"{product.Cost:N0} G";

            Button buyButton = row.transform.Find("BuyButton").GetComponent<Button>();
            buyButton.onClick.AddListener(() => Buy(productIndex));
            row.SetActive(true);
        }

        Close();
    }

    public void Open()
    {
        RefreshGold();
        _resultText.text = string.Empty;
        _shopPanel.SetActive(true);
    }

    public void Close()
    {
        _shopPanel.SetActive(false);
    }

    private void Buy(int productIndex)
    {
        bool bought = _shop.TryBuy(_inventory, productIndex);
        RefreshGold();

        _resultText.text = bought ? $"{_shop.Products[productIndex].Name} 1개를 구매했습니다." : "구매에 실패했습니다. 골드와 가방 공간을 확인해 주세요.";
    }

    private void RefreshGold()
    {
        _goldText.text = $"{_inventory.Gold:N0} G";
    }
}