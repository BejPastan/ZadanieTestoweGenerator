using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct CellData
{
    public float Height { get; private set;}
    public GroundType Ground { get; private set; }
    public Transform Element { get; private set; }
    public Vector2 Slope { get; private set; }
    public List<Elements> Elements { get; private set; }
    private readonly List<Transform> elementsTransform;
    public CellData(float height, GroundType ground, Vector2 slope, Transform element)
    {
        this.Height = height;
        this.Ground = ground;
        this.Element = element;
        this.Slope = slope;
        Elements = new List<Elements>();
        elementsTransform = new List<Transform>();

        if(element.GetComponent<SpriteRenderer>() == null )
        {
            element.AddComponent<SpriteRenderer>();
        }

        element.GetComponent<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(ground);
    }

    public readonly void AddElement(Elements element, Transform elementTrans)
    {
        Elements.Add(element);
        elementsTransform.Add(elementTrans);
        elementTrans.transform.SetParent(this.Element);
        elementTrans.transform.localPosition = Vector3.zero;
        elementTrans.GetComponentInChildren<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(element);
    }

    public void SetGroundType(GroundType ground)
    {
        this.Ground = ground;
        Element.GetComponent<SpriteRenderer>().sprite = WorldGenerator.instance.elementSprite.GetSprite(ground);
    }

    public readonly void RemoveElement(Elements element)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            if (Elements[i] == element)
            {
                GameObject.Destroy(elementsTransform[i].gameObject);
                Elements.RemoveAt(i);
                elementsTransform.RemoveAt(i);
                return;
            }
        }
    }

    public readonly void RemoveAllElements()
    {
        //remove all elements
        while (Elements.Count > 0)
        {
            GameObject.Destroy(elementsTransform[0].gameObject);
            Elements.RemoveAt(0);
        }
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

