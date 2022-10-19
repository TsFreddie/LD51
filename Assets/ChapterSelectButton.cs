using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChapterSelectButton : MonoBehaviour
{
    public string LevelName;
    
    public void SelectLevel()
    {
        GameManager.Instance.LoadLevel(LevelName);
    }
}
