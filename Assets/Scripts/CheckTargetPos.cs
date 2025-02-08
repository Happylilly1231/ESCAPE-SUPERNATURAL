using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckTargetPos : MonoBehaviour
{
    bool obstacleDetected;
    public bool isMoveOkay = true;
    public float radius; // -1이면 사각형
    Renderer rend;

    void Awake()
    {
        rend = gameObject.GetComponent<Renderer>();
    }

    void Update()
    {
        Collider[] hits;
        if (radius == -1)
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();

            // 박스 콜라이더 중심을 월드 좌표 기준으로 변환
            Vector3 worldCenter = boxCollider.transform.TransformPoint(boxCollider.center);
            Debug.Log("%%% " + transform.position + " / " + worldCenter);

            // 박스 콜라이더의 실제 크기 계산 (월드 기준)
            Vector3 worldSize = Vector3.Scale(boxCollider.size, transform.lossyScale);  // 월드 공간 크기
            Vector3 halfSize = worldSize / 2;

            hits = Physics.OverlapBox(worldCenter, halfSize, transform.rotation, GameManager.instance.mapLayerMask);
        }
        else
        {
            hits = Physics.OverlapSphere(transform.position, radius, GameManager.instance.mapLayerMask);
        }

        obstacleDetected = false;
        foreach (Collider hit in hits)
        {
            Debug.Log("checkTargetPos : " + hit.gameObject.name);
            if (hit.gameObject.tag == "OuterWall" || hit.gameObject.tag == "InnerWall" || hit.gameObject.tag == "Door" || hit.gameObject.tag == "Ceiling")
            {
                obstacleDetected = true;
                break;
            }
        }

        isMoveOkay = !obstacleDetected;

        if (isMoveOkay)
        {
            rend.material.color = Color.yellow;
        }
        else
        {
            rend.material.color = Color.red;
        }
    }
}
