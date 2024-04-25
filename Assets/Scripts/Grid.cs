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
}
