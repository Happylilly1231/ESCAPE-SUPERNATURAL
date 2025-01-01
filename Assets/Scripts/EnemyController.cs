using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public Animator anim;
    public BoxCollider collisionBox;
    public Transform target; // 타겟 = 플레이어
    float sightDistance = 10f;

    NavMeshAgent nav;
    Vector2 moveVec;
    LayerMask playerLayerMask;
    float fovAngle = 110f; // 시야각
    bool playerInSight = false; // 플레이어가 시야 내에 있는지 여부
    Vector3 originalPos;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        playerLayerMask = LayerMask.GetMask("Player");
        originalPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // 속력에 따라 Moving 상태 설정
        if (nav.velocity.magnitude < 0.1f)
        {
            anim.SetBool("Moving", false);
        }
        else
        {
            anim.SetBool("Moving", true);
        }

        Vector3 dirToTarget = target.position - transform.position; // 타겟과 적 간의 방향 벡터 계산

        float angleToTarget = Vector3.Angle(transform.forward, dirToTarget); // 앞을 바라보는 방향 벡터와 타겟과 적 간의 방향 벡터 사이의 각도 계산

        if (angleToTarget < fovAngle / 2f && dirToTarget.magnitude <= sightDistance) // 각도는 시야 각의 절반과 비교해야 함
        {
            if (Physics.Raycast(transform.position + collisionBox.center + Vector3.up * 0.5f, dirToTarget.normalized, out RaycastHit hit, sightDistance, playerLayerMask))
            {
                Debug.Log("타겟 발견 - 추적 중...");

                playerInSight = true;

                // 타겟 추적하기
                nav.SetDestination(target.position);

                // 움직이는 방향에 맞는 애니메이션 설정
                SetAnimMoveVec();
            }
            else
            {
                playerInSight = false;
            }
        }
        else
        {
            playerInSight = false;
        }

        if (!playerInSight)
        {
            nav.SetDestination(originalPos); // 원래 위치로 이동
        }
    }

    void OnDrawGizmos()
    {
        Vector3 leftBoundary = Quaternion.Euler(0, -fovAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fovAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * sightDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * sightDistance);
    }

    // 움직이는 방향에 맞는 애니메이션 설정
    void SetAnimMoveVec()
    {
        Vector3 velocity = nav.velocity; // NavMeshAgent의 속도 벡터 가져오기

        Vector3 localVelocity = transform.InverseTransformDirection(velocity); // 속도 벡터를 로컬 공간으로 변환 (적의 기준으로 방향 설정)

        // X와 Y를 블렌드 트리에 전달할 값으로 설정
        float inputX = localVelocity.x;
        float inputY = localVelocity.z; // NavMeshAgent는 z축을 앞으로 사용

        moveVec = new Vector2(inputX, inputY).normalized; // 속도를 정규화하여 블렌드 트리에 적합하게 변환

        // 애니메이터에 파라미터 전달
        anim.SetFloat("InputX", moveVec.x);
        anim.SetFloat("InputY", moveVec.y);
    }
}


