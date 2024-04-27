using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
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

    private async void GenerateWorld()
    {
        HeightMapGenerator.GenerateHeightMap(settings.width, settings.length, settings.maxElevation, settings.minElevation, out float[,] heightMap, out Vector2[,] slopeGradient, settings.cellSize, settings.seed);

        Grid.instance.Cells = new CellData[settings.width, settings.length];
        VisualiseHeightMap(ref heightMap, ref slopeGradient);
        List<Vector2Int> rivers = new();
        for(int i = 0; i < settings.riverCount; i++)
        {
            Vector2Int[] river = WaterGenerator.GenerateWater(ref heightMap, out List<Vector2Int> lakes);
            //Debug.LogWarning($"river: {river.Length} lakes: {lakes.Count}");
            VisualiseRivers(ref river);
            rivers.AddRange(river);
            VisualiseLakes(ref lakes);
        }
        List<Vector2Int> forests = ElementsGeneration.GenerateForests(ref rivers);
        VisualiseElement(ref forests, Elements.forest, new GroundType[] { GroundType.earth });

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
                            //here goes seelcting ground texture based on height
                            Debug.Log($"slope: {slopeMap[x, z].magnitude} create earth");
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
                //remove all elements after from array
                Array.Resize(ref riverMap, i);
                break;
            }
            Vector2Int river = riverMap[i];
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

    public void VisualiseElement(ref List<Vector2Int> elements, Elements elementType, GroundType[] allowedGrounds)
    {
        
        //Debug.Log($"elements: {elements.Count}");
        foreach(Vector2Int element in elements)
        {
            if (allowedGrounds.Contains(Grid.instance.Cells[element.x, element.y].Ground) == false)
            {
                //Debug.LogWarning("wrong ground");
                continue;
            }
            //get cell
            CellData cell = Grid.instance.Cells[element.x, element.y];
            //create element object
            Transform elementObj = Instantiate(elementPref, new Vector3(element.x, element.y, 0), Quaternion.identity).transform;
            elementObj.name = $"element_{elementType}_{element.x}_{element.y}";
            //add element to cell
            cell.AddElement(elementType, elementObj);
        }
    }
}

