using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ElementsGeneration
{
    public static List<Vector2Int> GenerateForests(ref List<Vector2Int> rivers, GroundType[] allowedGrounds)
    {
        List<Vector2Int> forests = new();
        List<Vector2Int> neighbours = new();
        float[,] heightMap = Grid.instance.GetHeightMap();

        //get river neighbours
        for (int i = 0; i < rivers.Count; i++)
        {
            neighbours.AddRange(HeightMapService.GetLowestStrictNeighbour(rivers[i], ref heightMap));
        }
        neighbours = neighbours.Distinct().ToList();
        for (int i = 0; i < neighbours.Count; i++)
        {
            for (int j = 0; j < rivers.Count; j++)
            {
                if (neighbours[i] == rivers[j])
                {
                    neighbours.RemoveAt(i);
                    i--;
                    break;
                }
            }
        }
        //do untill have any neighbours
        for(int i = 0; i < 10000; i++)
        {
            Vector2Int neighbour = neighbours[0];

            //calc distance to river
            float chance = 1f;
            if (FindClosestElement(Elements.river, neighbour, out Vector2Int closestRiver))
            {
                //Debug.Log("closest river: " + closestRiver);
                float distance = Vector2.Distance(neighbour, closestRiver);
                chance -= 0.1f * distance;
                //check if have any neighbours in forests list
                foreach(Vector2Int nextTo in HeightMapService.GetLowestStrictNeighbour(neighbour, ref heightMap))
                {
                    if(forests.Contains(nextTo))
                    {
                        chance += 0.018f;
                    }
                }

                float diceRoll = UnityEngine.Random.Range(0f, 1f);
                if(diceRoll < chance)
                {
                    forests.Add(neighbour);

                    Vector2Int[] potentialNewFields =  HeightMapService.GetLowestStrictNeighbour(neighbour, ref heightMap);

                    for (int j = 0; j < potentialNewFields.Length; j++)
                    {
                        if (forests.Contains(potentialNewFields[j]) == false && allowedGrounds.Contains(Grid.instance.GetCellData(potentialNewFields[j].x, potentialNewFields[j].y).Ground))
                        {
                            neighbours.Add(potentialNewFields[j]);
                        }
                    }
                }
                                
            }
            neighbours.RemoveAt(0);
            if(neighbours.Count == 0)
            {
                break;
            }
        }
        //Debug.Log("forests: " + forests.Count);
        return forests;
    }

    public static List<Vector2Int> GenerateSettlements(int amount, List<Vector2Int> rivers)
    {
        List<Vector2Int> settlements = new();
        float[,] heightMap = Grid.instance.GetHeightMap();
        for(int i = 0; i < amount; i++)
        {
            //get random river point
            Vector2Int riverPoint = rivers[UnityEngine.Random.Range(0, rivers.Count)];

            //get their neighbours
            Vector2Int[] neighbours = HeightMapService.GetLowestStrictNeighbour(riverPoint, ref heightMap);
            Vector2Int settelment = new();
            //select one that has no water or river and is plains or highlands
            for (int j = 0; j < neighbours.Length; j++)
            {
                
                if (Grid.instance.GetCellData(neighbours[j].x, neighbours[j].y).Ground == GroundType.plains || Grid.instance.GetCellData(neighbours[j].x, neighbours[j].y).Ground == GroundType.highLands)
                {
                    if (Grid.instance.GetCellData(neighbours[j].x, neighbours[j].y).Elements.Contains(Elements.river) == false)
                    {
                        settelment = neighbours[j];
                        break;
                    }
                }
            }

            int stoneNumber = 0;
            List<Vector2Int> fields = new();

            for(int x = -4; x < 5; x++)
            {
                for(int y = -4; y < 5; y++)
                {
                    if(Vector2.Distance(new Vector2Int(0, 0), new Vector2Int(x, y)) <= 4 && Grid.instance.GetCellData(x + settelment.x, y+settelment.y)!= null)
                    {
                        switch(Grid.instance.GetCellData(settelment.x + x, settelment.y + y).Ground)
                        {
                            case GroundType.slopes:
                                stoneNumber++;
                                break;
                            case GroundType.mountains:
                                stoneNumber++;
                                break;
                            case GroundType.plains:
                                if(Grid.instance.GetCellData(settelment.x + x, settelment.y + y).Elements.Contains(Elements.river) == false && Grid.instance.GetCellData(settelment.x + x, settelment.y + y).Elements.Contains(Elements.forest) == false)
                                {
                                    fields.Add(new Vector2Int(settelment.x + x, settelment.y + y));
                                }
                                break;
                            case GroundType.highLands:
                                if (Grid.instance.GetCellData(settelment.x + x, settelment.y + y).Elements.Contains(Elements.river) == false && Grid.instance.GetCellData(settelment.x + x, settelment.y + y).Elements.Contains(Elements.forest) == false)
                                {
                                    fields.Add(new Vector2Int(settelment.x + x, settelment.y + y));
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            if(stoneNumber <= 0 || fields.Count < 4)
            {
                i--;
            }
            else
            {
                settlements.Add(settelment);
            }
            rivers.Remove(riverPoint);
            if(rivers.Count == 0)
            {
                break;
            }
        }

        return settlements;
    }

    public static List<Vector2Int> GeneratePaths(ref List<Vector2Int> settelments)
    {
        if(settelments.Count < 2)
        {
            Debug.Log("not enough settelments");
            return new List<Vector2Int>();
        }
        //from each settelments go out 2 paths, one to closest and one to farthest
        List<Vector2Int> paths = new();

        foreach(Vector2Int settelment in settelments)
        {
            //get closest settelment
            Vector2Int closestSettelment = new();
            Vector2Int farthestSettelment = new();
            float closestDistance = float.MaxValue;
            float farthestDistance = 0;
            for(int i = 0; i < settelments.Count; i++)
            {
                if(settelment != settelments[i])
                {
                    float distance = Vector2.Distance(settelment, settelments[i]);
                    if(distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSettelment = settelments[i];
                    }
                    if(distance > farthestDistance)
                    {
                        farthestDistance = distance;
                        farthestSettelment = settelments[i];
                    }
                }
            }

            //get path to closest settelment using A*
            List<Vector2Int> path = Grid.instance.GetPath(settelment, closestSettelment);
            path.AddRange(Grid.instance.GetPath(settelment, farthestSettelment));
        }


        return paths;
    }

    private static bool FindClosestElement(Elements elementToFind,  Vector2Int startPos, out Vector2Int closestElement)
    {
        closestElement = new();

        bool inRange;
        int radius = 0;
        while(true)
        {
            inRange = false;
            for (int x = -radius; x <= 0; x++)
            {
                for (int y = -radius; y <= 0; y++)
                {
                    CellData cell = Grid.instance.GetCellData(startPos.x + x, startPos.y + y);
                    //check x, y
                    if(cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if(cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x + x, startPos.y + y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check -x, y
                    cell = Grid.instance.GetCellData(startPos.x - x, startPos.y + y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x - x, startPos.y + y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check x, -y
                    cell = Grid.instance.GetCellData(startPos.x + x, startPos.y - y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x + x, startPos.y - y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //check -x, -y
                    cell = Grid.instance.GetCellData(startPos.x - x, startPos.y - y);
                    if (cell != null)
                    {
                        inRange = true;
                        //Debug.Log("in range");
                        if (cell.Elements.Contains(elementToFind))
                        {
                            closestElement = new Vector2Int(startPos.x - x, startPos.y - y);
                            //Debug.Log("found element");
                            return true;
                        }
                    }

                    //optimization for not checking all cells every time, but only cells on the edge of the square
                    if(x!=-radius)
                    {
                        break;
                    }   
                }
            }
            if(inRange == false)
            {
                //Debug.Log("not in range");
                return false;
            }
            radius++;
        }
    }
}