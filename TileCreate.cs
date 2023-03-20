using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


public class TileCreate : MonoBehaviour
{
    /*
     * Ÿ�ϸ��� �����ϴ� ��ũ��Ʈ�Դϴ�.
     * ó������ �����ϴ� ���� �ƴ� ����Ƽ ���������� �̸� ����� ���� Ÿ�ϼ��� �����Ͽ� ������ Ÿ���� ��ġ�ϴ� �մϴ�.
     * �����ڰ� Ÿ�ϸ��� ���������� Ȯ���� �� �ְ� Ÿ�ϸ��� ���� �����ϰ� �����ϱ� ���մϴ�.
     */

    #region ����
    [HideInInspector] public int rows, columns; //��, ��

    public Count wallCount = new Count(0, 1); //�ܺ��� ���� ����� ������ Ÿ�Ͽ� ���� ��ġ
    public Count innerwallCount = new Count(0, 1); //������ ������ �� ������ ���� ���� �ʴ� ��ġ�� ã�� ���� ��ġ
    public Count exitCount = new Count(0, 1); //Ż�Ⱑ���� Ÿ��

    public Transform boardHolder, contentHolder; //������ Ÿ���� ��ġ�ϴ� ����
    public List<Tile> TileList = new List<Tile>(); //���忡 ��ġ�� Ÿ���� ����Ʈ 

    public TilesPrefab backgroundTiles, contentTiles; //��� Ÿ�ϰ� ���빰 Ÿ�Ͽ� ����� �������� ����

