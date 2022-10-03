using UnityEngine;
using System;

[RequireComponent(typeof(LogicTrigger))]
public class Checkpoint : Switchable
{
    public int CheckpointProgress;
    public Transform CameraPosition;

    public void Awake()
    {
        GameManager.Instance.OnReset += WorldReset;
    }

    public void OnDestroy()
    {
        GameManager.Instance.OnReset -= WorldReset;
    }

    private void WorldReset()
    {
        gameObject.SetActive(GameManager.Instance.CheckpointProgress < CheckpointProgress);
    }

    public override void Trigger()
    {
        // Only finish the level if the checkpoint is active
        if (GameManager.Instance.State != GameManager.GameState.Replaying) return;
        
        GameManager.Instance.Checkpoint(this);
    }
}
