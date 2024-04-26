using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField]
    WorldSettings settings;

    public ElementSprite elementSprite;

    [SerializeField]
    GameObject cellPref;
    [SerializeField]
    GameObject elementPref;

    [SerializeField]
    MeshRenderer meshRenderer;

    private void Start()
    {
        Grid.instance.SetMapSize(settings.width, settings.length);
        GenerateWorld();
    }

    private void GenerateWorld()
    {
        HeightMapGenerator.GenerateHeightMap(settings.width, settings.length, settings.maxElevation, settings.minElevation, out float[,] heightMap, out Vector2[,] slopeGradient, settings.cellSize);

        Grid.instance.Cells = new CellData[settings.width, settings.length];
        VisualiseHeightMap(ref heightMap, ref slopeGradient);
        for(int i = 0; i < settings.riverCount; i++)
        {
            Vector2Int[] river = WaterGenerator.GenerateWater(ref heightMap, out List<Vector2Int> lakes);
            Debug.LogWarning($"river: {river.Length} lakes: {lakes.Count}");
            VisualiseRivers(ref river);
            VisualiseLakes(ref lakes);
        }
    }

    public void VisualiseHeightMap(ref float[,] heightMap, ref Vector2[,] slopeMap)
    {
        //calc elevation range
        float elevationRange = settings.maxElevation - settings.minElevation;

        //get texture
        Texture2D texture = new(settings.width, settings.length);
        //set pixels
        for (int i = 0; i < settings.width; i++)
        {
            for (int j = 0; j < settings.length; j++)
            {
                texture.SetPixel(i, j, new Color(heightMap[i, j]/elevationRange, heightMap[i, j]/elevationRange, heightMap[i, j] / elevationRange));
            }
        }
        //apply texture
        texture.Apply();
        //set texture to mesh
        meshRenderer.material.mainTexture = texture;


        //generate cell Prefab
        for (int x = 0; x < settings.width; x++)
        {
            for (int z = 0; z < settings.length; z++)
            {
                Transform cell = Instantiate(cellPref, new Vector3(x, z, 0), Quaternion.identity).transform;
                cell.name = $"Cell_{x}_{z}";

                switch (heightMap[x, z])
                {
                    case float n when n < 0:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.water, slopeMap[x, z], cell);
                            continue;
                        }
                }

                switch (slopeMap[x, z].magnitude)
                {
                    case float n when n < 1f:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.earth, slopeMap[x, z], cell);
                            continue;
                        }
                    default:
                        {
                            Debug.Log("slope: " + slopeMap[x, z].magnitude);
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.stone, slopeMap[x, z], cell);
                            continue;
                        }
                }

            }
        }
    }

    public void VisualiseRivers(ref Vector2Int[] riverMap)
    {
        for (int i = 0; i < riverMap.Length; i++)
        {
            if (Grid.instance.Cells[riverMap[i].x, riverMap[i].y].Ground == GroundType.water)
            {
                break;
            }
            Vector2Int river = riverMap[i];
            //check if river is not on the water
            if (Grid.instance.Cells[river.x, river.y].Ground == GroundType.water)
            {
                break;
            }
            //create river object
            
            //calc river position
            Vector2 cross1, cross2;
            if(i == 0)
            {
                cross1 = riverMap[i];
            }
            else
            {
                cross1 = riverMap[i] + riverMap[i-1];
                cross1 /= 2;
            }

            if(i == riverMap.Length-1)
            {
                cross2 = riverMap[i];
            }
            else
            {
                cross2 = riverMap[i] + riverMap[i + 1];
                cross2 /= 2;
            }
            //calc angle beetwen points
            float angle = Mathf.Atan2(cross2.y - cross1.y, cross2.x - cross1.x) * Mathf.Rad2Deg;
            //calc distance beetwen points
            float distance = Vector2.Distance(cross1, cross2);
            Vector2 pos = (cross1 + cross2) / 2;


            Transform riverObj = Instantiate(elementPref, new Vector3(river.x, river.y, 0), Quaternion.identity).transform;
            riverObj.GetChild(0).localScale = new Vector3(distance, 1, 1);
            //set rotation from angle
            riverObj.GetChild(0).localRotation = Quaternion.Euler(0, 0, angle);
            riverObj.GetChild(0).position = new Vector2(pos.x, pos.y);
            //add river to cell
            Grid.instance.Cells[river.x, river.y].AddElement(Elements.river, riverObj);
        }
    }

    public void VisualiseLakes(ref List<Vector2Int> lakes)
    {
        for (int i = 0; i < lakes.Count; i++)
        {
            //get cell
            CellData cell = Grid.instance.Cells[lakes[i].x, lakes[i].y];
            //remove all elements
            cell.RemoveAllElements();
            //change ground to water
            cell.SetGroundType(GroundType.water);
        }
    }
}

