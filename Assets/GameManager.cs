using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    public CanvasGroup Intro;
    public RawImage RenderArea;
    public string FirstLevel;
    public GameObject EndingUI;
    public TMP_Text ResetCounter;

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
    private static readonly int ShaderTransition = Shader.PropertyToID("_Transition");

    private Scene _loadedLevel;

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

#if UNITY_EDITOR
        if (SceneManager.sceneCount == 2)
        {
            _loadedLevel = SceneManager.GetSceneAt(1);
            await UniTask.Delay(1000);
            StartGame();
            return;
        }
#endif

        // Intro sequence
        Intro.gameObject.SetActive(true);
        Intro.alpha = 0.0f;

        await UniTask.Delay(1000);

        while (Intro.alpha < 1.0f)
        {
            Intro.alpha += Time.deltaTime * 2.0f;
            await UniTask.Yield();
        }
        Intro.alpha = 1.0f;

        await UniTask.Delay(2500);

        while (Intro.alpha > 0.0f)
        {
            Intro.alpha -= Time.deltaTime * 2.0f;
            await UniTask.Yield();
        }
        Intro.gameObject.SetActive(false);
        Intro.alpha = 0.0f;
        LoadLevel(FirstLevel);
    }

    private bool CheckLevelSelect()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            FirstLevel = "1-1";
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            FirstLevel = "2-1";
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            FirstLevel = "3-1";
            return true;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            FirstLevel = "4-1";
            return true;
        }
        return false;
    }

    private float _transition = 0.0f;
    private int _resetCount = 0;

    private async void LoadLevel(string level)
    {
        CheckpointProgress = 0;
        LockWorld();

        if (Player != null) Player.StartVanish();

        await UniTask.Delay(1000);

        while (_transition < 1.0f)
        {
            _transition += Time.deltaTime;
            RenderArea.material.SetFloat(ShaderTransition, _transition);
            await UniTask.Yield();
        }

        RenderArea.material.SetFloat(ShaderTransition, 1.0f);
        await UniTask.Yield();
        if (_loadedLevel.IsValid())
            await SceneManager.UnloadSceneAsync(_loadedLevel);
        await SceneManager.LoadSceneAsync(level, LoadSceneMode.Additive);
        _loadedLevel = SceneManager.GetSceneAt(1);

        while (_transition < 2.0f)
        {
            _transition += Time.deltaTime;
            RenderArea.material.SetFloat(ShaderTransition, _transition);
            await UniTask.Yield();
        }

        _transition = 0.0f;
        RenderArea.material.SetFloat(ShaderTransition, 0.0f);

        await UniTask.Delay(1000);
        StartGame();
    }

    protected void Update()
    {
        CheckLevelSelect();
        Timer.text = $"{Frame * Time.fixedDeltaTime:0.00}";
        Slider.value = Frame / ((float)InputRecording.Length - 1);

        if (State != GameState.Inactive && Input.GetKeyDown(KeyCode.R))
        {
            ResetGame();
        }

        _enterDown |= Input.GetKeyDown(KeyCode.Return);
        _actionDown |= Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.A);
    }

    public void StartGame()
    {
        if (State != GameState.Inactive)
        {
            Debug.LogWarning("Game is not inactive, can not start game");
            return;
        }

        if (Player != null) Player.Animator.enabled = true;
        Track.SpawnDropTrack(InputRecording, 0);
        for (var i = 0; i < InputRecording.Length; i++) InputRecording[i] = default;

        ResetWorld();
        State = GameState.Awaiting;
        StateAnimator.Play(AnimInactiveToAwaiting);
        SetVoidMode();
        // if (Player != null) Player.CancelVanish();
        HideSnapshot();
        AudioManager.Instance.Play("startgame");
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
        if (State != GameState.Awaiting)
        {
            _resetCount++;
            ResetCounter.text = _resetCount.ToString();
        }
        if (State == GameState.Inactive) StateAnimator.Play(AnimInactiveToAwaiting);
        if (State == GameState.Recording) StateAnimator.Play(AnimRecordingToAwaiting);
        if (State == GameState.Replaying) StateAnimator.Play(AnimReplayingToAwaiting);
        State = GameState.Awaiting;
        SetVoidMode();
        // if (Player != null) Player.CancelVanish();
        HideSnapshot();
        AudioManager.Instance.Play("awaiting");
    }

    private bool _enterDown;
    private bool _actionDown;

    public void FixedUpdate()
    {
        DoFixedUpdate();

        _enterDown = false;
        _actionDown = false;
    }

    public void DoReplayLogic()
    {
        if (!_enterDown)
            StepFrame();
        if (Frame == InputRecording.Length || _enterDown)
        {
            ResetWorld();
            if (State == GameState.Recording)
            {
                HideSnapshot();
                SetNormalMode();
                StateAnimator.Play(AnimRecordingToReplaying);
            }
            else
                StateAnimator.Play(AnimReplayingToReplaying);
            AudioManager.Instance.Play("replaying");
            OnWorldStart?.Invoke();
            State = GameState.Replaying;
        }
    }

    public void DoFixedUpdate()
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
            AudioManager.Instance.Play("recording");
        }

        if (!_died && State == GameState.Replaying && _actionDown && Frame > 30)
        {
            State = GameState.Recording;
            if (Player != null) Player.StartVanish();
            ShowSnapshot();
            SetVoidMode();
            Track.SpawnDropTrack(InputRecording, Frame);
            for (var i = Frame; i < InputRecording.Length; i++) InputRecording[i] = default;
            StateAnimator.Play(AnimReplayingToRecording);
            AudioManager.Instance.Play("recording");
            _resetCount++;
            ResetCounter.text = _resetCount.ToString();
        }

        if (State == GameState.Recording)
        {
            InputRecording[Frame] = CurrentInput;
        }

        if (State == GameState.Replaying || State == GameState.Recording)
        {
            DoReplayLogic();
            if (State == GameState.Replaying && Input.GetKey(KeyCode.F))
            {
                DoReplayLogic();
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
        _died = false;
        if (Player != null) Player.CancelVanish();
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

    public void Finish(string levelName)
    {
        if (_died) return;
        SkipInput = true;

        if (!IsReplaying)
        {
            AudioManager.Instance.Play("checkpoint");
            return;
        }
        AudioManager.Instance.Play("finish_line");
        GameManager.Instance.LoadLevel(levelName);
    }

    public void Die()
    {
        if (_died) return;
        AudioManager.Instance.Play("die");
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
        AudioManager.Instance.Play("checkpoint");
        if (IsReplaying)
        {
            CheckpointProgress = checkpoint.CheckpointProgress;
            CameraController.Instance.MoveToTarget(checkpoint.CameraPosition.position);
            Player.SaveInitState();
            LockWorld();

            SetNormalMode();
            // if (Player != null) Player.CancelVanish();

            await UniTask.Delay(1500);

            StartGame();

        }

        SkipInput = true;
        Player.LockPlayer();
    }

    public async void Ending()
    {
        LockWorld();
        SetVoidMode();
        EndingUI.SetActive(true);
    }
}
