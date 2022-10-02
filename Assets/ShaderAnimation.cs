using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShaderAnimation : MonoBehaviour
{
    public static readonly int _StateFromColor = Shader.PropertyToID("_StateFromColor");
    public static readonly int _StateToColor = Shader.PropertyToID("_StateToColor");
    public static readonly int _Progress = Shader.PropertyToID("_Progress");

    public Color FromColor;
    public Color ToColor;
    public float Progress = 0;

    public void Update()
    {
        Shader.SetGlobalColor(_StateFromColor, FromColor);
        Shader.SetGlobalColor(_StateToColor, ToColor);
        Shader.SetGlobalFloat(_Progress, Progress);
    }
}
