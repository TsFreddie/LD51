using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Switch : MonoBehaviour
{
    [Header("要触发的陷阱")]
    public Switchable Target;

    private Collider2D switchColl;
    private List<Collider2D> _colliders;

    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += SwitchUpdate;
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


    private void OnDestroy()
    {
        GameManager.Instance.OnFixedUpdateWorld -= SwitchUpdate;
    }
}
