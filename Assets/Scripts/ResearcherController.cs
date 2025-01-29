using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearcherController : MonoBehaviour
{

    enum ResearcherState { Idle, Patrol, Stolen, Find }
    ResearcherState currentState = ResearcherState.Idle;

    public Animator anim;
    public List<Transform> targets; // 타겟 = 플레이어들
    public Transform[] patrolPoints; // 순찰 경로 포인트들
    public int keyCardLevel;
    public bool havingKeyCard;

    Vector2 moveVec;
    float fovAngle = 110f; // 시야각
    Vector3 targetPos;
    float sightDistance = 7f; // 시야 내에서 탐지할 수 있는 거리
    float detectDistance = 2.4f; // 시야 밖에서 탐지할 수 있는 거리

    int currentPatrolIndex;

    float idleTimer;
    float idleDuration = 10f;

    float stolenTimer;
    GameObject stealer; // 빼앗은 사람

    float patrolSpeed = 3f;

    Vector3 moveDir;

    bool isLooking;

    void Awake()
    {
        havingKeyCard = true;
    }

    void Start()
    {
        transform.position = patrolPoints[0].position; // 처음 순찰 지점으로 위치 초기화
    }

    void Update()
    {
        FindPlayer(); // 보이는 플레이어 찾기

        switch (currentState) // 현재 상태가
        {
            case ResearcherState.Idle: // 가만히 있을 때(대기할 때)
                Idle(); // 대기
                break;
            case ResearcherState.Patrol: // 돌아다니는 중일 때
                Patrol(); // 순찰
                break;
            case ResearcherState.Stolen: // 카드키를 빼앗겼을 때
                Stolen(); // 빼앗김
                break;
            case ResearcherState.Find: // 카드키를 빼앗은 사람을 발견했을 때
                Find(); // 발견
                break;
        }

        // 움직이는 방향에 맞는 애니메이션 설정
        SetAnimMoveVec();
    }

    // 주변에 있는 적들 중 가장 가까운 적을 찾는 함수
    void FindPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, sightDistance, GameManager.instance.playerLayerMask);

        foreach (Collider hit in hits)
        {
            Vector3 dirToTarget = hit.transform.position - transform.position; // 적과 플레이어 간의 방향 벡터 계산

            float angleToTarget = Vector3.Angle(transform.forward, dirToTarget); // 앞을 바라보는 방향 벡터와 타겟과 적 간의 방향 벡터 사이의 각도 계산

            if (!(angleToTarget < fovAngle / 2f && dirToTarget.magnitude <= sightDistance) && hit.gameObject.GetComponent<PlayerController>().isWalking) // 플레이어가 시야 밖에 있는데, 천천히 걸어올 때
                continue;

            if ((angleToTarget < fovAngle / 2f && dirToTarget.magnitude <= sightDistance) || dirToTarget.magnitude <= detectDistance) // 타겟이 추적 조건에 맞는지 비교(각도는 시야 각의 절반과 비교해야 함)
            {
                currentState = ResearcherState.Find;
                transform.LookAt(hit.transform);
                break;
            }
        }

        if (isLooking)
        {
            isLooking = false;
            currentState = ResearcherState.Patrol;
        }
    }

    // 가만히 있을 때(대기할 때) 함수
    void Idle()
    {
        Debug.Log("연구원 Idle 상태입니다.");

        // 10초 동안 대기 후 다시 순찰
        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration)
        {
            idleTimer = 0f;
            currentState = ResearcherState.Patrol; // 순찰 상태로 변경
            SetNextPatrolPoint(); // 현재 순찰 지점으로 이동
        }
    }

    // 현재 순찰 지점으로 이동 함수
    void SetNextPatrolPoint()
    {
        if (patrolPoints.Length > 0)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // 현재 순찰 지점 갱신
            targetPos = patrolPoints[currentPatrolIndex].position;
            // nav.SetDestination(targetPos); // 현재 순찰 지점으로 이동
        }
    }

    // 순찰 중 함수
    void Patrol()
    {
        Debug.Log("연구원 Patrol 상태입니다.");

        // 순찰 지점으로 이동
        moveDir = (targetPos - transform.position).normalized;
        moveDir.y = 0;
        transform.position += moveDir * patrolSpeed * Time.deltaTime;

        // 목표 방향으로의 회전 계산
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);

        // 부드럽게 회전
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, patrolSpeed * Time.deltaTime);

        // 도착했는지 확인
        if (Vector3.Distance(transform.position, targetPos) < 0.2f)
        {
            currentState = ResearcherState.Idle; // 다음 순찰 지점으로 이동하기 전 대기하기
        }
    }

    // 카드키를 빼앗겼을 때 함수
    void Stolen()
    {
        Debug.Log("연구원 Stolen 상태입니다.");

        stolenTimer += Time.deltaTime;

        if (stolenTimer >= 3f)
        {
            stolenTimer = 0f;
            // 빼앗은 사람의 방향을 무조건 쳐다봄
            if (stealer != null)
            {
                isLooking = true;
                transform.LookAt(stealer.transform);
            }
        }
    }

    void Find()
    {
        Debug.Log("연구원 Find 상태입니다.");

        // 게임 오버
        GameManager.instance.GameOver();
    }

    // 카드키 뺏기 함수
    public void StealKeyCard(GameObject player)
    {
        stealer = player;
        havingKeyCard = false;
        Debug.Log(stealer + "에게 " + keyCardLevel + "급 카드키를 빼앗겼습니다.");
        currentState = ResearcherState.Stolen;
    }

    void OnDrawGizmos()
    {
        Vector3 leftBoundary = Quaternion.Euler(0, -fovAngle / 2f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, fovAngle / 2f, 0) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * sightDistance);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * sightDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * detectDistance);
    }

    // 움직이는 방향에 맞는 애니메이션 설정
    void SetAnimMoveVec()
    {
        // X와 Z를 블렌드 트리에 전달할 값으로 설정
        float inputX = moveDir.x;
        float inputY = moveDir.z; // NavMeshAgent는 z축을 앞으로 사용

        moveVec = new Vector2(inputX, inputY).normalized; // 속도를 정규화하여 블렌드 트리에 적합하게 변환

        // 애니메이터에 파라미터 전달
        anim.SetFloat("InputX", moveVec.x);
        anim.SetFloat("InputY", moveVec.y);
    }
}
