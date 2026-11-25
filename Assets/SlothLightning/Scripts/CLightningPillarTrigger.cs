using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CLightningPillarTrigger : MonoBehaviour
{
    private CPlayer _player;

    private void Awake()
    {
        _player = GetComponent<CPlayer>();
    }


    private void OnTriggerEnter(Collider other)
    {

        
    }

}
