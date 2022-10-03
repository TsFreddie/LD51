using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public TMP_Text Timer;
    public Slider Slider;
    public TrackManager Track;

    public static GameManager Instance { get; private set; }
    public PlayerControl Player;
    public SpriteRenderer PlayerSnapshot;

    // public InputState LastInput { get; private set; }
    public InputState CurrentInput { get; private set; }

    public int Frame { get; private set; }

    public Action OnReset;
    public Action OnFixedUpdate;

    public GameState State = GameState.Inactive;

    private static readonly int AnimInactiveToAwaiting = Animator.StringToHash("Inactive-Await");
    private static readonly int AnimReplayingToRecording = Animator.StringToHash("Replay-Record");
    private static readonly int AnimAwaitingToRecording = Animator.StringToHash("Await-Record");
    private static readonly int AnimRecordingToReplaying = Animator.StringToHash("Record-Replay");
    private static readonly int AnimReplayingToInactive = Animator.StringToHash("Replay-Inactive");
    private static readonly int AnimReplayingToAwaiting = Animator.StringToHash("Replay-Await");
    private static readonly int AnimRecordingToAwaiting = Animator.StringToHash("Record-Await");
    private static readonly int AnimReplayingToReplaying = Animator.StringToHash("Replay-Replay");

    public Action OnFixedUpdateWorld;
    public bool Died = false;

    private static readonly int ShaderEffectFactor = Shader.PropertyToID("_EffectFactor");


    protected async void Awake()
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

    protected void Update()
    {
        Timer.text = $"{Frame * Time.fixedDeltaTime:0.00}";
        Slider.value = Frame / ((float)InputRecording.Length - 1);

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }
    }

    public void StartGame()
    {
        if (State != GameState.Inactive)
        {
            Debug.LogWarning("Game is not inactive, can not start game");
            return;
        }

        ResetWorld();
        State = GameState.Awaiting;
        StateAnimator.Play(AnimInactiveToAwaiting);
        SetVoidMode();
        if (Player != null) Player.CancelVanish();
        HideSnapshot();
    }

    public void ResetGame()
    {
        Track.SpawnDropTrack(InputRecording, 0);
        for (var i = 0; i < InputRecording.Length; i++) InputRecording[i] = default;
        ResetWorld();
        if (State == GameState.Inactive) StateAnimator.Play(AnimInactiveToAwaiting);
        if (State == GameState.Recording) StateAnimator.Play(AnimRecordingToAwaiting);
        if (State == GameState.Replaying) StateAnimator.Play(AnimReplayingToAwaiting);
        State = GameState.Awaiting;
        SetVoidMode();
        if (Player != null) Player.CancelVanish();
        HideSnapshot();
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
            if (Player != null) Player.StartVanish();
            ShowSnapshot();
            StateAnimator.Play(AnimAwaitingToRecording);
        }

        if (State == GameState.Replaying && CurrentInput.Active)
        {
            State = GameState.Recording;
            if (Player != null) Player.StartVanish();
            ShowSnapshot();
            SetVoidMode();
            Track.SpawnDropTrack(InputRecording, Frame);
            for (var i = Frame; i < InputRecording.Length; i++) InputRecording[i] = default;
            StateAnimator.Play(AnimReplayingToRecording);
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
                if (State == GameState.Recording)
                {
                    if (Player != null) Player.CancelVanish();
                    HideSnapshot();
                    SetNormalMode();
                    StateAnimator.Play(AnimRecordingToReplaying);
                }
                else
                    StateAnimator.Play(AnimReplayingToReplaying);
                State = GameState.Replaying;
            }
        }

        if (State == GameState.Awaiting)
        {
            Track.UpdateTrack(InputRecording, 0);
        }
        else
        {
            Track.UpdateTrack(InputRecording, (State == GameState.Recording) ? Frame : -1);
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

        OnFixedUpdateWorld?.Invoke();
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

    public void IsDied(bool died)
    {
        Died = died;
        Debug.Log("Died");
    }

    public void Transition(TransitionDestination.DestinationTag destinationTag)
    {
        TransitionDestination destination = null;

        var entrances = FindObjectsOfType<TransitionDestination>();

        for (int i = 0; i < entrances.Length; i++)
        {
            if (entrances[i].destinationTag == destinationTag)
            {
                destination = entrances[i];
                break;
            }
        }

        if (destination == null) return;
        Debug.Log(destination.transform.position);
    }

    private bool _voidMode;
    private float _voidFactor;

    public async void SetVoidMode()
    {
        _voidMode = true;
        while (_voidMode && _voidFactor < 1f)
        {
            _voidFactor += Time.deltaTime * 2.0f;
            Shader.SetGlobalFloat(ShaderEffectFactor, Mathf.Clamp(_voidFactor, 0, 1));
            await UniTask.Yield();
        }
    }

    public async void SetNormalMode()
    {
        _voidMode = false;
        while (!_voidMode && _voidFactor > 0f)
        {
            _voidFactor -= Time.deltaTime * 2.0f;
            Shader.SetGlobalFloat(ShaderEffectFactor, Mathf.Clamp(_voidFactor, 0, 1));
            await UniTask.Yield();
        }
    }

    private bool _snapshotOn;
    private float _snapshotAlpha;
    public void ShowSnapshot()
    {
        if (Player == null) return;
        PlayerSnapshot.sprite = Player.Sprite.sprite;
        PlayerSnapshot.flipX = Player.Sprite.flipX;
        PlayerSnapshot.flipY = Player.Sprite.flipY;
        var t = Player.Sprite.transform;
        var snapT = transform;
        snapT.position = t.position;
        snapT.rotation = t.rotation;
        _snapshotAlpha = 1;
        var color = PlayerSnapshot.color;
        color.a = _snapshotAlpha;
        PlayerSnapshot.color = color;
        _snapshotOn = true;
    }
    
    public async void HideSnapshot()
    {
        if (Player == null) return;
        _snapshotOn = false;
        while (!_snapshotOn && _snapshotAlpha > 0f)
        {
            _snapshotAlpha -= Time.deltaTime * 2.0f;
            var color = PlayerSnapshot.color;
            color.a = _snapshotAlpha;
            PlayerSnapshot.color = color;
            await UniTask.Yield();
        }
    }


    public void OnDestroy()
    {
        Shader.SetGlobalFloat(ShaderEffectFactor, 0);
    }
}
