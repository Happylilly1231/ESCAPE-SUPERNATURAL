using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 물질통과 초능력
public class PhasingAbility : MonoBehaviour, ISupernatural
{
    public bool isPhasing;
    Collider col;
    Collider zeroFrictionCol;
    Rigidbody rigid;
    float duration = 2f;
    float durationRemainTime;
    bool canUIUpdate;
    bool canPass;

    public bool CanUIUpdate { get => canUIUpdate; set => canUIUpdate = value; }
    public float SupernaturalCoolDown { get => supernaturalCoolDown; set => supernaturalCoolDown = value; }
    public float CooldownRemainTime { get => cooldownRemainTime; set => cooldownRemainTime = value; }
    public bool IsSupernaturalReady { get => isSupernaturalReady; set => isSupernaturalReady = value; }

    float supernaturalCoolDown = 5f; // 초능력 쿨타임 (초 단위)
    float cooldownRemainTime;
    bool isSupernaturalReady = true; // 초능력 사용 가능 여부

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void Activate()
    {
        if (gameObject.GetComponent<PlayerController>().isGrounded)
        {
            StartCoroutine(Phasing()); // 물질 통과 코루틴 실행
        }
        else
        {
            UIManager.instance.ShowGuide("공중에서 능력을 쓸 수 없습니다.");
        }
    }

    public void Deactivate()
    {
        CanUIUpdate = false;
    }

    // 물질 통과
    IEnumerator Phasing()
    {
        isPhasing = true;
        col = GetComponent<PlayerController>().col; // 앉으면 콜라이더 사이즈가 바뀔 수도 있어서 여기서 초기화
        zeroFrictionCol = GetComponent<PlayerController>().zeroFrictionCol;

        canPass = true;
        rigid.useGravity = false; // 떨어지지 않도록 중력 해제
        col.isTrigger = true; // 콜라이더를 Trigger되게 하기
        zeroFrictionCol.isTrigger = true;

        // 지속시간만큼 지속
        durationRemainTime = duration;
        while (durationRemainTime > 0)
        {
            if (!canPass)
            {
                durationRemainTime = 0;
                Debug.Log("canPass : false");
            }

            durationRemainTime -= Time.deltaTime;

            if (canUIUpdate)
            {
                // 지속시간 UI에서 업데이트
                UIManager.instance.duraitonDisableImg.fillAmount = durationRemainTime / duration;
                UIManager.instance.durationRemainTimeText.text = durationRemainTime.ToString("F1") + "s";
            }

            yield return null;
        }
        if (canUIUpdate)
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

    void OnTriggerEnter(Collider other)
    {
        // if (other.gameObject.tag != "CanPhasing" && other.gameObject.tag != "Weapon" && other.gameObject.tag != "Floor")
        // {
        //     Debug.Log("OnTriggerEnter " + other.gameObject.name);
        //     col.isTrigger = false;
        // }

        if (other.gameObject.tag == "OuterWall")
        {
            Debug.Log("OnTriggerEnter : " + other.gameObject.tag);
            canPass = false;
            if (canUIUpdate)
            {
                UIManager.instance.durationImg.color = Color.red;
                UIManager.instance.ShowGuide("해당 물체는 통과할 수 없습니다.");
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

            if (canUIUpdate)
            {
                UIManager.instance.cooldownDisableImg.fillAmount = CooldownRemainTime / SupernaturalCoolDown;
                UIManager.instance.cooldownRemainTimeText.text = CooldownRemainTime.ToString("F1") + "s";
            }
            yield return null;
        }
        if (canUIUpdate)
            UIManager.instance.cooldownRemainTimeText.text = "";

        IsSupernaturalReady = true;
    }
}
