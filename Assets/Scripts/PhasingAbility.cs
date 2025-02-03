using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 물질통과 초능력
public class PhasingAbility : MonoBehaviour, ISupernatural
{
    public bool isPhasing;
    public float backwardForce = 1f;
    CapsuleCollider col;
    public CapsuleCollider zeroFrictionCol;
    Rigidbody rigid;
    float duration = 2f;
    float durationRemainTime;
    bool canPass;

    public float SupernaturalCoolDown { get => supernaturalCoolDown; set => supernaturalCoolDown = value; }
    public float CooldownRemainTime { get => cooldownRemainTime; set => cooldownRemainTime = value; }
    public bool IsSupernaturalReady { get => isSupernaturalReady; set => isSupernaturalReady = value; }

    float supernaturalCoolDown = 5f; // 초능력 쿨타임 (초 단위)
    float cooldownRemainTime;
    bool isSupernaturalReady = true; // 초능력 사용 가능 여부
    float yValue;

    Vector3 safePos;
    PlayerController playerController;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        col = playerController.col; // 앉으면 콜라이더 사이즈가 바뀔 수도 있어서 여기서 초기화
        // zeroFrictionCol = GetComponent<PlayerController>().zeroFrictionCol;
    }

    public void Activate()
    {
        if (playerController.isGrounded && !isPhasing)
        {
            StartCoroutine(Phasing()); // 물질 통과 코루틴 실행
        }
        else
        {
            UIManager.instance.ShowGuide("공중에서 능력을 쓸 수 없습니다.", true);
        }
    }

    public void Deactivate()
    {
        if (isPhasing)
        {
            durationRemainTime = 0;
            CheckOverlapWithObjects();
        }
    }

    // 물질 통과
    IEnumerator Phasing()
    {
        rigid.velocity = Vector3.zero;
        safePos = transform.position;

        isPhasing = true;

        canPass = true;
        // yValue = transform.position.y;
        rigid.useGravity = false; // 떨어지지 않도록 중력 해제
        col.isTrigger = true; // 콜라이더를 Trigger되게 하기
        zeroFrictionCol.isTrigger = true;

        // 지속시간만큼 지속
        durationRemainTime = duration;
        while (durationRemainTime > 0)
        {
            // transform.position = new Vector3(transform.position.x, yValue, transform.position.z);

            CheckOverlapWithObjects();

            // if (!canPass)
            // {
            //     durationRemainTime = 0;
            //     Debug.Log("canPass : false");
            // }

            durationRemainTime -= Time.deltaTime;

            if (playerController.canUIUpdate)
            {
                // 지속시간 UI에서 업데이트
                UIManager.instance.duraitonDisableImg.fillAmount = durationRemainTime / duration;
                UIManager.instance.durationRemainTimeText.text = durationRemainTime.ToString("F1") + "s";
            }

            yield return null;
        }
        CheckOverlapWithObjects();

        if (playerController.canUIUpdate)
        {
            UIManager.instance.durationRemainTimeText.text = "";
            UIManager.instance.durationImg.color = Color.white;
        }

        // yield return new WaitForSeconds(duration); // 2초 동안 지속

        rigid.useGravity = true; // 중력 다시 활성화
        col.isTrigger = false; // 다시 원래대로 돌아오기
        zeroFrictionCol.isTrigger = false;

        isPhasing = false;

        StartCoroutine(UpdateSupernaturalCooldown()); // 쿨타임 업데이트 시작
    }

    // void OnTriggerEnter(Collider other)
    // {
    //     if (other.gameObject.tag == "OuterWall")
    //     {
    //         safePos = transform.position;
    //         Debug.Log("OnTriggerEnter : " + other.gameObject.tag);
    //     }
    // }

    // void OnTriggerStay(Collider other)
    // {
    //     if (isPhasing)
    //     {
    //         if (other.gameObject.tag == "OuterWall")
    //         {
    //             transform.position = safePos;
    //             if (playerController.canUIUpdate)
    //             {
    //                 UIManager.instance.durationImg.color = Color.red;
    //                 UIManager.instance.ShowGuide("해당 물체는 통과할 수 없습니다.", false);
    //             }
    //         }
    //         else
    //         {
    //             if (playerController.canUIUpdate)
    //             {
    //                 UIManager.instance.durationImg.color = Color.yellow;
    //                 Debug.Log("OnTriggerEnter " + other.gameObject.name);
    //             }

    //             if (durationRemainTime <= 0)
    //             {
    //                 transform.position = safePos;
    //             }
    //         }
    //     }
    // }

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

    // 물질 통과 종료 후 겹쳐 있는지 체크
    void CheckOverlapWithObjects()
    {
        // 캡슐의 중심과 크기를 가져옴
        Vector3 capsuleCenter = transform.position + zeroFrictionCol.center;
        float radius = zeroFrictionCol.radius;
        float height = zeroFrictionCol.height;
        Vector3 direction = transform.up;  // 캡슐이 Y축으로 배치된 경우

        // OverlapCapsule 호출
        Collider[] colliders = Physics.OverlapCapsule(
            capsuleCenter - direction * height / 2f,  // 캡슐 하단
            capsuleCenter + direction * height / 2f,  // 캡슐 상단
            radius
        );

        bool isCurPosSafe = true;
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != gameObject && !collider.isTrigger)  // 자기 자신, 여러 트리거 
            {
                isCurPosSafe = false;
                Debug.Log("TTT : " + collider.gameObject.name);

                if (collider.gameObject.tag == "OuterWall")
                {
                    transform.position = safePos;
                    if (playerController.canUIUpdate)
                    {
                        UIManager.instance.durationImg.color = Color.red;
                        UIManager.instance.ShowGuide("해당 물체는 통과할 수 없습니다.", false);
                    }
                    break;
                }
                else
                {
                    if (playerController.canUIUpdate)
                    {
                        UIManager.instance.durationImg.color = Color.yellow;
                    }

                    if (durationRemainTime <= 0)
                    {
                        transform.position = safePos;
                        break;
                    }
                }
            }
        }

        if (isCurPosSafe)
        {
            safePos = transform.position;
            if (playerController.canUIUpdate)
                UIManager.instance.durationImg.color = Color.white;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 capsuleCenter = transform.position + zeroFrictionCol.center;
        float radius = zeroFrictionCol.radius;
        float height = zeroFrictionCol.height;
        Vector3 direction = transform.up;  // 캡슐이 Y축으로 배치된 경우

        // 하단과 상단 위치 계산
        Vector3 capsuleBottom = capsuleCenter - direction * (height / 2f - radius);  // 하단 위치
        Vector3 capsuleTop = capsuleCenter + direction * (height / 2f - radius);     // 상단 위치

        // Gizmo로 캡슐의 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(capsuleBottom, radius);  // 하단 원
        Gizmos.DrawWireSphere(capsuleTop, radius);     // 상단 원

        // 하단과 상단을 잇는 선을 그려서 캡슐 형태를 시각화
        Gizmos.DrawLine(capsuleBottom, capsuleTop);
    }
}