    private List<GameObject> randomFloors = new List<GameObject>(); //������ �ٴ� ���� Ÿ��
    private List<GameObject> randomInnerWalls = new List<GameObject>(); //���� ������ �� ���� Ÿ��
    private List<GameObject> randomOuterWalls = new List<GameObject>(); //�ܺ� ������ �� ���� Ÿ��
    private List<GameObject> gateZones = new List<GameObject>(); //����, ���� Ÿ��
    #endregion

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && TileList.Count == 0)
        {
            BoardSetup();
        }
    }

    #region ���� �ʱ�ȭ
    #endregion

    #region �ʱ� ���̽� ���� �¾�
    private void BoardSetup()
    {
        if(boardHolder != null) //�̸� �����ص� Ÿ�ϸ��� �̿��Ͽ� �ۼ�
        {
            //Ÿ�� ����Ʈ �ʱ�ȭ
            randomFloors.Clear();
            randomInnerWalls.Clear();
            randomOuterWalls.Clear();
            gateZones.Clear();

            //Ÿ�� �˻� �� �з�
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
                Debug.LogError("gateZone�� �ּ� 2�� �̻��̾�� �մϴ�.");
                return;
            }

            //�ʱ� Ÿ�ϸ� �۾�
            //Ÿ�Ͽ� �Էµ� �±׿� ���� Ÿ���� Ư���� Ÿ�Ϸ� ����
            RandomInitTile(ref randomFloors, randomFloors.Count, "floor", "floor", backgroundTiles.floorTiles, backgroundTiles.floorTiles);
            RandomInitTile(ref randomInnerWalls, Random.Range(innerwallCount.minimum, innerwallCount.maximum + 1), "wall", "floor", backgroundTiles.floorTiles);
            RandomInitTile(ref randomOuterWalls, randomOuterWalls.Count, "outerWall", "outerWall", backgroundTiles.outerwallTiles, backgroundTiles.outerwallTiles);
            RandomInitTile(ref gateZones, 1, "startZone", "floor", backgroundTiles.startTiles);
            RandomInitTile(ref gateZones, Random.Range(exitCount.minimum, exitCount.maximum + 1), "exitZone", "floor", backgroundTiles.exitTiles);

            //Ÿ�� ����Ʈ ����
            InitializeList(boardHolder);

            //���� ���λ��� ��ġ
            BoardCreate();
        }
        else //Ÿ�ϸ��� �ڵ����� ���� �ۼ�
        {
            Debug.Log("������ ���尡 �����ϴ�.");

            /*
            boardHolder = new GameObject("Board").transform;
            boardHolder.transform.position = Vector3.zero;

            for (int x = -1; x < columns + 1; x++)
                for (int y = -1; y < rows + 1; y++)
                {
                    GameObject toInstantiate = backgroundTiles.floorTiles[Random.Range(0, backgroundTiles.floorTiles.Length)]; //���ζ�� ����Ÿ����
                    if (x == -1 || x == columns || y == -1 || y == rows)
                        toInstantiate = backgroundTiles.outerwallTiles[Random.Range(0, backgroundTiles.outerwallTiles.Length)]; //�ܰ��̶�� �ܰ�Ÿ����

                    //Ÿ�� ����
                    GameObject instance = Instantiate(toInstantiate, new Vector3(x, y, 0f), Quaternion.identity) as GameObject;
                    instance.transform.SetParent(boardHolder); //������ Ÿ���� ���忡 �ִ´�.
                }
            */
        }
    }

    private void RandomInitTile(ref List<GameObject> _innerTileList, int _count, string _successTag, string _failedTag = "floor", GameObject[] _successTiles = null, GameObject[] _failedTiles = null)
    {
        //���õ� Ÿ�� ����
        //��� Ÿ���� �����ϴ� ���� �ƴ� �� Ÿ�� �� ������ �Ϻ� Ÿ���� �����Ͽ� ����
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

        //���� Ÿ�� ����
        //���õ��� ���� ������ Ÿ�ϵ��� ����
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
        //������ �� Ÿ���� ��������Ʈ�� ��ü
        if(baseTile.GetComponent<SpriteRenderer>() && changeTile.GetComponent<SpriteRenderer>())
            baseTile.GetComponent<SpriteRenderer>().sprite = changeTile.GetComponent<SpriteRenderer>().sprite;

    }

    private void InitializeList(Transform _boardHolder)
    {
        //Ÿ�� ����Ʈ �ʱ�ȭ
        TileList.Clear();

        //�ۿ��� ���ϱ�
        GetMatrix();

        //������ �� Ÿ������ �Է�
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
        //������ ����� ���Ѵ�.
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

    #region ���� ���빰 �ۼ�
    private void BoardCreate()
    {
        //Ÿ�� ���θ� �����Ѵ�
        //ó���� ��׶��� Ÿ���� ��ġ�ϰ� �� �� ������ Ÿ���� ��ġ�Ѵ�
        //1) ���� �� Ÿ�� ����
        int _wallCount = Random.Range(wallCount.minimum, wallCount.maximum + 1);
        Debug.Log("�� ���� �� : " + _wallCount);

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

        //2) ���� �ڳ� Ÿ�� ����
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

        //�̿ܿ� ���� �̺�Ʈ Ÿ�� ����, ���� ���� Ÿ�� ���� ���� �߰�
        // ...
        // ...

        //������ Ÿ�� ����
        ContentCreate();
    }

    private void ContentCreate()
    {
        //���������� Ÿ�� ���� ��ġ
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

    #region ���� Ž�� �˰���
    Tile RandomTile(string _tag = "floor")
    {
        //������ �±׸� ���� ������ Ÿ���� ã�� ��ȯ
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
            Debug.LogError("�ش� �±��� Ÿ���� �����ϴ�.");
            return null;
        }
    }

    Tile RandomCreatableWallTile()
    {
        //���� ���� ������ Ÿ���� ã�� ��ȯ
        //���� ������ Ÿ���� �˻��� �����ϵ�, ��θ� ���� ��ġ�� �����Ѵ�
        List<Tile> _randomTileList = new List<Tile>();
        for (int x = 1; x < columns - 1; x++)
            for (int y = 1; y < rows - 1; y++)
            {
                //��ġ ������ ���� Ž���Ѵ�.
                int i = rows * x + y;
                if (TileList[i].tag.Equals("floor"))
                {
                    //Ÿ���� ������ ���� 9ĭ�� ������ ���� �ִ��� üũ�Ѵ�.
                    int existCount = 0;
                    for (int a = 0; a < 9; a++)
                    {
                        int n = i + (Mathf.FloorToInt(a / 3) - 1) * rows + (a % 3 - 1);
                        if(TileList[n].tag.Equals("wall") || TileList[n].tag.Equals("outerWall") || TileList[n].tag.Equals("innerWall"))
                            existCount++;

                        if (existCount >= 2)
                            break;
                    }

                    //������ ���� 2�� �̸��̶�� ���� ��ġ�� �� �ִ� Ÿ�Ϸ� ����
                    if(existCount < 2)
                        _randomTileList.Add((Tile)TileList[i].Clone());
                }
            }

        if (_randomTileList.Count > 0)
        {
            Debug.Log("��ġ ������ ���� �� : " + _randomTileList.Count);
            int randomIndex = Random.Range(0, _randomTileList.Count);
            Tile randomTile = _randomTileList[randomIndex];
            return randomTile;
        }
        else
        {
            Debug.LogError("���� ��ġ�� �� �ִ� �ٴ�Ÿ���� �����ϴ�.");
            return null;
        }
    }

    Tile SearchCornerTiler()
    {
        List<Tile> _randomTileList = new List<Tile>();
        for (int x = 1; x < columns - 1; x++)
            for (int y = 1; y < rows - 1; y++)
            {
                //��ġ ������ ���� Ž���Ѵ�.
                int i = rows * x + y;
                if (TileList[i].tag.Equals("floor"))
                {
                    //Ÿ�ϰ� ������ 4ĭ�� ���� �ִ��� üũ�Ѵ�.
                    int existCount = 0;

                    int[] n = new int[] { i - rows, i - 1, i + rows, i + 1 };
                    for (int a = 0; a < n.Length; a++)
                        if (a < 2
                            && (TileList[n[a]].tag.Equals("wall") || TileList[n[a]].tag.Equals("outerWall") || TileList[n[a]].tag.Equals("innerWall"))
                            && (TileList[n[a + 2]].tag.Equals("wall") || TileList[n[a + 2]].tag.Equals("outerWall") || TileList[n[a + 2]].tag.Equals("innerWall")))
                            break;
                        else if (TileList[n[a]].tag.Equals("wall") || TileList[n[a]].tag.Equals("outerWall") || TileList[n[a]].tag.Equals("innerWall"))
                            existCount++;


                    //Ÿ���� ������ ���� 9ĭ�� ������ ���� �ִ��� üũ�Ѵ�.
                    int existCount_B = 0;
                    for (int a = 0; a < 9; a++)
                    {
                        int m = i + (Mathf.FloorToInt(a / 3) - 1) * rows + (a % 3 - 1);
                        if (TileList[m].tag.Equals("wall") || TileList[m].tag.Equals("outerWall") || TileList[m].tag.Equals("innerWall"))
                            existCount_B++;
                    }


                    //���� Ÿ���� �ڳ� Ÿ������ �����Ѵ�.
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
            Debug.LogError("�ڳ� Ÿ���� ã�� �� �����ϴ�.");
            return null;
        }
    }
    #endregion

}
