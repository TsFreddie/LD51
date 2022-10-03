using UnityEngine;

public class Bullet : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private Sprite _initSprite;
    private Vector3 _initPosition;

    public float Speed;

    public void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _initSprite = _spriteRenderer.sprite;
        _initPosition = transform.position;
        GameManager.Instance.OnReset += WorldReset;
        GameManager.Instance.OnFixedUpdate += WorldUpdate;
        GameManager.Instance.OnWorldStart += WorldStart;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
        GameManager.Instance.OnFixedUpdate -= WorldUpdate;
        GameManager.Instance.OnWorldStart -= WorldStart;
    }

    private void WorldReset()
    {
        _animator.enabled = false;
        _spriteRenderer.sprite = _initSprite;
        transform.position = _initPosition;
    }

    private void WorldUpdate()
    {
        transform.Translate(-Vector3.right * Speed * Time.deltaTime);
    }

    private void WorldStart()
    {
        _animator.enabled = true;
    }
}
