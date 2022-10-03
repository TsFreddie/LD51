using System;

public readonly struct AudioEmitter
{
    private readonly AudioEmitterInternal _internal;
    private readonly int _id;

    internal AudioEmitter(AudioEmitterInternal internalEmitter)
    {
        _internal = internalEmitter;
        _id = internalEmitter.Id;
    }

    public bool IsValid()
    {
        return _internal != null && _internal.Id == _id;
    }

    /// <summary>
    /// 停止音频。该发生器会被释放，不能再次播放，需要再次播放请直接使用 FRAudioManager
    /// </summary>
    public void Stop()
    {
        if (!IsValid()) throw new InvalidOperationException("Emitter is invalid or has died.");
        _internal.Stop();
    }
    /// <summary>
    /// 淡出音频。淡出后会被释放，不能再次播放，需要再次播放请直接使用 FRAudioManager
    /// </summary>
    public void FadeOut(float time)
    {
        if (!IsValid()) throw new InvalidOperationException("Emitter is invalid or has died.");
        _internal.FadeOut(time);
    }
}
