using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinder
{
    /// <summary>
    /// Function used to determine the shortest path between 2 hex nodes<br/>
    /// Path can only have hexes that are not obstacles
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="target">Destination</param>
    /// <returns></returns>
    public static List<HexGridLayout.HexNode> FindPath(HexGridLayout.HexNode start, HexGridLayout.HexNode target)
    {
        if (start == null || target == null)
            return null;

        // Keep 2 lists, one with nodes that we have yet to search and one with processed nodes
        List<HexGridLayout.HexNode> toSearch = new List<HexGridLayout.HexNode>() { start };
        List<HexGridLayout.HexNode> processed = new List<HexGridLayout.HexNode>();

        while (toSearch.Count > 0)
        {
            HexGridLayout.HexNode current = toSearch[0];
            // Pick the best node from the toSearch list
            foreach (HexGridLayout.HexNode node in toSearch)
            {
                if (node.F < current.F || node.F == current.F && node.H < current.H)
                    current = node;
            }

            processed.Add(current);
            toSearch.Remove(current);

            // If the current node is the destination, create the path by going backwards through connections
            if (current == target)
            {
                List<HexGridLayout.HexNode> path = new List<HexGridLayout.HexNode>();
                HexGridLayout.HexNode currentNodeInPath = target;

                while (currentNodeInPath != start)
                {
                    path.Add(currentNodeInPath);
                    currentNodeInPath = currentNodeInPath.connection;
                }

                // Lastly, path is reversed so that it is from start to target
                path.Reverse();
                return path;
            }

            // Get all neighbours of the current node that are not obstacles
            List<HexGridLayout.HexNode> neighbours = current.GetNeighbours(HexGridLayout.instance.hexNodes).Where(h => !processed.Contains(h) && !h.hexRenderer.IsObstacle()).ToList();
            foreach (HexGridLayout.HexNode neighbour in neighbours)
            {
                bool inSearch = toSearch.Contains(neighbour);
                float costToNeighbour = current.G + current.Distance(neighbour);

                if (!inSearch || costToNeighbour < neighbour.G)
                {
                    // Update neighbour node
                    neighbour.G = costToNeighbour;
                    neighbour.connection = current;

                    if (!inSearch)
                    {
                        // Add neighbour in the toSearch list
                        neighbour.H = neighbour.Distance(target);
                        toSearch.Add(neighbour);
                    }
                }
            }
        }

        // If there are no more nodes to search and the target was not reached, we cannot find a path
        return null;
    }
}
