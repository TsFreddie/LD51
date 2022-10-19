using UnityEngine;

public class Door : Switchable
{
    public Transform DoorTransform;
    public SpriteRenderer RendererForSize;
    public BoxCollider2D Collider;
    private bool _open = false;

    public void Awake()
    {
        GameManager.Instance.OnReset += WorldReset;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
    }

    public void Update()
    {
        if (_open)
        {
            var target = DoorTransform.up * RendererForSize.size.y;
            DoorTransform.localPosition = Vector3.Lerp(DoorTransform.localPosition, target, 10.0f * Time.deltaTime);   
        }
    }

    private void WorldReset()
    {
        Collider.enabled = true;
        _open = false;
        DoorTransform.localPosition = Vector3.zero;
    }

    public override void Trigger()
    {
        if (!_open)
        {
            _open = true;
            Collider.enabled = false;
            AudioManager.Instance.Play("door_open");
            CaptionManager.Instance.ShowCaption("door open", 2.0f, CaptionType.Item);
        }
    }
}
