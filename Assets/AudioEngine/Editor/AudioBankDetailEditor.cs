using UnityEngine;
using UnityEditor;

public class AudioBankDetailEditor : EditorWindow
{
    public static AudioBankDetailEditor Instance;

    public static void ShowWindow(AudioBank bank, AudioEvent audioEvent)
    {
        if (Instance == null)
        {
            Instance = CreateWindow<AudioBankDetailEditor>("Audio Detail");
        }

        Instance.SetAudioEvent(bank, audioEvent);
        Instance.ShowUtility();
    }

    private AudioEvent _audioEvent;
    private AudioBank _bank;

    private GameObject _audioSourceProxyGo;
    private AudioSource _audioSourceProxy;
    private Vector2 _scrollPos;

    protected void OnEnable()
    {
        _audioSourceProxyGo = new GameObject("AudioSourceProxy") { hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy };
        _audioSourceProxy = _audioSourceProxyGo.AddComponent<AudioSource>();
        Instance = this;
    }

    protected void OnDisable()
    {
        DestroyImmediate(_audioSourceProxyGo);
        Instance = null;
    }

    public void SetAudioEvent(AudioBank bank, AudioEvent audioEvent)
    {
        _audioEvent = audioEvent;
        _bank = bank;

        if (_audioEvent != null)
            AudioEventToProxy(_audioEvent);
        Repaint();
    }

    private void AudioEventToProxy(AudioEvent audioEvent)
    {
        if (_audioSourceProxyGo == null)
        {
            _audioSourceProxyGo = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            _audioSourceProxy = _audioSourceProxyGo.AddComponent<AudioSource>();
        }

        audioEvent.TransferToAudioSource(_audioSourceProxy);
    }

    private void ProxyToAudioEvent(AudioEvent audioEvent)
    {
        if (_audioSourceProxyGo == null)
        {
            _audioSourceProxyGo = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            _audioSourceProxy = _audioSourceProxyGo.AddComponent<AudioSource>();
        }

        audioEvent.TransferFromAudioSource(_audioSourceProxy);
    }

    protected void OnGUI()
    {
        if (_audioEvent == null || _bank == null)
        {
            EditorGUILayout.LabelField("Please select an audio event to edit.");
            return;
        }

        if (Instance != this)
        {
            Close();
            DestroyImmediate(this);
            return;
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, new GUIStyle() { padding = new RectOffset(10, 10, 10, 10) });
        EditorGUI.BeginChangeCheck();
        _audioEvent.EventName = EditorGUILayout.TextField(new GUIContent("Key"), _audioEvent.EventName);
        _audioEvent.RandomWeight = EditorGUILayout.FloatField(new GUIContent("Weight"), _audioEvent.RandomWeight);
        var oldRandomPitchMin = _audioEvent.RandomPitchMin;
        var oldRandomPitchMax = _audioEvent.RandomPitchMax;
        EditorGUILayout.MinMaxSlider(new GUIContent("Random Pitch"), ref _audioEvent.RandomPitchMin, ref _audioEvent.RandomPitchMax, -3.0f, 3.0f);
        if (!oldRandomPitchMax.Equals(_audioEvent.RandomPitchMax) || !oldRandomPitchMin.Equals(_audioEvent.RandomPitchMin))
        {
            GUI.FocusControl(null);
        }
        EditorGUILayout.BeginHorizontal();
        _audioEvent.RandomPitchMin = EditorGUILayout.FloatField(new GUIContent("Pitch Min"), _audioEvent.RandomPitchMin);
        _audioEvent.RandomPitchMax = EditorGUILayout.FloatField(new GUIContent("Pitch Max"), _audioEvent.RandomPitchMax);
        if (!oldRandomPitchMax.Equals(_audioEvent.RandomPitchMax) || !oldRandomPitchMin.Equals(_audioEvent.RandomPitchMin))
        {
            _audioEvent.RandomPitchMax = Mathf.Clamp(_audioEvent.RandomPitchMax, -3.0f, 3.0f);
            _audioEvent.RandomPitchMin = Mathf.Clamp(_audioEvent.RandomPitchMin, -3.0f, 3.0f);
            _audioSourceProxy.pitch = (_audioEvent.RandomPitchMax + _audioEvent.RandomPitchMin) / 2.0f;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
        _audioEvent.StopWhenSourceDies = EditorGUILayout.Toggle(new GUIContent("Stop When Source Dies"), _audioEvent.StopWhenSourceDies);
        _audioEvent.KeepLoopingWhenSourceDies = EditorGUILayout.Toggle(new GUIContent("Loop Forever"), _audioEvent.KeepLoopingWhenSourceDies);
        _audioEvent.DoNotTrackSourceMovement = EditorGUILayout.Toggle(new GUIContent("Detached"), _audioEvent.DoNotTrackSourceMovement);
        _audioEvent.AllowMultipleInSingleFrame = EditorGUILayout.Toggle(new GUIContent("Allow Multiple In Same Frame"), _audioEvent.AllowMultipleInSingleFrame);
        EditorGUILayout.Separator();
        var oldPitch = _audioSourceProxy.pitch;
        var builtinEditor = Editor.CreateEditor(_audioSourceProxy);
        builtinEditor.OnInspectorGUI();
        DestroyImmediate(builtinEditor);
        if (!oldPitch.Equals(_audioSourceProxy.pitch))
        {
            var pitch = _audioSourceProxy.pitch;
            _audioEvent.RandomPitchMin = pitch;
            _audioEvent.RandomPitchMax = pitch;
        }
        if (EditorGUI.EndChangeCheck())
        {
            ProxyToAudioEvent(_audioEvent);
            EditorUtility.SetDirty(_bank);
        }

        EditorGUILayout.EndScrollView();
    }
}
