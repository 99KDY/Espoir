using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CLobbyZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<CPlayer> (out _))
        {
            CGameManager.Instance.SetLobbyIn(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<CPlayer>(out _))
        {
            CGameManager.Instance.SetLobbyIn(false);
        }
    }
}
