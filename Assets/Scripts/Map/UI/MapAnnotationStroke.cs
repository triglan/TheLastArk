using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Serializable, normalized map coordinates; player slots are zero-based.</summary>
[Serializable]
public class MapAnnotationStroke
{
    public string id;
    public int playerSlot;
    public List<Vector2> points = new List<Vector2>();
}
