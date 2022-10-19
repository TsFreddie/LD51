using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Caption : MonoBehaviour
{
    public TMP_Text Text;
    public Image Border;

    private float _appearTime = float.MinValue;
    private float _keepTime = 0.5f;
    public string CaptionText { get; private set; }

    public float LeftTime => _appearTime + _keepTime - Time.time;

    public void SetCaption(string caption, float time, Color color)
    {
        enabled = true;
        _appearTime = Time.time;
        CaptionText = caption;
        Text.text = caption;
        _keepTime = time;
        Border.color = color;
        Text.color = color;
    }

    public void Update()
    {
        var borderColor = Border.color;
        borderColor.a = Mathf.Clamp01(1 - ((Time.time - _appearTime) / 0.45f));
        Border.color = borderColor;
        Text.alpha = Mathf.Clamp01((_keepTime - (Time.time - _appearTime)) / 0.25f);

        if (Time.time - _appearTime > _keepTime)
        {
            CaptionText = null;
            enabled = false;
        }
    }
}
