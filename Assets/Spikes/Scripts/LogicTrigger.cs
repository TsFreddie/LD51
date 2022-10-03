using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    // TODO!C: CHINESE
    [Header("要触发的机关")]
    public Switchable Target;

    public Sprite OffSprite;
    public Sprite OnSprite;

    [Header("可重复触发")]
    public bool Repeatable;

    private Collider2D switchColl;
    private List<Collider2D> _colliders;

    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += SwitchUpdate;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnFixedUpdateWorld -= SwitchUpdate;
    }

    // Start is called before the first frame update
    void Start()
    {
        switchColl = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void SwitchUpdate()
    {
        switchColl.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);

        if (_colliders.Count > 0)
        {
            Target.Trigger();
        }
    }
}
