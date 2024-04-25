using System;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField]
    WorldSettings settings;

    [SerializeField]
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
        Vector2[,] slopeGradient;
        float[,] heightMap;
        HeightMapGenerator.GenerateHeightMap(settings.width, settings.length, settings.maxElevation, settings.minElevation, out heightMap, out slopeGradient, settings.mainScale);

        Grid.instance.Cells = new CellData[settings.width, settings.length];
        VisualiseHeightMap(ref heightMap, ref slopeGradient);
        for(int i = 0; i < settings.riverCount; i++)
        {
            Vector2Int[] river = WaterGenerator.GenerateWater(ref heightMap);
            VisualiseRivers(ref river);
        }
    }

    public void VisualiseHeightMap(ref float[,] heightMap, ref Vector2[,] slopeMap)
    {
        //calc elevation range
        float elevationRange = settings.maxElevation - settings.minElevation;

        //get texture
        Texture2D texture = new Texture2D(settings.width, settings.length);
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
                            break;
                        }
                }

                switch (slopeMap[x, z].magnitude)
                {
                    case float n when n < 0.4f:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.earth, slopeMap[x, z], cell);
                            continue;
                            break;
                        }
                    default:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.stone, slopeMap[x, z], cell);
                            continue;
                            break;
                        }
                }

            }
        }
    }

    public void VisualiseRivers(ref Vector2Int[] riverMap)
    {
        for (int i = 0; i < riverMap.Length; i++)
        {
            Vector2Int river = riverMap[i];
            //check if river is not on the water
            if (Grid.instance.Cells[river.x, river.y].ground == GroundType.water)
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
            Debug.Log($"cross1: {cross1}, cross2: {cross2}");
            //calc angle beetwen points
            float angle = Mathf.Atan2(cross2.y - cross1.y, cross2.x - cross1.x) * Mathf.Rad2Deg;
            //calc distance beetwen points
            float distance = Vector2.Distance(cross1, cross2);
            Vector2 pos = (cross1 + cross2) / 2;
            Debug.Log($"angle: {angle}, distance: {distance}, pos: {pos}");


            Transform riverObj = Instantiate(elementPref, new Vector3(river.x, river.y, 0), Quaternion.identity).transform;
            riverObj.GetChild(0).localScale = new Vector3(distance, 1, 1);
            //set rotation from angle
            riverObj.GetChild(0).localRotation = Quaternion.Euler(0, 0, angle);
            riverObj.GetChild(0).position = new Vector3(pos.x, pos.y, 0);
            //add river to cell
            Grid.instance.Cells[river.x, river.y].AddElement(Elements.river, riverObj);
            Debug.Log("river: " + river);
        }
    }
}

public static class HeightMapGenerator
{
    public static void GenerateHeightMap(int width, int length, float maxElevation, float minElevation, out float[,] heightMap, out Vector2[,] gradientMap, float mainScale)
    {
        //List<float[,]> heightMapLayers = new List<float[,]>();
        //List<Vector2[,]> slopeLayers = new List<Vector2[,]>();
        //List<float> scale = new List<float>();

        heightMap = new float[width, length];
        gradientMap = new Vector2[width, length];

        float highestPoint = 0;
        float lowestPoint = 10;

        for (int i = 0; i < 4; i++)
        {
            float scale = 1f / (i + 1);
            scale *= 1/mainScale;
            //scale.Add(1f/i); 
            float[,] newHeightMap = new float[width, length];
            Vector2[,] newGradientMap;

            //heightMapLayers.Add(new float[width, length]);
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < length; z++)
                {
                    newHeightMap[x, z] = Mathf.PerlinNoise((float)x / (float)width * (scale), (float)z / (float)length * (scale));
                }
            }
            ////calc gradinet for first layer
            newGradientMap = (CalcGradient(newHeightMap));

