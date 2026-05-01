using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Class that stores two items of specified types
/// </summary>
/// <typeparam name="T">Type of the first item</typeparam>
/// <typeparam name="K">Type of the second item</typeparam>
[System.Serializable]
public class Pair<T,K>
{
    public T item1;
    public K item2;

    public Pair(T item1, K item2)
    {
        this.item1 = item1;
        this.item2 = item2;
    }

}

/// <summary>
/// Class containing general utils functions
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Function used to set layers recursively for all children of a gameobject
    /// </summary>
    /// <param name="obj">Starting gameobject</param>
    /// <param name="layer">Layer to be set</param>
    public static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}