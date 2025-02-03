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
    public GameObject cloneOutPosBox;
    int cloneCnt; // 꺼낼 분신 수
    float duration; // 지속 시간
    float limitDistance = 10f; // 분신 위치 조정 제한 거리
    float durationRemainTime; // 남은 지속시간

    public float SupernaturalCoolDown { get => supernaturalCoolDown; set => supernaturalCoolDown = value; }
    public float CooldownRemainTime { get => cooldownRemainTime; set => cooldownRemainTime = value; }
    public bool IsSupernaturalReady { get => isSupernaturalReady; set => isSupernaturalReady = value; }

    float supernaturalCoolDown = 10f; // 초능력 쿨타임 (초 단위)
    float cooldownRemainTime;
    bool isSupernaturalReady = true; // 초능력 사용 가능 여부

    Color cloneImgColor;
    int selectId = -1;
    public bool isMoving = false;
    public bool isSelectingToFollow = false;
    bool isCloning;

    PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            UIManager.instance.cloneImgs[i].color = Color.gray;
        }
    }

    public void Activate()
    {
        if (!isCloning)
        {
            cloneOutPosBox.SetActive(true);

            cloningUI.SetActive(true); // 분신 수를 선택할 UI 활성화

            Cursor.lockState = CursorLockMode.None; // 커서 해제
            Cursor.visible = true; // 커서 보이기
        }
    }

    public void Deactivate()
    {
        cloneOutPosBox.SetActive(false);
        cloningUI.SetActive(false); // 분신 수를 선택할 UI 비활성화
        UIManager.instance.cloneUI.SetActive(false);
    }

    // 분신술
    IEnumerator Cloning()
    {
        isCloning = true;
        UIManager.instance.cloneUI.SetActive(true);

        selectId = -1;
        isMoving = false;
        isSelectingToFollow = false;

        // 분신 수만큼 분신 활성화
        for (int i = 0; i < cloneCnt; i++)
        {
            clones[i].transform.position = transform.position + transform.forward * 1.5f * (i + 1); // 분신 나올 위치 설정
            clones[i].SetActive(true); // 분신 활성화
            UIManager.instance.cloneImgs[i].color = Color.white;
        }

        // 지속 시간 -> 1명 : 40초 / 2명 : 30초 / 3명 : 20초
        switch (cloneCnt)
        {
            case 1:
                duration = 40f;
                break;
            case 2:
                duration = 30f;
                break;
            case 3:
                duration = 20f;
                break;
        }

        // 지속시간만큼 지속
        durationRemainTime = duration;
        while (durationRemainTime > 0)
        {
            durationRemainTime -= Time.deltaTime;

            if (playerController.canUIUpdate)
            {
                // 지속시간 UI에서 업데이트
                UIManager.instance.duraitonDisableImg.fillAmount = durationRemainTime / duration;
                UIManager.instance.durationRemainTimeText.text = durationRemainTime.ToString("F1") + "s";
            }

            yield return null;
        }

        isCloning = false;

        if (playerController.canUIUpdate)
            UIManager.instance.durationRemainTimeText.text = "";

        // 활성화했던 분신 비활성화
        for (int i = 0; i < cloneCnt; i++)
        {
            UIManager.instance.cloneUI.SetActive(false);
            clones[i].SetActive(false);
            UIManager.instance.cloneImgs[i].color = Color.gray;
        }

        clonePosCircle.SetActive(false); // 목표 위치 표시 원 비활성화

        StartCoroutine(UpdateSupernaturalCooldown()); // 쿨타임 업데이트 시작
    }

    public void SelectCloneCount(int cnt)
    {
        if (!cloneOutPosBox.GetComponent<CheckTargetPos>().isMoveOkay)
        {
            UIManager.instance.ShowGuide("해당 위치가 분신을 꺼내기에 불완전합니다.", true);
        }
        else
        {
            cloneOutPosBox.SetActive(false);

            cloneCnt = cnt; // 꺼낼 분신 수 할당
            StartCoroutine(Cloning()); // 분신술 코루틴 실행
            cloningUI.SetActive(false); // UI 비활성화
            Cursor.lockState = CursorLockMode.Locked; // 커서 고정
            Cursor.visible = false; // 커서 숨기기
        }
    }

    void Update()
    {
        if (isCloning)
        {
            if (!GameManager.instance.isAllowOnlyUIInput)
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    isSelectingToFollow = true;
                }
                else if (Input.GetKeyDown(KeyCode.T) && !isSelectingToFollow && !isMoving) // 분신이 따라오는 동안에는 목표 위치 설정 못함
                {
                    isMoving = true;
                }

                if (isSelectingToFollow || isMoving)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1))
                    {
                        Debug.Log("1번 분신 선택");
                        selectId = 0;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha2))
                    {
                        Debug.Log("2번 분신 선택");
                        selectId = 1;
                    }
                    else if (Input.GetKeyDown(KeyCode.Alpha3))
                    {
                        Debug.Log("3번 분신 선택");
                        selectId = 2;
                    }
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isCloning)
        {
            // 분신 이동 제어 함수 실행
            ControlCloneMove();
        }
    }

    // 분신 이동 제어 코루틴
    void ControlCloneMove()
    {
        if (selectId != -1 && selectId < cloneCnt)
        {
            GameObject selectedClone = clones[selectId];
            CloneController selectedCloneController = selectedClone.GetComponent<CloneController>();

            // 분신 따라오기 여부 선택이 시작됐을 때
            if (isSelectingToFollow)
            {
                if (isMoving)
                {
                    isMoving = false;
                    clonePosCircle.SetActive(false); // 목표 위치 표시 원 비활성화
                }

                selectedCloneController.IsFollow = !selectedCloneController.IsFollow;

                if (selectedCloneController.IsFollow) // 선택된 분신 추적 시작
                {
                    // 분신이 분신술 캐릭터를 따라오기
                    Debug.Log("분신이 추적을 시작합니다.");
                    ColorUtility.TryParseHtmlString("#A87BBE", out cloneImgColor);
                    UIManager.instance.cloneImgs[selectId].color = cloneImgColor;
                }
                else // 선택된 분신 추적 해제
                {
                    // 분신이 분신술 캐릭터를 따라오지 않기
                    Debug.Log("분신이 추적을 해제합니다.");
                    UIManager.instance.cloneImgs[selectId].color = Color.white;
                }
                isSelectingToFollow = false;
                selectId = -1;
            }
            else if (isMoving && !selectedCloneController.IsFollow)
            {
                cloneImgColor = Color.yellow;
                UIManager.instance.cloneImgs[selectId].color = cloneImgColor;

                clonePosCircle.SetActive(true); // 목표 위치 표시 원 활성화

                if (!GameManager.instance.isAllowOnlyUIInput)
                {
                    // 위치 조정
                    Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 방향으로 화면에서 레이 쏘기
                    if (Physics.Raycast(ray, out RaycastHit hit, limitDistance, GameManager.instance.mapLayerMask)) // 맵 레이어만 검출하고 제한 거리 내에서만 가능
                    {
                        clonePosCircle.transform.position = hit.point; // 충돌 지점에 원 표시
                    }
                    Debug.Log("분신 위치 조정 중...");

                    // T키 -> 위치 확정 후 분신 이동
                    if (Input.GetKeyDown(KeyCode.T) && !GameManager.instance.isAllowOnlyUIInput)
                    {
                        if (!clonePosCircle.GetComponent<CheckTargetPos>().isMoveOkay)
                        {
                            UIManager.instance.ShowGuide("해당 위치가 순간이동하기에 불완전합니다.", true);
                        }
                        else
                        {
                            isMoving = false;
                            Debug.Log("분신 위치 확정!");
                            selectedCloneController.MoveToTargetPos(clonePosCircle.transform.position); // 확정한 위치를 해당 클론의 목표 위치로 이동
                            clonePosCircle.SetActive(false); // 목표 위치 표시 원 비활성화
                            UIManager.instance.cloneImgs[selectId].color = Color.white;
                            selectId = -1;
                        }
                    }
                    else if (!clonePosCircle.GetComponent<CheckTargetPos>().isMoveOkay)
                    {
                        UIManager.instance.ShowGuide("해당 위치가 이동하기에 불완전합니다.", true);
                    }
                }
            }
        }
    }

    // 초능력 쿨타임 업데이트 함수
    IEnumerator UpdateSupernaturalCooldown()
    {
        IsSupernaturalReady = false;

        CooldownRemainTime = SupernaturalCoolDown;
        while (CooldownRemainTime > 0)
        {
            CooldownRemainTime -= Time.deltaTime;

            if (playerController.canUIUpdate)
            {
                UIManager.instance.cooldownDisableImg.fillAmount = CooldownRemainTime / SupernaturalCoolDown;
                UIManager.instance.cooldownRemainTimeText.text = CooldownRemainTime.ToString("F1") + "s";
            }
            yield return null;
        }
        if (playerController.canUIUpdate)
            UIManager.instance.cooldownRemainTimeText.text = "";

        IsSupernaturalReady = true;
    }
}
