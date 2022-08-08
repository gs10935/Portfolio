using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SoundObject : MonoBehaviour
{
    // 게임 내에서 사운드출력이 필요한 경우 오브젝트에 추가되는 스크립트입니다.
    //

    #region 변수
    //보유한 사운드 목록
    public List<SoundInfo> SeList = new List<SoundInfo>();

    //현재 재생중인 오디오 목록
    private List<AudioList> AudioList = new List<AudioList>();
    private float updateTime;
    #endregion

    private void Update()
    {
        if (updateTime < 0 && AudioList.Count > 0)
        {
            updateTime = 0.8f;
            SE_Check();
        }
        else
            updateTime -= Time.deltaTime;
    }

    #region 사운드 정지
    private void SE_Check() //만료된 사운드 정지
    {
        for (int i = AudioList.Count - 1; i >= 0; i--)
        {
            if (AudioList[i] != null && AudioList[i].audioSource != null && !AudioList[i].audioSource.isPlaying)
                Destroy(AudioList[i].audioSource);
            if (AudioList[i].audioSource == null)
                AudioList.RemoveAt(i);
        }
    }

    public void SE_Stop() //실행중인 사운드 정지
    {
        for (int i = 0; i < AudioList.Count; i++)
            Destroy(AudioList[i].audioSource);
        AudioList.Clear();
    }

    private IEnumerator PrintCoroutine;
    public void SE_Stop_B(float _time = 1) //실행중인 사운드 정지 B
    {
        if (PrintCoroutine != null)
            StopCoroutine(PrintCoroutine);
        PrintCoroutine = SE_StopGradually(_time);
        StartCoroutine(PrintCoroutine);
    }
    IEnumerator SE_Stop_Slow(float _time = 1)
    {
        float _lastTime = _time;
        while (AudioList.Count > 0)
        {
            for (int i = AudioList.Count - 1; i >= 0; i--)
                if (AudioList[i].audioSource)
                {
                    if (AudioList[i].audioSource.volume > 0)
                        AudioList[i].audioSource.volume -= Time.deltaTime / _time;
                    else if (_lastTime <= 0 || AudioList[i].audioSource.volume <= 0)
                        Destroy(AudioList[i].audioSource);
                }

            if (_lastTime <= 0)
                AudioList.Clear();

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }
    #endregion

    #region 사운드 재생
    public void SE_Play(string name, string groundName = "") //등록된 사운드를 재생
    {
        for (int i = 0; i < SeList.Count; i++)
            if (SeList[i].Name.Equals(name) && groundName.Trim().Equals(SeList[i].GroundType))
            {
                SetExternalSound(SeList[i].SoundClip[Random.Range(0, SeList[i].SoundClip.Length)], SeList[i].isLoop, SeList[i].maxOverlapCount);
                break;
            }
    }

    public void SetExternalSound(AudioClip _clip, bool _loop = false, int _maxOverlap = 2, bool isBGM = false, bool isRPC = true) //외부 사운드를 재생
    {
        //중복된 사운드 제한
        int _overlap = 0;
        for (int i = 0; i < AudioList.Count; i++)
            if (AudioList[i].audioSource != null && AudioList[i].audioSource.clip == _clip && (_overlap < _maxOverlap))
            {
                _overlap++;
                break;
            }
            else if (AudioList[i].audioSource != null && AudioList[i].audioSource.clip == _clip && _overlap >= _maxOverlap)
            {
                Destroy(AudioList[i].audioSource);
                AudioList.RemoveAt(i);
                break;
            }

        //오디오소스 추가
        AudioList extAudio = new AudioList();
        extAudio.audioSource = gameObject.AddComponent<AudioSource>();
        extAudio.audioSource.clip = _clip;
        extAudio.audioSource.loop = _loop;
        extAudio.audioSource.volume = 1 ;
        extAudio.audioSource.minDistance = 5;
        extAudio.audioSource.maxDistance = 50;
        extAudio.audioSource.spread = 180;
        extAudio.isBGM = isBGM;
        extAudio.baseVolume = 1;

        if (isBGM)
            extAudio.audioSource.spatialBlend = 0;
        else
            extAudio.audioSource.spatialBlend = 1;

        //오디오 진행 및 리스트 추가
        extAudio.audioSource.Play();
        AudioList.Add(extAudio);
    }
    #endregion
}
