using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

public class CQuaterView : MonoBehaviour
{
    #region 인스팩터
    [Header("Inspector")]
    [SerializeField] private GameObject _player;
    [SerializeField] private Transform _cameraTr;

    [Header("QuaterView Info")]
    [SerializeField] private Vector3 _quaterOffset = new Vector3(10f, 8f, 10f);
    [SerializeField] private float _quaterHeight = 1.5f;
    [SerializeField] private float _quaterSharpness = 5.0f;

    [Header("QuaterView Zoom")]
    [SerializeField] private float _zoomSharpness = 10.0f;
    [SerializeField] private float _minZoom = -5f;
    [SerializeField] private float _maxZoom = 7f;

    #endregion


    private void Awake()
    {
        if(_cameraTr == null)
        {
            _cameraTr = Camera.main.transform;
        }

        InitQuaterView();
    }

    private void Update()
    {
        QuaterZoom();

    }

    private void LateUpdate()
    {

        FollowQuaterView();
       
    }
    

    private void InitQuaterView()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;

        desiredPos = _player.transform.position + _quaterOffset; 
        
        Vector3 lookPlayerPos = desiredPos + Vector3.up * _quaterHeight;

        desiredRot = Quaternion.LookRotation(lookPlayerPos - desiredPos, Vector3.up);

        ApplyCam(desiredPos, desiredRot, _quaterSharpness);
    }

    private void FollowQuaterView()
    {
        Vector3 desiredPos;
        Quaternion desiredRot;

        desiredPos = _player.transform.position + _quaterOffset;

        Vector3 lookPlayerPos = _player.transform.position + Vector3.up * _quaterHeight * _quaterSharpness;

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

    private void QuaterZoom()
    {
        if(_cameraTr == null)
        {
            return;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        // 1. 카메라 가르키는 방향 찾기
        Vector3 zoomCam = _player.transform.position - _cameraTr.position;

        zoomCam +=  zoomCam.normalized * scroll * _zoomSharpness;

        float zoomdis = Mathf.Clamp(zoomCam.magnitude, _minZoom, _maxZoom);

        _cameraTr.position = zoomCam * zoomdis;
        
       
    }


}