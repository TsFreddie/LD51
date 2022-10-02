using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpikesChild : MonoBehaviour
{
    private Collider2D Child;
    private List<Collider2D> _colliders;

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
    
    private void FixedUpdate()
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
}
