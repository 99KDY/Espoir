using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class CInventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject _InventoryPanel;
    [SerializeField] private CInventory _inventory;
    [SerializeField] private Transform _slotContent;
    [SerializeField] private GameObject _itemTooltip;
    [SerializeField] private Image _tooltipIcon;
    [SerializeField] private TMP_Text _tooltipName;
    [SerializeField] private TMP_Text _tooltipInfo;
    [SerializeField] private GameObject _itemContextMenu;
    [SerializeField] private Button _useButton;
    [SerializeField] private Button _quickSlotButton;
    [SerializeField] private Transform _quickSlotContent;
    [SerializeField] private Button _moveButton;
    [SerializeField] private Button _discardButton;
    // 현재 전투 여부. 나중에 실제 게임 상태와 연결.
    [SerializeField] private bool _isBattle;
    [SerializeField] private Image _necklaceIcon;
    [SerializeField] private Image _earringIcon;
    [SerializeField] private Image _braceletIcon;
    [SerializeField] private Image _ringIcon;

    private Image[] _itemIcons;
    private TMP_Text[] _quantityTexts;
    private int _selectedSlotIndex = -1;
    private bool _isMoving = false;
    private bool _isRegisteringQuickSlot = false;
    private Image[] _quickSlotIcons;
    private TMP_Text[] _quickSlotQuantityTexts;
    private Sprite[] _quickSlotSavedIcons;

    private void Update()// 인벤토리 여는 키 I키
    {
        if (Input.GetKeyUp(KeyCode.I))
        {
            if (_InventoryPanel.activeSelf)
            {
                _InventoryPanel.SetActive(false);
                _itemTooltip.SetActive(false);
                _itemContextMenu.SetActive(false);
                CGameManager.Instance.SetGameState(EGameState.Idle);
            }
            else if (_inventory.CanOpenInventory && CGameManager.Instance.GameState == EGameState.Idle)
            {
                _InventoryPanel.SetActive(true);
                RefresgEqipment();
                CGameManager.Instance.SetGameState(EGameState.UI);
            }
        }

        if (_InventoryPanel.activeSelf && CGameManager.Instance.BattleState != EBattleState.None)
        {
            _InventoryPanel.SetActive(false);
            _itemTooltip.SetActive(false);
            _itemContextMenu.SetActive(false);
            CGameManager.Instance.SetGameState(EGameState.Idle);
        }

        if (_itemContextMenu.activeSelf && Input.GetMouseButtonDown(0))
        {
            RectTransform menuRect = _itemContextMenu.GetComponent<RectTransform>();
            
            if (!RectTransformUtility.RectangleContainsScreenPoint(menuRect, Input.mousePosition, null))
            {
                _itemContextMenu.SetActive(false);
            }
        }
    }

    private void Awake()
    {
        _itemIcons = new Image[_inventory.MaxItemSlot];
        _quantityTexts = new TMP_Text[_inventory.MaxItemSlot];

        for (int i = 0; i < _itemIcons.Length; i++)
        {
            Transform slot = _slotContent.GetChild(i);
            Transform icon = slot.Find("ItemIcon");
            Transform quantity = slot.Find("QuantityText");

            _itemIcons[i] = icon.GetComponent<Image>();
            _quantityTexts[i] = quantity.GetComponent<TMP_Text>();

            int slotIndex = i;

            EventTrigger trigger = slot.GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger = slot.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;

            enterEntry.callback.AddListener((data) =>{ShowTooltip(_inventory.GetItem(slotIndex), slotIndex);});
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;

            exitEntry.callback.AddListener((data) => {HideTooltip();});
            trigger.triggers.Add(exitEntry);

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;

            clickEntry.callback.AddListener((data) =>
            {
                PointerEventData pointerData = (PointerEventData)data;

                if (pointerData.button == PointerEventData.InputButton.Right)
                {
                    _isMoving = false;
                    ShowContextMenu(slotIndex);
                }
                else if (pointerData.button == PointerEventData.InputButton.Left)
                {
                    if (_isMoving)
                    {
                        SelectMoveDestination(slotIndex);
                    }
                }
            });

            trigger.triggers.Add(clickEntry);
            _moveButton.onClick.AddListener(OnClickMoveButton);
            _useButton.onClick.AddListener(OnClickUseButton);
            _discardButton.onClick.AddListener(OnClickDiscardButton);
            _quickSlotButton.onClick.AddListener(OnClickQuickSlotButton);
        }

        for (int i = 0; i < _inventory.QuickSlotLength; i++)
        {
            Transform quickSlot = _quickSlotContent.GetChild(i);

            EventTrigger trigger = quickSlot.GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger = quickSlot.gameObject.AddComponent<EventTrigger>();
            }

            int quickIndex = i;

            EventTrigger.Entry clickEntry = new EventTrigger.Entry();
            clickEntry.eventID = EventTriggerType.PointerClick;

            clickEntry.callback.AddListener((data) =>{if (((PointerEventData)data).button == PointerEventData.InputButton.Left)
                {
                    SelectQuickSlot(quickIndex);
                }
            });

            trigger.triggers.Add(clickEntry);
        }
        
        _quickSlotIcons = new Image[_inventory.QuickSlotLength];
        _quickSlotQuantityTexts = new TMP_Text[_inventory.QuickSlotLength];
        _quickSlotSavedIcons = new Sprite[_inventory.QuickSlotLength];

        for (int i = 0; i < _inventory.QuickSlotLength; i++)
        {
            Transform slot = _quickSlotContent.GetChild(i);

            _quickSlotIcons[i] =
                slot.Find("ItemIcon").GetComponent<Image>();

            _quickSlotQuantityTexts[i] =
                slot.Find("QuantityText").GetComponent<TMP_Text>();
        }
    }

    private void RefresgEqipment()
    {
        RefreshEquipmentIcon(_necklaceIcon, EEquipmentSlot.Necklace);
        RefreshEquipmentIcon(_earringIcon, EEquipmentSlot.Earring);
        RefreshEquipmentIcon(_braceletIcon, EEquipmentSlot.Bracelet);
        RefreshEquipmentIcon(_ringIcon, EEquipmentSlot.Ring);
    }

    private void RefreshEquipmentIcon(Image icon, EEquipmentSlot slot)
    {
        if (icon == null) return;

        CItemEquipment equipment = _inventory.GetEquipment(slot);
        icon.sprite = equipment?.ItemIcon;
        icon.enabled = icon.sprite != null;
    }

    private void RefreshInventory()
    {
        for (int i = 0; i < _itemIcons.Length; i++)
        {
            CItem item = _inventory.GetItem(i);
            
            if (i == 0) Debug.Log($"첫 슬롯 아이템: {item?.Name ?? "없음"}");

            if (item == null)
            {
                _itemIcons[i].gameObject.SetActive(false);
                _quantityTexts[i].gameObject.SetActive(false);
            }

            else
            {
                _itemIcons[i].sprite = item.ItemIcon;
                _itemIcons[i].gameObject.SetActive(true);

                if (item is CItemConsumable)
                {
                    _quantityTexts[i].text = item.Quantity.ToString();
                    _quantityTexts[i].gameObject.SetActive(true);
                }

                else
                {
                    _quantityTexts[i].gameObject.SetActive(false);
                }
            }
            RefreshQuickSlots();
        }
        RefresgEqipment();
    }

    private void Start()
    {
        RefreshInventory();
        _InventoryPanel.SetActive(false);
        _itemTooltip.SetActive(false);
        _itemContextMenu.SetActive(false);
    }

    private void OnEnable()
    {
        _inventory.OnInventoryChanged += RefreshInventory;
    }

    private void OnDisable()
    {
        _inventory.OnInventoryChanged -= RefreshInventory;
    }

    private void SetUIPosition(GameObject targetUI, int slotIndex)
    {
        RectTransform slotRect = _slotContent.GetChild(slotIndex).GetComponent<RectTransform>();
        RectTransform targetRect = targetUI.GetComponent<RectTransform>();

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        Vector3 rightCenter = (corners[2] + corners[3]) / 2f;
        targetRect.position = rightCenter;
    }

    private void ShowTooltip(CItem item , int slotIndex)
    {
        if (item == null || _itemContextMenu.activeSelf) return;
        
        _tooltipIcon.sprite = item.ItemIcon;
        _tooltipName.text = item.Name;
        _tooltipInfo.text = item.Description;
        _itemTooltip.SetActive(true);

        SetUIPosition(_itemTooltip, slotIndex);
    }

    private void HideTooltip()
    {
        _itemTooltip.SetActive(false);
    }

    private void ShowContextMenu(int slotIndex)
    {
        CItem item = _inventory.GetItem(slotIndex);

        if (item == null) return;

        _itemTooltip.SetActive(false);
        _itemContextMenu.SetActive(true);
        _selectedSlotIndex = slotIndex;

        SetUIPosition(_itemContextMenu, slotIndex);
    }


    private void OnClickUseButton()
    {
        CItem item = _inventory.GetItem(_selectedSlotIndex);
        if (item == null) return;
        bool success = false;

        if (item is CItemConsumable)
        {
            success = _inventory.TryUseItem(_selectedSlotIndex, _isBattle);
        }
        else if (item is CItemEquipment)
        {
            success = _inventory.TryEquipItem(_selectedSlotIndex, _isBattle);
        }

        if (!success)
        {
            Debug.LogWarning("아이템 사용 또는 장착에 실패했습니다.");
        }

        CloseContextMenu();
    }

    private void CloseContextMenu()
    {
        _itemContextMenu.SetActive(false);
        _selectedSlotIndex = -1;
    }

    private void OnClickMoveButton()
    {
        if (_selectedSlotIndex == -1) return;

        _isMoving = true;
        _itemContextMenu.SetActive(false);
    }

    private void SelectMoveDestination(int destinationIndex)
    {
        if (!_isMoving) return;

        bool success = _inventory.TryMoveItem( _selectedSlotIndex, destinationIndex);

        if (!success)
        {
            Debug.LogWarning("아이템 이동 실패");
        }

        _isMoving = false;
        _selectedSlotIndex = -1;
    }
    private void OnClickDiscardButton()
    {
        if (_selectedSlotIndex == -1) return;

        bool success = _inventory.TryDiscardItem(_selectedSlotIndex, 1);

        if (!success)
        {
            Debug.LogWarning("아이템 버리기 실패");
        }

        CloseContextMenu();
    }
    private void OnClickQuickSlotButton()
    {
        if (!(_inventory.GetItem(_selectedSlotIndex) is CItemConsumable))
            return;

        _isRegisteringQuickSlot = true;
        _itemContextMenu.SetActive(false);
    }

    private void SelectQuickSlot(int quickIndex)
    {
        if (!_isRegisteringQuickSlot) return;

        CItem item = _inventory.GetItem(_selectedSlotIndex);

        if (item is CItemConsumable)
        {
            bool success = _inventory.RegisterQuickSlot(quickIndex, item.Id);

            if (success)
            {
                Debug.Log($"{quickIndex + 1}번 퀵슬롯 등록");
            }
        }

        _isRegisteringQuickSlot = false;
        _selectedSlotIndex = -1;
    }
    private void RefreshQuickSlots()
    {
        for (int i = 0; i < _quickSlotIcons.Length; i++)
        {
            int itemId = _inventory.GetQuickSlotId(i);

            if (itemId == -1)
            {
                _quickSlotIcons[i].gameObject.SetActive(false);
                _quickSlotQuantityTexts[i].gameObject.SetActive(false);
                continue;
            }

            for (int j = 0; j < _inventory.MaxItemSlot; j++)
            {
                CItem item = _inventory.GetItem(j);

                if (item != null && item.Id == itemId)
                {
                    _quickSlotSavedIcons[i] = item.ItemIcon;
                    break;
                }
            }

            if (_quickSlotSavedIcons[i] != null)
            {
                _quickSlotIcons[i].sprite = _quickSlotSavedIcons[i];
                _quickSlotIcons[i].gameObject.SetActive(true);
            }

            _quickSlotQuantityTexts[i].text = _inventory.GetTotalQuantity(itemId).ToString();
            _quickSlotQuantityTexts[i].gameObject.SetActive(true);
        }
    }
}