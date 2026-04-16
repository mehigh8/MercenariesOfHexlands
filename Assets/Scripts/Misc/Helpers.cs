using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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