using TMPro;
using UnityEngine;

public class Breakable : Switchable
{
    public int BreakCount;
    public Animator Animator;
    public NumberDisplay Text;
    public Collider2D CollisionCollider;

    private int _breaks;
    private bool _broken;

    public void Awake()
    {
        GameManager.Instance.OnReset += WorldReset;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
    }

    private void WorldReset()
    {
        _breaks = 0;
        _broken = false;
        CollisionCollider.enabled = true;
        Animator.SetBool("Break", false);
        UpdateText();
    }

    private void UpdateText()
    {
        if (_breaks >= BreakCount)
        {
            Text.Number = -1;
        }
        else
        {
            Text.Number = (BreakCount - _breaks);
        }
    }

    public override void Trigger()
    {
        if (_broken) return;
        _breaks += 1;
        if (_breaks >= BreakCount)
        {
            Animator.SetBool("Break", true);
            CollisionCollider.enabled = false;
            AudioManager.Instance.Play("breakable_break");
            _broken = true;
        }
        else
        {
            Animator.SetTrigger("JumpInto");
            AudioManager.Instance.Play("breakable_jumpInto");
        }
        UpdateText();
    }
}
