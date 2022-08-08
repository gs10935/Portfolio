using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(BodySearch))]
public class RangeAction : MonoBehaviourPunCallbacks
{
    // �� ��ũ��Ʈ���� �ѱ⸦ ����� �׼ǰ� �ִϸ��̼��� �ٷ�ϴ�.
    // �ѱ⸶�� ����, �߻�, ���� ���� �ִϸ��̼� Ÿ���� �ٸ��� �ۼ��� �ִϸ��̼��� �Էµ� ������ ����� �� �ֵ��� �մϴ�.
    // ����, ���� ���� ���� �Ϻ� �ִϸ��̼��� ���൵�� �̾���� �� �ֵ��� �Ͽ� 2D ��������Ʈ �ִϸ��̼����ε� �ڿ������� ����� ��ȯ�˴ϴ�.

    #region ���� & �ʱ�ȭ
    public LayerMask TargetMask; //Ÿ���� ���̾��ũ
    public float ViewDistance; //���� �Ÿ�
    public bool TargetAutoAiming, ShotCancel; //�ڵ����ؿɼ�, ������Ұ��ɻ���

    private Transform CharacterBody; //�÷��̾�� ĳ����
    private CharacterState charstate; //�÷��̾��� �ൿ����
    private Status status; //�÷��̾��� �������ͽ�
    private CharacterAnimation charanim; //�÷��̾��� �ִϸ��̼� ����
    private CharacterControl charctrl; //�÷��̾��� ĳ������Ʈ�ѷ�
    private Animator mainanimator, weaponanimator; //�÷��̾��� ���� �ִϸ�����, ���� �ִϸ�����

    void Start()
    {
        CharacterBody = GetComponent<BodySearch>().CharacterBody.transform;
        charstate = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterState>();
        status = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>()._Controller.GetComponent<Status>();
        charctrl = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>()._Controller.GetComponent<CharacterControl>();
        charanim = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>()._Sprite.GetComponent<CharacterAnimation>();
        mainanimator = GetComponent<BodySearch>().CharacterBody.GetComponent<Animator>();
        weaponanimator = GetComponent<BodySearch>().CharacterBody.GetComponent<CharacterManager>().WeaponSprite.GetComponent<Animator>();
    }
    #endregion

    void Update()
    {
        InputAction();
    }

    // ��ǲ �׼�
    private void InputAction()
    {
        // ���� ������ �����϶� ������ ����Ű�� �Է��Ͽ� ���ػ��� �׼����� ��ȯ�մϴ�. ���ػ��¿��� �߰��� Ű�� �Է��Ͽ� �ѱ�׼��� ����� �� �ֽ��ϴ�.
        if (0 <= charstate.charstate_action.Sprite_MotionChange && charstate.charstate_action.Sprite_MotionChange < 1000
            && Input.GetButton("keyV") && !status.status_equip.SubWeapon.Name.Equals("") 
            && (charstate.Check_Action(true, false, false, false, false, true) || CheckExceptions()))
        {
            // ������ ���� �ִϸ��̼ǰ��� �޾� �ѱ�׼��� ����
            WeaponAction(status.status_equip.SubWeapon.motion_startAim, status.status_equip.SubWeapon.motion_Aim, status.status_equip.SubWeapon.motion_shot, status.status_equip.SubWeapon.motion_Reload);
        }
        else if(0 < charstate.charstate_action.Sprite_MotionChange && charstate.charstate_action.Sprite_MotionChange < 1000 && ShotCancel)
        {
            if(charstate.charstate_action.ActionNum == 1)
                charanim.GetAnimationPlaytime("Walk", 0, mainanimator, 1); //�ȱ� �ִϸ��̼ǿ��� ���൵�� �̾�޴´�
            charstate.charstate_action.Sprite_MotionChange = 0;
            charstate.charstate_target.Target._target = null;
            mainanimator.SetFloat("MotionSpeed", 1);
            ShotCancel = false;
        }
    }

