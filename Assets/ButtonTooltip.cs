using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTooltip : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public GameObject TargetTooltip;

    public void OnSelect(BaseEventData eventData)
    {
        TargetTooltip.SetActive(true);
    }
    public void OnDeselect(BaseEventData eventData)
    {
        TargetTooltip.SetActive(false);
    }
}
