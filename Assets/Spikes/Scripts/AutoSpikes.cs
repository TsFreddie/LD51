using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AutoSpikes : MonoBehaviour
{
    public float moveSpeed = 2f;

    public float spikesHeight = 1f;
    
    private Collider2D autoTrapColl;
    private List<Collider2D> _colliders;

    private Transform spike;
    private Vector2 target;

    // Start is called before the first frame update
    void Start()
    {
        autoTrapColl = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
        spike = transform.GetChild(0).transform;

        target = Vector2.zero;
        var ChildCollider = spike.GetComponent<BoxCollider2D>();
        
        ChildCollider.size = new Vector2(ChildCollider.size.x, spikesHeight);
        spike.position = transform.TransformPoint(new Vector3(0, -(0.5f+(spikesHeight/2)), 0));

        target = transform.TransformPoint(new Vector2(0, (spikesHeight / 2) - 0.5f));

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

    IEnumerator MoveChild(Vector2 target)
    {
        while (Vector2.Distance(spike.position,target)>=0.01f)
        {
            spike.position = Vector2.MoveTowards((spike.position), target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }




}
