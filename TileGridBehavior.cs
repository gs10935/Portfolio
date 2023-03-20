using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGridBehavior : MonoBehaviour
{
    /*
     * Ư�� Ÿ�Ͽ��� �ٸ� Ÿ�Ϸ� �̵��� ���ִ����� �Ǵ��ϰ� ���� ã���� �ϴ� ��ũ��Ʈ�Դϴ�.
     * ���������� ���������� �����ϰ� �������� �������� ������������ �Ÿ��� ��θ� �����մϴ�.
     */

    #region ����
    public TileCreate _tileCreate; //Ÿ�� ����Ʈ ������ �����´�
    public List<GameObject> path = new List<GameObject>(); //����~���������� ��� ����Ʈ

    private int rows = 0;
    private int columns = 0;
    private int scale = 1;
    private List<Tile> TileList; //���� ��ġ�� Ÿ���� ����Ʈ
    private List<Tile> tileChanger = new List<Tile>(); //Ÿ�� ���� ��� ����Ʈ

    public int startX = 0;
    public int startY = 0;
    public int endX = 2;
    public int endY = 2;
    public int moveCase = 0; //Ÿ�� �̵� ���. 0 : �ִܰŸ�, 1 : 90�� ����

    private bool findDistance = false;
    #endregion

    private void Awake()
    {
        if (!_tileCreate && GetComponent<TileCreate>())
            _tileCreate = GetComponent<TileCreate>();
        else
            Debug.Log("Missing Board : " + _tileCreate);
    }

    // Update is called once per frame
    void Update()
    {
        if (findDistance && _tileCreate)
            SetBehavior(endX, endY);
    }

    private void FixedUpdate()
    {
        if (tileChanger.Count > 0)
            ChangeTileOption(tileChanger);

    }

    #region Ÿ�� ���
    public void SetStartPoint(int _x, int _y)
    {
        startX = _x;
        startY = _y;
    }

    public void SetBehavior(int _endX, int _endY)
    {
        endX = _endX;
        endY = _endY;

        rows = _tileCreate.rows;
        columns = _tileCreate.columns;
        TileList = _tileCreate.TileList; //Ÿ�ϸ���Ʈ�� �޾ƿ´�.

        SetDistance(); //�������� �������� ��ü Ÿ���� �̵��� �ʿ��� �Ÿ�(visited)�� �����Ѵ�.
        SetPath(); //���������� ���������� �����ϱ� ���� ������ Ÿ���� ����Ʈ ����
        SetTileChanger(); //Ÿ�� ������� ��ó��
        findDistance = false;
    }
    #endregion

    #region Ÿ�� �ʱ�ȭ ���
    void SetDistance()
    {
        InitialSetup(); //������(startX, startY)�� �κ��� visited = 0, ������ Ÿ���� ��� visited = -1 �̴�.
        for (int step = 1; step < rows * columns; step++)
            foreach (Tile _tile in TileList)
            {
                //���� Ÿ�� (visited = 0)�� �������� Ÿ���� ������.
                if (_tile.targetTile && _tile.visited == step - 1) //���� step���� ���� visited Ÿ�ϸ��� Ž�� (�� ó�� Ÿ���� ���� Ÿ�� visited = 0)
                    TestFourDirections(_tile.gridX, _tile.gridY, step);
            }
    }

    void SetPath()
    {
        int step;
        int x = endX;
        int y = endY;
        List<Tile> tempList = new List<Tile>();
        path.Clear();

        if (TileList[x % rows + y * rows].targetTile && TileList[x % rows + y * rows].visited > 0)
        {
            path.Add(TileList[x % rows + y * rows].targetTile);
            step = TileList[x % rows + y * rows].visited - 1;
            TileList[x % rows + y * rows].option.SetOption(1);
        }
        else
        {
            Debug.Log("�������� ������ �� �����ϴ�.");
            return;
        }

        for (int i = step; step > -1; step--)
        {
            //���������� ���������� ���� ������ Ÿ���� �����Ѵ�.
            if (TestDirection(x, y, step, 1)) //��
                tempList.Add((Tile)TileList[x % rows + (y + 1) * rows].Clone()); //[x, y + 1]
            if (TestDirection(x, y, step, 2)) //������
                tempList.Add((Tile)TileList[(x + 1) % rows + y * rows].Clone()); //[x + 1, y]
            if (TestDirection(x, y, step, 3)) //�Ʒ�
                tempList.Add((Tile)TileList[x % rows + (y - 1) * rows].Clone()); //[x, y - 1]
            if (TestDirection(x, y, step, 4)) //����
                tempList.Add((Tile)TileList[(x - 1) % rows + y * rows].Clone()); //[x - 1, y]

            //Ÿ�� ��������� �����Ѵ�.
            Tile tempTile = FindClosest(TileList[endX % rows + endY * rows], tempList, moveCase);
            path.Add(tempTile.targetTile); //�̵���ο� ���������� �߰�.
            TileList[tempTile.number].option.SetOption(1);
            x = tempTile.gridX;
            y = tempTile.gridY;
            tempList.Clear();
        }

    }

    private void SetTileChanger()
    {
        tileChanger = TileList.ToList();
    }
    #endregion

    #region Ÿ�� �˻�, ó��
    void InitialSetup()
    {
        //��� Ÿ������ �ʱ�ȭ
        foreach (Tile _tile in TileList)
            if (_tile.targetTile && _tile.targetTile.activeInHierarchy)
            {
                _tile.visited = -1;
                _tile.option.SetOption(0.5f);
            }

        //�������� �Ǵ� Ÿ���� �Ÿ����� �ʱ�ȭ
        TileList[startX % rows + startY * rows].visited = 0;

    }
    bool TestDirection(int x, int y, int step, int direction)
    {
        //[x, y] Ÿ���� �������� 4���⿡ ��ġ�� Ÿ���� �̵����� ������ üũ�Ѵ�.
        //Ÿ�� üũ�� �� Ÿ���� visited�� -1 (��Ž�� ����)���� �Ѵ�.
        switch (direction)
        {
            //����
            case 4:
                if (x - 1 > -1 && TileList[(x - 1)% rows + y * rows].targetTile && TileList[(x - 1) % rows + y * rows].visited == step)
                    return true;
                else
                    return false;
            //�Ʒ�
            case 3:
                if (y - 1 > -1 && TileList[x % rows + (y - 1) * rows].targetTile && TileList[x % rows + (y - 1) * rows].visited == step)
                    return true;
                else
                    return false;
            //������
            case 2:
                if (x + 1 < rows && TileList[(x + 1) % rows + y * rows].targetTile && TileList[(x + 1) % rows + y * rows].visited == step)
                    return true;
                else
                    return false;
            //��
            case 1:
                if (y + 1 < columns && TileList[x % rows + (y + 1) * rows].targetTile && TileList[x % rows + (y + 1) * rows].visited == step)
                    return true;
                else
                    return false;
        }

        return false;
    }

    void SetVisited(int x, int y, int step)
    {
        //[x,y] Ÿ�Ͽ� ���������κ��� �̵��Ÿ��� ����
        if (TileList[x % rows + y * rows].targetTile && 
            (TileList[x % rows + y * rows].tag.Equals("floor") || TileList[x % rows + y * rows].tag.Equals("staticFloor") || TileList[x % rows + y * rows].tag.Equals("startZone") || TileList[x % rows + y * rows].tag.Equals("exitZone")))
            TileList[x % rows + y * rows].visited = step;
    }

    void TestFourDirections(int x, int y, int step)
    {
        if (TestDirection(x, y, -1, 1)) //��
            SetVisited(x, y + 1, step);
        if (TestDirection(x, y, -1, 2)) // ������
            SetVisited(x + 1, y, step);
        if (TestDirection(x, y, -1, 3)) //�Ʒ�
            SetVisited(x, y - 1, step);
        if (TestDirection(x, y, -1, 4)) //����
            SetVisited(x - 1, y, step);
    }

    Tile FindClosest(Tile targetLocation, List<Tile> list, int switchNum)
    {
        //���� �Ÿ��� ���� ���� ����� Ÿ���� �����ϴ� �Լ��̴�.
        int indexNumber = 0;

        switch (switchNum)
        {
            //�������� �����ϱ� (90�� ����)
            case 1:
                bool _goX = Mathf.Abs(endX - startX) < Mathf.Abs(endY - startY);
                int _closestNum = rows + columns;

                for (int i = 0; i < list.Count; i++)
                    if (_goX && Mathf.Abs(endX - list[i].gridX) <= _closestNum) //x������ �켱�Ͽ� �̵�
                    {
                        _closestNum = Mathf.Abs(endX - list[i].gridX);
                        indexNumber = i;
                    }
                    else if (!_goX && Mathf.Abs(endY - list[i].gridY) <= _closestNum)//y������ �켱�Ͽ� �̵�
                    {
                        _closestNum = Mathf.Abs(endY - list[i].gridY);
                        indexNumber = i;
                    }

                return list[indexNumber];

            //������ �Ÿ��� ���� ����� Ÿ�� ���ϱ�
            case 0:
                float currentDistance = scale * rows * columns;
                for (int i = 0; i < list.Count; i++)
                    if (Vector3.Distance(targetLocation.targetTile.transform.position, list[i].targetTile.transform.position) < currentDistance)
                    {
                        currentDistance = Vector3.Distance(targetLocation.targetTile.transform.position, list[i].targetTile.transform.position);
                        indexNumber = i;
                    }

                return list[indexNumber];
        }

        return list[indexNumber];
    }

    private void ChangeTileOption(List<Tile> _list)
    {

        for (int i = _list.Count - 1; i >= 0; i--)
        {
            if (_list[i].targetTile)
            {
                Color _color = _list[i].targetTile.GetComponent<SpriteRenderer>().color;
                float _tempH, _tempS, _tempV;
                Color.RGBToHSV(_color, out _tempH, out _tempS, out _tempV);

                if (_list[i].option.hsv_v < 0)
                    _list[i].option.hsv_v = 0;
                else if (_list[i].option.hsv_v > 1)
                    _list[i].option.hsv_v = 1;

                if(_list[i].targetTile)
                    _list[i].targetTile.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(0, 0, Mathf.MoveTowards(_tempV, _list[i].option.hsv_v, Time.deltaTime));
                if (_list[i].contentTile)
                    _list[i].contentTile.GetComponent<SpriteRenderer>().color = Color.HSVToRGB(0, 0, Mathf.MoveTowards(_tempV, _list[i].option.hsv_v, Time.deltaTime));

                if (Mathf.Abs(_tempV - _list[i].option.hsv_v) <= float.Epsilon)
                    _list.RemoveAt(i);
            }
        }
    }
    #endregion
}
