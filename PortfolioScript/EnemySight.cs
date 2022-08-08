using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BodySearch))]
public class EnemySight : MonoBehaviour
{
    // �� ��ũ��Ʈ���� ���� �þ߿� ��׷ν�Ʈ ������ ���� �ٷ�ϴ�.
    // ���� �þ߰��� �þ� �Ÿ� ���� ������ �� ������ ���� ���� Ÿ���� Ž���Ͽ� ��׷ν�Ʈ�� �߰��մϴ�.
    // ���� Ÿ���� ������ ��ġ�� ����� �� �ֽ��ϴ�. Ÿ�ٰ��� �Ÿ��� ��� ���� ������ ���¿��� ��ֹ� � �������ٸ� �ش� ��ġ���� �����ϰ�, ������ �����ϸ� Ÿ���� �ν�Ʈ�մϴ�.

    #region ����
    [SerializeField] private float _viewAnglel, _viewDistance, _viewHeight, _traceDistance, _confirmDistance, _attackAngle; //Ž�� �ɼ�
    [SerializeField] private LayerMask _targerMask, _obstacleMask; //Ÿ�� ����ũ, ��ֹ�
    [SerializeField] private string[] _targetTag; //Ÿ�� �±�

    public List<TargetList> currentTargetList = new List<TargetList>(); //���� Ÿ�� ���

    private Vector3 _lastTargetPos; //������ �°� ��ġ
    private bool _keepTarget; //Ÿ�� �ֽû���
    private float _sightInterval; //������Ʈ �ֱ�

    private Transform _charbody; //ĳ���� Ʈ������
    private CharacterState _charstate; //ĳ���� ����
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

    #region �þ߿��� Ÿ�� ��ġ
    private void FindVisibleTargets()
    {
        Vector3 facingDir = new Vector3(_charbody.localScale.x, 0, 0); //Ⱦ��ũ�� �þ�
        Vector3 _pos = _charbody.position + Vector3.up * _viewHeight; //�þ� ������
        DrawView(_viewDistance, _viewAnglel, _traceDistance, _pos); //�þ߰� �׸���

        #region ���ο� Ÿ���� Ž��
        //���ο� Ÿ���� Ž��
        if (!_charstate.charstate_target.Target._target)
        {
            //�þ� �Ÿ� �� Ÿ�ٷ��̾��� ��� �ö��̴� �޾ƿ���
            Collider[] targets = Physics.OverlapSphere(_pos, _viewDistance, _targerMask);

            for (int i = 0; i < targets.Length; i++)
                for (int s = 0; s < _targetTag.Length; s++)
                    if (targets[i].tag.Equals(_targetTag[s]))
                    {
                        Transform target = targets[i].transform;
                        Vector3 targetposition = target.transform.position; //Ÿ�� ��ġ
                        Vector3 dirToTarget = (targetposition - _pos).normalized; //Ÿ���� ����

                        //_transform.forward�� dirToTarget�� ��� ���������̹Ƿ� �������� �� ���Ͱ� �̷�� ���� Cos���� ����.
                        //�������� �þ߰�/2�� Cos������ ũ�� �þ߿� ���� ���̴�.
                        if (Vector3.Dot(facingDir, dirToTarget) > Mathf.Cos((_viewAnglel / 2) * Mathf.Deg2Rad))
                        {
                            float distToTarget = Vector3.Distance(_pos, targetposition);
                            if (!Physics.Raycast(_pos, dirToTarget, distToTarget, _obstacleMask)) //��ֹ��� ������
                            {
                                Debug.DrawLine(_pos, targetposition, Color.red);
                                AddAggroTarget(target, 0); //��׷� ��Ʈ�� Ÿ�� �߰�
                            }
                        }

                        break;
                    }
        }
        #endregion
        #region ���� Ÿ���� ����
        //���� Ÿ���� ����
        else if (_charstate.charstate_target.Target._target)
        {
            //���� �Ÿ����� Ÿ���� �˻�
            Collider[] tracetargets = Physics.OverlapSphere(_pos, _traceDistance, _targerMask); 
            bool isWatchTarget = false; //�ֽû��� üũ
            bool isLost = true; //Ÿ�� �ν�Ʈ ����

            for (int i = 0; i < tracetargets.Length; i++)
                if (System.Object.ReferenceEquals(_charstate.charstate_target.Target._target, tracetargets[i].transform))
                {
                    isWatchTarget = true;
                    Transform target = tracetargets[i].transform;
                    Vector3 targetposition = target.transform.position; //Ÿ�� ��ġ
                    Vector3 dirToTarget = (targetposition - _pos).normalized; //Ÿ���� ����
                    float distToTarget = Vector3.Distance(_pos, targetposition); //Ÿ�ٰ��� �Ÿ�

                    //�� Ÿ�� ����
                    if (!Physics.Raycast(_pos, dirToTarget, distToTarget, _obstacleMask) || _keepTarget) //���̿� ��ֹ��� ���ų�, �ֽû��¶��
                    {
                        Debug.DrawLine(_pos, targetposition, Color.red);
                        _lastTargetPos = targetposition; //Ÿ�� ��ġ ���
                        isWatchTarget = true; //Ÿ�� �ֽ� Ȱ��ȭ
                        isLost = false; //���� �ֽû��¶�� Ÿ���� ��� �����Ѵ�
                    }

                    break;
                }

            _keepTarget = isWatchTarget; //�ֽû��� ����

            if (!isWatchTarget && Vector3.Distance(_pos, _lastTargetPos) < _confirmDistance) //���� �����ġ���� Ÿ�� ������ �����ߴٸ� Ÿ���� �ν�Ʈ�Ѵ�
                DelAggroTarget();
        }
        #endregion
    }


