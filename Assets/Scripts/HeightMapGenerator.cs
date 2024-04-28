using UnityEngine;


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