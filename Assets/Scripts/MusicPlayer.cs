using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public string MusicName;

    void Start()
    {
        if (string.IsNullOrEmpty(MusicName))
        {
            MusicManager.Instance.Stop();
        }
        else
        {
            MusicManager.Instance.Play(MusicName, MusicFadeMode.FadeOutCutIn);
        }
    }
}
