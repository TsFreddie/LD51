using ForestRush.Whale;
using UnityEngine;
using UnityEditor;

public class FRAudioBankDetailEditor : EditorWindow
{
    public static FRAudioBankDetailEditor Instance;

    public static void ShowWindow(FRAudioBank bank, FRAudioEvent audioEvent)
    {
        if (Instance == null)
        {
            Instance = CreateWindow<FRAudioBankDetailEditor>("音频事件设置");
        }

        Instance.SetAudioEvent(bank, audioEvent);
        Instance.ShowUtility();
    }

    private FRAudioEvent _audioEvent;
    private FRAudioBank _bank;

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

    public void SetAudioEvent(FRAudioBank bank, FRAudioEvent audioEvent)
    {
        _audioEvent = audioEvent;
        _bank = bank;

        if (_audioEvent != null)
            AudioEventToProxy(_audioEvent);
        Repaint();
    }

    private void AudioEventToProxy(FRAudioEvent audioEvent)
    {
        if (_audioSourceProxyGo == null)
        {
            _audioSourceProxyGo = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            _audioSourceProxy = _audioSourceProxyGo.AddComponent<AudioSource>();
        }

        audioEvent.TransferToAudioSource(_audioSourceProxy);
    }

    private void ProxyToAudioEvent(FRAudioEvent audioEvent)
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
            EditorGUILayout.LabelField("选择一个音频库中的事件进行编辑");
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
