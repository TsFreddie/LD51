using System;
using UnityEngine;
using System.Collections.Generic;

namespace ForestRush.Whale
{
    /// <summary>
    /// 音频管理器
    /// </summary>
    public class FRAudioManager : MonoBehaviour
    {
        private static FRAudioManager s_instance;
        private static bool s_initialized;
        public static FRAudioManager Instance
        {
            get
            {
                if (!s_initialized && s_instance == null)
                {
                    var go = new GameObject("AudioManager");
                    s_instance = go.AddComponent<FRAudioManager>();
                    s_initialized = true;
                }
                return s_instance;
            }
        }

        private static AudioListener _activeAudioListener;
        public static AudioListener ActiveListener
        {
            get
            {
                if (_activeAudioListener != null && _activeAudioListener.isActiveAndEnabled)
                    return _activeAudioListener;

                var audioListeners = FindObjectsOfType<AudioListener>(false);
                _activeAudioListener = Array.Find(audioListeners, audioListener => audioListener.enabled);
                return _activeAudioListener;
            }
        }

        private class AudioEventGroup
        {
            private List<FRAudioEvent> _events = new List<FRAudioEvent>();
            private List<float> _weightCache = new List<float>();
            private float _totalWeight = 0;

            public void AddEvent(FRAudioEvent ev)
            {
                _events.Add(ev);
                _totalWeight += ev.RandomWeight;
                _weightCache.Add(_totalWeight);

            }

            public void RemoveEvent(FRAudioEvent ev)
            {
                var index = _events.IndexOf(ev);
                if (index < 0)
                    throw new Exception("Event not found");

                _events.RemoveAt(index);
                for (var i = index; i < _weightCache.Count; i++)
                    _weightCache[i] -= ev.RandomWeight;
                _totalWeight -= ev.RandomWeight;
            }

            public FRAudioEvent GetEvent()
            {
                var rnd = UnityEngine.Random.value * _totalWeight;
                var index = _weightCache.BinarySearch(rnd);
                if (index < 0)
                {
                    index = ~index;
                    if (index >= _weightCache.Count) // 其实不应该出现
                        index = 0;
                }
                return _events[index];
            }
        }

        [SerializeField]
        private FRAudioBank[] _persistentBanks;

        private HashSet<FRAudioEmitterInternal> _activeEmitters = new HashSet<FRAudioEmitterInternal>();
        private Queue<FRAudioEmitterInternal> _pooledEmitters = new Queue<FRAudioEmitterInternal>();
        private HashSet<FRAudioBank> _loadedAudioBanks = new HashSet<FRAudioBank>();
        private Dictionary<string, AudioEventGroup> _audioEventGroups = new Dictionary<string, AudioEventGroup>();

        private int _audioId;

        private int GetAudioId()
        {
            _audioId++;
            if (_audioId < 0)
                _audioId = 0;
            return _audioId;
        }

        private FRAudioEmitterInternal GetAudioEmitter()
        {
            if (_pooledEmitters.Count > 0)
            {
                var pooled = _pooledEmitters.Dequeue();
                pooled.Enable(GetAudioId());
                _activeEmitters.Add(pooled);
                return pooled;
            }
            var emitter = FRAudioEmitterInternal.Create(transform);
            emitter.Enable(GetAudioId());
            _activeEmitters.Add(emitter);
            emitter.OnEmitterStop += OnEmitterStop;
            return emitter;
        }

        private void OnEmitterStop(FRAudioEmitterInternal emitter)
        {
            if (_activeEmitters.Remove(emitter))
                _pooledEmitters.Enqueue(emitter);
            else
                throw new Exception("Emitter not pooled by this manager");
        }

        public void LoadAudioBank(FRAudioBank bank)
        {
            foreach (var ev in bank.AudioEvents)
            {
                foreach (var evName in SplitNames(ev.EventName))
                {
                    if (_audioEventGroups.TryGetValue(evName, out var group))
                    {
                        group.AddEvent(ev);
                    }
                    else
                    {
                        var newGroup = new AudioEventGroup();
                        newGroup.AddEvent(ev);
                        _audioEventGroups.Add(ev.EventName, newGroup);
                    }
                }
            }
        }

        public void UnloadAudioBank(FRAudioBank bank)
        {
            foreach (var ev in bank.AudioEvents)
            {
                foreach (var evName in SplitNames(ev.EventName))
                {
                    if (_audioEventGroups.TryGetValue(evName, out var group))
                    {
                        group.RemoveEvent(ev);
                    }
                }
            }
        }

        private static IEnumerable<string> SplitNames(string eventName)
        {
            yield return eventName;

            var lastIndex = -1;
            while (true)
            {
                lastIndex = eventName.IndexOf(":", lastIndex + 1, StringComparison.Ordinal);
                if (lastIndex < 0) yield break;
                var subName = eventName.Substring(lastIndex + 1);
                yield return subName;
            }
        }

