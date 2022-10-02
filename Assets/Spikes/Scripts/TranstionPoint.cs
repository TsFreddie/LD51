using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranstionPoint : MonoBehaviour
{
    
   [Header("传送目标点")]  public TransitionDestination.DestinationTag destinationTag;
   
    private Collider2D stillTrapColl;
    private List<Collider2D> _colliders;

    // Start is called before the first frame update
    void Start()
    {
        stillTrapColl = GetComponent<PolygonCollider2D>();
        _colliders = new List<Collider2D>();
    }
    
    private void FixedUpdate()
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
}
