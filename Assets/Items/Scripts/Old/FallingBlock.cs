using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingBlock  : Switchable
{
    [Header("目标点位置")] public Transform target;
    
    public float waitTime = 1f;
 
    public override void Trigger()
    {
        StartCoroutine(WaitTime(waitTime));
    }

    IEnumerator WaitTime(float time)
    {
        yield return new WaitForSeconds(time);
        StartCoroutine(MoveChild(target.position));
    }

    IEnumerator MoveChild(Vector2 target)
    {
        while (Vector2.Distance(transform.position,target)>=0.01f)
        {
            transform.position = Vector2.MoveTowards((transform.position), target, 1f * Time.deltaTime);
            yield return null;
        }
    }
}
