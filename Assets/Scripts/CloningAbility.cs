using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 분신술 초능력
public class CloningAbility : MonoBehaviour, ISupernatural
{
    public GameObject[] clones; // 분신들(최대 3명)
    public GameObject cloningUI; // 분신 수를 선택할 UI
    public GameObject clonePosCircle; // 분신 위치 표시 원
    int cloneCnt; // 꺼낼 분신 수
    float duration; // 지속 시간
    float limitDistance = 10f; // 분신 위치 조정 제한 거리
    float durationRemainTime; // 남은 지속시간

    bool canUIUpdate;

    public bool CanUIUpdate { get => canUIUpdate; set => canUIUpdate = value; }

    public void Activate()
    {
        cloningUI.SetActive(true); // 분신 수를 선택할 UI 활성화

        Cursor.lockState = CursorLockMode.None; // 커서 해제
        Cursor.visible = true; // 커서 보이기
    }

    public void Deactivate()
    {
        CanUIUpdate = false;
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

        // 분신 이동 제어 코루틴 실행
        StartCoroutine(ControlCloneMove());

        duration = 30f / cloneCnt; // 지속 시간 -> 1명 : 15초 / 2명 : 10초 / 3명 : 5초

        // 지속시간만큼 지속
        durationRemainTime = duration;
        while (durationRemainTime > 0)
        {
            durationRemainTime -= Time.deltaTime;

            if (CanUIUpdate)
            {
                // 지속시간 UI에서 업데이트
                UIManager.instance.duraitonDisableImg.fillAmount = durationRemainTime / duration;
                UIManager.instance.durationRemainTimeText.text = durationRemainTime.ToString("F1") + "s";
            }

            yield return null;
        }
        if (CanUIUpdate)
            UIManager.instance.durationRemainTimeText.text = "";

        // yield return new WaitForSeconds(duration); // 지속 시간만큼 지속

        // 활성화했던 분신 비활성화
        for (int i = 0; i < cloneCnt; i++)
        {
            clones[i].SetActive(false);
        }
    }

    public void SelectCloneCount(int cnt)
    {
        cloneCnt = cnt; // 꺼낼 분신 수 할당
        StartCoroutine(Cloning()); // 분신술 코루틴 실행
        cloningUI.SetActive(false); // UI 비활성화
        Cursor.lockState = CursorLockMode.Locked; // 커서 고정
        Cursor.visible = false; // 커서 숨기기
    }

    // 분신 이동 제어 코루틴
    IEnumerator ControlCloneMove()
    {
        bool isMoving = false;
        bool isSelectingToFollow = false;
        GameObject selectedClone = null;

        while (true)
        {
            // T키 -> 분신 위치 조정 시작
            if (Input.GetKeyDown(KeyCode.T) && !isSelectingToFollow && selectedClone == null)
            {
                Debug.Log("분신 위치 조정 시작 " + selectedClone);
                isMoving = true;
            }

            // V키 -> 분신 따라오기 여부 선택 시작
            if (Input.GetKeyDown(KeyCode.V) && !isMoving)
            {
                Debug.Log("분신 따라오기 여부 선택 시작 " + selectedClone);
                isSelectingToFollow = true;
            }

            // Alpha 1, 2, 3키 -> 분신 선택
            if (isMoving || isSelectingToFollow)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    Debug.Log("1번 분신 선택");
                    selectedClone = clones[0];
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    Debug.Log("2번 분신 선택");
                    selectedClone = clones[1];
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    Debug.Log("3번 분신 선택");
                    selectedClone = clones[2];
                }
            }

            // 분신 위치 조정이 시작됐을 때
            if (isMoving && selectedClone != null)
            {
                clonePosCircle.SetActive(true); // 목표 위치 표시 원 활성화

                // 위치 조정
                Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 방향으로 화면에서 레이 쏘기
                if (Physics.Raycast(ray, out RaycastHit hit, limitDistance, GameManager.instance.mapLayerMask)) // 맵 레이어만 검출하고 제한 거리 내에서만 가능
                {
                    clonePosCircle.transform.position = hit.point; // 충돌 지점에 원 표시
                }
                Debug.Log("분신 위치 조정 중...");

                // T키 -> 위치 확정 후 분신 이동
                if (Input.GetKeyDown(KeyCode.T))
                {
                    Debug.Log("분신 위치 확정!");
                    CloneController selectedCloneController = selectedClone.GetComponent<CloneController>();
                    selectedCloneController.MoveToTargetPos(clonePosCircle.transform.position); // 확정한 위치를 해당 클론의 목표 위치로 이동
                    clonePosCircle.SetActive(false); // 목표 위치 표시 원 비활성화
                    isMoving = false;
                    selectedClone = null;
                }
            }

            // 분신 따라오기 여부 선택이 시작됐을 때
            if (isSelectingToFollow && selectedClone != null)
            {
                CloneController selectedCloneController = selectedClone.GetComponent<CloneController>();

                if (!selectedCloneController.IsFollow) // 선택된 분신이 따라오지 않고 있을 때
                {
                    // 분신이 분신술 캐릭터를 따라오기
                    selectedCloneController.IsFollow = true;
                }
                else // 선택된 분신이 따라오고 있는 중일 때
                {
                    // 분신이 분신술 캐릭터를 따라오지 않기
                    selectedCloneController.IsFollow = false;
                    selectedClone = null;
                }
                isSelectingToFollow = false;
            }

            yield return null;
        }
    }

    // // 초능력 지속시간 업데이트 함수
    // IEnumerator UpdateSupernaturalDuration()
    // {
    //     isSupernaturalReady = false;

    //     durationRemainTime = duration;
    //     while (durationRemainTime > 0)
    //     {
    //         durationRemainTime -= Time.deltaTime;

    //         UIManager.instance.duraitonDisableImg.fillAmount = durationRemainTime / duration;

    //         UIManager.instance.durationRemainTimeText.text = durationRemainTime.ToString("F1") + "s";

    //         yield return null;
    //     }
    //     UIManager.instance.durationRemainTimeText.text = "";

    //     isSupernaturalReady = true;
    // }
}
