using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    public Animator LockBannerAnimator;
    public InputState[] InputRecording;

    public TMP_Text Timer;
    public Slider Slider;
    public TrackManager Track;
    public CanvasGroup MenuGroup;
    public RawImage RenderArea;
    public string FirstLevel;
    public GameObject EndingUI;
    public TMP_Text ResetCounter;
    public PlayerInput PlayerInput;
    public Animator TrackAnimator;

    public static GameManager Instance { get; private set; }
    public PlayerControl Player;
    public SpriteRenderer PlayerSnapshot;

    public MenuFadeIn TitleGroup;
    public MenuFadeIn GameGroup;
    public CanvasGroup PauseGroup;
    public GameObject PauseFirstSelected;

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
    public Action OnWorldPause;
    public Action OnWorldResume;

    public GameState State = GameState.Inactive;

    private static readonly int AnimInactiveToAwaiting = Animator.StringToHash("Inactive-Await");
    private static readonly int AnimReplayingToRecording = Animator.StringToHash("Replay-Record");
    private static readonly int AnimAwaitingToRecording = Animator.StringToHash("Await-Record");
    private static readonly int AnimRecordingToReplaying = Animator.StringToHash("Record-Replay");
    private static readonly int AnimReplayingToInactive = Animator.StringToHash("Replay-Inactive");
    private static readonly int AnimReplayingToAwaiting = Animator.StringToHash("Replay-Await");
    private static readonly int AnimRecordingToAwaiting = Animator.StringToHash("Record-Await");
    private static readonly int AnimReplayingToReplaying = Animator.StringToHash("Replay-Replay");
    private static readonly int AnimSuccess = Animator.StringToHash("Success");
    private static readonly int AnimFailed = Animator.StringToHash("Failed");
    private static readonly int AnimInactive = Animator.StringToHash("Inactive");

    private bool _died = false;
    private bool _manualReset = false;

    private static readonly int ShaderEffectFactor = Shader.PropertyToID("_EffectFactor");
    private static readonly int ShaderTransition = Shader.PropertyToID("_Transition");

    private Scene _loadedLevel;
    private bool _gameRunning;
    private bool _pausing;

    private float _resetHeldTime;
    private Vector2 _initCameraPosition;

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
            TitleGroup.FadeOutSilently();
            GameGroup.FadeInSilently();
            PauseGroup.alpha = 0;
            _gameRunning = true;
            InputManager.Instance.SetGameMode();
            _loadedLevel = SceneManager.GetSceneAt(1);
            await UniTask.Delay(1000);
            StartGame();
            return;
        }
