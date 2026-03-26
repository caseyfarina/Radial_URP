using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

/// <summary>
/// Burst-compiled job for calculating quadratic Bezier curve points
/// for the CurvedConnectionDrawer line connections.
/// </summary>
[BurstCompile]
public struct CalculateCurvePointsJob : IJob
{
    [ReadOnly] public NativeArray<float3> SourcePosition;
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> CurveDirections;
    [WriteOnly] public NativeArray<float3> LinePoints;

    public float CurvatureAmount;
    public int LineSegments;
    public int ActiveCount;

    // Line trimming parameters
    public float SourceTrimPercentage;
    public float TargetTrimPercentage;
    public int UseFixedDistance; // Using int as bool in Burst jobs
    public float SourceTrimDistance;
    public float TargetTrimDistance;

    public void Execute()
    {
        float3 startPos = SourcePosition[0];

        for (int i = 0; i < ActiveCount; i++)
        {
            float3 endPos = TargetPositions[i];
            DrawCurvedLine(i, startPos, endPos);
        }
    }

    private void DrawCurvedLine(int lineIndex, float3 start, float3 end)
    {
        float3 direction = end - start;
        float totalDistance = math.length(direction);

        if (totalDistance < 0.001f)
        {
            for (int i = 0; i < LineSegments; i++)
            {
                int pointIndex = lineIndex * LineSegments + i;
                LinePoints[pointIndex] = start;
            }
            return;
        }

        float3 perpendicular = CurveDirections[lineIndex];

        float3 middle = start + direction * 0.5f + perpendicular * totalDistance * CurvatureAmount * 0.25f;

        float sourceTrim, targetTrim;

        if (UseFixedDistance == 1)
        {
            sourceTrim = math.min(SourceTrimDistance, totalDistance * 0.45f);
            targetTrim = math.min(TargetTrimDistance, totalDistance * 0.45f);
        }
        else
        {
            sourceTrim = totalDistance * SourceTrimPercentage;
            targetTrim = totalDistance * TargetTrimPercentage;

            float maxTotalTrim = totalDistance * 0.9f;
            if (sourceTrim + targetTrim > maxTotalTrim)
            {
                float scale = maxTotalTrim / (sourceTrim + targetTrim);
                sourceTrim *= scale;
                targetTrim *= scale;
            }
        }

        for (int i = 0; i < LineSegments; i++)
        {
            float t = i / (float)(LineSegments - 1);
            int pointIndex = lineIndex * LineSegments + i;

            if (sourceTrim > 0 || targetTrim > 0)
            {
                float trimStartT = sourceTrim / totalDistance;
                float trimEndT = 1 - (targetTrim / totalDistance);

                if (trimStartT >= trimEndT)
                {
                    trimStartT = 0;
                    trimEndT = 1;
                }

                t = trimStartT + t * (trimEndT - trimStartT);
            }

            LinePoints[pointIndex] = CalculateBezierPoint(start, middle, end, t);
        }
    }

    private float3 CalculateBezierPoint(float3 p0, float3 p1, float3 p2, float t)
    {
        float u = 1.0f - t;
        float tt = t * t;
        float uu = u * u;

        float3 point = uu * p0;
        point += 2.0f * u * t * p1;
        point += tt * p2;

        return point;
    }
}
