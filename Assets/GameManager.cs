using System;
using System.Threading.Tasks;
using UnityEngine;

public struct InputState
{
    public int Move;
    public bool Jump;

    public bool Active => Move != 0 || Jump;
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
    public enum GameState
    {
        Awaiting,
        Recording,
        Replaying,
        Inactive,
    }

    public Animator StateAnimator;
    public InputState[] InputRecording;

    public static GameManager Instance { get; private set; }

    // public InputState LastInput { get; private set; }
    public InputState CurrentInput { get; private set; }

    public int Frame { get; private set; }

    public Action OnReset;
    public Action OnFixedUpdate;

    public GameState State = GameState.Inactive;
    
    private static readonly int AnimAwaiting = Animator.StringToHash("Await");
    private static readonly int AnimRecording = Animator.StringToHash("Record");
    private static readonly int AnimReplaying = Animator.StringToHash("Replay");
    private static readonly int AnimInactive = Animator.StringToHash("Inactive");

    public async void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        var frames = Mathf.RoundToInt(10.0f / Time.fixedDeltaTime);
        InputRecording = new InputState[frames];
        Debug.Log($"Game Manager Initialized with {frames} frames of input");

        await Task.Delay(1000);
        StartGame();
    }

    public void StartGame()
    {
        if (State != GameState.Inactive)
        {
            Debug.LogWarning("Game is not inactive, can not start game");
            return;
        }
        
        State = GameState.Awaiting;
        StateAnimator.SetTrigger(AnimAwaiting);
    }

    public void FixedUpdate()
    {
        // LastInput = CurrentInput;
        CurrentInput = new InputState
        {
            Move = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0),
            Jump = Input.GetKey(KeyCode.Space),
        };

        if (State == GameState.Awaiting && CurrentInput.Active)
        {
            ResetWorld();
            State = GameState.Recording;
            StateAnimator.SetTrigger(AnimRecording);
        }

        if (State == GameState.Replaying && CurrentInput.Active)
        {
            State = GameState.Recording;
            StateAnimator.SetTrigger(AnimRecording);
        }

        if (State == GameState.Recording)
        {
            InputRecording[Frame] = CurrentInput;
        }

        if (State == GameState.Replaying || State == GameState.Recording)
        {
            StepFrame();
            if (Frame == InputRecording.Length)
            {
                ResetWorld();
                State = GameState.Replaying;
                StateAnimator.SetTrigger(AnimReplaying);
            }
        }
    }

    private void ResetWorld()
    {
        Frame = 0;
        OnReset?.Invoke();
    }

    private void StepFrame()
    {
        OnFixedUpdate?.Invoke();
        Frame++;
    }

    public FrameInput FetchInput()
    {
        var last = Frame > 0 ? InputRecording[Frame - 1] : default;
        var current = InputRecording[Frame];

        return new FrameInput()
        {
            LastFrame = last,
            CurrentFrame = current,
        };
    }
}
