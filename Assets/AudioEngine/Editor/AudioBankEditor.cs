using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

[CustomEditor(typeof(AudioBank))]
public class AudioBankEditor : UnityEditor.Editor
{
    private const float FieldHeight = 20.0f;
    private const float FieldMarginY = 5.0f;
    private const float FieldMarginX = 5.0f;


    private AudioBank _bank;
    private ReorderableList _reorderableList;

    private void OnEnable()
    {
        _bank = (AudioBank)target;
        if (_bank.AudioEvents == null)
        {
            _bank.AudioEvents = new List<AudioEvent>();
        }

        _reorderableList = new ReorderableList(_bank.AudioEvents, typeof(AudioEvent), true, true, true, true);
        _reorderableList.drawHeaderCallback = (rect) =>
        {
            EditorGUI.LabelField(rect, new GUIContent("Audio Bank"));
        };
        _reorderableList.drawElementCallback = DrawElementCallback;
        _reorderableList.elementHeightCallback = (_) => FieldHeight * 3.0f + FieldMarginY * 2.0f;
        _reorderableList.onSelectCallback = (reorderableList) =>
        {
            if (reorderableList.index >= 0 && reorderableList.index < reorderableList.count)
            {
                if (reorderableList.list[reorderableList.index] is AudioEvent audioEvent)
                {
                    SetSelectedAudioEvent(audioEvent);
                }
            }
        };
    }

    private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
    {
        var audioEvent = _reorderableList.list[index] as AudioEvent;
        if (audioEvent == null) return;

        var labelRect = rect;
        labelRect.width = 50.0f;
        labelRect.height = FieldHeight;
        var fieldRect = rect;
        fieldRect.x = rect.x + labelRect.width + FieldMarginX;
        fieldRect.width = rect.width - labelRect.width - FieldMarginX;
        fieldRect.height = FieldHeight;
        EditorGUI.LabelField(labelRect, new GUIContent("Key"));
        audioEvent.EventName = EditorGUI.TextField(fieldRect, audioEvent.EventName);

        labelRect.y += FieldHeight + FieldMarginY;
        fieldRect.y += FieldHeight + FieldMarginY;
        EditorGUI.LabelField(labelRect, new GUIContent("Audio"));
        audioEvent.AudioClip = EditorGUI.ObjectField(fieldRect, audioEvent.AudioClip, typeof(AudioClip), false) as AudioClip;

        labelRect.width = 50.0f;
        fieldRect.x = rect.x + labelRect.width + FieldMarginX;
        fieldRect.width = 50.0f;
        labelRect.y += FieldHeight + FieldMarginY;
        fieldRect.y += FieldHeight + FieldMarginY;
        EditorGUI.LabelField(labelRect, new GUIContent("Wgt"));
        audioEvent.RandomWeight = EditorGUI.FloatField(fieldRect, audioEvent.RandomWeight);
        fieldRect.x = fieldRect.x + fieldRect.width + FieldMarginX;
        fieldRect.width = rect.width - fieldRect.width - labelRect.width - FieldMarginX * 3.0f - 50.0f;
        DrawAudioEventOptionsMask(fieldRect, audioEvent);

        var buttonRect = rect;
        buttonRect.x = rect.x + rect.width - 50.0f;
        buttonRect.y = fieldRect.y;
        buttonRect.width = 50.0f;
        buttonRect.height = FieldHeight;

        if (GUI.Button(buttonRect, new GUIContent("...")))
        {
            _reorderableList.index = index;
            ShowDetailedEditor(audioEvent);
        }
    }

    private void OnDisable()
    {
        SetSelectedAudioEvent(null);
    }

    private void ShowDetailedEditor(AudioEvent audioEvent)
    {
        AudioBankDetailEditor.ShowWindow(_bank, audioEvent);
    }

    private void SetSelectedAudioEvent(AudioEvent audioEvent)
    {
        if (AudioBankDetailEditor.Instance != null)
        {
            AudioBankDetailEditor.Instance.SetAudioEvent(_bank, audioEvent);
        }
    }

    public override void OnInspectorGUI()
    {
        if (_reorderableList == null) return;
        EditorGUI.BeginChangeCheck();
        _reorderableList.DoLayoutList();
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(_bank);
        }
    }

    private void DrawAudioEventOptionsMask(Rect rect, AudioEvent audioEvent)
    {
        var names = new string[]
        {
            "Loop",
            "Stop when destroyed",
            "Keep looping when destroyed",
            "Don't follow transform",
            "Allow multiple in same frame"
        };

        var mask = 0;
        if (audioEvent.Loop) mask |= 1 << 0;
        if (audioEvent.StopWhenSourceDies) mask |= 1 << 1;
        if (audioEvent.KeepLoopingWhenSourceDies) mask |= 1 << 2;
        if (audioEvent.DoNotTrackSourceMovement) mask |= 1 << 3;
        if (audioEvent.AllowMultipleInSingleFrame) mask |= 1 << 4;
        if ((mask & 0xF) == 0xF) mask = ~0;

        mask = EditorGUI.MaskField(rect, mask, names);

        audioEvent.Loop = (mask & (1 << 0)) > 0;
        audioEvent.StopWhenSourceDies = (mask & 1 << 1) > 0;
        audioEvent.KeepLoopingWhenSourceDies = (mask & 1 << 2) > 0;
        audioEvent.DoNotTrackSourceMovement = (mask & 1 << 3) > 0;
        audioEvent.AllowMultipleInSingleFrame = (mask & 1 << 4) > 0;
    }
}
