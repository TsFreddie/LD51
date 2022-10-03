using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpikesChild : MonoBehaviour
{
    private Collider2D Child;
    private List<Collider2D> _colliders;
    
    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += SpikesChildUpdate;
    }

    // Start is called before the first frame update
    void Start()
    {
        Child = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void SpikesChildUpdate()
    {
        Child.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            GameManager.Instance.IsDied(true);
        }
    }


    private void OnDestroy()
    {
        GameManager.Instance.OnFixedUpdateWorld -= SpikesChildUpdate;
    }
}
