using System;
using UnityEngine;

public class LogicTrigger : MonoBehaviour
{
    public Switchable Target;

    private Sprite _offSprite;
    public Sprite OnSprite;

    public string OffSound;
    public string OnSound;

    public SpriteRenderer SpriteRenderer;

    public bool Repeatable;

    private bool _triggered;
    private Collider2D _collider;

    protected void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _offSprite = SpriteRenderer.sprite;
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
        SpriteRenderer.sprite = _offSprite;
        SpriteRenderer.enabled = _offSprite != null;
    }

    private void SwitchUpdate()
    {
        if (!_collider.enabled) return;

        var result = _collider.OverlapColliderAll(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        });

        // Player touched it
        if (result.Count > 0)
        {
            if (!_triggered)
            {
                if (Target != null)
                    Target.Trigger();
                SpriteRenderer.sprite = OnSprite;
                SpriteRenderer.enabled = OnSprite != null;
                _triggered = true;
                if (!string.IsNullOrEmpty(OnSound))
                    AudioManager.Instance.PlayAt(OnSound, transform);
            }
        }
        else
        {
            if (_triggered) _triggered = false;
            if (Repeatable)
            {
                SpriteRenderer.sprite = _offSprite;
                SpriteRenderer.enabled = _offSprite != null;
                if (!string.IsNullOrEmpty(OffSound))
                    AudioManager.Instance.PlayAt(OffSound, transform);
            }
        }
    }
}
