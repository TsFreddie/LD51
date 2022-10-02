using System;
using UnityEngine;

public struct InputState
{
    public int Move;
    public bool Jump;
}

public struct FrameInput
{
    public InputState LastFrame;
    public InputState CurrentFrame;

    public int Move => CurrentFrame.Move;
    public bool JumpDown => CurrentFrame.Jump && !LastFrame.Jump;
    public bool JumpUp => !CurrentFrame.Jump && LastFrame.Jump;
    public bool Jump => CurrentFrame.Jump;
}

public class GameManager : MonoBehaviour
{
    public FrameInput[] InputRecording;

    public static GameManager Instance { get; private set; }

    public InputState LastInput { get; private set; }
    public InputState CurrentInput { get; private set; }

    public Action OnFixedUpdate;

    public Action OnFixedUpdateSpikes;

    public bool Died = false;

    public void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        var frames = Mathf.FloorToInt(10.0f / Time.fixedDeltaTime);
        InputRecording = new FrameInput[frames];
        Debug.Log($"Game Manager Initialized with {frames} frames of input");
    }

    public void FixedUpdate()
    {
        LastInput = CurrentInput;
        CurrentInput = new InputState
        {
            Move = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0),
            Jump = Input.GetKey(KeyCode.Space),
        };

        OnFixedUpdate?.Invoke();

        OnFixedUpdateSpikes?.Invoke();
    }

    public FrameInput FetchInput()
    {
        return new FrameInput()
        {
            LastFrame = LastInput,
            CurrentFrame = CurrentInput
        };
    }

    public void IsDied(bool died)
    {
        Died = died;
    }
}