    #endregion

    #region DrawRay
    private void DrawView(float viewdistance, float angle, float tracedistance, Vector3 Pos)
    {
        //����׿� �ֽð� �׸���
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

    //ĳ���� ���⿡ ���� �þ߰� ��ȯ
    private Vector3 DirFromAngle(float angleInDegrees)
    {
        //�¿� ȸ���� ����
        if (_charstate.charstate_behavior.FacingRight) //���ʹ���
            angleInDegrees += _charbody.eulerAngles.y + 90f;
        else
            angleInDegrees += _charbody.eulerAngles.y - 90f;

        //��� ���Ͱ� ��ȯ
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
    #endregion

    #region ��׷� ����Ʈ ����
    public void AggroManage() // ��׷� ��Ʈ ����
    {
        //Ÿ�������� �Ұ����� ����� �����
        if (_charstate.charstate_target.Target._target && !CheckTarget(_charstate.charstate_target.Target._target))
            DelAggroTarget();

        //���� ��׷� Ÿ�� ���
        if (_charstate.Check_Action(true, false, false, true, false, true))
            NextAggroTarget(true);
    }

    private bool CheckTarget(Transform _target)
    {
        bool _check = true;
        CharacterState _targetstate = _target.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterState>();

        if(!_target.gameObject.activeInHierarchy || _targetstate.charstate_limit.State_Death) //Ÿ���� ��Ȱ��ȭ �ǰų� �׾��ٸ�
            _check = false;

        if(_check)
            for (int i = 0; i < _targetTag.Length; i++)
                if(_targetTag[i].Equals(_target.tag)) //Ÿ�� �±׸� üũ�Ѵ�
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

            //�̹� ��׷� ��Ʈ�� ��� �� Ÿ������ üũ�Ѵ�
            for (int i = currentTargetList.Count - 1; i >= 0; i--)
                if (currentTargetList[i]._target)
                {
                    if (System.Object.ReferenceEquals(currentTargetList[i]._target, _target))
                    {
                        currentTargetList[i]._aggro += _aggro;
                        if (_charstate.charstate_target.Target._aggro < currentTargetList[i]._aggro) //���� Ÿ�ٺ��� ��׷� ��ġ�� ���ٸ� ����
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

            //���� ��׷� ��Ʈ�� ���ٸ�
            if (!isExsit)
            {
                TargetList newList = new TargetList();
                newList._target = _target;
                newList._aggro = _aggro;
                currentTargetList.Add(newList);
            }
        }
    }

    private void DelAggroTarget() //����Ÿ���� ����
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
    
    private void NextAggroTarget() //���� Ÿ�� ���
    {
        //��׷� ��Ʈ���� ��׷� ��ġ�� ���� ���� Ÿ������ �����Ѵ�.
        float _dist = 0;
        for (int i = currentTargetList.Count - 1; i >= 0; i--)
            if (currentTargetList[i]._target)
            {
                //��׷� �ڿ�����
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

                //Ÿ�� ��ü
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
