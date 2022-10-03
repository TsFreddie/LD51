using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    
    private Vector2 _targetPosition;

    public Camera Camera;

    public void MoveToTarget(Vector3 target)
    {
        _targetPosition = target;
    }

    public void Awake()
    {
        _targetPosition = transform.position;
        Instance = this;
    }

    public void Update()
    {
        var cameraPos = transform.position;
        var targetPos = new Vector3(_targetPosition.x, _targetPosition.y, cameraPos.z);
        cameraPos = Vector3.Lerp(cameraPos, targetPos, Time.deltaTime * 10.0f);
        transform.position = cameraPos;
    }

    public void SetRecordingMode()
    {
        
    }

    public void SetNormalMode()
    {
        
    }

    public void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
