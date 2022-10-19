using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MenuFadeIn : MonoBehaviour
{
    public RectTransform Rect;
    public CanvasGroup Group;
    public float FadeTime = 1.0f;
    public bool HideByDefault = true;
    public Vector2 FadeDirection = Vector2.up;

    private Vector2 _initAnchoredPosition;
    private int _direction = 0;
    private bool _overriden = false;

    public void Start()
    {
        _initAnchoredPosition = Rect.anchoredPosition;
        if (!_overriden)
        {
            if (HideByDefault)
            {
                Group.alpha = 0;
            }
            else
            {
                Group.alpha = 1;
            }
        }

        InputManager.Instance.OnBack += OnBack;
    }

    private void OnBack(CanvasGroup obj)
    {
        if (obj == Group)
        {
            FadeOut();
        }
    }

    public void OnDestroy()
    {
        InputManager.Instance.OnBack -= OnBack;
    }

    public void FadeIn()
    {
        AudioManager.Instance.Play("ui/menuin");
        FadeInSilently();
    }

    public async void FadeInSilently()
    {
        _overriden = true;
        _direction = 1;
        Group.interactable = true;
        gameObject.SetActive(true);
        while (Group.alpha < 1.0f)
        {
            var alpha = Group.alpha;
            alpha += Time.deltaTime / FadeTime;
            Group.alpha = alpha;
            Rect.anchoredPosition = _initAnchoredPosition + FadeDirection * 20 * (1.0f - alpha);
            try { await UniTask.Yield(this.GetCancellationTokenOnDestroy()); }
            catch { return; }
            if (_direction != 1) { return; }
        }

        Group.alpha = 1.0f;
        _direction = 0;
    }

    public void FadeOut()
    {
        AudioManager.Instance.Play("ui/menuout");
        FadeOutSilently();
    }

    public async void FadeOutSilently()
    {
        _overriden = true;
        _direction = -1;
        Group.interactable = false;
        while (Group.alpha > 0.0f)
        {
            var alpha = Group.alpha;
            alpha -= Time.deltaTime / FadeTime;
            Group.alpha = alpha;
            Rect.anchoredPosition = _initAnchoredPosition + FadeDirection * 20 * (1.0f - alpha);
            try { await UniTask.Yield(this.GetCancellationTokenOnDestroy()); }
            catch { return; }
            if (_direction != -1) { return; }
        }

        Group.alpha = 0.0f;
        _direction = 0;
        gameObject.SetActive(false);
    }
}
