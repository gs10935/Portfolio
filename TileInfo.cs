using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfo : MonoBehaviour
{
    private List<Tile> tile;
    private int num;

    public void SetTileInfo(List<Tile> _list, int _num)
    {
        tile = _list;
        num = _num;
    }

    public Tile GetTileInfo()
    {
        return tile[num];
    }
}