#endif

        // Wait 1 second before activating menu
        await UniTask.Delay(1000);
        MenuGroup.interactable = true;
    }

    private float _transition = 0.0f;
    private int _resetCount = 0;

    public async void LoadLevel(string level)
    {
        TitleGroup.FadeOutSilently();
        GameGroup.FadeInSilently();
        _gameRunning = true;
        InputManager.Instance.SetGameMode();
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

    private AudioEmitter _resetEmitter;

    protected void Update()
    {
        if (!_gameRunning) return;

        if (PlayerInput.actions["Pause"].WasPressedThisFrame())
        {
            if (!_pausing)
            {
                if (State != GameState.Inactive) Pause();
            }
            else
            {
                Unpause();
            }
        }

        Timer.text = $"{Frame * Time.fixedDeltaTime:0.00}";
        Slider.value = Frame / ((float)InputRecording.Length - 1);

        var canReset = State != GameState.Awaiting && State != GameState.Inactive && !_pausing;
        if (canReset)
        {
            if (PlayerInput.actions["Reset"].WasPressedThisFrame())
            {
                TrackAnimator.Play("Erase");
                _resetEmitter = AudioManager.Instance.Play("reset");
                _resetHeldTime = Time.unscaledTime;
            }

            if (PlayerInput.actions["Reset"].IsPressed() && Time.unscaledTime - _resetHeldTime >= 1.0f)
            {
                ResetGame();
                _resetHeldTime = float.MinValue;
            }
        }

        if (!canReset || PlayerInput.actions["Reset"].WasReleasedThisFrame())
        {
            if (_resetEmitter.IsValid())
            {
                _resetEmitter.Stop();
            }
            TrackAnimator.Play("Idle");
        }

        if (_pausing) return;

        _enterDown |= PlayerInput.actions["Play"].WasPressedThisFrame();
        _actionDown |= PlayerInput.actions["Move"].WasPressedThisFrame() || PlayerInput.actions["Jump"].WasPressedThisFrame();
    }

    public void StartGame()
    {
        if (State != GameState.Inactive)
        {
            Debug.LogWarning("Game is not inactive, can not start game");
            return;
        }

        Player.SaveInitState();
        _initCameraPosition = CameraController.Instance.TargetPosition;

        if (Player != null) Player.Animator.enabled = true;
        Track.SpawnDropTrack(InputRecording, 0);
        for (var i = 0; i < InputRecording.Length; i++) InputRecording[i] = default;

        ResetWorld();
        State = GameState.Awaiting;
        StateAnimator.Play(AnimInactiveToAwaiting);
        SetVoidMode();
        // if (Player != null) Player.CancelVanish();
        HideSnapshot();
        AudioManager.Instance.Play("awaiting");
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
        LockBannerAnimator.SetBool(AnimFailed, false);
        LockBannerAnimator.SetBool(AnimSuccess, false);
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
        if (_gameRunning && !_pausing)
            DoFixedUpdate();

        _enterDown = false;
        _actionDown = false;
    }

    public void Pause()
    {
        if (_gameRunning)
        {
            _pausing = true;
            InputManager.Instance.NullifyMenu();
            InputManager.Instance.OpenMenu(PauseGroup, PauseFirstSelected);
            PauseGroup.interactable = true;
            PauseGroup.alpha = 1;
            InputManager.Instance.SetMenuMode();
        }
    }

    public void Unpause()
    {
        if (_pausing)
        {
            _pausing = false;
            PauseGroup.interactable = false;
            PauseGroup.alpha = 0;
            InputManager.Instance.SetGameMode();
        }
    }

    public async void ReturnToMainMenu()
    {
        _pausing = false;
        _gameRunning = false;

        PauseGroup.alpha = 0;
        PauseGroup.interactable = false;

        while (_transition < 1.0f)
        {
            _transition += Time.deltaTime;
            RenderArea.material.SetFloat(ShaderTransition, _transition);
            await UniTask.Yield();
        }

        if (_loadedLevel.IsValid())
            await SceneManager.UnloadSceneAsync(_loadedLevel);

        _transition = 0.0f;
        RenderArea.material.SetFloat(ShaderTransition, 0.0f);
        GameGroup.FadeOutSilently();
        TitleGroup.FadeInSilently();

        InputManager.Instance.NullifyMenu();
        await UniTask.Delay(500);
        InputManager.Instance.SwitchToTitleMenu();
        
        for (var i = 0; i < InputRecording.Length; i++) InputRecording[i] = default;
        Frame = 0;
        _resetCount = 0;
        ResetCounter.text = _resetCount.ToString();
        _enterDown = false;
        _actionDown = false;
        PlayerSnapshot.sprite = null;
        PlayerSnapshot.color = Color.clear;

    }

    public void DoReplayLogic()
    {
        StepFrame();

        var shouldReset = Frame == InputRecording.Length || _enterDown;
        if (shouldReset)
        {
            _manualReset = _enterDown;
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
        var moveInput = PlayerInput.actions["Move"].ReadValue<float>();

        CurrentInput = !SkipInput ? new InputState
        {
            Move = moveInput > 0 ? 1 : moveInput < 0 ? -1 : 0,
            Jump = PlayerInput.actions["Jump"].IsPressed(),
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

        if (!_died && State == GameState.Replaying && _actionDown && (_manualReset || Frame > 30))
        {
            State = GameState.Recording;
            if (Player != null) Player.StartVanish();
            ShowSnapshot();
            SetVoidMode();
            Track.SpawnDropTrack(InputRecording, Frame);
            for (var i = Frame; i < InputRecording.Length; i++) InputRecording[i] = default;
            StateAnimator.Play(AnimReplayingToRecording);
            LockBannerAnimator.SetBool(AnimFailed, false);
            LockBannerAnimator.SetBool(AnimSuccess, false);
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
            if (State == GameState.Replaying && PlayerInput.actions["FastForward"].IsPressed())
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
        CameraController.Instance.MoveToTarget(_initCameraPosition);
    }

    private void StepFrame()
    {
        Physics2D.SyncTransforms();
        OnFixedUpdate?.Invoke();
        Physics2D.SyncTransforms();
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
            CaptionManager.Instance.ShowCaption("success", 2.0f, CaptionType.Item);
            LockBannerAnimator.SetBool(AnimSuccess, true);
            return;
        }
        AudioManager.Instance.Play("finish_line");
        CaptionManager.Instance.ShowCaption("advance", 2.0f, CaptionType.Success);
        LockBannerAnimator.SetBool(AnimFailed, false);
        LockBannerAnimator.SetBool(AnimSuccess, false);

        GameManager.Instance.LoadLevel(levelName);
    }

    public void Die()
    {
        if (_died) return;
        AudioManager.Instance.Play("die");
        CaptionManager.Instance.ShowCaption("failed", 2.0f, CaptionType.Fail);
        if (!IsReplaying) LockBannerAnimator.SetBool(AnimFailed, true);
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
        CaptionManager.Instance.ShowCaption("success", 2.0f, CaptionType.Item);
        if (!IsReplaying) LockBannerAnimator.SetBool(AnimSuccess, true);
        if (IsReplaying)
        {
            LockBannerAnimator.SetBool(AnimFailed, false);
            LockBannerAnimator.SetBool(AnimSuccess, false);
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

    public void Ending()
    {
        LockWorld();
        SetVoidMode();
        EndingUI.SetActive(true);
    }
}
