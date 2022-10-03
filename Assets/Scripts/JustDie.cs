using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LogicTrigger))]
public class JustDie : Switchable
{
    public override void Trigger()
    {
        GameManager.Instance.Die();
    }
}
