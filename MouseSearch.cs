using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSearch : MonoBehaviour
{
    #region ����
    public TileGridBehavior _gridBehavior;

    public bool pointCheck;
    public LayerMask targetLayer;
    public GameObject currentSelectedTile;

    private Camera mainCamera;
    #endregion

    private void Awake()
    {
        mainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
    }

    private void Update()
    {
        if(pointCheck)
        {
            SearchPoint();
        }
    }

    private void SearchPoint()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 1f);

        if (Physics.Raycast(ray, out hit, 9999, targetLayer))
            if(currentSelectedTile == null || !System.Object.ReferenceEquals(currentSelectedTile.gameObject, hit.transform.gameObject))
            {
                Debug.Log("Ÿ�� ���� : " + currentSelectedTile.gameObject + ", " + hit.transform.gameObject);
                currentSelectedTile = hit.transform.gameObject;

                if (currentSelectedTile.GetComponent<TileInfo>())
                {
                    _gridBehavior.SetBehavior(currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().gridX, currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().gridY);

                    if (currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().targetTile)
                        currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().targetTile.GetComponent<SpriteRenderer>().color = Color.cyan;

                    if (currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().contentTile)
                        currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().contentTile.GetComponent<SpriteRenderer>().color = Color.cyan;
                }
            }

        //���콺 ��Ŭ�� : ��ŸƮ ��ǥ ������
        if(currentSelectedTile && currentSelectedTile.GetComponent<TileInfo>() && Input.GetMouseButton(0))
            _gridBehavior.SetStartPoint(currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().gridX, currentSelectedTile.GetComponent<TileInfo>().GetTileInfo().gridY);
    }

}
