using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct CellData
{
    public float height { get; private set;}
    public GroundType ground { get; private set; }
    public Transform element { get; private set; }
    public Vector2 slope { get; private set; }
    public List<Elements> elements { get; private set; }
    private List<Transform> elementsTransform;
    public CellData(float height, GroundType ground, Vector2 slope, Transform element)
    {
        this.height = height;
        this.ground = ground;
        this.element = element;
        this.slope = slope;
        elements = new List<Elements>();
        elementsTransform = new List<Transform>();

        if(element.GetComponent<SpriteRenderer>() == null )
        {
            element.AddComponent<SpriteRenderer>();
        }

        element.GetComponent<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(ground);
    }

    public void AddElement(Elements element, Transform elementTrans)
    {
        elements.Add(element);
        elementsTransform.Add(elementTrans);
        elementTrans.transform.SetParent(this.element);
        elementTrans.transform.localPosition = Vector3.zero;
        elementTrans.GetComponentInChildren<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(element);
    }
}

public enum GroundType
{
    stone,
    earth,
    water
}

public enum Elements
{
    river,
    forest
}

[CreateAssetMenu(fileName = "ElementSprite", menuName = "ElementSprite")]
public class ElementSprite : ScriptableObject
{
    [SerializeField]
    GroundType[] groundType;
    [SerializeField]
    Sprite[] sprite;

    [SerializeField]
    Elements[] elements;
    [SerializeField]
    Sprite[] elementSprite;

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

    public Sprite GetSprite(Elements element)
    {
        for (int i = 0; i < this.elements.Length; i++)
        {
            if (this.elements[i] == element)
            {
                return elementSprite[i];
            }
        }
        return null;
    }
}

