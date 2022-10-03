using System;
using System.Collections;
using UnityEngine;

internal class AudioEmitterInternal : MonoBehaviour
{
    private Transform _attachedTransform;
    private bool _nonTracked;
    private AudioEvent _event;

    internal AudioSource AudioSource { get; private set; }

    internal int Id { get; private set; }
    internal Action<AudioEmitterInternal> OnEmitterStop;

    private IEnumerator FadeOutCoroutine(float time)
    {
        var startVolume = AudioSource.volume;

        while (AudioSource.volume > 0)
        {
            AudioSource.volume -= startVolume * Time.unscaledDeltaTime / time;
            yield return null;
        }

        AudioSource.Stop();
        AudioSource.volume = startVolume;
    }

    private IEnumerator FadeInCoroutine(float time)
    {
        var endVolume = _event.Volume;
        AudioSource.volume = 0;
        AudioSource.Play();

        while (AudioSource.volume < endVolume)
        {
            AudioSource.volume += endVolume * Time.unscaledDeltaTime / time;
            if (AudioSource.volume > endVolume)
                AudioSource.volume = endVolume;
            yield return null;
        }

        AudioSource.volume = endVolume;
    }

    internal void Enable(int id)
    {
        Id = id;
        gameObject.SetActive(true);
    }

    internal void MoveTo(Transform attachedTransform)
    {
        transform.position = attachedTransform.position;
        _attachedTransform = attachedTransform;
        _nonTracked = false;
    }

    internal void MoveTo(Vector3 position)
    {
        transform.position = position;
        _attachedTransform = null;
        _nonTracked = true;
    }

    internal void SetEvent(AudioEvent ev)
    {
        _event = ev;
        if (_event != null)
            _event.TransferToAudioSource(AudioSource);
        else
            AudioSource.clip = null;
    }

    internal void FadeOut(float time)
    {
        StopAllCoroutines();
        if (time == 0.0f)
            AudioSource.Stop();
        else
            StartCoroutine(FadeOutCoroutine(time));
    }

    internal void FadeIn(float time)
    {
        StopAllCoroutines();
        if (time == 0.0f)
            AudioSource.Play();
        else
            StartCoroutine(FadeInCoroutine(time));
    }

    internal void Play()
    {
        AudioSource.Play();
    }


    internal void PlayDelayed(float delay)
    {
        AudioSource.PlayDelayed(delay);
    }


    internal void PlayScheduled(double dspTime)
    {
        AudioSource.PlayScheduled(dspTime);
    }

    protected void LateUpdate()
    {
        if (Finished)
        {
            OnEmitterStop?.Invoke(this);
            Revoke();
            return;
        }

        if (_nonTracked) return;

        if (!_event.DoNotTrackSourceMovement)
        {
            if (_attachedTransform != null && _attachedTransform.hasChanged)
            {
                transform.position = _attachedTransform.position;
            }
        }

        // If we're a looping sound and our parent has died, release the loop.
        if (AudioSource.loop)
        {
            if (_attachedTransform == null && !_event.KeepLoopingWhenSourceDies)
            {
                AudioSource.loop = false;
            }
        }

        // If attached object is dead and the emitter is flagged to die if so, stop playing
        if (_event.StopWhenSourceDies && _attachedTransform == null)
        {
            AudioSource.Stop();
        }
    }

    internal void Stop()
    {
        AudioSource.Stop();
        OnEmitterStop?.Invoke(this);
        Revoke();
    }

    private void Revoke()
    {
        StopAllCoroutines();
        AudioSource.clip = null;
        gameObject.SetActive(false);
        Id = -1;
    }

    private bool Finished
    {
        get
        {
            return Id >= 0 && !AudioSource.isPlaying;
        }
    }

    internal static AudioEmitterInternal Create(Transform parent)
    {
        var go = new GameObject("AudioEmitter");
        go.SetActive(false);
        var emitter = go.AddComponent<AudioEmitterInternal>();
        emitter.AudioSource = go.AddComponent<AudioSource>();
        emitter.AudioSource.playOnAwake = false;
        go.transform.parent = parent;
        return emitter;
    }
}
