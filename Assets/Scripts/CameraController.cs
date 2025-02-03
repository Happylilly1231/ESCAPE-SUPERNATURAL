using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject player; // 카메라가 따라갈 대상
    public float distance = 2f; // 기본 거리
    public float minDistance = 0.5f; // 최소 줌
    public float maxDistance = 4f; // 최대 줌
    public float zoomSpeed = 2f; // 줌 속도

    public float rotationSpeed = 3f; // 회전 속도
    public float minVerticalAngle = -10f; // 아래쪽 제한 (바닥이 너무 많이 보이지 않게)
    public float maxVerticalAngle = 25f; // 위쪽 제한 (천장이 보이지 않도록)

    public Vector3 cameraOffset = new Vector3(0, 2.6f, 0); // 카메라 위치 보정값

    float currentY = 10f; // 초기 각도 (약간 내려다보는 시점)

    void Update()
    {
        if (!GameManager.instance.isAllowOnlyUIInput)
        {
            // 마우스 드래그로 회전 조작
            if (Time.timeScale == 1f)
            {
                currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
                currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle); // 각도 제한

                // 마우스 휠로 줌 조작
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance); // 줌 범위 제한
            }
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 플레이어가 바라보는 방향으로 카메라 회전
        Quaternion playerRotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
        Quaternion rotation = playerRotation * Quaternion.Euler(currentY, 0, 0); // X축 고정, Y축만 회전

        Vector3 targetPos = player.transform.position + cameraOffset + rotation * new Vector3(0, 0, -distance);

        // Raycast로 맵 감지
        if (Physics.Raycast(player.transform.position + cameraOffset, targetPos - (player.transform.position + cameraOffset), out RaycastHit hit, distance, GameManager.instance.mapLayerMask))
        {
            distance = minDistance;
            targetPos = player.transform.position + cameraOffset + rotation * new Vector3(0, 0, -distance);
        }

        transform.position = targetPos;

        transform.LookAt(player.transform.position + cameraOffset); // 대상 바라보기
    }
}