        private FRAudioEvent SearchEvent(string eventName)
        {
            foreach (var subName in SplitNames(eventName))
            {
                if (_audioEventGroups.TryGetValue(subName, out var group))
                    return group.GetEvent();
            }

            return null;
        }

        private FRAudioEvent SearchEvent(AudioName eventName)
        {
            foreach (var subName in eventName.Names)
            {
                if (_audioEventGroups.TryGetValue(subName, out var group))
                    return group.GetEvent();
            }

            return null;
        }

        private FRAudioEmitter PlayAt(FRAudioEvent ev, Transform target)
        {
            if (ev == null) return default;
           
            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.Play();
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter PlayAt(FRAudioEvent ev, Vector3 target)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.Play();
            return new FRAudioEmitter(emitter);
        }


        private FRAudioEmitter FadeInAt(FRAudioEvent ev, Transform target, float time)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.FadeIn(time);
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter FadeInAt(FRAudioEvent ev, Vector3 target, float time)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.FadeIn(time);
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter PlayDelayedAt(FRAudioEvent ev, Transform target, float delay)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.PlayDelayed(delay);
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter PlayDelayedAt(FRAudioEvent ev, Vector3 target, float delay)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.PlayDelayed(delay);
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter PlayScheduledAt(FRAudioEvent ev, Transform target, double dspTime)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.PlayScheduled(dspTime);
            return new FRAudioEmitter(emitter);
        }

        private FRAudioEmitter PlayScheduledAt(FRAudioEvent ev, Vector3 target, double dspTime)
        {
            if (ev == null) return default;

            var emitter = GetAudioEmitter();
            emitter.SetEvent(ev);
            emitter.MoveTo(target);
            emitter.PlayScheduled(dspTime);
            return new FRAudioEmitter(emitter);
        }

        public FRAudioEmitter FadeInAt(string eventName, Transform target, float time) => FadeInAt(SearchEvent(eventName), target, time);
        public FRAudioEmitter FadeInAt(AudioName eventName, Transform target, float time) => FadeInAt(SearchEvent(eventName), target, time);
        public FRAudioEmitter FadeInAt(string eventName, Vector3 target, float time) => FadeInAt(SearchEvent(eventName), target, time);
        public FRAudioEmitter FadeInAt(AudioName eventName, Vector3 target, float time) => FadeInAt(SearchEvent(eventName), target, time);
        public FRAudioEmitter PlayAt(string eventName, Transform target) => PlayAt(SearchEvent(eventName), target);
        public FRAudioEmitter PlayAt(AudioName eventName, Transform target) => PlayAt(SearchEvent(eventName), target);
        public FRAudioEmitter PlayAt(string eventName, Vector3 target) => PlayAt(SearchEvent(eventName), target);
        public FRAudioEmitter PlayAt(AudioName eventName, Vector3 target) => PlayAt(SearchEvent(eventName), target);
        public FRAudioEmitter PlayDelayedAt(string eventName, Transform target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
        public FRAudioEmitter PlayDelayedAt(AudioName eventName, Transform target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
        public FRAudioEmitter PlayDelayedAt(string eventName, Vector3 target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
        public FRAudioEmitter PlayDelayedAt(AudioName eventName, Vector3 target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
        public FRAudioEmitter PlayScheduledAt(string eventName, Transform target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
        public FRAudioEmitter PlayScheduledAt(AudioName eventName, Transform target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
        public FRAudioEmitter PlayScheduledAt(string eventName, Vector3 target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
        public FRAudioEmitter PlayScheduledAt(AudioName eventName, Vector3 target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);

        public FRAudioEmitter FadeIn(string eventName, float time) => FadeInAt(SearchEvent(eventName), ActiveListener.transform, time);
        public FRAudioEmitter FadeIn(AudioName eventName, float time) => FadeInAt(SearchEvent(eventName), ActiveListener.transform, time);
        public FRAudioEmitter Play(string eventName) => PlayAt(SearchEvent(eventName), ActiveListener.transform);
        public FRAudioEmitter Play(AudioName eventName) => PlayAt(SearchEvent(eventName), ActiveListener.transform);
        public FRAudioEmitter PlayDelayed(string eventName, float delay) => PlayDelayedAt(SearchEvent(eventName), ActiveListener.transform, delay);
        public FRAudioEmitter PlayDelayed(AudioName eventName, float delay) => PlayDelayedAt(SearchEvent(eventName), ActiveListener.transform, delay);
        public FRAudioEmitter PlayScheduled(string eventName, double dspTime) => PlayScheduledAt(SearchEvent(eventName), ActiveListener.transform, dspTime);
        public FRAudioEmitter PlayScheduled(AudioName eventName, double dspTime) => PlayScheduledAt(SearchEvent(eventName), ActiveListener.transform, dspTime);

        protected void Awake()
        {
            if (s_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
            DontDestroyOnLoad(gameObject);
            if (_persistentBanks != null && _persistentBanks.Length > 0)
            {
                foreach (var bank in _persistentBanks)
                {
                    LoadAudioBank(bank);
                }
            }
        }
    }
}
