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
        VisualiseElement(ref forests, Elements.forest, new GroundType[] { GroundType.plains, GroundType.highLands, GroundType.mountains, GroundType.mountainTop});

        List<Vector2Int> settlements = ElementsGeneration.GenerateSettlements(4, rivers);
        VisualiseElement(ref settlements, Elements.settlement, new GroundType[] { GroundType.plains, GroundType.highLands });
        foreach (Vector2Int settlement in settlements)
        {
            //remove forest from around settlement in radius 2
            for (int x = -2; x < 3; x++)
            {
                for (int y = -2; y < 3; y++)
                {
                    if (Vector2.Distance(new Vector2Int(0, 0), new Vector2Int(x, y)) <= 2 && Grid.instance.GetCellData(x + settlement.x, y + settlement.y)!= null)
                    {
                        if (Grid.instance.GetCellData(settlement.x + x, settlement.y + y).Elements.Contains(Elements.forest))
                        {
                            Grid.instance.GetCellData(settlement.x + x, settlement.y + y).RemoveElement(Elements.forest);
                        }
                    }
                }
            }

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
                            //here goes seelcting ground texture based on height
                            switch(heightMap[x, z])
                            {
                                case < 20f:
                                    {
                                        Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.plains, slopeMap[x, z], cell);
                                        continue;
                                    }
                                case < 40f:
                                    {
                                        Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.highLands, slopeMap[x, z], cell);
                                        continue;
                                    }
                                case < 60f:
                                    {
                                        Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.mountains, slopeMap[x, z], cell);
                                        continue;
                                    }
                                default:
                                    {
                                        Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.mountainTop, slopeMap[x, z], cell);
                                        continue;
                                    }
                            }
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.plains, slopeMap[x, z], cell);
                            continue;
                        }
                    default:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.slopes, slopeMap[x, z], cell);
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

    public void VisualiseElement(ref List<Vector2Int> locations, Elements elementType, GroundType[] allowedGrounds)
    {
        for (int i = 0; i < locations.Count; i++)
        {
            Vector2Int element = locations[i];
            if (allowedGrounds.Contains(Grid.instance.Cells[element.x, element.y].Ground) == false)
            {
                locations.RemoveAt(i);
                i--;
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

[CreateAssetMenu(fileName = "WorldSettings", menuName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    public int seed;

    public int width, length;
    public float maxElevation, minElevation;

    public float cellSize;

    public int riverCount;
}