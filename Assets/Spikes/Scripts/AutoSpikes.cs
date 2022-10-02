using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpikes : MonoBehaviour
{
    public float moveSpeed = 2f;

    public float spikesHeight = 1f;
    
    private Collider2D autoTrapColl;
    private List<Collider2D> _colliders;

    private Transform target;

    // Start is called before the first frame update
    void Start()
    {
        autoTrapColl = GetComponent<PolygonCollider2D>();
        _colliders = new List<Collider2D>();

        var ChildCollider = transform.GetComponentInChildren<BoxCollider2D>();
        
        ChildCollider.size = new Vector2(ChildCollider.size.x, spikesHeight);
        ChildCollider.transform.position = new Vector3(0, -(0.5f+(spikesHeight/2)), 0);

        target.position = new Vector2(0, (spikesHeight / 2) - 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void FixedUpdate()
    {
        autoTrapColl.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            StartCoroutine(MoveChild(target));
        }
    }

    IEnumerator MoveChild(Transform target)
    {
        while (Vector2.Distance(transform.position,target.position)>=0.01f)
        {
            transform.position = Vector2.MoveTowards((transform.position), target.position, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
    
    
}