public static class HeightMapGenerator
{
    public static void GenerateHeightMap(int width, int length, float maxElevation, float minElevation, out float[,] heightMap, out Vector2[,] gradientMap, float mainScale, float seed)
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
                    newHeightMap[x, z] = Mathf.PerlinNoise(((float)x / (float)width) * (scale) + seed, ((float)z / (float)length) * (scale) + seed);
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
        gradientMap = MakeGradientMap(heightMap, mainScale);
        Erode(ref heightMap, mainScale, gradientMap);
        gradientMap = MakeGradientMap(heightMap, mainScale);
    }

    private static Vector2[,] MakeGradientMap(float[,] heightMap, float cellSize)
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
                    gradientVector = CalcGradient(ref heightMap, new Vector2Int(i, j), cellSize);
                }
                gradient[i, j] = gradientVector;
                Debug.Log($"out gradient: {gradient[i, j].magnitude}");
            }
        }

        return gradient;
    }

    private static Vector2 CalcGradient(ref float[,] heightMap, Vector2Int point, float cellSize)
    {
        Vector2 gradientVector = new();
        if (point.x > 0 && point.y > 0 && point.x < heightMap.GetLength(0) - 1 && point.y < heightMap.GetLength(1) - 1)
        {
            gradientVector.x = heightMap[point.x + 1, point.y] - heightMap[point.x - 1, point.y];
            gradientVector.y = heightMap[point.x, point.y + 1] - heightMap[point.x, point.y - 1];
            Debug.Log($"gradient: {gradientVector.magnitude}");
            gradientVector /= 2;
            gradientVector /= cellSize;
            Debug.Log($"gradient: {gradientVector.magnitude}");
        }
        return gradientVector;
    }

    private static void Erode(ref float[,] heightMap, float cellSize, Vector2[,] gradientMap)
    {
        for (int i = 0; i < heightMap.GetLength(0); i++)
        {
            for (int j = 0; j < heightMap.GetLength(1); j++)
            {
                Debug.Log($"gradient: {gradientMap[i, j].magnitude} height: {heightMap[i, j]}");
                heightMap[i, j] *= (-0.2f * Mathf.Pow(gradientMap[i, j].magnitude, 2))+1;
                Debug.Log($"height: {heightMap[i, j]}");
            }
        }
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

public static class ElementsGeneration
{
    public static List<Vector2Int> GenerateForests(ref List<Vector2Int> rivers)
    {
        List<Vector2Int> forests = new();
        List<Vector2Int> neighbours = new();
        float[,] heightMap = Grid.instance.GetHeightMap();

        //get river neighbours
        for (int i = 0; i < rivers.Count; i++)
        {
            neighbours.AddRange(HeightMapService.GetLowestStrictNeighbour(rivers[i], ref heightMap));
        }
        neighbours = neighbours.Distinct().ToList();
        for (int i = 0; i < neighbours.Count; i++)
        {
            for (int j = 0; j < rivers.Count; j++)
            {
                if (neighbours[i] == rivers[j])
                {
                    neighbours.RemoveAt(i);
                    i--;
                    break;
                }
            }
        }
        //do untill have any neighbours
        for(int i = 0; i < 10000; i++)
        {
            //Debug.Log($"i: {i} neighbours: {neighbours.Count}");
            //get first neighbour
            Vector2Int neighbour = neighbours[0];
            
            //calc distance to river
            Vector2Int closestRiver;
            float chance = 1f;
            if(FindClosestElement(Elements.river, neighbour, out closestRiver))
            {
                //Debug.Log("closest river: " + closestRiver);
                float distance = Vector2.Distance(neighbour, closestRiver);
                chance -= 0.2f * distance;
                //check if have any neighbours in forests list
                foreach(Vector2Int nextTo in HeightMapService.GetLowestStrictNeighbour(neighbour, ref heightMap))
                {
                    if(forests.Contains(nextTo))
                    {
                        chance += 0.035f;
                    }
                }

                //Debug.Log($"chance: {chance} distance: {distance}");

                float diceRoll = UnityEngine.Random.Range(0f, 1f);
                //Debug.Log($"diceRoll: {diceRoll} chance: {chance}");
                if(diceRoll < chance)
                {
                    //Debug.LogWarning("forest");
                    forests.Add(neighbour);

                    Vector2Int[] potentialNewFields =  HeightMapService.GetLowestStrictNeighbour(neighbour, ref heightMap);

                    //check if potential new fields are not in forests list
                    for (int j = 0; j < potentialNewFields.Length; j++)
                    {
                        if (forests.Contains(potentialNewFields[j]) == false && Grid.instance.GetCellData(potentialNewFields[j].x, potentialNewFields[j].y).Ground == GroundType.earth)
                        {
                            neighbours.Add(potentialNewFields[j]);
                        }
                    }
                }
                                
            }
            neighbours.RemoveAt(0);
            if(neighbours.Count == 0)
            {
                break;
            }
        }
        //Debug.Log("forests: " + forests.Count);
        return forests;
    }

    private static bool FindClosestElement(Elements elementToFind,  Vector2Int startPos, out Vector2Int closestElement)
    {
        closestElement = new();

        bool inRange;
        int radius = 0;
        while(true)
        {
            inRange = false;
            for (int x = -radius; x <= 0; x++)
            {
                for (int y = -radius; y <= 0; y++)
                {
                    CellData cell = Grid.instance.GetCellData(startPos.x + x, startPos.y + y);
                    //check x, y
                    if(cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if(cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x + x, startPos.y + y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check -x, y
                    cell = Grid.instance.GetCellData(startPos.x - x, startPos.y + y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x - x, startPos.y + y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check x, -y
                    cell = Grid.instance.GetCellData(startPos.x + x, startPos.y - y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x + x, startPos.y - y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check -x, -y
                    cell = Grid.instance.GetCellData(startPos.x - x, startPos.y - y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x - x, startPos.y - y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //optimization for not checking all cells every time, but only cells on the edge of the square
                    if(x!=-radius)
                    {
                        break;
                    }   
                }
            }
            if(inRange == false)
            {
                //Debug.Log("not in range");
                return false;
            }
            radius++;
        }
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

[CreateAssetMenu(fileName = "WorldSettings", menuName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    public int seed;

    public int width, length;
    public float maxElevation, minElevation;

    public float cellSize;

    public int riverCount;
}