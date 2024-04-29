using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class WaterGenerator
{
    public static List<Vector2Int> GenerateWater(ref float[,] heightMap, out List<Vector2Int> lakes)
    {
        //get random point
        Vector2Int startPoint = new(UnityEngine.Random.Range(0, heightMap.GetLength(0)), UnityEngine.Random.Range(0, heightMap.GetLength(1)));
        return GenerateRiver(ref heightMap, startPoint, out lakes).ToList();
    }

    public static Vector2Int[] GenerateRiver(ref float[,] heightMap, Vector2Int startPoint, out List<Vector2Int> lakes)
    {
        lakes = new List<Vector2Int>();
        Vector2Int[] river = new Vector2Int[1];
        river[0] = startPoint;
        while (true)
        {
            Vector2Int currentPoint = river[^1];
            //get lowest neighbour
            Vector2Int[] neighbour = HeightMapService.GetLowestNeighbour(currentPoint, ref heightMap);

            //check if lowest neighbour is water or is out of bounds or is the same as current point
            Vector2Int lowestNeighbour = new();
            for (int j = 0; j < neighbour.Length; j++)
            {
                if
                (
                    Grid.instance.Cells[neighbour[j].x, neighbour[j].y].Ground == GroundType.water ||
                    neighbour[j].x < 0 || neighbour[j].x >= heightMap.GetLength(0) ||
                    neighbour[j].y < 0 || neighbour[j].y >= heightMap.GetLength(1)
                )
                {
                    continue;
                }
                else
                {
                    lowestNeighbour = neighbour[j];
                    break;
                }
            }

            //if lowest neighbour is higher than current point break
            if (heightMap[lowestNeighbour.x, lowestNeighbour.y] >= heightMap[currentPoint.x, currentPoint.y])
            {
                List<Vector2Int> lake = GenerateLake(lowestNeighbour, 4);

                lakes.AddRange(lake);
                //Debug.Log($"lake: {lakes.Count} break at i:{i}");

                break;
            }
            Array.Resize(ref river, river.Length + 1);
            river[^1] = lowestNeighbour;
        }

        return river;
    }

    public static List<Vector2Int> GenerateLake(Vector2Int startPoint, int minSize)
    {
        float[,] heightMap = Grid.instance.GetHeightMap();
        List<Vector2Int> lake = new();
        List<Vector2Int> neighbour = new()
        {
            startPoint
        };
        do
        {
            for (int j = 0; j < 1000; j++)
            {
                lake.Add(neighbour[0]);
                neighbour.RemoveAt(0);

                neighbour.AddRange(HeightMapService.GetLowestStrictNeighbour(lake[^1], ref heightMap));

                //check if neighbours are not duplicates with lake and with neighbour
                HeightMapService.SortByHeight(ref neighbour);
                neighbour.RemoveAll(x => lake.Contains(x));
                for (int i = 0; i < neighbour.Count-1; i++)
                {
                    if (neighbour[i] == neighbour[i + 1])
                    {
                        neighbour.RemoveAt(i);
                    }
                }

                if (heightMap[neighbour[0].x, neighbour[0].y] > heightMap[lake[0].x, lake[0].y])
                {
                    break;
                }
            }
        } while (lake.Count < minSize);
        //Debug.Log("lake: " + lake.Count);
        return lake;
    }
}
