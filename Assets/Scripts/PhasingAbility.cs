using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 물질통과 초능력
public class PhasingAbility : MonoBehaviour, ISupernatural
{
    Collider col;
    Rigidbody rigid;
    float duration = 2f;
    float durationRemainTime;
    bool canUIUpdate;
    public bool CanUIUpdate { get => canUIUpdate; set => canUIUpdate = value; }

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void Activate()
    {
        StartCoroutine(Phasing()); // 물질 통과 코루틴 실행
    }

    void Update()
    {
        // T키 -> 소리 내서 적 유인하기
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("소리 내서 적 유인하기 시작");
        }
    }

    public void Deactivate()
    {
        CanUIUpdate = false;
    }

    // 물질 통과
    IEnumerator Phasing()
    {
        col = GetComponent<Collider>(); // 앉으면 콜라이더 사이즈가 바뀔 수도 있어서 여기서 초기화

        rigid.useGravity = false; // 떨어지지 않도록 중력 해제
        col.isTrigger = true; // 콜라이더를 Trigger되게 하기

        // 지속시간만큼 지속
        durationRemainTime = duration;
        while (durationRemainTime > 0)
        {
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
            col.isTrigger = false;
            if (canUIUpdate)
                UIManager.instance.durationImg.color = Color.red;
        }
    }

    // void OnTriggerExit(Collider other)
    // {
    //     if (other.gameObject.tag == "OuterWall")
    //     {
    //         Debug.Log("OnTriggerExit : " + other.gameObject.tag);
    //         col.isTrigger = true;
    //         if (canUIUpdate)
    //             UIManager.instance.cooldownImg.color = Color.white;
    //     }
    // }
}
