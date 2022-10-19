using System.Collections.Generic;
using UnityEngine;

public enum CaptionType
{
    Info,
    Item,
    Success,
    Danger,
    Fail,
}

public class CaptionManager : MonoBehaviour
{
    public static CaptionManager Instance { get; private set; }
    public List<Caption> Captions = new List<Caption>();

    public Color CaptionColor(CaptionType type)
    {
        return type switch
        {
            CaptionType.Info => Color.white,
            CaptionType.Item => new Color32(164, 212, 239, 255),
            CaptionType.Success => new Color32(125, 235, 114, 255),
            CaptionType.Fail => new Color32(203, 57, 87, 255),
            CaptionType.Danger => new Color32(239, 93, 93, 255),
            _ => Color.white,
        };
    }

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowCaption(string caption, float time, CaptionType type)
    {
        Caption firstUnused = null;

        // Find same caption or first unused caption
        foreach (var cap in Captions)
        {
            if (cap.CaptionText == caption)
            {
                cap.SetCaption(caption, time, CaptionColor(type));
                return;
            }

            if (firstUnused == null && !cap.enabled)
            {
                firstUnused = cap;
            }
        }

        // Use the first unused caption
        if (firstUnused != null)
        {
            firstUnused.SetCaption(caption, time, CaptionColor(type));
            return;
        }

        // Find least used caption
        var leastTimeCaption = Captions[0];
        var leastTime = leastTimeCaption.LeftTime;

        for (int i = 1; i < Captions.Count; i++)
        {
            var cap = Captions[i];
            if (cap.LeftTime < leastTime)
            {
                leastTime = cap.LeftTime;
                leastTimeCaption = cap;
            }
        }

        leastTimeCaption.SetCaption(caption, time, CaptionColor(type));
    }
}
