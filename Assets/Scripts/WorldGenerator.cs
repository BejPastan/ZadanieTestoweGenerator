using System.Collections.Generic;
using TreeEditor;
using Unity.Mathematics;
using Unity.VisualScripting;
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

    [SerializeField]
    public ElementSprite elementSprite;

    [SerializeField]
    GameObject cellPref;

    [SerializeField]
    MeshRenderer meshRenderer;

    private void Start()
    {
        Grid.instance.SetMapSize(settings.width, settings.length);
        GenerateBase();
    }

    private void GenerateBase()
    {
        Vector2[,] slopeGradient;
        float[,] heightMap;
        HeightMapGenerator.GenerateHeightMap(settings.width, settings.length, settings.maxElevation, settings.minElevation, out heightMap, out slopeGradient, settings.mainScale);

        Grid.instance.Cells = new CellData[settings.width, settings.length];
        VisualiseHeightMap(ref heightMap, ref slopeGradient);
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
    public static Vector2Int GenerateWater(ref float[,] heightMap, ref Vector2[,] gradientMap)
    {
        
    }
}


[CreateAssetMenu(fileName = "WorldSettings", menuName = "WorldSettings")]
public class WorldSettings : ScriptableObject
{
    public int seed;

    public int width, length;
    public float maxElevation, minElevation;

    public float mainScale;
}