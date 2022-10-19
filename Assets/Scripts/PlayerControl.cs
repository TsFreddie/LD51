using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public struct RayRange
{
    public RayRange(float x1, float y1, float x2, float y2, Vector2 dir)
    {
        Start = new Vector2(x1, y1);
        End = new Vector2(x2, y2);
        Dir = dir;
    }

    public readonly Vector2 Start, End, Dir;
}

public class PlayerControl : MonoBehaviour
{
    private static readonly int ShaderVanishFactor = Shader.PropertyToID("_Vanish");

    public struct PlayerState
    {
        public Vector2 Velocity;
        public Vector2 Position;
        public bool Jumping;
        public bool Landing;
        public bool FaceRight;
        public int LastJumpFrame;
        public int LastGroundFrame;
        public bool Grounded;
        public bool CoyoteUsable;
    }

    public BoxCollider2D Collider;
    public SpriteRenderer Sprite;
    public Animator Animator;

    public bool FacingRightWhenStart = true;

    [Header("Movement")]
    [SerializeField] private float _acceleration = 90.0f;
    [SerializeField] private float _moveSpeed = 13.0f;
    [SerializeField] private float _deceleration = 60.0f;

    [Header("Jumping")]
    [SerializeField] private float _coyoteTimeThreshold = 0.1f;
    [SerializeField] private float _jumpBuffer = 0.1f;
    [SerializeField] private float _jumpVelocity = 20f;

    [Header("Detection")]
    [SerializeField] private int _detectorCount = 3;
    [SerializeField] private float _detectionRayLength = 0.1f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private int _collisionIteration = 10;
    [SerializeField] private float _rayBuffer = 0.1f;

    private PlayerState _state;
    private PlayerState _initState;
    private Sprite _initSprite;

    private bool _stopped;
    private bool _vanish;
    private float _vanishFactor;

    public void LockPlayer()
    {
        _stopped = true;
        Animator.enabled = false;
    }

    public async void StartVanish()
    {
        _vanish = true;
        while (_vanish && _vanishFactor < 1f)
        {
            if (this == null) return;
            _vanishFactor += Time.deltaTime;
            Sprite.material.SetFloat(ShaderVanishFactor, Mathf.Clamp(_vanishFactor, 0, 1));
            await UniTask.Yield();
        }
    }

    public async void CancelVanish()
    {
        _vanish = false;
        while (!_vanish && _vanishFactor > 0f)
        {
            if (this == null) return;
            _vanishFactor -= Time.deltaTime * 4.0f;
            Sprite.material.SetFloat(ShaderVanishFactor, Mathf.Clamp(_vanishFactor, 0, 1));
            await UniTask.Yield();
        }
    }

    private IEnumerable<Vector2> Rays(RayRange range)
    {
        for (var i = 0; i < _detectorCount; i++)
        {
            var t = (float)i / (_detectorCount - 1);
            yield return Vector2.Lerp(range.Start, range.End, t);
        }
    }

    protected void Awake()
    {
        GameManager.Instance.Player = this;
        GameManager.Instance.OnFixedUpdate += Process;
        GameManager.Instance.OnReset += ResetInitState;
        _state.LastJumpFrame = int.MinValue;
        _state.LastGroundFrame = int.MinValue;
        _state.Position = transform.position;
        _state.FaceRight = FacingRightWhenStart;
        
        _state.Grounded = true;
        
        if (Sprite.flipX != _state.FaceRight)
            Sprite.flipX = _state.FaceRight;
        _initState = _state;
        _initSprite = Sprite.sprite;
    }

