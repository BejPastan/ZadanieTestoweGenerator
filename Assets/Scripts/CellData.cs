using Unity.VisualScripting;
using UnityEngine;

public struct CellData
{
    public float height { get; private set;}
    public GroundType ground { get; private set; }
    public Transform element { get; private set; }
    public Vector2 slope { get; private set; }

    public CellData(float height, GroundType ground, Vector2 slope, Transform element)
    {
        this.height = height;
        this.ground = ground;
        this.element = element;
        this.slope = slope;

        if(element.GetComponent<SpriteRenderer>() == null )
        {
            element.AddComponent<SpriteRenderer>();
        }

        element.GetComponent<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(ground);
    }
}

public enum GroundType
{
    stone,
    earth,
    water
}


[CreateAssetMenu(fileName = "ElementSprite", menuName = "ElementSprite")]
public class ElementSprite : ScriptableObject
{
    [SerializeField]
    GroundType[] groundType;
    [SerializeField]
    Sprite[] sprite;

    public Sprite GetSprite(GroundType groundType)
    {
        for (int i = 0; i < this.groundType.Length; i++)
        {
            if (this.groundType[i] == groundType)
            {
                return sprite[i];
            }
        }
        return null;
    }
}

