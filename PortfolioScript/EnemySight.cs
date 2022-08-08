using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BodySearch))]
public class EnemySight : MonoBehaviour
{
    // 이 스크립트에선 적의 시야와 어그로시트 관리에 대해 다룹니다.
    // 적의 시야각과 시야 거리 등을 설정할 수 있으며 범위 안의 타겟을 탐색하여 어그로시트에 추가합니다.
    // 적은 타겟의 마지막 위치를 기억할 수 있습니다. 타겟과의 거리가 어느 정도 떨어진 상태에서 장애물 등에 가려진다면 해당 위치까지 추적하고, 포착에 실패하면 타겟을 로스트합니다.

    #region 변수
    [SerializeField] private float _viewAnglel, _viewDistance, _viewHeight, _traceDistance, _confirmDistance, _attackAngle; //탐색 옵션
    [SerializeField] private LayerMask _targerMask, _obstacleMask; //타겟 마스크, 장애물
    [SerializeField] private string[] _targetTag; //타겟 태그

    public List<TargetList> currentTargetList = new List<TargetList>(); //현재 타겟 목록

    private Vector3 _lastTargetPos; //마지막 태겟 위치
    private bool _keepTarget; //타겟 주시상태
    private float _sightInterval; //업데이트 주기

    private Transform _charbody; //캐릭터 트랜스폼
    private CharacterState _charstate; //캐릭터 상태
    private bool _drawRay = true;

    private void Start()
    {
        _charbody = GetComponent<BodySearch>().CharacterBody.transform;
        _charstate = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterState>();
    }
    #endregion

    private void Update()
    {
        if (_sightInterval > 0)
            _sightInterval -= Time.deltaTime;
        else
        {
            _sightInterval = 0.4f;
            AggroManage();
        }
    }

    #region 시야에서 타겟 서치
    private void FindVisibleTargets()
    {
        Vector3 facingDir = new Vector3(_charbody.localScale.x, 0, 0); //횡스크롤 시야
        Vector3 _pos = _charbody.position + Vector3.up * _viewHeight; //시야 시작점
        DrawView(_viewDistance, _viewAnglel, _traceDistance, _pos); //시야각 그리기

        #region 새로운 타겟을 탐색
        //새로운 타겟을 탐색
        if (!_charstate.charstate_target.Target._target)
        {
            //시야 거리 내 타겟레이어의 모든 컬라이더 받아오기
            Collider[] targets = Physics.OverlapSphere(_pos, _viewDistance, _targerMask);

            for (int i = 0; i < targets.Length; i++)
                for (int s = 0; s < _targetTag.Length; s++)
                    if (targets[i].tag.Equals(_targetTag[s]))
                    {
                        Transform target = targets[i].transform;
                        Vector3 targetposition = target.transform.position; //타겟 위치
                        Vector3 dirToTarget = (targetposition - _pos).normalized; //타겟의 방향

                        //_transform.forward와 dirToTarget은 모두 단위벡터이므로 내적값은 두 벡터가 이루는 각의 Cos값과 같다.
                        //내적값이 시야각/2의 Cos값보다 크면 시야에 들어온 것이다.
                        if (Vector3.Dot(facingDir, dirToTarget) > Mathf.Cos((_viewAnglel / 2) * Mathf.Deg2Rad))
                        {
                            float distToTarget = Vector3.Distance(_pos, targetposition);
                            if (!Physics.Raycast(_pos, dirToTarget, distToTarget, _obstacleMask)) //장애물이 없으면
                            {
                                Debug.DrawLine(_pos, targetposition, Color.red);
                                AddAggroTarget(target, 0); //어그로 시트에 타겟 추가
                            }
                        }

                        break;
                    }
        }
        #endregion
        #region 기존 타겟을 추적
        //기존 타겟을 추적
        else if (_charstate.charstate_target.Target._target)
        {
            //추적 거리에서 타겟을 검색
            Collider[] tracetargets = Physics.OverlapSphere(_pos, _traceDistance, _targerMask); 
            bool isWatchTarget = false; //주시상태 체크
            bool isLost = true; //타겟 로스트 유무

            for (int i = 0; i < tracetargets.Length; i++)
                if (System.Object.ReferenceEquals(_charstate.charstate_target.Target._target, tracetargets[i].transform))
                {
                    isWatchTarget = true;
                    Transform target = tracetargets[i].transform;
                    Vector3 targetposition = target.transform.position; //타겟 위치
                    Vector3 dirToTarget = (targetposition - _pos).normalized; //타겟의 방향
                    float distToTarget = Vector3.Distance(_pos, targetposition); //타겟과의 거리

                    //▶ 타겟 추적
                    if (!Physics.Raycast(_pos, dirToTarget, distToTarget, _obstacleMask) || _keepTarget) //사이에 장애물이 없거나, 주시상태라면
                    {
                        Debug.DrawLine(_pos, targetposition, Color.red);
                        _lastTargetPos = targetposition; //타겟 위치 기억
                        isWatchTarget = true; //타겟 주시 활성화
                        isLost = false; //만약 주시상태라면 타겟을 계속 포착한다
                    }

                    break;
                }

            _keepTarget = isWatchTarget; //주시상태 갱신

            if (!isWatchTarget && Vector3.Distance(_pos, _lastTargetPos) < _confirmDistance) //최종 기억위치에서 타겟 포착을 실패했다면 타겟을 로스트한다
                DelAggroTarget();
        }
        #endregion
    }


