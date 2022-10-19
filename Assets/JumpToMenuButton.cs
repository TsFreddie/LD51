using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpToMenuButton : MonoBehaviour
{
    public CanvasGroup TargetCanvasGroup;
    public GameObject FirstSelectedObject;
    
    public void JumpToMenu()
    {
        InputManager.Instance.OpenMenu(TargetCanvasGroup, FirstSelectedObject);
    }
}
