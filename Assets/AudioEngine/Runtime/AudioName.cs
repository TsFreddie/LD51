using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 缓存声音事件键值，避免使用过多的前缀时计算量太大
/// </summary>
public readonly struct AudioName
{
    private readonly string[] _names;
    public IReadOnlyList<string> Names => _names;

    public AudioName(string name)
    {
        var rentBuffer = ArrayPool<string>.Shared.Rent(16);
        var lastIndex = name.Length - 1;
        var count = 0;
        do
        {
            lastIndex = name.LastIndexOf(":", lastIndex - 1, StringComparison.Ordinal);
            if (lastIndex < -1) break;
            rentBuffer[count] = name.Substring(lastIndex + 1);
            count++;
        }
        while (lastIndex >= 0 || count >= rentBuffer.Length);

        _names = new string[count];

        for (var i = 0; i < count; i++)
        {
            _names[i] = rentBuffer[count - i - 1];
        }
        ArrayPool<string>.Shared.Return(rentBuffer);
    }

    public AudioEmitter FadeInAt(Transform target, float time) => AudioManager.Instance.FadeInAt(this, target, time);
    public AudioEmitter FadeInAt(Vector3 target, float time) => AudioManager.Instance.FadeInAt(this, target, time);
    public AudioEmitter PlayAt(Transform target) => AudioManager.Instance.PlayAt(this, target);
    public AudioEmitter PlayAt(Vector3 target) => AudioManager.Instance.PlayAt(this, target);
    public AudioEmitter PlayDelayedAt(Transform target, float delay) => AudioManager.Instance.PlayDelayedAt(this, target, delay);
    public AudioEmitter PlayDelayedAt(Vector3 target, float delay) => AudioManager.Instance.PlayDelayedAt(this, target, delay);
    public AudioEmitter PlayScheduledAt(Transform target, double dspTime) => AudioManager.Instance.PlayScheduledAt(this, target, dspTime);
    public AudioEmitter PlayScheduledAt(Vector3 target, double dspTime) => AudioManager.Instance.PlayScheduledAt(this, target, dspTime);
    public AudioEmitter FadeIn(float time) => AudioManager.Instance.FadeIn(this, time);
    public AudioEmitter Play() => AudioManager.Instance.Play(this);
    public AudioEmitter PlayDelayed(float delay) => AudioManager.Instance.PlayDelayed(this, delay);
    public AudioEmitter PlayScheduled(double dspTime) => AudioManager.Instance.PlayScheduled(this, dspTime);
}
