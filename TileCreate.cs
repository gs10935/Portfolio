using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


public class TileCreate : MonoBehaviour
{
    /*
     * 타일맵을 생성하는 스크립트입니다.
     * 처음부터 생성하는 것이 아닌 유니티 프리펩으로 미리 만들어 놓은 타일셋을 참조하여 무작위 타일을 배치하는 합니다.
     * 개발자가 타일맵을 가시적으로 확인할 수 있고 타일맵을 직접 구성하고 수정하기 편리합니다.
     */

    #region 변수
    [HideInInspector] public int rows, columns; //행, 열

    public Count wallCount = new Count(0, 1); //외부의 벽과 연결된 무작위 타일에 벽을 설치
    public Count innerwallCount = new Count(0, 1); //내부의 무작위 빈 공간에 길을 막지 않는 위치를 찾아 벽을 설치
    public Count exitCount = new Count(0, 1); //탈출가능한 타일

    public Transform boardHolder, contentHolder; //실제로 타일을 배치하는 보드
    public List<Tile> TileList = new List<Tile>(); //보드에 배치된 타일의 리스트 

    public TilesPrefab backgroundTiles, contentTiles; //배경 타일과 내용물 타일에 사용할 프리펩을 보관

    private List<GameObject> randomFloors = new List<GameObject>(); //랜덤한 바닥 생성 타일
    private List<GameObject> randomInnerWalls = new List<GameObject>(); //내부 랜덤한 벽 생성 타일
    private List<GameObject> randomOuterWalls = new List<GameObject>(); //외부 랜덤한 벽 생성 타일
    private List<GameObject> gateZones = new List<GameObject>(); //시작, 종료 타일
    #endregion

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && TileList.Count == 0)
        {
            BoardSetup();
        }
    }

    #region 보드 초기화
    #endregion

    #region 초기 베이스 보드 셋업
    private void BoardSetup()
    {
        if(boardHolder != null) //미리 생성해둔 타일맵을 이용하여 작성
        {
            //타일 리스트 초기화
            randomFloors.Clear();
            randomInnerWalls.Clear();
            randomOuterWalls.Clear();
            gateZones.Clear();

            //타일 검색 및 분류
            for (int i = 0; i < boardHolder.childCount; i++)
            {
                if (boardHolder.GetChild(i).tag.Equals("randomFloor"))
                    randomFloors.Add(boardHolder.GetChild(i).gameObject);
                else if (boardHolder.GetChild(i).tag.Equals("randomInnerWall"))
                    randomInnerWalls.Add(boardHolder.GetChild(i).gameObject);
                else if (boardHolder.GetChild(i).tag.Equals("randomOuterWall"))
                    randomOuterWalls.Add(boardHolder.GetChild(i).gameObject);
                else if (boardHolder.GetChild(i).tag.Equals("gateZone"))
                    gateZones.Add(boardHolder.GetChild(i).gameObject);
            }

            if (gateZones.Count < 2)
            {
                Debug.LogError("gateZone은 최소 2개 이상이어야 합니다.");
                return;
            }

            //초기 타일링 작업
            //타일에 입력된 태그에 따라 타일을 특정한 타일로 변경
            RandomInitTile(ref randomFloors, randomFloors.Count, "floor", "floor", backgroundTiles.floorTiles, backgroundTiles.floorTiles);
            RandomInitTile(ref randomInnerWalls, Random.Range(innerwallCount.minimum, innerwallCount.maximum + 1), "wall", "floor", backgroundTiles.floorTiles);
            RandomInitTile(ref randomOuterWalls, randomOuterWalls.Count, "outerWall", "outerWall", backgroundTiles.outerwallTiles, backgroundTiles.outerwallTiles);
            RandomInitTile(ref gateZones, 1, "startZone", "floor", backgroundTiles.startTiles);
            RandomInitTile(ref gateZones, Random.Range(exitCount.minimum, exitCount.maximum + 1), "exitZone", "floor", backgroundTiles.exitTiles);

            //타일 리스트 갱신
            InitializeList(boardHolder);

            //보드 세부사항 배치
            BoardCreate();
        }
        else //타일맵을 자동으로 새로 작성
        {
            Debug.Log("참조할 보드가 없습니다.");

            /*
            boardHolder = new GameObject("Board").transform;
            boardHolder.transform.position = Vector3.zero;

            for (int x = -1; x < columns + 1; x++)
                for (int y = -1; y < rows + 1; y++)
                {
                    GameObject toInstantiate = backgroundTiles.floorTiles[Random.Range(0, backgroundTiles.floorTiles.Length)]; //내부라면 내부타일을
                    if (x == -1 || x == columns || y == -1 || y == rows)
                        toInstantiate = backgroundTiles.outerwallTiles[Random.Range(0, backgroundTiles.outerwallTiles.Length)]; //외곽이라면 외곽타일을

                    //타일 생성
                    GameObject instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(boardHolder); //생성된 타일을 보드에 넣는다.
                }
            */
        }
    }

    private void RandomInitTile(ref List<GameObject> _innerTileList, int _count, string _successTag, string _failedTag = "floor", GameObject[] _successTiles = null, GameObject[] _failedTiles = null)
    {
        //선택된 타일 변경
        //모든 타일을 변경하는 것이 아닌 총 타일 중 랜덤한 일부 타일을 선택하여 변경
        for (int i = 0; i < _count; i++)
            if (_innerTileList.Count > 0)
            {
                int randomIndex = Random.Range(0, _innerTileList.Count);
                GameObject tileChoice = _innerTileList[randomIndex];
                _innerTileList.RemoveAt(randomIndex);

                tileChoice.tag = _successTag;
                if (_successTiles != null)
                    SwapTiles(tileChoice, _successTiles[Random.Range(0, _successTiles.Length)]);
            }
            else
                break;

        //비선택 타일 변경
        //선택되지 못한 나머지 타일들을 변경
        for (int i = 0; i < _innerTileList.Count; i++)
        {
            _innerTileList[i].tag = _failedTag;
            if (_failedTiles != null)
                SwapTiles(_innerTileList[i], _failedTiles[Random.Range(0, _failedTiles.Length)]);
            else
                SwapTiles(_innerTileList[i], backgroundTiles.floorTiles[Random.Range(0, backgroundTiles.floorTiles.Length)]);
        }
    }

    private void SwapTiles(GameObject baseTile, GameObject changeTile)
    {
        //지정된 두 타일의 스프라이트를 교체
        if(baseTile.GetComponent<SpriteRenderer>() && changeTile.GetComponent<SpriteRenderer>())
            baseTile.GetComponent<SpriteRenderer>().sprite = changeTile.GetComponent<SpriteRenderer>().sprite;

    }

    private void InitializeList(Transform _boardHolder)
    {
        //타일 리스트 초기화
        TileList.Clear();

        //핼열값 구하기
        GetMatrix();

        //보드의 각 타일정보 입력
        for (int i = 0; i < _boardHolder.childCount; i++)
        {
            Tile _newTile = new Tile();

            _newTile.targetTile = _boardHolder.GetChild(i).gameObject;
            _newTile.gridX = i % rows;
            _newTile.gridY = Mathf.FloorToInt(i / rows);
            _newTile.number = i;
            _newTile.tag = _boardHolder.GetChild(i).tag;

            TileList.Add(_newTile);
            _boardHolder.GetChild(i).GetComponent<SortingGroup>().sortingOrder = -Mathf.FloorToInt(i / rows);

            if (_newTile.targetTile.GetComponent<TileInfo>())
                _newTile.targetTile.GetComponent<TileInfo>().SetTileInfo(TileList, i);
            else
                _newTile.targetTile.AddComponent<TileInfo>().SetTileInfo(TileList, i);
        }
    }

    private void GetMatrix()
    {
        //보드의 행렬을 구한다.
        if (boardHolder != null)
        {
            rows = 0;
            float _y = 0;
            for (int i = 0; i < boardHolder.childCount; i++)
            {
                if (i == 0)
                {
                    _y = boardHolder.GetChild(i).position.y;
                    rows++;
                }
                else if (Mathf.Approximately(boardHolder.GetChild(i).position.y, _y))
                    rows++;
            }

            columns = (int)Mathf.Ceil(boardHolder.childCount / rows);
        }
    }

    private bool checkBoard()
    {
        if (boardHolder && boardHolder.childCount % rows == 0 && boardHolder.childCount % columns == 0 && rows * columns == boardHolder.childCount)
            return true;
        else
            return false;
    }

    #endregion

    #region 보드 내용물 작성
    private void BoardCreate()
    {
        //타일 내부를 구성한다
        //처음은 백그라운드 타일을 설치하고 그 후 콘텐츠 타일을 설치한다
        //1) 랜덤 벽 타일 생성
        int _wallCount = Random.Range(wallCount.minimum, wallCount.maximum + 1);
        Debug.Log("벽 생성 수 : " + _wallCount);

        for (int i = 0; i < _wallCount; i++)
        {
            Tile _wall = RandomCreatableWallTile();
            if (_wall != null)
            {
                _wall.targetTile.tag = "wall";
                _wall.tag = "wall";
                SwapTiles(_wall.targetTile, backgroundTiles.wallTiles[Random.Range(0, backgroundTiles.wallTiles.Length)]);
                TileList[_wall.number] = (Tile)_wall.Clone();
            }
            else
                break;
        }

        //2) 랜덤 코너 타일 생성
        int _cornerCount = Random.Range(0, _wallCount / 2);
        for (int i = 0; i < _cornerCount; i++)
        {
            Tile _corner = SearchCornerTiler();
            if (_corner != null)
            {
                _corner.targetTile.tag = "wall";
                _corner.tag = "wall";
                SwapTiles(_corner.targetTile, backgroundTiles.wallTiles[Random.Range(0, backgroundTiles.wallTiles.Length)]);
                TileList[_corner.number] = (Tile)_corner.Clone();
            }
            else
                break;
        }

        //이외에 랜덤 이벤트 타일 생성, 랜덤 몬스터 타일 생성 등을 추가
        // ...
        // ...

        //콘텐츠 타일 생성
        ContentCreate();
    }

    private void ContentCreate()
    {
        //최종적으로 타일 내용 배치
        for (int i = 0; i < TileList.Count; i++)
        {
            GameObject _newContent;

            if (TileList[i].tag.Equals("outerWall") && contentTiles.outerwallTiles.Length > 0)
                _newContent = contentTiles.outerwallTiles[Random.Range(0, contentTiles.outerwallTiles.Length)];
            else if (TileList[i].tag.Equals("innerWall"))
                _newContent = contentTiles.innerwallTiles[Random.Range(0, contentTiles.innerwallTiles.Length)];
            else if (TileList[i].tag.Equals("wall"))
                _newContent = contentTiles.wallTiles[Random.Range(0, contentTiles.wallTiles.Length)];
            else if (TileList[i].tag.Equals("startZone"))
                _newContent = contentTiles.startTiles[Random.Range(0, contentTiles.startTiles.Length)];
            else if (TileList[i].tag.Equals("exitZone"))
                _newContent = contentTiles.exitTiles[Random.Range(0, contentTiles.exitTiles.Length)];
            else
                _newContent = null;

            if (_newContent != null)
            {
                GameObject _content = Instantiate(_newContent, contentHolder.transform);
                _content.transform.localPosition = TileList[i].targetTile.transform.localPosition;
                _content.GetComponent<SortingGroup>().sortingOrder = -Mathf.FloorToInt(i / rows);

                TileList[i].contentTile = _content;
            }
        }
    }
    #endregion

    #region 보드 탐색 알고리즘
    Tile RandomTile(string _tag = "floor")
    {
        //지정한 태그를 가진 무작위 타일을 찾아 반환
        List<Tile> _randomTileList = new List<Tile>();
        for (int i = 0; i < TileList.Count; i++)
            if(TileList[i].tag.Equals(_tag))
                _randomTileList.Add((Tile)TileList[i].Clone());

        if (_randomTileList.Count > 0)
        {
            int randomIndex = Random.Range(0, _randomTileList.Count);
            Tile randomTile = _randomTileList[randomIndex];
            return randomTile;
        }
        else
        {
            Debug.LogError("해당 태그의 타일이 없습니다.");
            return null;
        }
    }

    Tile RandomCreatableWallTile()
    {
        //벽을 생성 가능한 타일을 찾아 반환
        //벽은 무작위 타일을 검색해 생성하되, 경로를 막는 배치는 제외한다
        List<Tile> _randomTileList = new List<Tile>();
        for (int x = 1; x < columns - 1; x++)
            for (int y = 1; y < rows - 1; y++)
            {
                //설치 가능한 벽을 탐색한다.
                int i = rows * x + y;
                if (TileList[i].tag.Equals("floor"))
                {
                    //타일을 포함한 주위 9칸에 인접한 벽이 있는지 체크한다.
                    int existCount = 0;
                    for (int a = 0; a < 9; a++)
                    {
                        int n = i + (Mathf.FloorToInt(a / 3) - 1) * rows + (a % 3 - 1);
                        if(TileList[n].tag.Equals("wall") || TileList[n].tag.Equals("outerWall") || TileList[n].tag.Equals("innerWall"))
                            existCount++;

                        if (existCount >= 2)
                            break;
                    }

                    //인접한 벽이 2개 미만이라면 벽을 설치할 수 있는 타일로 설정
                    if(existCount < 2)
                        _randomTileList.Add((Tile)TileList[i].Clone());
                }
            }

        if (_randomTileList.Count > 0)
        {
            Debug.Log("설치 가능한 벽의 수 : " + _randomTileList.Count);
            int randomIndex = Random.Range(0, _randomTileList.Count);
            Tile randomTile = _randomTileList[randomIndex];
            return randomTile;
        }
        else
        {
            Debug.LogError("벽을 설치할 수 있는 바닥타일이 없습니다.");
            return null;
        }
    }

    Tile SearchCornerTiler()
    {
        List<Tile> _randomTileList = new List<Tile>();
        for (int x = 1; x < columns - 1; x++)
            for (int y = 1; y < rows - 1; y++)
            {
                //설치 가능한 벽을 탐색한다.
                int i = rows * x + y;
                if (TileList[i].tag.Equals("floor"))
                {
                    //타일과 인접한 4칸에 벽이 있는지 체크한다.
                    int existCount = 0;

                    int[] n = new int[] { i - rows, i - 1, i + rows, i + 1 };
                    for (int a = 0; a < n.Length; a++)
                        if (a < 2
                            && (TileList[n[a]].tag.Equals("wall") || TileList[n[a]].tag.Equals("outerWall") || TileList[n[a]].tag.Equals("innerWall"))
                            && (TileList[n[a + 2]].tag.Equals("wall") || TileList[n[a + 2]].tag.Equals("outerWall") || TileList[n[a + 2]].tag.Equals("innerWall")))
                            break;
                        else if (TileList[n[a]].tag.Equals("wall") || TileList[n[a]].tag.Equals("outerWall") || TileList[n[a]].tag.Equals("innerWall"))
                            existCount++;


                    //타일을 포함한 주위 9칸에 인접한 벽이 있는지 체크한다.
                    int existCount_B = 0;
                    for (int a = 0; a < 9; a++)
                    {
                        int m = i + (Mathf.FloorToInt(a / 3) - 1) * rows + (a % 3 - 1);
                        if (TileList[m].tag.Equals("wall") || TileList[m].tag.Equals("outerWall") || TileList[m].tag.Equals("innerWall"))
                            existCount_B++;
                    }


                    //현재 타일이 코너 타일인지 결정한다.
                    if (existCount == 2 && existCount_B == 2)
                        _randomTileList.Add((Tile)TileList[i].Clone());
                }
            }

        if (_randomTileList.Count > 0)
        {
            int randomIndex = Random.Range(0, _randomTileList.Count);
            Tile _result = _randomTileList[randomIndex];
            return _result;
        }
        else
        {
            Debug.LogError("코너 타일을 찾을 수 없습니다.");
            return null;
        }
    }
    #endregion

}
