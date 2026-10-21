using UnityEngine;

public static class TargetSelectionPolicy
{
    public const float AlignmentTieThreshold = 0.02f;

    public static bool IsBetterCandidate(
        float alignment,
        float sqrDistance,
        float bestAlignment,
        float bestSqrDistance,
        bool hasBestCandidate)
    {
        if (!hasBestCandidate)
        {
            return true;
        }

        if (alignment > bestAlignment + AlignmentTieThreshold)
        {
            return true;
        }

        bool hasSimilarAlignment =
            Mathf.Abs(alignment - bestAlignment) <= AlignmentTieThreshold;

        return hasSimilarAlignment && sqrDistance < bestSqrDistance;
    }
}
