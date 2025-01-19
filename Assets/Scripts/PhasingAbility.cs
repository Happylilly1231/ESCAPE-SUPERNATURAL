using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 물질통과 초능력
public class PhasingAbility : MonoBehaviour, ISupernatural
{
    Collider col;
    Rigidbody rigid;

    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    public void Activate()
    {
        StartCoroutine(Phasing()); // 물질 통과 코루틴 실행
    }

    // 물질 통과
    IEnumerator Phasing()
    {
        col = GetComponent<Collider>(); // 앉으면 콜라이더 사이즈가 바뀔 수도 있어서 여기서 초기화

        rigid.useGravity = false; // 떨어지지 않도록 중력 해제
        col.isTrigger = true; // 콜라이더를 Trigger되게 하기

        yield return new WaitForSeconds(2f); // 2초 동안 지속

        rigid.useGravity = true; // 중력 다시 활성화
        col.isTrigger = false; // 다시 원래대로 돌아오기
    }
}
