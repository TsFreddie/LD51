using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovableSpikes : MonoBehaviour
{

    [Header("坐标点")] public Transform[] targets;

    public float moveSpeed=1f;
    
    
    private Collider2D movableSpikesColl;
    private List<Collider2D> _colliders;

    private int index = 0;
    
    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += MovableSpikesUpdate;
    }
    
    void Start()
    {
        movableSpikesColl = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
        StartCoroutine(MoveChild(targets[0].position));
    }

    private void MovableSpikesUpdate()
    {
        movableSpikesColl.OverlapCollider(new ContactFilter2D()
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

        if (index==1)
        {
            index--;
        }
        else
        {
            index++;
        }

        StartCoroutine(MoveChild(targets[index].position));
    }
}
