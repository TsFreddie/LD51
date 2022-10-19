using UnityEngine;

public class HiddenSpike : MonoBehaviour
{
    private Collider2D _collider;

    private bool _triggered = false;
    private bool _isHidden = true;
    private int _leftFrame = -1;
    private float _leftTime = float.MinValue;

    private Vector3 _visualInitPosition;

    public GameObject Visual;
    public Transform TargetPosition;

    protected void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        GameManager.Instance.OnReset += WorldReset;
        GameManager.Instance.OnFixedUpdateWorld += WorldUpdate;
        _visualInitPosition = Visual.transform.localPosition;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
        GameManager.Instance.OnFixedUpdateWorld -= WorldUpdate;
    }

    private void WorldReset()
    {
        Visual.SetActive(false);
        _isHidden = true;
        _leftFrame = -1;
        _leftTime = float.MinValue;
        _triggered = false;
    }

    private void Update()
    {
        if (!_isHidden && GameManager.Instance.IsReplaying)
        {
            Visual.SetActive(true);
            Visual.transform.localPosition = Vector3.Lerp(_visualInitPosition, TargetPosition.localPosition, Mathf.Clamp((Time.time - _leftTime) / 0.2f, 0.0f, 1.0f));
        }
    }

    private void WorldUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (!_collider.enabled) return;

        var result = _collider.OverlapColliderAll(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        });

        // Player touched it
        if (result.Count > 0)
        {
            if (!_triggered)
            {
                _triggered = true;
                if (_leftFrame != -1 && GameManager.Instance.Frame - _leftFrame > 3)
                {
                    GameManager.Instance.Die();
                }
            }
        }
        else
        {
            if (_triggered)
            {
                _triggered = false;
                if (_isHidden)
                {
                    _leftFrame = GameManager.Instance.Frame;
                    _leftTime = Time.time;
                    _isHidden = false;
                    AudioManager.Instance.Play("hidden_spike_show");
                    CaptionManager.Instance.ShowCaption("trap", 2.0f, CaptionType.Danger);
                }
            }
        }
    }
}
