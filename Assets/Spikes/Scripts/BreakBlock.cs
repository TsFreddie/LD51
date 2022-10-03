using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakBlock : MonoBehaviour
{
    [Header("碰撞次数")] public int collisionNumber=1;
    
    
    private Collider2D breakBlock;
    private List<Collider2D> _colliders;

    private int nowCollNumber = 0;
    private bool CanColl = false;
    private bool isLeave = false;
    
    protected void Awake()
    {
        GameManager.Instance.OnFixedUpdateWorld += BreakBlockUpdate;
    }

    // Start is called before the first frame update
    void Start()
    {
        breakBlock = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void BreakBlockUpdate()
    {
        breakBlock.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            isLeave = false;
        }
        else
        {
            isLeave = true;
            CanColl = true;
        }

        if (!isLeave && CanColl)
        {
            nowCollNumber += 1;
            CanColl = false;
        }

        if (nowCollNumber>=collisionNumber)
        {
            Destroy(gameObject);
        }
        
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnFixedUpdateWorld -= BreakBlockUpdate;
    }
}
