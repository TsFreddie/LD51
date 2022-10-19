using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteStopper : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Sprite _initSprite;

    public void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _initSprite = _spriteRenderer.sprite;
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
        if (_animator != null)
            _animator.enabled = false;
        if (_spriteRenderer != null)
            _spriteRenderer.sprite = _initSprite;
    }

    private void WorldStart()
    {
        if (_animator != null)
            _animator.enabled = true;
    }
}
