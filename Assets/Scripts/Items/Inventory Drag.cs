using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] Transform parent;
    bool _isDragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
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
            Vector3 offset = Input.mousePosition - parent.position;
            parent.position = Input.mousePosition - offset;
        }
        
    }

}
