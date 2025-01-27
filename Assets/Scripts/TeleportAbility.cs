using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 순간이동 초능력
public class TeleportAbility : MonoBehaviour, ISupernatural
{
    public GameObject teleportPosCircle; // 순간이동 목표 위치를 나타내는 원
    float limitDistance = 10f; // 제한 거리
    bool canUIUpdate;

    public bool CanUIUpdate { get => canUIUpdate; set => canUIUpdate = value; }

    public void Activate()
    {
        StartCoroutine(Teleport()); // 순간이동 코루틴 실행
    }

    public void Deactivate()
    {
        CanUIUpdate = false;
    }

    // 순간 이동
    IEnumerator Teleport()
    {
        teleportPosCircle.SetActive(true); // 목표 위치 표시 원 활성화

        while (true)
        {
            // 위치 조정
            Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(Input.mousePosition); // 마우스 방향으로 화면에서 레이 쏘기
            if (Physics.Raycast(ray, out RaycastHit hit, limitDistance, GameManager.instance.mapLayerMask)) // 맵 레이어만 검출하고 제한 거리 내에서만 가능
            {
                teleportPosCircle.transform.position = hit.point; // 충돌 지점에 원 표시
            }

            // T키 -> 위치 확정 후 순간 이동
            if (Input.GetKeyDown(KeyCode.T))
            {
                gameObject.transform.position = teleportPosCircle.transform.position; // 확정한 위치로 순간 이동
                teleportPosCircle.SetActive(false); // 목표 위치 표시 원 비활성화
                break; // 반복문 종료
            }

            yield return null;
        }
    }
}
