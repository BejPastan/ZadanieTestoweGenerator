using System.Collections.Generic;
using TreeEditor;
using Unity.Mathematics;
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
        //generate cell Prefab
        for (int x = 0; x < settings.width; x++)
        {
            for (int z = 0; z < settings.length; z++)
            {
                Transform cell = Instantiate(cellPref, new Vector3(x, z, 0), Quaternion.identity).transform;

                switch(heightMap[x, z])
                {
                    case float n when n<0:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.water, slopeMap[x, z], cell);
                            break;
                        }
                }

                switch (slopeMap[x, z].magnitude)
                {
                    case float n when n < 0.3f:
                        {
                            Grid.instance.Cells[x, z] = new CellData(heightMap[x, z], GroundType.earth, slopeMap[x,z], cell);
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
            }
        }

        Debug.LogWarning("hightstPoint: " + highestPoint);
        //normalize height map
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < length; j++)
            {
                Debug.Log("heightMap before: " + heightMap[i, j]);
                heightMap[i, j] /= highestPoint;
                Debug.Log("heightMap after: " + heightMap[i, j]);
                heightMap[i, j] = Mathf.Lerp(minElevation, maxElevation-minElevation, heightMap[i, j]);
                Debug.Log("heightMap: " + heightMap[i, j]);
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
                float slope;
                if (i > 0 && j > 0 && i < heightMap.GetLength(0) - 1 && j < heightMap.GetLength(1) - 1)
                {
                    gradientVector.x = heightMap[i + 1, j] - heightMap[i - 1, j];
                    gradientVector.y = heightMap[i, j + 1] - heightMap[i, j - 1];

                    Debug.Log("gradientVector: " + gradientVector);

                    slope = gradientVector.x * 4 / (3 * gradientVector.y + 4);
                    gradientVector.x *= slope;

                    slope = gradientVector.y * 4 / (3 * gradientVector.x + 4);
                    gradientVector.y *= slope;

                    Debug.Log("gradientVector after: " + gradientVector);
                }
                gradient[i, j] = gradientVector;
            }
        }

        return gradient;
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