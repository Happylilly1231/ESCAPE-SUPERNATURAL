using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 분신술 초능력
public class CloningAbility : MonoBehaviour, ISupernatural
{
    public GameObject[] clones; // 분신들(최대 3명)
    public GameObject cloningUI; // 분신 수를 선택할 UI
    int cloneCnt; // 꺼낼 분신 수
    float duration; // 지속 시간

    public void Activate()
    {
        cloningUI.SetActive(true); // 분신 수를 선택할 UI 활성화

        Cursor.lockState = CursorLockMode.None; // 커서 해제
        Cursor.visible = true; // 커서 보이기
    }

    // 분신술
    IEnumerator Cloning()
    {
        // 분신 수만큼 분신 활성화
        for (int i = 0; i < cloneCnt; i++)
        {
            clones[i].transform.position = transform.position + transform.forward * 2f * (i + 1); // 분신 나올 위치 설정
            clones[i].SetActive(true); // 분신 활성화
        }

        duration = 15f / cloneCnt; // 지속 시간 -> 1명 : 15초 / 2명 : 10초 / 3명 : 5초
        yield return new WaitForSeconds(duration); // 지속 시간만큼 지속

        // 활성화했던 분신 비활성화
        for (int i = 0; i < cloneCnt; i++)
        {
            clones[i].SetActive(false);
        }
    }

    public void selectCloneCount(int cnt)
    {
        cloneCnt = cnt; // 꺼낼 분신 수 할당
        StartCoroutine(Cloning()); // 분신술 코루틴 실행
        cloningUI.SetActive(false); // UI 비활성화
        Cursor.lockState = CursorLockMode.Locked; // 커서 고정
        Cursor.visible = false; // 커서 숨기기
    }
}
