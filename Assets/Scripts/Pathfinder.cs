using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public HexGridLayout hexGrid;

    private void Awake()
    {
        hexGrid = FindAnyObjectByType<HexGridLayout>();
    }

    public List<HexGridLayout.HexNode> FindPath(HexGridLayout.HexNode start, HexGridLayout.HexNode target)
    {
        print(HexGridLayout.instance.hexNodes.Count);
        if (start == null || target == null)
            return null;

        List<HexGridLayout.HexNode> toSearch = new List<HexGridLayout.HexNode>() { start };
        List<HexGridLayout.HexNode> processed = new List<HexGridLayout.HexNode>();

        while (toSearch.Count > 0)
        {
            HexGridLayout.HexNode current = toSearch[0];
            foreach (HexGridLayout.HexNode node in toSearch)
            {
                if (node.F < current.F || node.F == current.F && node.H < current.H)
                    current = node;
            }

            processed.Add(current);
            toSearch.Remove(current);

            if (current == target)
            {
                List<HexGridLayout.HexNode> path = new List<HexGridLayout.HexNode>();
                HexGridLayout.HexNode currentNodeInPath = target;

                while (currentNodeInPath != start)
                {
                    path.Add(currentNodeInPath);
                    currentNodeInPath = currentNodeInPath.connection;
                }
                // Debug.Log("Found path with length: " + path.Count);

                path.Reverse();
                return path;
            }

            List<HexGridLayout.HexNode> neighbours = current.GetNeighbours(HexGridLayout.instance.hexNodes).Where(h => !processed.Contains(h)).ToList();
            foreach (HexGridLayout.HexNode neighbour in neighbours)
            {
                bool inSearch = toSearch.Contains(neighbour);
                float costToNeighbour = current.G + current.Distance(neighbour);

                if (!inSearch || costToNeighbour < neighbour.G)
                {
                    neighbour.G = costToNeighbour;
                    neighbour.connection = current;

                    if (!inSearch)
                    {
                        neighbour.H = neighbour.Distance(target);
                        toSearch.Add(neighbour);
                    }
                }
            }
        }
        return null;
    }
}