public static class HeightMapGenerator
{
    public static void GenerateHeightMap(int width, int length, float maxElevation, float minElevation, out float[,] heightMap, out Vector2[,] gradientMap, float mainScale)
    {
        heightMap = new float[width, length];
        gradientMap = new Vector2[width, length];

        float highestPoint = 0;
        float lowestPoint = 10;

        for (int i = 0; i < 4; i++)
        {
            float scale = 1f / (i + 1);
            scale *= mainScale;
            float[,] newHeightMap = new float[width, length];

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    newHeightMap[x, z] = Mathf.PerlinNoise(((float)x / (float)width) * (scale), ((float)z / (float)length) * (scale));
                }
            }

            //combine height maps
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    heightMap[j, k] += newHeightMap[j, k] * scale;
                }
            }
        }

        //find highest point
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                if (heightMap[i, j] > highestPoint)
                {
                    highestPoint = heightMap[i, j];
                }
                if (heightMap[i, j] < lowestPoint)
                {
                    lowestPoint = heightMap[i, j];
                }
            }
        }

        float multiplier = (maxElevation - minElevation)/(highestPoint-lowestPoint);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                heightMap[i, j] = (heightMap[i, j]-lowestPoint) * multiplier + minElevation;
            }
        }
        gradientMap = CalcGradient(heightMap, mainScale);
    }

    private static Vector2[,] CalcGradient(float[,] heightMap, float cellSize)
    {
        //yeeee, I need to change this to calc gradient with gettin into account map size
        
        Vector2[,] gradient = new Vector2[heightMap.GetLength(0), heightMap.GetLength(1)];

        //get cells from around the current cell
        for (int i = 0; i < heightMap.GetLength(0); i++)
        {
            for (int j = 0; j < heightMap.GetLength(1); j++)
            {
                Vector2 gradientVector = new();
                if (i > 0 && j > 0 && i < heightMap.GetLength(0) - 1 && j < heightMap.GetLength(1) - 1)
                {
                    gradientVector.x = heightMap[i + 1, j] - heightMap[i - 1, j];
                    gradientVector.y = heightMap[i, j + 1] - heightMap[i, j - 1];
                    Debug.Log($"x: {gradientVector.x} y: {gradientVector.y}");
                    gradientVector /= 2;
                    gradientVector /= cellSize;
                    Debug.Log($"x: {gradientVector.x} y: {gradientVector.y} magnitude: {gradientVector.magnitude}");
                }
                gradient[i, j] = gradientVector;
            }
        }

        return gradient;
    }
}

public static class WaterGenerator
{
    public static Vector2Int[] GenerateWater(ref float[,] heightMap, out List<Vector2Int> lakes)
    {
        //get random point
        Vector2Int startPoint = new(UnityEngine.Random.Range(0, heightMap.GetLength(0)), UnityEngine.Random.Range(0, heightMap.GetLength(1)));
        return GenerateRiver(ref heightMap, startPoint, out lakes);
    }

    public static Vector2Int[] GenerateRiver(ref float[,] heightMap, Vector2Int startPoint, out List<Vector2Int> lakes)
    {
        lakes = new List<Vector2Int>();
        Vector2Int[] river = new Vector2Int[1];
        river[0] = startPoint;
        for (int i = 0; i < 1000; i++)
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
                List<Vector2Int> lake = GenerateLake(lowestNeighbour, 10);

                lakes.AddRange(lake);
                Debug.Log($"lake: {lakes.Count} break at i:{i}");

                break;
            }
            Array.Resize(ref river, river.Length + 1);
            river[^1] = lowestNeighbour;
        }

        return river;
    }


    //move this  to HeightMapService


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
        Debug.Log("lake: " + lake.Count);
        return lake;
    }
}


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

        //sort neighbours by height
        SortByHeight(ref neighbours);


        return neighbours;
    }
}

[CreateAssetMenu(fileName = "WorldSettings", menuName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    public int seed;

    public int width, length;
    public float maxElevation, minElevation;

    public float cellSize;

    public int riverCount;
}