    #endregion

    #region DrawRay
    private void DrawView(float viewdistance, float angle, float tracedistance, Vector3 Pos)
    {
        //디버그용 주시각 그리기
        if (Input.GetKeyDown(KeyCode.P))
            _drawRay = !_drawRay;

        if (_drawRay)
        {
            float Distance;

            if (_charstate.charstate_target.Target._target)
                Distance = tracedistance;
            else
                Distance = viewdistance;

            Vector3 leftBoundary = DirFromAngle(-angle / 2);
            Vector3 rightBoundary = DirFromAngle(angle / 2);
            Debug.DrawLine(Pos, Pos + leftBoundary * Distance, Color.blue);
            Debug.DrawLine(Pos, Pos + rightBoundary * Distance, Color.blue);

            if (_charstate.charstate_target.Target._target)
                Debug.DrawLine(_lastTargetPos, _lastTargetPos + Vector3.up, Color.red);
            if (_keepTarget && _charstate.charstate_target.Target._target)
                Debug.DrawLine(Pos, _charstate.charstate_target.Target._target.transform.position, Color.red);
        }
    }

    //캐릭터 방향에 따른 시야각 반환
    private Vector3 DirFromAngle(float angleInDegrees)
    {
        //좌우 회전값 갱신
        if (_charstate.charstate_behavior.FacingRight) //왼쪽방향
            angleInDegrees += _charbody.eulerAngles.y + 90f;
        else
            angleInDegrees += _charbody.eulerAngles.y - 90f;

        //경계 벡터값 반환
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
    #endregion

    #region 어그로 리스트 관리
    public void AggroManage() // 어그로 시트 관리
    {
        //타겟지정이 불가능한 대상은 지운다
        if (_charstate.charstate_target.Target._target && !CheckTarget(_charstate.charstate_target.Target._target))
            DelAggroTarget();

        //다음 어그로 타겟 잡기
        if (_charstate.Check_Action(true, false, false, true, false, true))
            NextAggroTarget(true);
    }

    private bool CheckTarget(Transform _target)
    {
        bool _check = true;
        CharacterState _targetstate = _target.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterState>();

        if(!_target.gameObject.activeInHierarchy || _targetstate.charstate_limit.State_Death) //타겟이 비활성화 되거나 죽었다면
            _check = false;

        if(_check)
            for (int i = 0; i < _targetTag.Length; i++)
                if(_targetTag[i].Equals(_target.tag)) //타겟 태그를 체크한다
                {
                    _check = true;
                    break;
                }
                else
                {
                    _check = false;
                }

        return _check;
    }

    public void AddAggroTarget(Transform _target, float _aggro)
    {
        if (_target.GetComponent<CharacterManager>() && CheckTarget(_target) && _charstate)
        {
            bool isExsit = false;

            //이미 어그로 시트에 등록 된 타겟인지 체크한다
            for (int i = currentTargetList.Count - 1; i >= 0; i--)
                if (currentTargetList[i]._target)
                {
                    if (System.Object.ReferenceEquals(currentTargetList[i]._target, _target))
                    {
                        currentTargetList[i]._aggro += _aggro;
                        if (_charstate.charstate_target.Target._aggro < currentTargetList[i]._aggro) //현재 타겟보다 어그로 수치가 높다면 갱신
                        {
                            _charstate.charstate_target.Target._target = currentTargetList[i]._target;
                            _charstate.charstate_target.Target._aggro = currentTargetList[i]._aggro;
                        }

                        isExsit = true;
                        break;
                    }
                }
                else
                    currentTargetList.RemoveAt(i);

            //기존 어그로 시트에 없다면
            if (!isExsit)
            {
                TargetList newList = new TargetList();
                newList._target = _target;
                newList._aggro = _aggro;
                currentTargetList.Add(newList);
            }
        }
    }

    private void DelAggroTarget() //현재타겟을 삭제
    {
        if (_charstate.Check_Action(true, false, false, true, false, true))
        {
            for (int i = 0; i < currentTargetList.Count; i++)
                if (System.Object.ReferenceEquals(currentTargetList[i]._target, _charstate.charstate_target.Target._target))
                {
                    currentTargetList.RemoveAt(i);
                    _charstate.charstate_target.Target._target = null;
                    _charstate.charstate_target.Target._aggro = 0;
                    break;
                }

            NextAggroTarget();
        }
    }
    
    private void NextAggroTarget() //다음 타겟 잡기
    {
        //어그로 시트에서 어그로 수치가 가장 높은 타겟으로 설정한다.
        float _dist = 0;
        for (int i = currentTargetList.Count - 1; i >= 0; i--)
            if (currentTargetList[i]._target)
            {
                //어그로 자연감소
                if (_isManage)
                {
                    if (currentTargetList[i]._aggro > 1000)
                        currentTargetList[i]._aggro = 1000;
                    if (currentTargetList[i]._aggro > 0)
                        currentTargetList[i]._aggro -= 40f * Time.deltaTime;
                    else
                        currentTargetList[i]._aggro = 0;

                    if (_charstate.charstate_target.Target._target && System.Object.ReferenceEquals(_charstate.charstate_target.Target._target, currentTargetList[i]._target))
                        _charstate.charstate_target.Target._aggro = currentTargetList[i]._aggro;
                }

                //타겟 교체
                if (!_charstate.charstate_target.Target._target)
                {
                    _dist = Vector3.Distance(_charbody.position, currentTargetList[i]._target.position);
                    _charstate.charstate_target.Target._target = currentTargetList[i]._target;
                    _charstate.charstate_target.Target._aggro = currentTargetList[i]._aggro;
                }
                else if (_charstate.charstate_target.Target._aggro < currentTargetList[i]._aggro
                    || (_charstate.charstate_target.Target._aggro <= currentTargetList[i]._aggro && Vector3.Distance(_charbody.position, currentTargetList[i]._target.position) < _dist))
                {
                    _dist = Vector3.Distance(_charbody.position, currentTargetList[i]._target.position);
                    _charstate.charstate_target.Target._target = currentTargetList[i]._target.transform;
                    _charstate.charstate_target.Target._aggro = currentTargetList[i]._aggro;
                }
            }
            else
                currentTargetList.RemoveAt(i);
    }
    #endregion
}
