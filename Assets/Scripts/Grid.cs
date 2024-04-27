using System;
using UnityEngine;

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
}
