using UnityEngine;
using UnityEngine.InputSystem;

public static class Utils
{
    public static bool VecApproxEquals(Vector3 vec0, Vector3 vec1)
    {
        for (int i=0; i<3; i++)
        {
            if (!Mathf.Approximately(vec0[i], vec1[i]))
                return false; 
        }
        return true;
    }

    public static bool IsActionHeld(InputAction action)
    {
        return action.IsPressed() &&
               action.GetTimeoutCompletionPercentage() >= 1.0f;
    }
}