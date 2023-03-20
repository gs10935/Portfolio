using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileGridBehavior : MonoBehaviour
{
    /*
     * 특정 타일에서 다른 타일로 이동할 수있는지를 판단하고 길을 찾도록 하는 스크립트입니다.
     * 시작지점과 도착지점을 설정하고 시작점을 기준으로 도착점까지의 거리와 경로를 결정합니다.
     */

    #region 변수
    public TileCreate _tileCreate; //타일 리스트 정보를 가져온다
    public List<GameObject> path = new List<GameObject>(); //시작~도착까지의 경로 리스트

    private int rows = 0;
    private int columns = 0;
    private int scale = 1;
    private List<Tile> TileList; //실제 배치된 타일의 리스트
    private List<Tile> tileChanger = new List<Tile>(); //타일 변경 대기 리스트

    public int startX = 0;
    public int startY = 0;
    public int endX = 2;
    public int endY = 2;
    public int moveCase = 0; //타일 이동 방식. 0 : 최단거리, 1 : 90도 꺾기

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

    #region 타일 명령
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
        TileList = _tileCreate.TileList; //타일리스트를 받아온다.

        SetDistance(); //시작점을 기준으로 전체 타일의 이동에 필요한 거리(visited)를 정의한다.
        SetPath(); //시작점에서 도착점까지 도달하기 위해 지나는 타일의 리스트 정리
        SetTileChanger(); //타일 변경사항 후처리
        findDistance = false;
    }
    #endregion

    #region 타일 초기화 방식
    void SetDistance()
    {
        InitialSetup(); //시작점(startX, startY)인 부분은 visited = 0, 나머지 타일은 모두 visited = -1 이다.
        for (int step = 1; step < rows * columns; step++)
            foreach (Tile _tile in TileList)
            {
                //시작 타일 (visited = 0)을 기점으로 타일을 넓힌다.
                if (_tile.targetTile && _tile.visited == step - 1) //이전 step으로 찍은 visited 타일만을 탐색 (맨 처음 타일은 시작 타일 visited = 0)
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
            Debug.Log("목적지에 도달할 수 없습니다.");
            return;
        }

        for (int i = step; step > -1; step--)
        {
            //도착점에서 시작점으로 가는 방향의 타일을 선택한다.
            if (TestDirection(x, y, step, 1)) //위
                tempList.Add((Tile)TileList[x % rows + (y + 1) * rows].Clone()); //[x, y + 1]
            if (TestDirection(x, y, step, 2)) //오른쪽
                tempList.Add((Tile)TileList[(x + 1) % rows + y * rows].Clone()); //[x + 1, y]
            if (TestDirection(x, y, step, 3)) //아래
                tempList.Add((Tile)TileList[x % rows + (y - 1) * rows].Clone()); //[x, y - 1]
            if (TestDirection(x, y, step, 4)) //왼쪽
                tempList.Add((Tile)TileList[(x - 1) % rows + y * rows].Clone()); //[x - 1, y]

            //타일 선정방식을 결정한다.
            Tile tempTile = FindClosest(TileList[endX % rows + endY * rows], tempList, moveCase);
            path.Add(tempTile.targetTile); //이동경로에 최종적으로 추가.
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

    #region 타일 검색, 처리
    void InitialSetup()
    {
        //모든 타일정보 초기화
        foreach (Tile _tile in TileList)
            if (_tile.targetTile && _tile.targetTile.activeInHierarchy)
            {
                _tile.visited = -1;
                _tile.option.SetOption(0.5f);
            }

        //시작점이 되는 타일의 거리정보 초기화
        TileList[startX % rows + startY * rows].visited = 0;

    }
    bool TestDirection(int x, int y, int step, int direction)
    {
        //[x, y] 타일을 기준으로 4방향에 위치한 타일의 이동가능 유무를 체크한다.
        //타일 체크시 이 타일의 visited는 -1 (미탐색 상태)여야 한다.
        switch (direction)
        {
            //왼쪽
            case 4:
                if (x - 1 > -1 && TileList[(x - 1)% rows + y * rows].targetTile && TileList[(x - 1) % rows + y * rows].visited == step)
                    return true;
                else
                    return false;
            //아래
            case 3:
                if (y - 1 > -1 && TileList[x % rows + (y - 1) * rows].targetTile && TileList[x % rows + (y - 1) * rows].visited == step)
                    return true;
                else
                    return false;
            //오른쪽
            case 2:
                if (x + 1 < rows && TileList[(x + 1) % rows + y * rows].targetTile && TileList[(x + 1) % rows + y * rows].visited == step)
                    return true;
                else
                    return false;
            //위
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
        //[x,y] 타일에 시작점으로부터 이동거리를 저장
        if (TileList[x % rows + y * rows].targetTile && 
            (TileList[x % rows + y * rows].tag.Equals("floor") || TileList[x % rows + y * rows].tag.Equals("staticFloor") || TileList[x % rows + y * rows].tag.Equals("startZone") || TileList[x % rows + y * rows].tag.Equals("exitZone")))
            TileList[x % rows + y * rows].visited = step;
    }

    void TestFourDirections(int x, int y, int step)
    {
        if (TestDirection(x, y, -1, 1)) //위
            SetVisited(x, y + 1, step);
        if (TestDirection(x, y, -1, 2)) // 오른쪽
            SetVisited(x + 1, y, step);
        if (TestDirection(x, y, -1, 3)) //아래
            SetVisited(x, y - 1, step);
        if (TestDirection(x, y, -1, 4)) //왼쪽
            SetVisited(x - 1, y, step);
    }

    Tile FindClosest(Tile targetLocation, List<Tile> list, int switchNum)
    {
        //실제 거리에 따른 가장 가까운 타일을 선택하는 함수이다.
        int indexNumber = 0;

        switch (switchNum)
        {
            //직선방향 유지하기 (90도 꺾기)
            case 1:
                bool _goX = Mathf.Abs(endX - startX) < Mathf.Abs(endY - startY);
                int _closestNum = rows + columns;

                for (int i = 0; i < list.Count; i++)
                    if (_goX && Mathf.Abs(endX - list[i].gridX) <= _closestNum) //x방향을 우선하여 이동
                    {
                        _closestNum = Mathf.Abs(endX - list[i].gridX);
                        indexNumber = i;
                    }
                    else if (!_goX && Mathf.Abs(endY - list[i].gridY) <= _closestNum)//y방향을 우선하여 이동
                    {
                        _closestNum = Mathf.Abs(endY - list[i].gridY);
                        indexNumber = i;
                    }

                return list[indexNumber];

            //물리적 거리가 가장 가까운 타일 구하기
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
