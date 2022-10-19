using System;
using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public GameObject ActionBarPrefab;
    public Sprite LeftSprite;
    public Sprite RightSprite;
    public Sprite ActionSprite;

    public int PixelWidth;

    private Stack<ActionBar> _pooledObjects;
    private List<ActionBar> _activeObjects;
    private HashSet<ActionBar> _funObjects;

    public void Awake()
    {
        _activeObjects = new List<ActionBar>();
        _pooledObjects = new Stack<ActionBar>();
    }

    public ActionBar AllocateActionBar()
    {
        if (_pooledObjects.Count == 0)
        {
            return Instantiate(ActionBarPrefab, transform).GetComponent<ActionBar>();
        }

        var bar = _pooledObjects.Pop();
        bar.gameObject.SetActive(true);
        bar.Broke = false;
        return bar;
    }

    public void ReleaseActionBar(ActionBar bar)
    {
        bar.gameObject.SetActive(false);
        _pooledObjects.Push(bar);
    }

    public void UpdateTrack(InputState[] states, int cutOffIndex = -1)
    {
        if (_activeObjects == null) return;

        foreach (var obj in _activeObjects)
        {
            ReleaseActionBar(obj);
        }
        _activeObjects.Clear();

        ActionBar lastMoveBar = null;
        ActionBar lastJumpBar = null;

        if (cutOffIndex == -1)
            cutOffIndex = states.Length;

        for (var i = 0; i < cutOffIndex + 1; i++)
        {
            var input = i < cutOffIndex ? states[i] : default;
            var lastInput = i > 0 ? states[i - 1] : default;

            // Check move
            if (input.Move != lastInput.Move)
            {
                if (lastMoveBar != null)
                {
                    lastMoveBar.EndTime = (float)i / states.Length;
                }

                if (input.Move != 0)
                {
                    var bar = AllocateActionBar();
                    _activeObjects.Add(bar);
                    bar.Lane = 5;
                    bar.SetIcon(input.Move > 0 ? RightSprite : LeftSprite);
                    bar.StartTime = (float)i / states.Length;
                    bar.MinSize = 0;
                    bar.PixelSize = 1.0f / PixelWidth;
                    bar.transform.SetAsLastSibling();
                    lastMoveBar = bar;
                }
                else
                {
                    lastMoveBar = null;
                }
            }

            // Check jump
            if (input.Jump != lastInput.Jump)
            {
                if (lastJumpBar != null)
                {
                    lastJumpBar.EndTime = (float)i / states.Length;
                }

                if (input.Jump)
                {
                    var bar = AllocateActionBar();
                    _activeObjects.Add(bar);
                    bar.SetIcon(ActionSprite);
                    bar.Lane = 12;
                    bar.StartTime = (float)i / states.Length;
                    bar.MinSize = 7.0f / PixelWidth;
                    bar.PixelSize = 1.0f / PixelWidth;
                    bar.transform.SetAsLastSibling();
                    lastJumpBar = bar;
                }
                else
                {
                    lastJumpBar = null;
                }
            }
        }
    }

    public void SpawnDropTrack(InputState[] states, int startIndex = 0, int cutOffIndex = -1)
    {
        ActionBar lastMoveBar = null;
        ActionBar lastJumpBar = null;

        if (cutOffIndex == -1)
            cutOffIndex = states.Length;

        for (var i = startIndex; i < cutOffIndex + 1; i++)
        {
            var input = i < cutOffIndex ? states[i] : default;
            var lastInput = i > startIndex ? states[i - 1] : default;

            // Check move
            if (input.Move != lastInput.Move)
            {
                if (lastMoveBar != null)
                {
                    lastMoveBar.EndTime = (float)i / states.Length;
                    var bar = lastMoveBar;
                    bar.Break(() => ReleaseActionBar(bar));
                }

                if (input.Move != 0)
                {
                    var bar = AllocateActionBar();
                    bar.SetIcon(input.Move > 0 ? RightSprite : LeftSprite);
                    bar.Lane = 5;
                    bar.StartTime = (float)i / states.Length;
                    bar.MinSize = 0;
                    bar.PixelSize = 1.0f / PixelWidth;
                    lastMoveBar = bar;
                }
                else
                {
                    lastMoveBar = null;
                }
            }

            // Check jump
            if (input.Jump != lastInput.Jump)
            {
                if (lastJumpBar != null)
                {
                    lastJumpBar.EndTime = (float)i / states.Length;
                    var bar = lastJumpBar;
                    bar.Break(() => ReleaseActionBar(bar));
                }

                if (input.Jump)
                {
                    var bar = AllocateActionBar();
                    bar.SetIcon(ActionSprite);
                    bar.Lane = 12;
                    bar.StartTime = (float)i / states.Length;
                    bar.MinSize = 7.0f / PixelWidth;
                    bar.PixelSize = 1.0f / PixelWidth;
                    lastJumpBar = bar;
                }
                else
                {
                    lastJumpBar = null;
                }
            }
        }
    }
}
