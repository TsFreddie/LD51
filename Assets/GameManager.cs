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
    public int CheckpointProgress { get; private set; }
    public bool SkipInput { get; private set; }

    // public InputState LastInput { get; private set; }
    public InputState CurrentInput { get; private set; }

    public int Frame { get; private set; }

    public bool IsReplaying => State == GameState.Replaying;

    public Action OnReset;
    public Action OnFixedUpdate;
    public Action OnFixedUpdateWorld;
    public Action OnWorldStart;

    public GameState State = GameState.Inactive;

    private static readonly int AnimInactiveToAwaiting = Animator.StringToHash("Inactive-Await");
    private static readonly int AnimReplayingToRecording = Animator.StringToHash("Replay-Record");
    private static readonly int AnimAwaitingToRecording = Animator.StringToHash("Await-Record");
    private static readonly int AnimRecordingToReplaying = Animator.StringToHash("Record-Replay");
    private static readonly int AnimReplayingToInactive = Animator.StringToHash("Replay-Inactive");
    private static readonly int AnimReplayingToAwaiting = Animator.StringToHash("Replay-Await");
    private static readonly int AnimRecordingToAwaiting = Animator.StringToHash("Record-Await");
    private static readonly int AnimReplayingToReplaying = Animator.StringToHash("Replay-Replay");
    private static readonly int AnimInactive = Animator.StringToHash("Inactive");

    private bool _died = false;

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

        _enterDown = Input.GetKeyDown(KeyCode.Return);
        _actionDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.A);
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

    public void LockWorld()
    {
        if (State == GameState.Replaying) StateAnimator.Play(AnimReplayingToInactive);
        if (State == GameState.Recording) StateAnimator.Play(AnimInactive);
        if (State == GameState.Awaiting) StateAnimator.Play(AnimInactive);
        State = GameState.Inactive;
        if (Player != null) Player.Animator.enabled = false;
    }

    public void ResetGame()
    {
        if (Player != null) Player.Animator.enabled = true;
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

    private bool _enterDown;
    private bool _actionDown;

    public void FixedUpdate()
    {
        CurrentInput = !SkipInput ? new InputState
        {
            Move = (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0),
            Jump = Input.GetKey(KeyCode.Space),
        } : default;

        if (State == GameState.Awaiting && _actionDown)
        {
            ResetWorld();
            State = GameState.Recording;
            if (Player != null) Player.StartVanish();
            ShowSnapshot();
            StateAnimator.Play(AnimAwaitingToRecording);
            OnWorldStart?.Invoke();
        }

        if (!_died && State == GameState.Replaying && _actionDown)
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
            if (!_enterDown)
                StepFrame();
            if (Frame == InputRecording.Length || _enterDown)
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

                OnWorldStart?.Invoke();
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

        _enterDown = false;
        _actionDown = false;
    }

    private void ResetWorld()
    {
        Frame = 0;
        _died = false;
        OnReset?.Invoke();
        SkipInput = false;
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

    public void Finish()
    {
        if (_died) return;
        SkipInput = true;
    }

    public void Die()
    {
        SkipInput = true;
        _died = true;

        if (Player != null)
        {
            Player.StartVanish();
            Player.LockPlayer();
        }

    }

    public async void Checkpoint(Checkpoint checkpoint)
    {
        if (IsReplaying)
        {
            CheckpointProgress = checkpoint.CheckpointProgress;
            CameraController.Instance.MoveToTarget(checkpoint.CameraPosition.position);
            Player.SaveInitState();
            LockWorld();

            SetNormalMode();
            if (Player != null) Player.CancelVanish();

            await UniTask.Delay(1500);

            ResetGame();

        }

        SkipInput = true;
        Player.LockPlayer();
    }
}
