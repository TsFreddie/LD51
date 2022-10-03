using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAudio : MonoBehaviour
{
    
    private Collider2D testAudio
        ;
    private List<Collider2D> _colliders;

    public AudioBank bank;
    
    protected void Awake()
    {
        AudioManager.Instance.LoadAudioBank(bank);
        GameManager.Instance.OnFixedUpdateWorld += TestAudioUpdate;
    }

    // Start is called before the first frame update
    void Start()
    {
        testAudio = GetComponent<BoxCollider2D>();
        _colliders = new List<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void TestAudioUpdate()
    {
        testAudio.OverlapCollider(new ContactFilter2D()
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask("Player")
        }, _colliders);
        
        if (_colliders.Count > 0)
        {
            //播放声音
            MusicManager.Instance.Play("BGM");
        }
    }
}