    protected void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.Player == this)
            GameManager.Instance.Player = null;
        GameManager.Instance.OnFixedUpdate -= Process;
        GameManager.Instance.OnReset -= ResetInitState;
    }

    private void OnDrawGizmos()
    {
        var offset = Collider.offset;
        var size = Collider.size;
        var b = new Bounds((Vector2)transform.position + offset, size);

        var raysDown = new RayRange(b.min.x + _rayBuffer, b.min.y, b.max.x - _rayBuffer, b.min.y, Vector2.down);
        var raysUp = new RayRange(b.min.x + _rayBuffer, b.max.y, b.max.x - _rayBuffer, b.max.y, Vector2.up);
        var raysLeft = new RayRange(b.min.x, b.min.y + _rayBuffer, b.min.x, b.max.y - _rayBuffer, Vector2.left);
        var raysRight = new RayRange(b.max.x, b.min.y + _rayBuffer, b.max.x, b.max.y - _rayBuffer, Vector2.right);

        // Bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(b.center, b.size);

        // Rays
        Gizmos.color = Color.blue;
        foreach (var range in new List<RayRange> { raysUp, raysRight, raysDown, raysLeft })
        {
            foreach (var point in Rays(range))
            {
                Gizmos.DrawRay(point, range.Dir * _detectionRayLength);
            }
        }
    }

    private void ResetInitState()
    {
        _state = _initState;
        transform.position = _state.Position;
        Sprite.flipX = _state.FaceRight;
        _stopped = false;
        Animator.enabled = true;
        Sprite.sprite = _initSprite;
        Animator.Play("Idle");
        Animator.SetBool("Running", false);
    }

    public void SaveInitState(bool onlyPosition = true)
    {
        _initState = _state;
        _initState.Position = transform.position;
        _initState.LastJumpFrame = int.MinValue;
        _initState.LastGroundFrame = int.MinValue;

        if (onlyPosition)
        {
            _initState.Jumping = false;
            _initState.Landing = false;
            _initState.CoyoteUsable = false;
            _initState.Velocity = Vector2.zero;
        }
    }

    private void Process()
    {
        if (_stopped) return;

        var t = transform;
        var lastPos = t.position;

        var game = GameManager.Instance;
        var input = game.FetchInput();

        if (input.JumpDown)
        {
            _state.LastJumpFrame = game.Frame;
        }

        if (input.Move > 0) _state.FaceRight = Sprite.flipX = true;
        else if (input.Move < 0) _state.FaceRight = Sprite.flipX = false;

        // Collision detection
        bool RunDetection(RayRange range)
        {
            return Rays(range).Any(point => Physics2D.Raycast(point, range.Dir, _detectionRayLength, _groundLayer));
        }

        var offset = Collider.offset;
        var size = Collider.size;
        var b = new Bounds((Vector2)transform.position + offset, size);

        var raysDown = new RayRange(b.min.x + _rayBuffer, b.min.y, b.max.x - _rayBuffer, b.min.y, Vector2.down);
        var raysUp = new RayRange(b.min.x + _rayBuffer, b.max.y, b.max.x - _rayBuffer, b.max.y, Vector2.up);
        var raysLeft = new RayRange(b.min.x, b.min.y + _rayBuffer, b.min.x, b.max.y - _rayBuffer, Vector2.left);
        var raysRight = new RayRange(b.max.x, b.min.y + _rayBuffer, b.max.x, b.max.y - _rayBuffer, Vector2.right);

        // Ray detection
        var colDown = RunDetection(raysDown);
        var colUp = RunDetection(raysUp);
        var colLeft = RunDetection(raysLeft);
        var colRight = RunDetection(raysRight);

        // Ground
        _state.Landing = false;
        if (_state.Grounded && !colDown)
        {
            _state.LastGroundFrame = game.Frame; // Only trigger when first leaving
            Animator.Play("Air");
        }
        else if (!_state.Grounded && colDown)
        {
            _state.CoyoteUsable = true; // Only trigger when first touching
            _state.Landing = true;
        }

        _state.Grounded = colDown;

        // Jumping
        if (input.Move != 0)
        {
            // Set horizontal move speed
            _state.Velocity.x += input.Move * _acceleration * Time.fixedDeltaTime;

            // clamped by max frame movement
            _state.Velocity.x = Mathf.Clamp(_state.Velocity.x, -_moveSpeed, _moveSpeed);
        }
        else
        {
            // No input. Let's slow the character down
            _state.Velocity.x = Mathf.MoveTowards(_state.Velocity.x, 0, _deceleration * Time.fixedDeltaTime);
        }

        if (_state.Velocity.x > 0 && colRight || _state.Velocity.x < 0 && colLeft)
        {
            // Don't walk through walls
            _state.Velocity.x = 0;
        }

        // Gravity
        if (!colDown)
        {
            // in the air
            _state.Velocity.y += Physics2D.gravity.y * Time.fixedDeltaTime;
        }
        else
        {
            // on the ground
            if (_state.Velocity.y < 0) _state.Velocity.y = 0;
        }

        // Jump
        var canUseCoyote = _state.CoyoteUsable && !colDown && _state.LastGroundFrame + Mathf.RoundToInt(_coyoteTimeThreshold / Time.fixedDeltaTime) > game.Frame;
        var hasBufferedJump = colDown && _state.LastJumpFrame + Mathf.RoundToInt(_jumpBuffer / Time.fixedDeltaTime) > game.Frame;
        if ((input.JumpDown && canUseCoyote) || hasBufferedJump)
        {
            _state.Velocity.y = _jumpVelocity;
            _state.CoyoteUsable = false;
            _state.LastGroundFrame = int.MinValue;
            _state.Jumping = true;
            _state.LastJumpFrame = int.MinValue;
        }
        else
        {
            _state.Jumping = false;
        }

        // MoveBox
        var pos = (Vector2)t.position + Collider.offset;
        var delta = _state.Velocity * Time.fixedDeltaTime;
        var furthestPoint = pos + delta;
        var distance = delta.magnitude;

        // check furthest movement. If nothing hit, move and don't do extra checks
        var hit = Physics2D.OverlapBox(furthestPoint, b.size, 0, _groundLayer);
        if (!hit)
        {
            pos = furthestPoint;
        }
        else if (distance > 0.00001f)
        {
            for (int i = 1; i < _collisionIteration; i++)
            {
                var time = (float)i / _collisionIteration;
                var newPos = Vector2.Lerp(pos, furthestPoint, time);

                if (Physics2D.OverlapBox(newPos, Collider.size, 0, _groundLayer))
                {
                    // We've landed on a corner or hit our head on a ledge. Nudge the player gently
                    if (i == 1)
                    {
                        if (_state.Velocity.y < 0) _state.Velocity.y = 0;
                        var dir = (Vector2)b.center - furthestPoint;
                        newPos += dir.normalized * distance;
                    }
                    else
                    {
                        var hits = 0;

                        var vHit = Physics2D.OverlapBox(new Vector2(pos.x, newPos.y), Collider.size, 0, _groundLayer);
                        if (vHit)
                        {
                            newPos.y = pos.y;
                            _state.Velocity.y = 0.0f;
                            hits++;
                        }

                        var hHit = Physics2D.OverlapBox(new Vector2(newPos.x, pos.y), Collider.size, 0, _groundLayer);
                        if (hHit)
                        {
                            newPos.x = pos.x;
                            _state.Velocity.x = 0.0f;
                            hits++;
                        }

                        if (hits == 0)
                        {
                            newPos.y = pos.y;
                            _state.Velocity.y = 0.0f;
                            newPos.x = pos.x;
                            _state.Velocity.x = 0.0f;
                        }
                    }
                }
                pos = newPos;
            }
        }

        t.position = pos - Collider.offset;

        // Animation and sound
        if (_state.Jumping)
        {
            AudioManager.Instance.Play("jump");
            CaptionManager.Instance.ShowCaption("jump", 1.0f, CaptionType.Info);
            Animator.Play("Prejump");
        }
        
        if (_state.Landing)
        {
            AudioManager.Instance.Play("landing");
            CaptionManager.Instance.ShowCaption("landing", 1.0f, CaptionType.Info);
            Animator.Play("Landing");
        }

        Animator.SetBool("Running", ((Vector2)(lastPos - t.position)).magnitude > 0.0001f);
    }
}
