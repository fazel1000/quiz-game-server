using System.Collections.Generic;
using UnityEngine;

public class DTWCalculator
{
    public float Calculate(List<float[]> a, List<float[]> b)
    {
        if (a.Count == 0 || b.Count == 0)
            return 1f;

        float[,] cost = new float[a.Count + 1, b.Count + 1];

        for(int i=0;i<=a.Count;i++)
            for(int j=0;j<=b.Count;j++)
                cost[i,j] = float.MaxValue;

        cost[0,0] = 0;

        for(int i=1;i<=a.Count;i++)
        {
            for(int j=1;j<=b.Count;j++)
            {
                float distance = Distance(a[i-1], b[j-1]);

                cost[i,j] = distance +
                    Mathf.Min(
                        cost[i-1,j],
                        Mathf.Min(cost[i,j-1], cost[i-1,j-1])
                    );
            }
        }

        return cost[a.Count,b.Count] /
               Mathf.Max(a.Count,b.Count);
    }

    private float Distance(float[] x, float[] y)
    {
        float sum = 0;

        for(int i=0;i<x.Length;i++)
        {
            float d=x[i]-y[i];
            sum += d*d;
        }

        return Mathf.Sqrt(sum);
    }
}