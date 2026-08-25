using UnityEngine;
using System;

public class Timer
{
    private float time;
    public const float INACTIVE_TIME = -1;
    private Action<float> updateAction;
    private Action endAction;

    public Timer(float time = INACTIVE_TIME, Action<float> updateAction = null, Action endAction = null)
    {
        StartTimer(time, updateAction, endAction);
    }

    public void Update(float deltaTime)
    {
        if (!IsRunning()) return;

        if (time - deltaTime <= 0.0f)
        {
            time = 0.0f;
            if (endAction != null) endAction.Invoke();
        }
        else
        {
            time -= deltaTime;
            if (updateAction != null) updateAction.Invoke(deltaTime);
        }
    }

    public void StartTimer(float time, Action<float> updateAction = null, Action endAction = null)
    {
        this.time = time;
        this.updateAction = updateAction;
        this.endAction = endAction;
    }
    public void AddRunningTime(float time)
    {
        if (IsRunning()) this.time += time;
    }
    public void EndTimer()
    {
        time = 0.0f;
    }
    public void Reset()
    {
        time = INACTIVE_TIME;
    }

    public float GetTime() { return time; }
    public bool IsRunning() { return time > 0.0f; }
    public bool IsFinished() { return Mathf.Approximately(time, 0.0f); }
    public bool IsInactive() { return time < 0.0f; }
}