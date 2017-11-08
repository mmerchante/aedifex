using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A grid based spatial data structure that lets the user find interest points 
/// fast based on position, while also accumulating importance on each cluster
/// </summary>
public class InterestPointGrid : MonoBehaviour
{
    public int resolution = 64;
    public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 1024f);

    public bool enableGizmos = false;
    
    // TODO: For now, I won't optimize this much...
    // ideally this should be a very flat structure with a hard max per cell
    private struct Cell
    {
        public List<InterestPoint> points;
        public float importanceSum;
    }

    private Cell[] grid;
    private Dictionary<InterestPoint, List<int>> indexMap = new Dictionary<InterestPoint, List<int>>(); // The other way around
    private Vector3 cellSize;
    private Vector3 inverseCellSize;

    private float totalImportanceSum = 0f;

    private void Awake()
    {
        grid = new Cell[resolution * resolution * resolution];
        cellSize = bounds.size / resolution;
        inverseCellSize = new Vector3(1f / cellSize.x, 1f / cellSize.y, 1f / cellSize.z);
    }

    public bool ContainsPoint(Vector3 p)
    {
        Vector3 localP = Vector3.Scale(p - bounds.min, inverseCellSize);
        int x = Mathf.FloorToInt(localP.x);
        int y = Mathf.FloorToInt(localP.y);
        int z = Mathf.FloorToInt(localP.z);
        return (x >= 0 && x < resolution) && (y >= 0 && y < resolution) && (z >= 0 && z < resolution);
    }

    protected int GetFlatIndexForIndices(int x, int y, int z)
    {
        x = Mathf.Clamp(x, 0, resolution - 1);
        y = Mathf.Clamp(y, 0, resolution - 1);
        z = Mathf.Clamp(z, 0, resolution - 1);

        return x + y * resolution + z * resolution * resolution;
    }
        
    public Vector3 ClampIndices(Vector3 p)
    {
        return Vector3.Max(Vector3.zero, Vector3.Min(p, Vector3.one * (resolution - 1f)));
    }

    public Vector3 GetIndicesForPosition(Vector3 p)
    {
        Vector3 localP = Vector3.Scale(p - bounds.min, inverseCellSize);

        int x = Mathf.Clamp(Mathf.RoundToInt(localP.x), 0, resolution - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(localP.y), 0, resolution - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(localP.z), 0, resolution - 1);

        return new Vector3(x, y, z);
    }

    protected int GetFlatIndexForPosition(Vector3 p)
    {
        // Translate to [0, resolution] first
        Vector3 localP = Vector3.Scale(p - bounds.min, inverseCellSize);

        int x = Mathf.Clamp(Mathf.FloorToInt(localP.x), 0, resolution - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(localP.y), 0, resolution - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(localP.z), 0, resolution - 1);

        return GetFlatIndexForIndices(x, y, z);
    }

    public float GetImportanceSumForPosition(Vector3 p)
    {
        int index = GetFlatIndexForPosition(p);
        return grid[index].importanceSum;
    }

    public float GetAverageImportanceForPosition(Vector3 p)
    {
        int index = GetFlatIndexForPosition(p);

        if(grid[index].points != null && grid[index].points.Count > 0)
            return grid[index].importanceSum / grid[index].points.Count;

        return 0f;
    }

    public void RemoveInterestPoint(InterestPoint p)
    {
        if (indexMap.ContainsKey(p))
        {
            foreach (int i in indexMap[p])
            {
                if (grid[i].points.Remove(p))
                {
                    grid[i].importanceSum -= p.importance;
                    totalImportanceSum -= p.importance;
                }
            }

            indexMap.Remove(p);
        }
    }

    private void OnDrawGizmos()
    {
        if (!enableGizmos)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (!Application.isPlaying)
            return;

        for(int z = 0; z < resolution; z++)
        {
            for(int y = 0; y < resolution; y++)
            {
                for(int x = 0; x < resolution; x++)
                {
                    float normalizedImportance = grid[GetFlatIndexForIndices(x, y, z)].importanceSum / totalImportanceSum;

                    if (normalizedImportance > 0.0001f)
                    {
                        Gizmos.color = Color.green * normalizedImportance * resolution * 10f;
                        Gizmos.DrawWireCube(bounds.center + Vector3.Scale(cellSize, new Vector3(x - resolution / 2, y - resolution / 2, z - resolution / 2)), cellSize * .99f);
                    }
                }
            }
        }
    }
    
    public void AddInterestPoint(InterestPoint p)
    {
        RemoveInterestPoint(p);
        
        Bounds b = p.GetBounds();
        Vector3 minIndices = GetIndicesForPosition(b.min);
        Vector3 maxIndices = GetIndicesForPosition(b.max);

        List<int> cellsTouched = new List<int>();

        for (int z = (int)minIndices.z; z <= (int)maxIndices.z; ++z)
        {
            for (int y = (int)minIndices.y; y <= (int)maxIndices.y; ++y)
            {
                for (int x = (int)minIndices.x; x <= (int)maxIndices.x; ++x)
                {
                    int index = GetFlatIndexForIndices(x, y, z);
                    
                    if (grid[index].points == null)
                        grid[index].points = new List<InterestPoint>();

                    grid[index].points.Add(p);
                    grid[index].importanceSum += p.importance;

                    totalImportanceSum += p.importance;
                    cellsTouched.Add(index);
                }
            }
        }


        indexMap[p] = cellsTouched;
    }
}
