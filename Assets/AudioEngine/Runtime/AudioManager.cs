using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    private static AudioManager s_instance;
    private static bool s_initialized;
    public static AudioManager Instance
    {
        get
        {
            if (!s_initialized && s_instance == null)
            {
                var go = new GameObject("AudioManager");
                s_instance = go.AddComponent<AudioManager>();
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
        private List<AudioEvent> _events = new List<AudioEvent>();
        private List<float> _weightCache = new List<float>();
        private float _totalWeight = 0;

        public void AddEvent(AudioEvent ev)
        {
            _events.Add(ev);
            _totalWeight += ev.RandomWeight;
            _weightCache.Add(_totalWeight);

        }

        public void RemoveEvent(AudioEvent ev)
        {
            var index = _events.IndexOf(ev);
            if (index < 0)
                throw new Exception("Event not found");

            _events.RemoveAt(index);
            for (var i = index; i < _weightCache.Count; i++)
                _weightCache[i] -= ev.RandomWeight;
            _totalWeight -= ev.RandomWeight;
        }

        public AudioEvent GetEvent()
        {
            var rnd = UnityEngine.Random.value * _totalWeight;
            var index = _weightCache.BinarySearch(rnd);
            if (index < 0)
            {
                index = ~index;
                if (index >= _weightCache.Count)
                    index = 0;
            }
            return _events[index];
        }
    }

    [SerializeField]
    private AudioBank[] _persistentBanks;

    private HashSet<AudioEmitterInternal> _activeEmitters = new HashSet<AudioEmitterInternal>();
    private Queue<AudioEmitterInternal> _pooledEmitters = new Queue<AudioEmitterInternal>();
    private HashSet<AudioBank> _loadedAudioBanks = new HashSet<AudioBank>();
    private Dictionary<string, AudioEventGroup> _audioEventGroups = new Dictionary<string, AudioEventGroup>();
    private HashSet<AudioEvent> _frameEvents = new HashSet<AudioEvent>();

    private int _audioId;

    private int GetAudioId()
    {
        _audioId++;
        if (_audioId < 0)
            _audioId = 0;
        return _audioId;
    }

    private AudioEmitterInternal GetAudioEmitter()
    {
        if (_pooledEmitters.Count > 0)
        {
            var pooled = _pooledEmitters.Dequeue();
            pooled.Enable(GetAudioId());
            _activeEmitters.Add(pooled);
            return pooled;
        }
        var emitter = AudioEmitterInternal.Create(transform);
        emitter.Enable(GetAudioId());
        _activeEmitters.Add(emitter);
        emitter.OnEmitterStop += OnEmitterStop;
        return emitter;
    }

    private void OnEmitterStop(AudioEmitterInternal emitter)
    {
        if (_activeEmitters.Remove(emitter))
            _pooledEmitters.Enqueue(emitter);
        else
            throw new Exception("Emitter not pooled by this manager");
    }

    public void LoadAudioBank(AudioBank bank)
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

    public void UnloadAudioBank(AudioBank bank)
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

    private AudioEvent SearchEvent(string eventName)
    {
        foreach (var subName in SplitNames(eventName))
        {
            if (_audioEventGroups.TryGetValue(subName, out var group))
                return group.GetEvent();
        }

        return null;
    }

    private AudioEvent SearchEvent(AudioName eventName)
    {
        foreach (var subName in eventName.Names)
        {
            if (_audioEventGroups.TryGetValue(subName, out var group))
                return group.GetEvent();
        }

        return null;
    }

    private AudioEmitter PlayAt(AudioEvent ev, Transform target)
    {
        if (ev == null) return default;

        if (!ev.AllowMultipleInSingleFrame)
        {
            if (_frameEvents.Contains(ev))
                return default;
            _frameEvents.Add(ev);
        }

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.Play();
        return new AudioEmitter(emitter);
    }

    private AudioEmitter PlayAt(AudioEvent ev, Vector3 target)
    {
        if (ev == null) return default;
        if (!ev.AllowMultipleInSingleFrame)
        {
            if (_frameEvents.Contains(ev))
                return default;
            _frameEvents.Add(ev);
        }

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.Play();
        return new AudioEmitter(emitter);
    }


    private AudioEmitter FadeInAt(AudioEvent ev, Transform target, float time)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.FadeIn(time);
        return new AudioEmitter(emitter);
    }

    private AudioEmitter FadeInAt(AudioEvent ev, Vector3 target, float time)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.FadeIn(time);
        return new AudioEmitter(emitter);
    }

    private AudioEmitter PlayDelayedAt(AudioEvent ev, Transform target, float delay)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.PlayDelayed(delay);
        return new AudioEmitter(emitter);
    }

    private AudioEmitter PlayDelayedAt(AudioEvent ev, Vector3 target, float delay)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.PlayDelayed(delay);
        return new AudioEmitter(emitter);
    }

    private AudioEmitter PlayScheduledAt(AudioEvent ev, Transform target, double dspTime)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.PlayScheduled(dspTime);
        return new AudioEmitter(emitter);
    }

    private AudioEmitter PlayScheduledAt(AudioEvent ev, Vector3 target, double dspTime)
    {
        if (ev == null) return default;

        var emitter = GetAudioEmitter();
        emitter.SetEvent(ev);
        emitter.MoveTo(target);
        emitter.PlayScheduled(dspTime);
        return new AudioEmitter(emitter);
    }

    public AudioEmitter FadeInAt(string eventName, Transform target, float time) => FadeInAt(SearchEvent(eventName), target, time);
    public AudioEmitter FadeInAt(AudioName eventName, Transform target, float time) => FadeInAt(SearchEvent(eventName), target, time);
    public AudioEmitter FadeInAt(string eventName, Vector3 target, float time) => FadeInAt(SearchEvent(eventName), target, time);
    public AudioEmitter FadeInAt(AudioName eventName, Vector3 target, float time) => FadeInAt(SearchEvent(eventName), target, time);
    public AudioEmitter PlayAt(string eventName, Transform target) => PlayAt(SearchEvent(eventName), target);
    public AudioEmitter PlayAt(AudioName eventName, Transform target) => PlayAt(SearchEvent(eventName), target);
    public AudioEmitter PlayAt(string eventName, Vector3 target) => PlayAt(SearchEvent(eventName), target);
    public AudioEmitter PlayAt(AudioName eventName, Vector3 target) => PlayAt(SearchEvent(eventName), target);
    public AudioEmitter PlayDelayedAt(string eventName, Transform target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
    public AudioEmitter PlayDelayedAt(AudioName eventName, Transform target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
    public AudioEmitter PlayDelayedAt(string eventName, Vector3 target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
    public AudioEmitter PlayDelayedAt(AudioName eventName, Vector3 target, float delay) => PlayDelayedAt(SearchEvent(eventName), target, delay);
    public AudioEmitter PlayScheduledAt(string eventName, Transform target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
    public AudioEmitter PlayScheduledAt(AudioName eventName, Transform target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
    public AudioEmitter PlayScheduledAt(string eventName, Vector3 target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);
    public AudioEmitter PlayScheduledAt(AudioName eventName, Vector3 target, double dspTime) => PlayScheduledAt(SearchEvent(eventName), target, dspTime);

    public AudioEmitter FadeIn(string eventName, float time) => FadeInAt(SearchEvent(eventName), ActiveListener.transform, time);
    public AudioEmitter FadeIn(AudioName eventName, float time) => FadeInAt(SearchEvent(eventName), ActiveListener.transform, time);
    public AudioEmitter Play(string eventName) => PlayAt(SearchEvent(eventName), ActiveListener.transform);
    public AudioEmitter Play(AudioName eventName) => PlayAt(SearchEvent(eventName), ActiveListener.transform);
    public AudioEmitter PlayDelayed(string eventName, float delay) => PlayDelayedAt(SearchEvent(eventName), ActiveListener.transform, delay);
    public AudioEmitter PlayDelayed(AudioName eventName, float delay) => PlayDelayedAt(SearchEvent(eventName), ActiveListener.transform, delay);
    public AudioEmitter PlayScheduled(string eventName, double dspTime) => PlayScheduledAt(SearchEvent(eventName), ActiveListener.transform, dspTime);
    public AudioEmitter PlayScheduled(AudioName eventName, double dspTime) => PlayScheduledAt(SearchEvent(eventName), ActiveListener.transform, dspTime);

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

        _frameEvents.Clear();
    }

    protected void LateUpdate()
    {
        _frameEvents.Clear();
    }
}
