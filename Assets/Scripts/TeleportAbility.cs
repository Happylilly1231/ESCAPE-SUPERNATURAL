using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 순간이동 초능력
public class TeleportAbility : MonoBehaviour, ISupernatural
{
    public GameObject teleportAlonePosCircle; // 혼자 순간이동 목표 위치를 나타내는 원
    public GameObject teleportTogetherPosCircle; // 함께 순간이동 목표 위치를 나타내는 원
    GameObject teleportPosCircle; // 순간이동 목표 위치를 나타내는 원
    public float limitDistance = 10f; // 제한 거리
    GameObject anotherTeleportCharacter;
    float canTeleportTogetherDistance = 3f;

    public float SupernaturalCoolDown { get => supernaturalCoolDown; set => supernaturalCoolDown = value; }
    public float CooldownRemainTime { get => cooldownRemainTime; set => cooldownRemainTime = value; }
    public bool IsSupernaturalReady { get => isSupernaturalReady; set => isSupernaturalReady = value; }

    float supernaturalCoolDown = 3f; // 초능력 쿨타임 (초 단위)
    float cooldownRemainTime;
    bool isSupernaturalReady = true; // 초능력 사용 가능 여부

    bool isTeleporting;
    bool canTeleport;
    PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        teleportPosCircle = teleportAlonePosCircle;
        teleportAlonePosCircle.SetActive(false);
        teleportTogetherPosCircle.SetActive(false);
    }

    public void Activate()
    {
        if (!isTeleporting)
        {
            canTeleport = true;
            StartCoroutine(Teleport()); // 순간이동 코루틴 실행
        }
    }

    public void Deactivate()
    {
        if (isTeleporting)
        {
            canTeleport = false;
            teleportPosCircle.SetActive(false);
        }
    }

    // 순간 이동
    IEnumerator Teleport()
    {
        isTeleporting = true;
        while (canTeleport)
        {
            FindAnotherTeleport();

            teleportPosCircle.SetActive(false);
            if (anotherTeleportCharacter != null)
            {
                teleportPosCircle = teleportTogetherPosCircle;
            }
            else
            {
                teleportPosCircle = teleportAlonePosCircle;
            }
            teleportPosCircle.SetActive(true);

            if (!GameManager.instance.isAllowOnlyUIInput)
            {
                // 위치 조정
                Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 방향으로 화면에서 레이 쏘기
                if (Physics.Raycast(ray, out RaycastHit hit, limitDistance, LayerMask.GetMask("Map") | LayerMask.GetMask("InvisibleMap"))) // 맵 레이어만 검출하고 제한 거리 내에서만 가능
                {
                    Debug.Log("hit.collider.gameObject: " + hit.collider.gameObject);
                    teleportPosCircle.transform.position = hit.point; // 충돌 지점에 원 표시
                }

                // T키 -> 위치 확정 후 순간 이동
                if (Input.GetKeyDown(KeyCode.T))
                {
                    if (!teleportPosCircle.GetComponent<CheckTargetPos>().isMoveOkay)
                    {
                        UIManager.instance.ShowGuide("해당 위치가 순간이동하기에 불완전합니다.", true);
                    }
                    else
                    {
                        transform.position = teleportPosCircle.transform.position; // 확정한 위치로 순간 이동
                        if (anotherTeleportCharacter != null)
                        {
                            anotherTeleportCharacter.GetComponent<PlayerController>().nav.enabled = false;
                            anotherTeleportCharacter.transform.position = transform.position + transform.forward * 2f;
                            Debug.Log("@@@ " + anotherTeleportCharacter.transform.position);
                            anotherTeleportCharacter.GetComponent<PlayerController>().nav.enabled = true;
                        }
                        // characterFindCollider.enabled = false; // 캐릭터 찾기 트리거 콜라이더 컴포넌트 비활성화
                        teleportPosCircle.SetActive(false);

                        StartCoroutine(UpdateSupernaturalCooldown()); // 쿨타임 업데이트 시작

                        break; // 반복문 종료
                    }
                }
            }

            yield return null;
        }
        isTeleporting = false;
    }

    void FindAnotherTeleport()
    {
        // 플레이어 레이어에서 감지 거리 안에 있는 플레이어 콜라이더들 가져오기
        Collider[] hits = Physics.OverlapSphere(transform.position, canTeleportTogetherDistance, GameManager.instance.playerLayerMask);

        // 가장 가까운 플레이어 찾기
        anotherTeleportCharacter = null;
        float minDistance = canTeleportTogetherDistance + 1f; // 최소 거리는 감지 거리에 1 더한 것으로 초기화
        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // 자기 자신 제외

            Vector3 dirToTarget = hit.transform.position - transform.position; // 적과 플레이어 간의 방향 벡터 계산

            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget); // 앞을 바라보는 방향 벡터와 타겟과 적 간의 방향 벡터 사이의 각도 계산

            if (dirToTarget.magnitude <= canTeleportTogetherDistance) // 타겟이 추적 조건에 맞는지 비교(각도는 시야 각의 절반과 비교해야 함)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position); // 적과의 거리 계산
                if (distance < minDistance) // 적과의 거리가 최소 거리보다 작을 때
                {
                    minDistance = distance; // 최소 거리 갱신
                    anotherTeleportCharacter = hit.gameObject; // 가장 가까운 적 설정
                }
            }
        }
        Debug.Log("anotherTeleportCharacter: " + anotherTeleportCharacter);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, canTeleportTogetherDistance);
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
