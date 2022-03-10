using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameItem : ScriptableObject
{
    new public string name;
    public string description;
    public GameObject prefab; 
    public Sprite prefabSprite;
    
    public abstract ItemType Type { get; }
}

public enum ItemType
{
    Tool,
    Material,
    Default
}