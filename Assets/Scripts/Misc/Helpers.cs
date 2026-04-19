using System.Collections;
using System.Collections.Generic;
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