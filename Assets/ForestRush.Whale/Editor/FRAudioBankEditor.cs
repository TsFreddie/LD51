using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;

namespace ForestRush.Whale.Editor
{
    [CustomEditor(typeof(FRAudioBank))]
    public class FRAudioBankEditor : UnityEditor.Editor
    {
        private const float FieldHeight = 20.0f;
        private const float FieldMarginY = 5.0f;
        private const float FieldMarginX = 5.0f;
        
        
        private FRAudioBank _bank;
        private ReorderableList _reorderableList;

        private void OnEnable()
        {
            _bank = (FRAudioBank)target;
            if (_bank.AudioEvents == null)
            {
                _bank.AudioEvents = new List<FRAudioEvent>();
            }

            _reorderableList = new ReorderableList(_bank.AudioEvents, typeof(FRAudioEvent), true, true, true, true);
            _reorderableList.drawHeaderCallback = (rect) =>
            {
                EditorGUI.LabelField(rect, new GUIContent("音频库"));
            };
            _reorderableList.drawElementCallback = DrawElementCallback;
            _reorderableList.elementHeightCallback = (_) => FieldHeight * 3.0f + FieldMarginY * 2.0f;
            _reorderableList.onSelectCallback = (reorderableList) =>
            {
                if (reorderableList.index >= 0 && reorderableList.index < reorderableList.count)
                {
                    if (reorderableList.list[reorderableList.index] is FRAudioEvent audioEvent)
                    {
                        SetSelectedAudioEvent(audioEvent);
                    }
                }
            };
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var audioEvent = _reorderableList.list[index] as FRAudioEvent;
            if (audioEvent == null) return;

            var labelRect = rect;
            labelRect.width = 50.0f;
            labelRect.height = FieldHeight;
            var fieldRect = rect;
            fieldRect.x = rect.x + labelRect.width + FieldMarginX;
            fieldRect.width = rect.width - labelRect.width - FieldMarginX;
            fieldRect.height = FieldHeight;
            EditorGUI.LabelField(labelRect, new GUIContent("键值"));
            audioEvent.EventName = EditorGUI.TextField(fieldRect, audioEvent.EventName);

            labelRect.y += FieldHeight + FieldMarginY;
            fieldRect.y += FieldHeight + FieldMarginY;
            EditorGUI.LabelField(labelRect, new GUIContent("音频"));
            audioEvent.AudioClip = EditorGUI.ObjectField(fieldRect, audioEvent.AudioClip, typeof(AudioClip), false) as AudioClip;

            labelRect.width = 50.0f;
            fieldRect.x = rect.x + labelRect.width + FieldMarginX;
            fieldRect.width = 50.0f;
            labelRect.y += FieldHeight + FieldMarginY;
            fieldRect.y += FieldHeight + FieldMarginY;
            EditorGUI.LabelField(labelRect, new GUIContent("权值"));
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

        private void ShowDetailedEditor(FRAudioEvent audioEvent)
        {
            FRAudioBankDetailEditor.ShowWindow(_bank, audioEvent);
        }

        private void SetSelectedAudioEvent(FRAudioEvent audioEvent)
        {
            if (FRAudioBankDetailEditor.Instance != null)
            {
                FRAudioBankDetailEditor.Instance.SetAudioEvent(_bank, audioEvent);
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

        private void DrawAudioEventOptionsMask(Rect rect, FRAudioEvent frAudioEvent)
        {
            var names = new string[]
            {
                "循环",
                "所属物件消失后立刻停止播放",
                "所属物件消失后继续循环",
                "不跟踪所属物件"
            };

            var mask = 0;
            if (frAudioEvent.Loop) mask |= 1 << 0;
            if (frAudioEvent.StopWhenSourceDies) mask |= 1 << 1;
            if (frAudioEvent.KeepLoopingWhenSourceDies) mask |= 1 << 2;
            if (frAudioEvent.DoNotTrackSourceMovement) mask |= 1 << 3;
            if ((mask & 0xF) == 0xF) mask = ~0;

            mask = EditorGUI.MaskField(rect, mask, names);

            frAudioEvent.Loop = (mask & (1 << 0)) > 0;
            frAudioEvent.StopWhenSourceDies = (mask & 1 << 1) > 0;
            frAudioEvent.KeepLoopingWhenSourceDies = (mask & 1 << 2) > 0;
            frAudioEvent.DoNotTrackSourceMovement = (mask & 1 << 3) > 0;
        }
    }
}
