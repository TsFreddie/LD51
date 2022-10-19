using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    public Portal Target;
    public Transform CameraPositionForThisPortal;

    private bool _isPlayerInsidePortal;
    private Collider2D _collider;

    protected void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        GameManager.Instance.OnReset += ResetSwitch;
        GameManager.Instance.OnFixedUpdateWorld += SwitchUpdate;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnReset -= ResetSwitch;
        GameManager.Instance.OnFixedUpdateWorld -= SwitchUpdate;
    }

    private void ResetSwitch()
    {
        _collider.enabled = true;
        _isPlayerInsidePortal = false;
    }

    private void SwitchUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (!_collider.enabled) return;

        var result = _collider.OverlapColliderAll(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        });

        // Player touched it
        if (result.Count > 0)
        {
            var player = result[0].GetComponent<PlayerControl>();
            if (!_isPlayerInsidePortal)
            {
                if (Target != null)
                {
                    var teleportDelta = Target.transform.position - transform.position;
                    player.transform.position += teleportDelta;
                    Physics2D.SyncTransforms();
                    Target._isPlayerInsidePortal = true;
                    if (Target.CameraPositionForThisPortal != null)
                        CameraController.Instance.MoveToTarget(Target.CameraPositionForThisPortal.position);
                    AudioManager.Instance.Play("portal");
                    CaptionManager.Instance.ShowCaption("portal", 1.5f, CaptionType.Item);
                }
            }

            _isPlayerInsidePortal = true;
        }
        else if (_isPlayerInsidePortal)
        {
            _isPlayerInsidePortal = false;
        }
    }
}
