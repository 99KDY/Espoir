using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class CQuaterView : MonoBehaviour
{
    #region 인스팩터
    [SerializeField] private GameObject _player;
    [SerializeField] private Transform _cameraTr;
    [SerializeField] private Vector3 _quaterOffset = new Vector3(8f, 10f, 8f);

    [SerializeField] private float _quaterHeight = 1.5f;
    [SerializeField] private float _quaterSharpness = 5.0f;
    [SerializeField] private float _followSpeed = 6.0f;

    #endregion


    private void Awake()
    {
        if(_cameraTr == null)
        {
            _cameraTr = Camera.main.transform;
        }

        InitQuaterCam();
    }

    private void LateUpdate()
    {

        FollowQuaterCam();
    }
    

    private void InitQuaterCam()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;

        desiredPos = _player.transform.position + _quaterOffset; 
        
        Vector3 lookPlayerPos = desiredPos + Vector3.up * _quaterHeight;

        desiredRot = Quaternion.LookRotation(lookPlayerPos - desiredPos, Vector3.up); 

    }

    private void FollowQuaterCam()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;

        desiredPos = _player.transform.position + _quaterOffset;

        Vector3 lookPlayerPos = desiredPos + Vector3.up * _quaterHeight * _quaterSharpness;

        desiredRot = Quaternion.LookRotation(lookPlayerPos - desiredPos, Vector3.up);

        ApplyCam(desiredPos, desiredRot, _quaterSharpness);

    }

    private void ApplyCam(Vector3 desiredPos, Quaternion desiredRot, float sharpness)
    {
        if(_cameraTr == null)
        {
            Debug.Log("CQuaterView 스크립트 카메라 비어있음.");
            return;
        }

        float t = Mathf.Exp(-sharpness * Time.deltaTime);
        _cameraTr.position = Vector3.Lerp(_cameraTr.position, desiredPos, t);
        _cameraTr.rotation = Quaternion.Slerp(_cameraTr.rotation, desiredRot, t);

    }


}