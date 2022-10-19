using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    public Vector2 TargetPosition { get; private set; }

    public void MoveToTarget(Vector3 target)
    {
        TargetPosition = target;
    }

    public void Awake()
    {
        TargetPosition = transform.position;
        Instance = this;
    }

    public void Update()
    {
        var cameraPos = transform.position;
        var targetPos = new Vector3(TargetPosition.x, TargetPosition.y, cameraPos.z);
        cameraPos = Vector3.Lerp(cameraPos, targetPos, Time.deltaTime * 10.0f);
        transform.position = cameraPos;
    }

    public void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
