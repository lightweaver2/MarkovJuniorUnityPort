using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public struct ColorWithBlock
{
    public Color c;
    public GameObject b;
}
[CreateAssetMenu(menuName = "ScriptableObject/BlockPalette")]
public class BlockPalette : ScriptableObject
{
    public List<ColorWithBlock> blockList;
    public GameObject defaultBlock;
}
