using UnityEngine;
using UnityEngine.EventSystems;

public class AudibleButton : MonoBehaviour, ISelectHandler
{
    public string SelectSound;

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is PointerEventData || eventData.GetType() == typeof(BaseEventData)) return;

        if (!string.IsNullOrEmpty(SelectSound))
            AudioManager.Instance.Play(SelectSound);
    }
}
