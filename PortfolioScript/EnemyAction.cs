using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(BodySearch))]
public class EnemyAction : MonoBehaviourPunCallbacks
{
    // 이 스크립트에선 적의 AI에 대해 다룹니다.
    // 적의 행동패턴은 일반, 전투로 나뉘며 현재 타겟의 유무와 타겟의 상태로 결정됩니다.
    // 행동 패턴은 캐릭터의 상태, 타겟과의 거리,내부 쿨타임 등에 따라 달라집니다.

    #region 변수
    private GameObject _charbody; //캐릭터 오브젝트
    private CharacterState _charstate; //캐릭터 상태
    private Status _status; //캐릭터 스테이터스
    private CharacterControl _charctrl; //캐릭터 컨트롤러
    private NavMeshAgent _navmeshAgent; //캐릭터 네비게이션
    private Gravity _gravity; //캐릭터 중력 상태

    private float _updateFrame, _eventNavTime, BattleInterval, WanderInterval; //업데이트 주기
    private int _currentSelectPatternNum = 0; //최근 선택된 패턴

    [SerializeField] private WanderPattern _wanderPattern; //평시패턴 옵션
    [SerializeField] private List<PatternList> _ultimatePattern; //특수기술 리스트
    [SerializeField] private List<PatternListGroup> _patternGroup = new List<PatternListGroup>(); //전투패턴 리스트
    private PatternList SelectedPattern;

    private void Start()
    {
        _charbody = GetComponent<BodySearch>().CharacterBody;
        _charstate = _charbody.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterState>();
        _status = _charbody.GetComponent<CharacterManager>()._Controller.GetComponent<Status>();
        _charctrl = _charbody.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterControl>();
        _navmeshAgent = _charbody.GetComponent<NavMeshAgent>();
        _gravity = _charbody.GetComponent<CharacterManager>()._Controller.GetComponent<Gravity>();
    }
    #endregion

