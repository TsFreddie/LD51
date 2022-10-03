using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enabler : Switchable
{
    public override void Trigger()
    {
        base.Trigger();
        GameManager.Instance.Ending();
    }
}
