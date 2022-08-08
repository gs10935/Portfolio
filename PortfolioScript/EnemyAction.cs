using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(BodySearch))]
public class EnemyAction : MonoBehaviourPunCallbacks
{
    // �� ��ũ��Ʈ���� ���� AI�� ���� �ٷ�ϴ�.
    // ���� �ൿ������ �Ϲ�, ������ ������ ���� Ÿ���� ������ Ÿ���� ���·� �����˴ϴ�.
    // �ൿ ������ ĳ������ ����, Ÿ�ٰ��� �Ÿ�,���� ��Ÿ�� � ���� �޶����ϴ�.

    #region ����
    private GameObject _charbody; //ĳ���� ������Ʈ
    private CharacterState _charstate; //ĳ���� ����
    private Status _status; //ĳ���� �������ͽ�
    private CharacterControl _charctrl; //ĳ���� ��Ʈ�ѷ�
    private NavMeshAgent _navmeshAgent; //ĳ���� �׺���̼�
    private Gravity _gravity; //ĳ���� �߷� ����

    private float _updateFrame, _eventNavTime, BattleInterval, WanderInterval; //������Ʈ �ֱ�
    private int _currentSelectPatternNum = 0; //�ֱ� ���õ� ����

    [SerializeField] private WanderPattern _wanderPattern; //������� �ɼ�
    [SerializeField] private List<PatternList> _ultimatePattern; //Ư����� ����Ʈ
    [SerializeField] private List<PatternListGroup> _patternGroup = new List<PatternListGroup>(); //�������� ����Ʈ
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
            PlayAI(); //ĳ���� AI
            _updateFrame = 0;
        }
        else
            _updateFrame += Time.deltaTime;

        NavManage(); //�׺���̼ǻ�� ����

        if (_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
            BattlePattern(); //�������� ����
        else if (!_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
            WanderPattern(); //�Ϲ����� ����
    }

    #region �׺���̼� ����
    private void NavManage() // ĳ������ Ư�����¿��� �׺���̼��� �����Ѵ�
    {
        if (_gravity.isGround && _charstate.Check_Action(true, false, false, true, false, true))
            _navmeshAgent.enabled = true;
        else if (_navmeshAgent.enabled && _eventNavTime <= 0)
            ResetNav(false);
        else
            _eventNavTime -= Time.deltaTime;
    }

    private void NavMove(Vector3 _targetPos, float _speed, GameObject _Target = null, float _stopDist = 0.5f) //�׺���̼����� ĳ���� �̵�
    {
        if (_navmeshAgent.enabled)
        {
            //����� ��ġ���� ��ǥ�� ���� �� �ִ� �׺���̼� ��ġ ����
            NavMeshHit hit;
            NavMesh.SamplePosition(_targetPos, out hit, 10f, 1);
            Vector3 Pos = hit.position;

            //�׺���̼����� �����Ͽ� Ÿ�ٿ��� �̵�
            _navmeshAgent.updatePosition = true; //�׺���̼� ��ġ ������Ʈ Ȱ��ȭ
            _navmeshAgent.nextPosition = _charbody.transform.position; //�׺���̼� ��ġ Ȱ��ȭ���ϰ� ��������ġ�� �����Ѵ�.(�����̵� ����)
            _navmeshAgent.speed = _speed; //�ӵ� ����
            _navmeshAgent.stoppingDistance = _stopDist; //���ߴ� �Ÿ� ����
            _navmeshAgent.SetDestination(Pos); //�׺���̼� �������� Ÿ�� ��ġ�� ��ȯ

            //�ִϸ��̼� ���� ��ȯ
            if (_Target)
                _charstate.charstate_action.ActionNum = 2;
            else
                _charstate.charstate_action.ActionNum = 1;

            //�������� �����ߴٸ� 
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
        //�׺���̼� ��� �ʱ�ȭ
        _navmeshAgent.nextPosition = _charbody.transform.position;
        _navmeshAgent.SetDestination(_charbody.transform.position);
        _navmeshAgent.enabled = _enable;
    }
    #endregion

    #region ĳ���� AI
    private void PlayAI()
    {
        //Ÿ�� ����, ���� ����
        if (_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
        {
            WanderInterval = 0;
            if (BattleInterval < 0)
                SetPattern_Battle();
        }
        //Ÿ�� ����, ��� ����
        else if(!_charstate.charstate_target.Target._target && _charstate.Check_Action(true, false, false, true, false, true))
        {
            BattleInterval = 0;
            if (WanderInterval < 0)
                SetPattern_Wander();
        }

        //���� �ֱ� ����
        BattleInterval -= _updateFrame;
        WanderInterval -= _updateFrame;

        //���� ��Ÿ�� ����
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
        //���� ���� �������� ���ð��� ���Ѵ�
        BattleInterval = Random.Range(2f, 4f);

        //���� ���� ������ ���ϵ��� �ɷ�����
        List<PatternList> _tempPattern = new List<PatternList>();
        bool isUlt = false;

        //Ư���� ��������� �浶�Ǿ��ٸ� �켱 ����Ѵ�
        for (int i = 0; i < _ultimatePattern.Count; i++)
            if (_ultimatePattern[i].CoolTime <= 0 &&
                (_ultimatePattern[i].availableHP_max <= 0 || (_ultimatePattern[i].availableHP_max > 0 && _status.status_base.Hp <= _status.status_base.MaxHp * _ultimatePattern[i].availableHP_max && _status.status_base.Hp >= _status.status_base.MaxHp * _ultimatePattern[i].availableHP_min)))
            {
                _tempPattern.Add(_ultimatePattern[i]);
                isUlt = true;
            }

        //Ư���� ��������� �������� ���ߴٸ� �Ϲ� ������ �߰�
        if(!isUlt)
        {
            //�Ÿ��� ���� ���� ����
            List<PatternList> _pattern = _patternGroup[_currentSelectPatternNum].patternList;
            for (int i = 0; i < _patternGroup.Count; i++)
                if (_charstate.charstate_target.Target._target &&
                    Vector3.Distance(_charstate.charstate_target.Target._target.position, _charbody.transform.position) >= _patternGroup[i].AvailableRange.x
                    && Vector3.Distance(_charstate.charstate_target.Target._target.position, _charbody.transform.position) < _patternGroup[i].AvailableRange.y)
                {
                    _pattern = _patternGroup[i].patternList;
                    break;
                }

            //��� ������ ���� �߰�
            for (int i = 0; i < _pattern.Count; i++)
                if (_pattern[i].CoolTime <= 0 &&
                    (_pattern[i].availableHP_max <= 0 || (_pattern[i].availableHP_max > 0 && _status.status_base.Hp <= _status.status_base.MaxHp * _pattern[i].availableHP_max && _status.status_base.Hp >= _status.status_base.MaxHp * _pattern[i].availableHP_min)))
                    _tempPattern.Add(_pattern[i]);
        }

        //�������� ���� ����
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
        //���� �ʱ�ȭ
        _charstate.Action_Cancelable();
        _charstate.Action_End();

        //�׺���̼� ��� �ʱ�ȭ
        ResetNav(true);

        //���� ���� �������� ���ð��� ���Ѵ�
        _wanderPattern.WanderNum = Random.Range(0, _wanderPattern.WanderPatternSep.y);
        WanderInterval = Random.Range(2f, 5f);

        _wanderPattern.WanderNavPos = _charbody.transform.position + Random.insideUnitSphere * _wanderPattern.WanderRadius;
    }

    private void BattlePattern()
    {
        if (SelectedPattern != null) //������ ������ �����Ѵٸ�
        {
            CharacterController targetctrl = _charstate.charstate_target.Target._target.GetComponent<CharacterController>();

            //���� ���� ���̰ų� Z�� ��ġ�� �ִٸ� ���� ������ �������� �̵��Ѵ�.
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
            //���� ������ �����̶�� ���� �ɼ��� �����Ѵ�
            else if (_charstate.charstate_action.ActionNum <= 2)
            {
                _charctrl.ForceDirection(_charstate.charstate_target.Target._target.transform.position);
                _charstate.Action_Start(SelectedPattern.ActionNum, _status.status_base.ActionSpeed * _status.status_result.Result_ActionSpeed, true, false);
                BattleInterval = Random.Range(SelectedPattern.Interval.x, SelectedPattern.Interval.y); //�����ֱ� ����
                SelectedPattern = null; //������ ������.
            }
        }
    }

    private void WanderPattern()
    {
        if (_wanderPattern.WanderNum <= _wanderPattern.WanderPatternSep.x) //��� ������
            _charstate.charstate_action.ActionNum = 0;
        else if (_wanderPattern.WanderNum > _wanderPattern.WanderPatternSep.x) //��� ��ȸ����
            NavMove(_wanderPattern.WanderNavPos, _status.status_base.Speed * _status.status_result.Result_MoveSpeed);
    }
    #endregion

}
