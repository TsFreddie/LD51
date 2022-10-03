using UnityEngine;

public class BankLoader : MonoBehaviour
{
    public AudioBank Bank;

    void Start()
    {
        AudioManager.Instance.LoadAudioBank(Bank);
    }

    void OnDestroy()
    {
        AudioManager.Instance.UnloadAudioBank(Bank);
    }
}
