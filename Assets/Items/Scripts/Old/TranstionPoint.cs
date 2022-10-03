using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranstionPoint : MonoBehaviour
{
    
   [Header("传送目标点")]  public TransitionDestination.DestinationTag destinationTag;
   
    private Collider2D stillTrapColl;
    private List<Collider2D> _colliders;
    
    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += TranstionUpdate;
    }

    // Start is called before the first frame update
    void Start()
    {
        stillTrapColl = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }
    
    private void TranstionUpdate()
    {
        stillTrapColl.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            GameManager.Instance.Transition(destinationTag);
        }
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnFixedUpdateWorld -= TranstionUpdate;
    }
}
