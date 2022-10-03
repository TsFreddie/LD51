using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DoorHelper : MonoBehaviour
{
    #if UNITY_EDITOR
    public void Update()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var thisRenderer = GetComponent<SpriteRenderer>();
        var thisBoxCollider = GetComponent<BoxCollider2D>();
        var mask = GetComponentInChildren<SpriteMask>();
        thisBoxCollider.size = thisRenderer.size;
        mask.transform.localScale = new Vector3(1, thisRenderer.size.y, 1);
        foreach (var renderer in renderers)
        {
            renderer.size = thisRenderer.size;
        }
    }
    #endif
}
