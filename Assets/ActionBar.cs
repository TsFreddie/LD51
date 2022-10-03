using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class ActionBar : MonoBehaviour
{
    public float StartTime;
    public float EndTime;
    public float MinSize;
    public float PixelSize;

    public int Lane;

    public RectTransform Bg;
    public Image BgImage;
    public Image Icon;
    public Color BrokeColor;
    public Color NormalColor;

    private bool _broke = false;
    public bool Broke
    {
        get => _broke;
        set
        {
            _broke = value;
            BgImage.color = _broke ? BrokeColor : NormalColor;
        }
    }

    public void SetIcon(Sprite icon)
    {
        if (Application.isPlaying)
            Icon.sprite = icon;
    }

    void Update()
    {
        if (Bg == null) return;
        if (Broke) return;

        SetupTransform();
    }

    private void SetupTransform()
    {
        var start = Mathf.FloorToInt(StartTime / PixelSize) * PixelSize;
        var delta = EndTime - StartTime;
        var end = Mathf.Max(Mathf.CeilToInt((start + delta) / PixelSize) * PixelSize, start + MinSize);

        Bg.anchorMin = new Vector2(start, 1);
        Bg.anchorMax = new Vector2(end, 1);
        Bg.anchoredPosition = new Vector2(0, -Lane);
        Bg.rotation = Quaternion.Euler(0, 0, 0);

        Icon.enabled = (end - start) / PixelSize >= 5.8f;
    }

    public async void Break(Action callback)
    {
        Broke = true;
        var velocity = new Vector2(Random.Range(4.5f, 8.5f), Random.Range(40.5f, 60.0f));
        var angularVelocity = Random.Range(0.0f, 30.0f);

        SetupTransform();

        var t = 0f;

        while (t < 2.5f)
        {
            t += Time.deltaTime;
            if (this == null) return;
            Bg.anchoredPosition += velocity * Time.deltaTime;
            Bg.rotation = Quaternion.Euler(0, 0, angularVelocity * t);
            angularVelocity -= 10.0f * Time.deltaTime;
            velocity.y -= 175f * Time.deltaTime;
            await UniTask.Yield();
        }

        if (this == null) return;
        callback?.Invoke();
    }
}
