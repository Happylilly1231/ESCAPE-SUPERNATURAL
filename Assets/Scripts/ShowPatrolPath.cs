using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowPatrolPath : MonoBehaviour
{
    public Transform[] patrolPoints; // 순찰 지점 배열

    void OnDrawGizmos()
    {
        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Gizmos.color = Color.red;
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f); // 순찰 지점 위치를 표시하는 원

                    if (i + 1 < patrolPoints.Length)
                    {
                        if (patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position); // 다음 순찰 지점까지 선 그리기
                        }
                    }
                }
            }
        }
    }
}
