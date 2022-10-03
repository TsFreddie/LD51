using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New AudioBank", menuName = "Whale/AudioBank")]
public class AudioBank : ScriptableObject
{
    public List<AudioEvent> AudioEvents;
}
