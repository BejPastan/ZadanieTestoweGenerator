using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Grid : MonoBehaviour
{
    public static Grid instance;

    private void Awake()
    {
        instance = this;
    }

    public CellData[,] Cells;

    public void SetMapSize(int width, int length)
    {
        Cells = new CellData[width, length];
    }

    public float[,] GetHeightMap()
    {
        float[,] heightMap = new float[Cells.GetLength(0), Cells.GetLength(1)];
        for (int i = 0; i < Cells.GetLength(0); i++)
        {
            for (int j = 0; j < Cells.GetLength(1); j++)
            {
                heightMap[i, j] = Cells[i, j].Height;
            }
        }
        return heightMap;
    }

    public int GetWidth()
    {
        return Cells.GetLength(0);
    }

    public int GetLength()
    {
        return Cells.GetLength(1);
    }

    public CellData GetCellData(int x, int y)
    {
        if(x < 0 || x >= Cells.GetLength(0) || y < 0 || y >= Cells.GetLength(1))
        {
            return null;
        }
        return Cells[x, y];
    }

    public List<Vector2Int> GetPath(Vector2Int start, Vector2Int end)
    {
        // A* algorithm
        List<Vector2Int> openSet = new List<Vector2Int>();
        List<Vector2Int> closedSet = new List<Vector2Int>();
        float[,] gCost = new float[Cells.GetLength(0), Cells.GetLength(1)];
        float[,] hCost = new float[Cells.GetLength(0), Cells.GetLength(1)];
        float[,] fCost = new float[Cells.GetLength(0), Cells.GetLength(1)];
        Vector2Int[,] cameFrom = new Vector2Int[Cells.GetLength(0), Cells.GetLength(1)];

        for (int i = 0; i < Cells.GetLength(0); i++)
        {
            for (int j = 0; j < Cells.GetLength(1); j++)
            {
                gCost[i, j] = Mathf.Infinity;
                hCost[i, j] = Vector2Int.Distance(new Vector2Int(i, j), end);
                fCost[i, j] = Mathf.Infinity;
            }
        }

        openSet.Add(start);
        while(openSet.Count>0)
        {
            //find the node in openSet with the lowest fScore
            Vector2Int current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (hCost[openSet[i].x, openSet[i].y] < hCost[current.x, current.y])
                {
                    current = openSet[i];
                }
            }
            List<Vector2Int> neighbours = new List<Vector2Int>();

            //get neighbours
            for(int i = -1; i <= 1; i++)
            {
                for(int j = -1; j <= 1; j++)
                {
                    if(i == 0 && j == 0)
                    {
                        continue;
                    }
                    if(current.x + i >= 0 && current.x + i < Cells.GetLength(0) && current.y + j >= 0 && current.y + j < Cells.GetLength(1))
                    {
                        neighbours.Add(new Vector2Int(current.x + i, current.y + j));
                    }
                }
            }

            //remove neighbours that are not walkable or are in closedSet
            for (int i = 0; i < neighbours.Count; i++)
            {
                if (Cells[neighbours[i].x, neighbours[i].y].Ground == GroundType.water)
                {
                    if (closedSet.Contains(neighbours[i]))
                    {
                        neighbours.RemoveAt(i);
                        i--;
                        continue;
                    }
                    closedSet.Add(neighbours[i]);
                    neighbours.RemoveAt(i);
                    i--;
                }
            }

            foreach(Vector2Int neighbour in neighbours)
            {
                if(neighbour == end)
                {
                    Vector2Int parent;
                    List<Vector2Int> path = new List<Vector2Int>();
                    path.Add(neighbour);
                    parent = current;
                    while(parent != start)
                    {
                        path.Add(parent);
                        parent = cameFrom[parent.x, parent.y];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }
                
                float gDist = Vector2Int.Distance(neighbour, current);
                float fDist = gDist + hCost[neighbour.x, neighbour.y];

                if(fDist < fCost[neighbour.x, neighbour.y])
                {
                    cameFrom[neighbour.x, neighbour.y] = current;
                    gCost[neighbour.x, neighbour.y] = gDist;
                    fCost[neighbour.x, neighbour.y] = fDist;
                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
            closedSet.Add(current);
            openSet.Remove(current);
        }

        Debug.LogError("Endless loop protection");
        return null;
    }
}
