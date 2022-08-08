using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(BodySearch))]
public class RangeAction : MonoBehaviourPunCallbacks
{
    // 이 스크립트에선 총기를 사용한 액션과 애니메이션을 다룹니다.
    // 총기마다 조준, 발사, 장전 등의 애니메이션 타입이 다르며 작성된 애니메이션을 입력된 값으로 사용할 수 있도록 합니다.
    // 또한, 조준 전과 후의 일부 애니메이션의 진행도를 이어받을 수 있도록 하여 2D 스프라이트 애니메이션으로도 자연스럽게 모션이 전환됩니다.

    #region 변수 & 초기화
    public LayerMask TargetMask; //타겟의 레이어마스크
    public float ViewDistance; //조준 거리
    public bool TargetAutoAiming, ShotCancel; //자동조준옵션, 조준취소가능상태

    private Transform CharacterBody; //플레이어블 캐릭터
    private CharacterState charstate; //플레이어의 행동상태
    private Status status; //플레이어의 스테이터스
    private CharacterAnimation charanim; //플레이어의 애니메이션 제어
    private CharacterControl charctrl; //플레이어의 캐릭터컨트롤러
    private Animator mainanimator, weaponanimator; //플레이어의 메인 애니메이터, 무기 애니메이터

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

    // 인풋 액션
    private void InputAction()
    {
        // 조준 가능한 상태일때 지정된 조준키를 입력하여 조준상태 액션으로 전환합니다. 조준상태에서 추가로 키를 입력하여 총기액션을 사용할 수 있습니다.
        if (0 <= charstate.charstate_action.Sprite_MotionChange && charstate.charstate_action.Sprite_MotionChange < 1000
            && Input.GetButton("keyV") && !status.status_equip.SubWeapon.Name.Equals("") 
            && (charstate.Check_Action(true, false, false, false, false, true) || CheckExceptions()))
        {
            // 무기의 고유 애니메이션값을 받아 총기액션을 구현
            WeaponAction(status.status_equip.SubWeapon.motion_startAim, status.status_equip.SubWeapon.motion_Aim, status.status_equip.SubWeapon.motion_shot, status.status_equip.SubWeapon.motion_Reload);
        }
        else if(0 < charstate.charstate_action.Sprite_MotionChange && charstate.charstate_action.Sprite_MotionChange < 1000 && ShotCancel)
        {
            if(charstate.charstate_action.ActionNum == 1)
                charanim.GetAnimationPlaytime("Walk", 0, mainanimator, 1); //걷기 애니메이션에서 진행도를 이어받는다
            charstate.charstate_action.Sprite_MotionChange = 0;
            charstate.charstate_target.Target._target = null;
            mainanimator.SetFloat("MotionSpeed", 1);
            ShotCancel = false;
        }
    }

    // 총기 조작
    private void WeaponAction(int motion_StartAim, int motion_Aim, int motion_Shot, int motion_Reload)
    {
        //자동조준 옵션 적용
        if(!CheckExceptions() && TargetAutoAiming)
            TargetSearch();
        else
            mainanimator.SetFloat("MotionSpeed", 1);

        //첫 모션 전환시점이고 본체 애니메이션이 걷기 또는 뛰기 중이라면, 애니메이션 진행도를 이어받는다
        if (charstate.charstate_action.Sprite_MotionChange == 0 && 
            (charstate.charstate_action.ActionNum == 1 || charstate.charstate_action.ActionNum == 2))
            charanim.GetAnimationPlaytime("AimWalk", 1, mainanimator);

        //무기 꺼내기 애니메이션
        if (charstate.charstate_action.Sprite_MotionChange <= 0 && charstate.charstate_action.Sprite_MotionChange != motion_StartAim)
        {
            ShotCancel = false;
            charstate.charstate_action.Sprite_MotionChange = motion_StartAim;
        }

        //무기 조준 애니메이션
        if (charstate.charstate_action.Sprite_MotionChange != motion_Shot && charstate.charstate_action.Sprite_MotionChange != motion_Reload && ShotCancel)
            charstate.charstate_action.Sprite_MotionChange = motion_Aim; 

        //무기 사용 애니메이션
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
        //무기 장전 애니메이션
        else if (((Input.GetButton("keyS") && status.status_equip.SubWeapon.Magazine < status.status_equip.SubWeapon.MaxMagazine)
            || (Input.GetButton("keyZ") && status.status_equip.SubWeapon.Magazine <= 0 && status.status_equip.SubWeapon.TotalMagazine > 0))
            && charstate.charstate_action.Sprite_MotionChange != motion_Reload && status.status_equip.SubWeapon.TotalMagazine > 0 && ShotCancel
            && !CheckExceptions())
        {
            ShotCancel = false;
            charstate.charstate_action.Sprite_MotionChange = motion_Reload;
        }

        //타겟 자동 지정 옵션 활성화
        if (Input.GetButtonDown("keyA"))
            TargetAutoAiming = !TargetAutoAiming;
    }

    //자동조준 옵션 사용시, 타겟 서치
    private void TargetSearch()
    {
        //가장 가까운 적들 중 z축이 가까운 적을 찾는다.
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

        //서치중 이동 처리
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

     //특수 행동상태에서 조준 가능여부
    private bool CheckExceptions()
    {
        bool check = false;
        if(charstate.charstate_action.ActionNum > 301 && charstate.charstate_action.ActionNum < 310) //로프에 매달린 상태로 조준
        {
            check = true;
        }

        return check;
    }
}