    private void Update()
    {
        if (_updateFrame > 0.4f)
        {
            PlayAI(); //캐릭터 AI
            _updateFrame = 0;
        }
        else
            _updateFrame += Time.deltaTime;

        NavManage(); //네비게이션사용 관리

        if (_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
            BattlePattern(); //전투패턴 진행
        else if (!_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
            WanderPattern(); //일반패턴 진행
    }

    #region 네비게이션 관리
    private void NavManage() // 캐릭터의 특정상태에서 네비게이션을 제한한다
    {
        if (_gravity.isGround && _charstate.Check_Action(true, false, false, true, false, true))
            _navmeshAgent.enabled = true;
        else if (_navmeshAgent.enabled && _eventNavTime <= 0)
            ResetNav(false);
        else
            _eventNavTime -= Time.deltaTime;
    }

    private void NavMove(Vector3 _targetPos, float _speed, GameObject _Target = null, float _stopDist = 0.5f) //네비게이션으로 캐릭터 이동
    {
        if (_navmeshAgent.enabled)
        {
            //가까운 위치에서 목표로 잡을 수 있는 네비게이션 위치 포착
            NavMeshHit hit;
            NavMesh.SamplePosition(_targetPos, out hit, 10f, 1);
            Vector3 Pos = hit.position;

            //네비게이션으로 추적하여 타겟에게 이동
            _navmeshAgent.updatePosition = true; //네비게이션 위치 업데이트 활성화
            _navmeshAgent.nextPosition = _charbody.transform.position; //네비게이션 위치 활성화를하고 마지막위치를 갱신한다.(순간이동 방지)
            _navmeshAgent.speed = _speed; //속도 조정
            _navmeshAgent.stoppingDistance = _stopDist; //멈추는 거리 조정
            _navmeshAgent.SetDestination(Pos); //네비게이션 목적지를 타겟 위치로 전환

            //애니메이션 상태 전환
            if (_Target)
                _charstate.charstate_action.ActionNum = 2;
            else
                _charstate.charstate_action.ActionNum = 1;

            //목적지에 도착했다면 
            if (_navmeshAgent.remainingDistance <= _navmeshAgent.stoppingDistance && _navmeshAgent.speed > 0.1f)
            {
                if (!_navmeshAgent.hasPath || _navmeshAgent.velocity.sqrMagnitude == 0f)
                    _charstate.charstate_action.ActionNum = 0;
            }
            else
                _charctrl.ForceDirection(Pos);
        }
    }

    private void ResetNav(bool _enable)
    {
        //네비게이션 경로 초기화
        _navmeshAgent.nextPosition = _charbody.transform.position;
        _navmeshAgent.SetDestination(_charbody.transform.position);
        _navmeshAgent.enabled = _enable;
    }
    #endregion

    #region 캐릭터 AI
    private void PlayAI()
    {
        //타겟 존재, 전투 패턴
        if (_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
        {
            WanderInterval = 0;
            if (BattleInterval < 0)
                SetPattern_Battle();
        }
        //타겟 없음, 평시 패턴
        else if(!_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
        {
            BattleInterval = 0;
            if (WanderInterval < 0)
                SetPattern_Wander();
        }

        //패턴 주기 갱신
        BattleInterval -= _updateFrame;
        WanderInterval -= _updateFrame;

        //패턴 쿨타임 갱신
        for (int i = 0; i < _patternGroup.Count; i++)
            for (int p = 0; p < _patternGroup[i].patternList.Count; p++)
                if (_patternGroup[i].patternList[p].CoolTime > 0)
                    _patternGroup[i].patternList[p].CoolTime -= Time.deltaTime;
        for (int i = 0; i < _ultimatePattern.Count; i++)
            if (_ultimatePattern[i].CoolTime > 0)
                _ultimatePattern[i].CoolTime -= Time.deltaTime;
    }

    private void SetPattern_Battle()
    {
        //다음 패턴 결정까지 대기시간을 정한다
        BattleInterval = Random.Range(2f, 4f);

        //먼저 선택 가능한 패턴들을 걸러낸다
        List<PatternList> _tempPattern = new List<PatternList>();
        bool isUlt = false;

        //특수기 사용조건이 충독되었다면 우선 사용한다
        for (int i = 0; i < _ultimatePattern.Count; i++)
            if (_ultimatePattern[i].CoolTime <= 0 &&
                (_ultimatePattern[i].availableHP_max <= 0 || (_ultimatePattern[i].availableHP_max > 0 && _status.status_base.Hp <= _status.status_base.MaxHp * _ultimatePattern[i].availableHP_max && _status.status_base.Hp >= _status.status_base.MaxHp * _ultimatePattern[i].availableHP_min)))
            {
                _tempPattern.Add(_ultimatePattern[i]);
                isUlt = true;
            }

        //특수기 사용조건을 충종하지 못했다면 일반 패턴을 추가
        if(!isUlt)
        {
            //거리에 따른 패턴 결정
            List<PatternList> _pattern = _patternGroup[_currentSelectPatternNum].patternList;
            for (int i = 0; i < _patternGroup.Count; i++)
                if (_charstate.charstate_target.Target._target &&
                    Vector3.Distance(_charstate.charstate_target.Target._target.position, _charbody.transform.position) >= _patternGroup[i].AvailableRange.x
                    && Vector3.Distance(_charstate.charstate_target.Target._target.position, _charbody.transform.position) < _patternGroup[i].AvailableRange.y)
                {
                    _pattern = _patternGroup[i].patternList;
                    break;
                }

            //사용 가능한 패턴 추가
            for (int i = 0; i < _pattern.Count; i++)
                if (_pattern[i].CoolTime <= 0 &&
                    (_pattern[i].availableHP_max <= 0 || (_pattern[i].availableHP_max > 0 && _status.status_base.Hp <= _status.status_base.MaxHp * _pattern[i].availableHP_max && _status.status_base.Hp >= _status.status_base.MaxHp * _pattern[i].availableHP_min)))
                    _tempPattern.Add(_pattern[i]);
        }

        //전투패턴 랜덤 선택
        float totalProb = 0;
        for (int i = 0; i < _tempPattern.Count; i++)
            totalProb += _tempPattern[i].Prob;
        float selectedProb = Random.Range(0, totalProb), select = 0;
        for (int i = 0; i < _tempPattern.Count; i++)
        {
            select += _tempPattern[i].Prob;
            if (select >= selectedProb)
            {
                SelectedPattern = _tempPattern[i];
                _tempPattern[i].CoolTime = _tempPattern[i].maxCoolTime;
                break;
            }
        }
    }

    private void SetPattern_Wander()
    {
        //상태 초기화
        _charstate.Action_Cancelable();
        _charstate.Action_End();

        //네비게이션 경로 초기화
        ResetNav(true);

        //다음 패턴 결정까지 대기시간을 정한다
        _wanderPattern.WanderNum = Random.Range(0, _wanderPattern.WanderPatternSep.y);
        WanderInterval = Random.Range(2f, 5f);

        _wanderPattern.WanderNavPos = _charbody.transform.position + Random.insideUnitSphere * _wanderPattern.WanderRadius;
    }

    private void BattlePattern()
    {
        if (SelectedPattern != null) //수행할 패턴이 존재한다면
        {
            CharacterController targetctrl = _charstate.charstate_target.Target._target.GetComponent<CharacterController>();

            //공격 범위 밖이거나 Z축 위치가 멀다면 공격 가능한 지점으로 이동한다.
            if (Vector3.Distance(_charstate.charstate_target.Target._target.transform.position, _charbody.transform.position) > targetctrl.radius + SelectedPattern.MaxRange || ((SelectedPattern.AxisZRange > 0 && Mathf.Abs(_charbody.transform.position.z - _charstate.charstate_target.Target._target.transform.position.z) > SelectedPattern.AxisZRange + _navmeshAgent.stoppingDistance)))
            {
                if (SelectedPattern.MinRange > SelectedPattern.MaxRange)
                    SelectedPattern.MinRange = SelectedPattern.MaxRange;
                Vector3 dest;
                if (SelectedPattern.AxisZRange > 0 && Mathf.Abs(_charbody.transform.position.z - _charstate.charstate_target.Target._target.transform.position.z) > SelectedPattern.AxisZRange)
                    dest = _charstate.charstate_target.Target._target.transform.position + (new Vector3(_charbody.transform.position.x, _charbody.transform.position.y, _charstate.charstate_target.Target._target.transform.position.z) - _charstate.charstate_target.Target._target.transform.position).normalized * SelectedPattern.MinRange;
                else
                    dest = _charstate.charstate_target.Target._target.transform.position + (_charbody.transform.position - _charstate.charstate_target.Target._target.transform.position).normalized * SelectedPattern.MinRange;

                NavMove(dest, _status.status_base.Speed * _status.status_base.RunSpeed * _status.status_result.Result_MoveSpeed, _charstate.charstate_target.Target._target.gameObject, targetctrl.radius);
            }
            //공격 가능한 조건이라면 패턴 옵션을 수행한다
            else if (_charstate.charstate_action.ActionNum <= 2)
            {
                _charctrl.ForceDirection(_charstate.charstate_target.Target._target.transform.position);
                _charstate.Action_Start(SelectedPattern.ActionNum, _status.status_base.ActionSpeed * _status.status_result.Result_ActionSpeed, true, false);
                BattleInterval = Random.Range(SelectedPattern.Interval.x, SelectedPattern.Interval.y); //공격주기 갱신
                SelectedPattern = null; //패턴을 끝낸다.
            }
        }
    }

    private void WanderPattern()
    {
        if (_wanderPattern.WanderNum <= _wanderPattern.WanderPatternSep.x) //평시 대기상태
            _charstate.charstate_action.ActionNum = 0;
        else if (_wanderPattern.WanderNum > _wanderPattern.WanderPatternSep.x) //평시 배회상태
            NavMove(_wanderPattern.WanderNavPos, _status.status_base.Speed * _status.status_result.Result_MoveSpeed);
    }
    #endregion

}
