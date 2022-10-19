using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.InputSystem;

public class EditorCamera : MonoBehaviour
{
    public PlayerInput MenuInput;
    public PixelScaler PixelScaler;
    public bool ZoomedOut;
    public PixelPerfectCamera PixelPerfectCamera;
    public Camera Camera;

    private Vector2 _lastDragPointer;
    private bool _isDragging;

    protected void Update()
    {
        var keyMoveSpeed = Camera.orthographicSize * Time.deltaTime * 2.0f;
        var move = MenuInput.actions["Navigate"].ReadValue<Vector2>();
        var t = transform;
        t.position = (Vector3)((Vector2)t.position + move * keyMoveSpeed) + new Vector3(0, 0, -10);

        var touchPoint = PixelScaler.ScreenToRenderTexture(MenuInput.actions["Point"].ReadValue<Vector2>());
        
        if (MenuInput.actions["RightClick"].IsPressed())
        {
            if (!_isDragging)
            {
                _lastDragPointer = touchPoint;
                _isDragging = true;
            }
            else
            {
                var lastWorldPoint = Camera.ScreenToWorldPoint(_lastDragPointer);
                transform.position -=
                    Camera.ScreenToWorldPoint(touchPoint) - lastWorldPoint;
                _lastDragPointer = touchPoint;
            }
        }
        else
        {
            _isDragging = false;
        }
    }
}
