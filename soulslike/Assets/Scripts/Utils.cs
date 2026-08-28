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

    /// <summary>
    /// A different variant of the Vector3.MoveTowards function except this separates the max move value into the magnitude
    /// and movetime as well as exposing the rate of change that was used for the movement
    /// </summary>
    /// <param name="currentVector"></param>
    /// <param name="targetVector"></param>
    /// <param name="maxRateOfChangeMagnitude"></param>
    /// <param name="moveTime"></param>
    /// <param name="rateOfChangeUsed"></param>
    /// <returns></returns>
    public static Vector3 MoveTowards(Vector3 currentVector, Vector3 targetVector, float maxRateOfChangeMagnitude, 
        float moveTime, ref Vector3 rateOfChangeUsed)
    {
        if (moveTime <= 0f)
        {
            rateOfChangeUsed = Vector3.zero;
            return currentVector;
        }

        Vector3 requiredRateOfChange = (targetVector - currentVector) / moveTime;
        if (requiredRateOfChange.sqrMagnitude > maxRateOfChangeMagnitude * maxRateOfChangeMagnitude)
        {
            rateOfChangeUsed = requiredRateOfChange.normalized * maxRateOfChangeMagnitude;
        }
        else
        {
            rateOfChangeUsed = requiredRateOfChange;
        }
        return currentVector + rateOfChangeUsed * moveTime;
    }

    public static bool IsActionHeld(InputAction action)
    {
        return action.IsPressed() &&
               action.GetTimeoutCompletionPercentage() >= 1.0f;
    }
}