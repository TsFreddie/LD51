using UnityEngine;
using System.Collections.Generic;

namespace ForestRush.Whale
{
    [CreateAssetMenu(fileName = "New AudioBank", menuName = "Whale/AudioBank")]
    public class FRAudioBank : ScriptableObject
    {
        public List<FRAudioEvent> AudioEvents;
    }
}
