using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Transform parent;
    bool _isDragging = false;
    Vector3 offset;

    public void OnPointerDown(PointerEventData eventData)
    {
        offset = parent.position - Input.mousePosition;
        _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
    }

    private void Update()
    {
        if (_isDragging)
        {
            parent.position = Input.mousePosition + offset;
        }
        
    }


}