    // �ѱ� ����
    private void WeaponAction(int motion_StartAim, int motion_Aim, int motion_Shot, int motion_Reload)
    {
        //�ڵ����� �ɼ� ����
        if(!CheckExceptions() && TargetAutoAiming)
            TargetSearch();
        else
            mainanimator.SetFloat("MotionSpeed", 1);

        //ù ��� ��ȯ�����̰� ��ü �ִϸ��̼��� �ȱ� �Ǵ� �ٱ� ���̶��, �ִϸ��̼� ���൵�� �̾�޴´�
        if (charstate.charstate_action.Sprite_MotionChange == 0 && 
            (charstate.charstate_action.ActionNum == 1 || charstate.charstate_action.ActionNum == 2))
            charanim.GetAnimationPlaytime("AimWalk", 1, mainanimator);

        //���� ������ �ִϸ��̼�
        if (charstate.charstate_action.Sprite_MotionChange <= 0 && charstate.charstate_action.Sprite_MotionChange != motion_StartAim)
        {
            ShotCancel = false;
            charstate.charstate_action.Sprite_MotionChange = motion_StartAim;
        }

        //���� ���� �ִϸ��̼�
        if (charstate.charstate_action.Sprite_MotionChange != motion_Shot && charstate.charstate_action.Sprite_MotionChange != motion_Reload && ShotCancel)
            charstate.charstate_action.Sprite_MotionChange = motion_Aim; 

        //���� ��� �ִϸ��̼�
        if (Input.GetButton("keyZ") && (charstate.charstate_action.Sprite_MotionChange == motion_Aim || ShotCancel) 
            && status.status_equip.SubWeapon.MaxMagazine > 0 && status.status_equip.SubWeapon.Magazine >= status.status_equip.SubWeapon.RequireBullet && status.status_base.Sta >= status.status_equip.SubWeapon.RequireSta)
        {
            if (charstate.charstate_action.Sprite_MotionChange == motion_Shot)
                charanim.ResetAnimRate(new string[] { "Weapon" });
            ShotCancel = false;
            charstate.charstate_action.Sprite_MotionChange = motion_Shot;
            status.status_equip.SubWeapon.Magazine -= status.status_equip.SubWeapon.RequireBullet;
            status.status_base.Sta -= status.status_equip.SubWeapon.RequireSta;
        }
        //���� ���� �ִϸ��̼�
        else if (((Input.GetButton("keyS") && status.status_equip.SubWeapon.Magazine < status.status_equip.SubWeapon.MaxMagazine)
            || (Input.GetButton("keyZ") && status.status_equip.SubWeapon.Magazine <= 0 && status.status_equip.SubWeapon.TotalMagazine > 0))
            && charstate.charstate_action.Sprite_MotionChange != motion_Reload && status.status_equip.SubWeapon.TotalMagazine > 0 && ShotCancel
            && !CheckExceptions())
        {
            ShotCancel = false;
            charstate.charstate_action.Sprite_MotionChange = motion_Reload;
        }

        //Ÿ�� �ڵ� ���� �ɼ� Ȱ��ȭ
        if (Input.GetButtonDown("keyA"))
            TargetAutoAiming = !TargetAutoAiming;
    }

    //�ڵ����� �ɼ� ����, Ÿ�� ��ġ
    private void TargetSearch()
    {
        //���� ����� ���� �� z���� ����� ���� ã�´�.
        Collider[] targets = Physics.OverlapBox(CharacterBody.transform.position, new Vector3(ViewDistance, 2, 0.8f), Quaternion.identity, TargetMask);
        Transform Target = null;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i].tag.Equals("Enemy") && !targets[i].GetComponent<CharacterManager>().isDeath)
            {
                if (!Target)
                    Target = targets[i].transform;
                else if (Target && Vector3.Distance(CharacterBody.transform.position, Target.position) > Vector3.Distance(CharacterBody.transform.position, targets[i].transform.position)
                    && Mathf.Abs(CharacterBody.transform.position.z - targets[i].transform.position.z) < 0.75f)
                    Target = targets[i].transform;
            }
        }

        charstate.charstate_target.Target._target = Target;

        //��ġ�� �̵� ó��
        if (charstate.charstate_target.Target._target && TargetAutoAiming)
        {
            charctrl.ForceDirection(charstate.charstate_target.Target._target.transform.position);
            if ((charstate.charstate_behavior.FacingRight && Input.GetAxis("Horizontal") > 0) || (!charstate.charstate_behavior.FacingRight && Input.GetAxis("Horizontal") < 0))
                mainanimator.SetFloat("MotionSpeed", 1);
            else if ((charstate.charstate_behavior.FacingRight && Input.GetAxis("Horizontal") < 0) || (!charstate.charstate_behavior.FacingRight && Input.GetAxis("Horizontal") > 0))
                mainanimator.SetFloat("MotionSpeed", -1);
        }
        else
            mainanimator.SetFloat("MotionSpeed", 1);
    }

     //Ư�� �ൿ���¿��� ���� ���ɿ���
    private bool CheckExceptions()
    {
        bool check = false;
        if(charstate.charstate_action.ActionNum > 301 && charstate.charstate_action.ActionNum < 310) //������ �Ŵ޸� ���·� ����
        {
            check = true;
        }

        return check;
    }
}