            //combine height maps and gradients
            for (int j = 0; j < width; j++)
            {
                for (int k = 0; k < length; k++)
                {
                    heightMap[j, k] += newHeightMap[j, k] * scale;
                    gradientMap[j, k] += newGradientMap[j, k] * scale;
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

        //Debug.LogWarning("hightstPoint: " + highestPoint);
        //Debug.LogWarning("lowestPoint: " + lowestPoint);
        //normalize height map

        float multiplier = (maxElevation - minElevation)/(highestPoint-lowestPoint);
        //Debug.Log("multiplier: " + multiplier);
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                //Debug.Log("heightMap before: " + heightMap[i, j]);
                heightMap[i, j] = (heightMap[i, j]-lowestPoint) * multiplier + minElevation;
                //Debug.Log("heightMap after: " + heightMap[i, j]);

                if (heightMap[i, j] < 0)
                {
                    //Debug.LogWarning("heightMap: " + heightMap[i, j]);
                }
                else
                {
                    //Debug.Log("heightMap: " + heightMap[i, j]);
                }
            }
        }
    }

    private static Vector2[,] CalcGradient(float[,] heightMap)
    {
        Vector2[,] gradient = new Vector2[heightMap.GetLength(0), heightMap.GetLength(1)];
        //get cells from around the current cell
        for (int i = 0; i < heightMap.GetLength(0); i++)
        {
            for (int j = 0; j < heightMap.GetLength(1); j++)
            {
                Vector2 gradientVector = new Vector2();
                if (i > 0 && j > 0 && i < heightMap.GetLength(0) - 1 && j < heightMap.GetLength(1) - 1)
                {
                    gradientVector.x = heightMap[i + 1, j] - heightMap[i - 1, j];
                    gradientVector.y = heightMap[i, j + 1] - heightMap[i, j - 1];
                }
                gradient[i, j] = gradientVector;
            }
        }

        return gradient;
    }
}

public static class WaterGenerator
{
    public static Vector2Int[] GenerateWater(ref float[,] heightMap)
    {
        //get random point
        Vector2Int startPoint = new Vector2Int(UnityEngine.Random.Range(0, heightMap.GetLength(0)), UnityEngine.Random.Range(0, heightMap.GetLength(1)));
        return GenerateRiver(ref heightMap, startPoint, out Vector2Int[]lakes);
    }

    public static Vector2Int[] GenerateRiver(ref float[,] heightMap, Vector2Int startPoint, out Vector2Int[] lakes)
    {
        lakes = new Vector2Int[0];
        Vector2Int[] river = new Vector2Int[1];
        river[0] = startPoint;
        for (int i = 0; i < 1000; i++)
        {
            Vector2Int currentPoint = river[river.Length - 1];
            //get lowest neighbour
            Vector2Int[] neighbour = GetLowestNeighbour(currentPoint, ref heightMap);

            //check if lowest neighbour is water or is out of bounds or is the same as current point
            Vector2Int lowestNeighbour = new Vector2Int();
            for (int j = 0; j < neighbour.Length; j++)
            {
                if
                (
                    Grid.instance.Cells[neighbour[j].x, neighbour[j].y].ground == GroundType.water ||
                    neighbour[j].x < 0 || neighbour[j].x >= heightMap.GetLength(0) ||
                    neighbour[j].y < 0 || neighbour[j].y >= heightMap.GetLength(1)
                )
                {
                    continue;
                }
                else
                {
                    if (heightMap[neighbour[j].x, neighbour[j].y] == heightMap[currentPoint.x, currentPoint.y])
                    {
                        break;
                        //end generating river
                        //generate lake
                    }
                    lowestNeighbour = neighbour[j];
                    break;
                }
            }

            //if lowest neighbour is higher than current point break
            if (heightMap[lowestNeighbour.x, lowestNeighbour.y] >= heightMap[currentPoint.x, currentPoint.y])
            {
                Debug.Log("break at: " + i);
                break;
            }
            Array.Resize(ref river, river.Length + 1);
            river[river.Length - 1] = lowestNeighbour;
        }

        return river;
    }

    private static Vector2Int[] GetLowestNeighbour(Vector2Int point, ref float[,] heightMap)
    {
        Vector2Int[] neighbours = new Vector2Int[0];

        //get cells from around the current cell
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if ((i == 0 && j == 0) || i+point.x < 0 || i+point.x >= heightMap.GetLength(0) || j+point.y < 0 || j+point.y >= heightMap.GetLength(1))
                {
                    continue;
                }
                Vector2Int neighbour = new Vector2Int(point.x + i, point.y + j);
                Array.Resize(ref neighbours, neighbours.Length + 1);
                neighbours[neighbours.Length - 1] = neighbour;
            }
        }

        //sort neighbours by height
        HeightMapService.SortByHeight(ref neighbours);


        return neighbours;

    }

    private static Vector2Int[] GenerateLake(Vector2Int startPoint, int minSize)
    {
        int currentSize = 0;
        do
        {
            
        }while(currentSize < minSize);


        return null;
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
                    Vector2Int temp = cells[i];
                    cells[i] = cells[j];
                    cells[j] = temp;
                }
            }
        }
    }
}

[CreateAssetMenu(fileName = "WorldSettings", menuName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    public int seed;

    public int width, length;
    public float maxElevation, minElevation;

    public float mainScale;

    public int riverCount;
}