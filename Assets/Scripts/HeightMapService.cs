using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class HeightMapService
{
    public static void SortByHeight(ref Vector2Int[] cells)
    {
        float[,] heightMap = Grid.instance.GetHeightMap();
        for (int i = 0; i < cells.Length; i++)
        {
            for (int j = 0; j < cells.Length; j++)
            {
                if (heightMap[cells[i].x, cells[i].y] < heightMap[cells[j].x, cells[j].y])
                {
                    (cells[j], cells[i]) = (cells[i], cells[j]);
                }
            }
        }
    }

    public static void SortByHeight(ref List<Vector2Int> cells)
    {
        float[,] heightMap = Grid.instance.GetHeightMap();
        for (int i = 0; i < cells.Count; i++)
        {
            for (int j = 0; j < cells.Count; j++)
            {
                if (heightMap[cells[i].x, cells[i].y] < heightMap[cells[j].x, cells[j].y])
                {
                    (cells[j], cells[i]) = (cells[i], cells[j]);
                }
            }
        }
        cells.ToList();
    }

    public static Vector2Int[] GetLowestNeighbour(Vector2Int point, ref float[,] heightMap)
    {
        Vector2Int[] neighbours = new Vector2Int[0];

        //get cells from around the current cell
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if ((i == 0 && j == 0) || i + point.x < 0 || i + point.x >= heightMap.GetLength(0) || j + point.y < 0 || j + point.y >= heightMap.GetLength(1))
                {
                    continue;
                }
                Vector2Int neighbour = new(point.x + i, point.y + j);
                Array.Resize(ref neighbours, neighbours.Length + 1);
                neighbours[^1] = neighbour;
            }
        }

        //sort neighbours by height
        SortByHeight(ref neighbours);


        return neighbours;
    }

    public static Vector2Int[] GetLowestStrictNeighbour(Vector2Int point, ref float[,] heightMap)
    {
        Vector2Int[] neighbours = new Vector2Int[4];

        neighbours[0] = new Vector2Int(point.x + 1, point.y);
        neighbours[1] = new Vector2Int(point.x - 1, point.y);
        neighbours[2] = new Vector2Int(point.x, point.y + 1);
        neighbours[3] = new Vector2Int(point.x, point.y - 1);

        //check if neighbours are not out of bounds
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbours[i].x < 0 || neighbours[i].x >= heightMap.GetLength(0) || neighbours[i].y < 0 || neighbours[i].y >= heightMap.GetLength(1))
            {
                //remove out of bounds neighbour
                neighbours[i] = neighbours[^1];
                Array.Resize(ref neighbours, neighbours.Length - 1);
                i--;
            }
        }

        //sort neighbours by height
        SortByHeight(ref neighbours);


        return neighbours;
    }

    
}