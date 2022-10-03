using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VanishingPlatform : Switchable
{
    public Animator Animator;
    public Collider2D CollisionCollider;

    private int _vanishingFrame = -1;
    private bool _animated = false;

    public void Awake()
    {
        GameManager.Instance.OnReset += WorldReset;
        GameManager.Instance.OnFixedUpdateWorld += WorldUpdate;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
        GameManager.Instance.OnFixedUpdateWorld -= WorldUpdate;
    }

    private void WorldReset()
    {
        _vanishingFrame = -1;
        _animated = false;
        CollisionCollider.enabled = true;
        Animator.SetBool("Break", false);
    }

    private void WorldUpdate()
    {
        var breakFrames = Mathf.RoundToInt(0.25f / Time.fixedDeltaTime);
        if (_vanishingFrame >= 0)
        {
            var deltaFrame = GameManager.Instance.Frame - _vanishingFrame;
            if (!_animated && deltaFrame > breakFrames - 2)
            {
                _animated = true;
                AudioManager.Instance.Play("vanishing_platform_break");
                Animator.SetBool("Break", true);
            }

            if (deltaFrame > breakFrames)
            {
                CollisionCollider.enabled = false;
            }
        }
    }

    public override void Trigger()
    {
        if (_vanishingFrame < 0)
        {
            _vanishingFrame = GameManager.Instance.Frame;
        }
    }
}
