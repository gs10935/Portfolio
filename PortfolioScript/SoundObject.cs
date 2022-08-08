using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SoundObject : MonoBehaviour
{
    // ���� ������ ��������� �ʿ��� ��� ������Ʈ�� �߰��Ǵ� ��ũ��Ʈ�Դϴ�.
    //

    #region ����
    //������ ���� ���
    public List<SoundInfo> SeList = new List<SoundInfo>();

    //���� ������� ����� ���
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

    #region ���� ����
    private void SE_Check() //����� ���� ����
    {
        for (int i = AudioList.Count - 1; i >= 0; i--)
        {
            if (AudioList[i] != null && AudioList[i].audioSource != null && !AudioList[i].audioSource.isPlaying)
                Destroy(AudioList[i].audioSource);
            if (AudioList[i].audioSource == null)
                AudioList.RemoveAt(i);
        }
    }

    public void SE_Stop() //�������� ���� ����
    {
        for (int i = 0; i < AudioList.Count; i++)
            Destroy(AudioList[i].audioSource);
        AudioList.Clear();
    }

    private IEnumerator PrintCoroutine;
    public void SE_Stop_B(float _time = 1) //�������� ���� ���� B
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

    #region ���� ���
    public void SE_Play(string name, string groundName = "") //��ϵ� ���带 ���
    {
        for (int i = 0; i < SeList.Count; i++)
            if (SeList[i].Name.Equals(name) && groundName.Trim().Equals(SeList[i].GroundType))
            {
                SetExternalSound(SeList[i].SoundClip[Random.Range(0, SeList[i].SoundClip.Length)], SeList[i].isLoop, SeList[i].maxOverlapCount);
                break;
            }
    }

    public void SetExternalSound(AudioClip _clip, bool _loop = false, int _maxOverlap = 2, bool isBGM = false, bool isRPC = true) //�ܺ� ���带 ���
    {
        //�ߺ��� ���� ����
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

        //������ҽ� �߰�
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

        //����� ���� �� ����Ʈ �߰�
        extAudio.audioSource.Play();
        AudioList.Add(extAudio);
    }
    #endregion
}
