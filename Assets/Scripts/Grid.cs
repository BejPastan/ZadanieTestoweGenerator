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
                heightMap[i, j] = Cells[i, j].height;
            }
        }
        return heightMap;
    }
}
