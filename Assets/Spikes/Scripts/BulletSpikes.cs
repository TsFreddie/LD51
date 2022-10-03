using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BulletSpikes : Switchable
{
    [Header("目标点位置")] public Transform target;
    
    public float moveSpeed = 1f;
    
    
    private Collider2D bulletColl;
    private List<Collider2D> _colliders;
    
    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += BulletSpikesUpdate;
    }
    
    void Start()
    {
        bulletColl = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }
    
    public override  void Trigger()
    {
        StartCoroutine(MoveChild(target.position));
    }
    
    private void BulletSpikesUpdate()
    {
        bulletColl.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            GameManager.Instance.IsDied(true);
        }
    }
    
    IEnumerator MoveChild(Vector2 target)
    {
        while (Vector2.Distance(transform.position,target)>=0.01f)
        {
            transform.position = Vector2.MoveTowards((transform.position), target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        
        
        
    }
}
