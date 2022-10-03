using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace ForestRush.Whale
{
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

        public FRAudioEmitter FadeInAt(Transform target, float time) => FRAudioManager.Instance.FadeInAt(this, target, time);
        public FRAudioEmitter FadeInAt(Vector3 target, float time) => FRAudioManager.Instance.FadeInAt(this, target, time);
        public FRAudioEmitter PlayAt(Transform target) => FRAudioManager.Instance.PlayAt(this, target);
        public FRAudioEmitter PlayAt(Vector3 target) => FRAudioManager.Instance.PlayAt(this, target);
        public FRAudioEmitter PlayDelayedAt(Transform target, float delay) => FRAudioManager.Instance.PlayDelayedAt(this, target, delay);
        public FRAudioEmitter PlayDelayedAt(Vector3 target, float delay) => FRAudioManager.Instance.PlayDelayedAt(this, target, delay);
        public FRAudioEmitter PlayScheduledAt(Transform target, double dspTime) => FRAudioManager.Instance.PlayScheduledAt(this, target, dspTime);
        public FRAudioEmitter PlayScheduledAt(Vector3 target, double dspTime) => FRAudioManager.Instance.PlayScheduledAt(this, target, dspTime);
        public FRAudioEmitter FadeIn(float time) => FRAudioManager.Instance.FadeIn(this, time);
        public FRAudioEmitter Play() => FRAudioManager.Instance.Play(this);
        public FRAudioEmitter PlayDelayed(float delay) => FRAudioManager.Instance.PlayDelayed(this, delay);
        public FRAudioEmitter PlayScheduled(double dspTime) => FRAudioManager.Instance.PlayScheduled(this, dspTime);
    }
}
