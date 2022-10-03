using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteStopper : MonoBehaviour
{
    public SpriteRenderer SpriteRenderer;
    public Animator Animator;
    private Sprite _initSprite;

    public void Awake()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();
        Animator = GetComponent<Animator>();
        _initSprite = SpriteRenderer.sprite;
        GameManager.Instance.OnReset += WorldReset;
        GameManager.Instance.OnWorldStart += WorldStart;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
        GameManager.Instance.OnWorldStart -= WorldStart;
    }

    private void WorldReset()
    {
        if (Animator != null)
            Animator.enabled = false;
        if (SpriteRenderer != null)
            SpriteRenderer.sprite = _initSprite;
    }

    private void WorldStart()
    {
        if (Animator != null)
            Animator.enabled = true;
    }
}
