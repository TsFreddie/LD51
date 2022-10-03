using TMPro;
using UnityEngine;

public class Breakable : Switchable
{
    public int BreakCount;
    public Animator Animator;
    public NumberDisplay Text;
    public Collider2D CollisionCollider;

    private int _breaks;

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
        _breaks += 1;
        if (_breaks >= BreakCount)
        {
            Animator.SetBool("Break", true);
            CollisionCollider.enabled = false;
            AudioManager.Instance.Play("breakable_break");
        }
        else
        {
            Animator.SetTrigger("JumpInto");
            AudioManager.Instance.Play("breakable_jumpInto");
        }
        UpdateText();
    }
}
