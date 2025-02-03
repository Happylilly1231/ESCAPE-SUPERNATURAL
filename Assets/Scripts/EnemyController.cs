using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    enum EnemyState { Idle, Patrol, Chase }
    EnemyState currentState = EnemyState.Idle;

    public Animator anim;
    public CapsuleCollider col;
    public List<Transform> targets; // 타겟 = 플레이어들
    public Canvas canvas;
    public GameObject enemyWeapon;
    public GameObject enemyPatrolPoints; // 순찰 경로 포인트 저장된 오브젝트
    Transform[] patrolPoints; // 순찰 경로 포인트들

    NavMeshAgent nav;
    Vector2 moveVec;
    float fovAngle = 110f; // 시야각
    Vector3 targetPos;
    float sightDistance = 10f; // 시야 내에서 탐지할 수 있는 거리
    float detectDistance = 2.4f; // 시야 밖에서 탐지할 수 있는 거리

    int maxHp = 100;
    float curHp;

    public Slider hpBar;
    public TextMeshProUGUI hpTxt;

    GameObject closestPlayer;
    float stopDistance = 0.1f;

    int currentPatrolIndex;

    float idleTimer;
    float idleDuration = 5f;
    float attackTimer;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        curHp = maxHp;
    }

    void Start()
    {
        patrolPoints = enemyPatrolPoints.GetComponent<ShowPatrolPath>().patrolPoints; // 순찰 지점 배열 가져오기
        transform.position = patrolPoints[0].position; // 처음 순찰 지점으로 위치 초기화
    }

    void Update()
    {
        FindClosestPlayer(); // 주변에 있는 적들 중 가장 가까운 적을 찾기

        switch (currentState) // 현재 상태가
        {
            case EnemyState.Idle: // 가만히 있을 때(대기할 때)
                Idle(); // 대기
                break;
            case EnemyState.Patrol: // 순찰 중일 때
                Patrol(); // 순찰
                break;
            case EnemyState.Chase: // 추적 중일 때
                Chase(); // 추적
                break;
        }

        // 움직이는 방향에 맞는 애니메이션 설정
        SetAnimMoveVec();

        // 총 각도 변경
        if (enemyWeapon != null) // 총이 선택되어있을 때(총을 들고 있을 때)
        {
            Weapon weapon = enemyWeapon.GetComponent<Weapon>();

            bool isPlayingFireAnimation = anim.GetCurrentAnimatorStateInfo(0).IsName("Fire");
            bool isDoingFireTranstion = anim.GetAnimatorTransitionInfo(0).IsName("Fire -> Idle") || anim.GetAnimatorTransitionInfo(0).IsName("Idle -> Fire");

            if (!isDoingFireTranstion)
            {
                if (isPlayingFireAnimation)
                {
                    weapon.canFireBullet = true;
                    enemyWeapon.transform.localRotation = Quaternion.Euler(weapon.fireRotation); // 총 각도 변경(발사 각도)
                }
                else
                {
                    weapon.canFireBullet = false;
                    enemyWeapon.transform.localRotation = Quaternion.Euler(weapon.originalRotation);
                }
            }
            else
            {
                weapon.canFireBullet = false;
                enemyWeapon.transform.localRotation = Quaternion.Euler(weapon.originalRotation);
            }
        }

        // // 거리에 따라 멈춤 설정
        // if (!nav.pathPending && nav.remainingDistance < stopDistance)
        // {
        //     nav.isStopped = true;
        //     anim.SetBool("Moving", false);
        // }
        // else
        // {
        //     nav.isStopped = false;
        //     anim.SetBool("Moving", true);
        // }

        // 체력바 따라다니고 플레이어 화면 바라보게 하기
        canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);
    }

    // 주변에 있는 적들 중 가장 가까운 적을 찾는 함수
    void FindClosestPlayer()
    {
        // 플레이어 레이어에서 감지 거리 안에 있는 플레이어 콜라이더들 가져오기
        Collider[] hits = Physics.OverlapSphere(transform.position, sightDistance, GameManager.instance.playerLayerMask);

        // 가장 가까운 플레이어 찾기
        closestPlayer = null;
        float minDistance = sightDistance + 1f; // 최소 거리는 감지 거리에 1 더한 것으로 초기화
        foreach (Collider hit in hits)
        {
            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized; // 적과 플레이어 간의 방향 벡터 계산

            Vector3 forwardDirection;
            if (nav.isStopped)
                forwardDirection = transform.forward;
            else
                forwardDirection = nav.velocity.normalized;

            float angleToTarget = Vector3.Angle(forwardDirection, dirToTarget); // 앞을 바라보는 방향 벡터와 타겟과 적 간의 방향 벡터 사이의 각도 계산

            float distance = Vector3.Distance(transform.position, hit.gameObject.transform.position); // 플레이어와의 거리 계산

            if ((angleToTarget < fovAngle / 2f && distance <= sightDistance) || distance <= detectDistance) // 타겟이 추적 조건에 맞는지 비교(각도는 시야 각의 절반과 비교해야 함)
            {
                Debug.DrawRay(transform.position + Vector3.up * 2f, dirToTarget * distance, Color.red);

                Ray ray = new Ray(transform.position + Vector3.up * 2f, dirToTarget);

                if (Physics.Raycast(ray, out RaycastHit hit2, distance, GameManager.instance.mapLayerMask)) // 사이에 장애물이 있지 않으면
                {
                    Debug.Log("closest : " + hit2.collider.gameObject.name);
                    continue;
                }

                if (distance < minDistance) // 플레이어와의 거리가 최소 거리보다 작을 때
                {
                    minDistance = distance; // 최소 거리 갱신
                    closestPlayer = hit.gameObject; // 가장 가까운 플레이어 설정
                }
            }
        }
        Debug.Log("closestPlayer: " + closestPlayer);

        if (closestPlayer != null) // 가장 가까운 플레이어가 존재한다면
        {
            currentState = EnemyState.Chase; // 추적 상태로 변경
            stopDistance = 5f;
        }
    }

    // 가만히 있을 때(대기할 때) 함수
    void Idle()
    {
        Debug.Log("적 Idle 상태입니다.");

        idleTimer += Time.deltaTime;

        // 멈춤
        nav.isStopped = true;
        anim.SetBool("Moving", false);

        if (idleTimer >= idleDuration) // 10초 동안 대기 후 다시 순찰
        {
            idleTimer = 0f;
            currentState = EnemyState.Patrol; // 순찰 상태로 변경
            stopDistance = 0.1f;
            SetNextPatrolPoint(); // 현재 순찰 지점으로 이동
        }
    }

    // 현재 순찰 지점으로 이동 함수
    void SetNextPatrolPoint()
    {
        if (patrolPoints.Length > 0)
        {
            // 멈춤 해제
            nav.isStopped = false;
            anim.SetBool("Moving", true);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; // 현재 순찰 지점 갱신
            targetPos = patrolPoints[currentPatrolIndex].position;
            nav.SetDestination(targetPos); // 현재 순찰 지점으로 이동
        }
    }

    // 순찰 중 함수
    void Patrol()
    {
        Debug.Log("적 Patrol 상태입니다.");

        // 목표 지점에 도달했는지 확인
        if (!nav.pathPending && nav.remainingDistance <= stopDistance) // 목표 지점 도달
        {
            currentState = EnemyState.Idle; // 다음 순찰 지점으로 이동하기 전 대기하기
            stopDistance = 0.1f;
        }
    }

    void Chase()
    {
        Debug.Log("적 Chase 상태입니다.");

        // 추적 조건에 맞는 가장 가까운 플레이어 추적
        if (closestPlayer)
        {
            targetPos = closestPlayer.transform.position;
            nav.SetDestination(targetPos);

            if (!nav.pathPending && nav.remainingDistance < stopDistance)
            {
                nav.isStopped = true;
                anim.SetBool("Moving", false);
            }
            else
            {
                nav.isStopped = false;
                anim.SetBool("Moving", true);
            }

            attackTimer += Time.deltaTime;

            if (nav.isStopped)
            {
                if (attackTimer >= 1.5f)
                {
                    Debug.Log("~~~ " + closestPlayer);
                    attackTimer = 0f;
                    // 1.5초마다 가장 가까운 플레이어 공격
                    Attack(closestPlayer);
                }
            }
        }
        else
        {
            currentState = EnemyState.Patrol; // 순찰 상태로 변경
            stopDistance = 0.1f;
            SetNextPatrolPoint();
        }
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

    // 피해 함수
    public void Damage(int amount)
    {
        if (curHp - amount <= 0)
        {
            curHp = 0;
            hpBar.value = 0;
            hpTxt.text = "0 / " + maxHp.ToString();
            Debug.Log("적이 죽었습니다.");
            Destroy(gameObject);
        }
        else
        {
            curHp -= amount;
            hpBar.value = curHp / maxHp;
            hpTxt.text = curHp.ToString() + " / " + maxHp.ToString();
        }
    }

    // 공격 함수
    void Attack(GameObject player)
    {
        Debug.Log("적이 플레이어를 공격합니다.");

        Weapon weapon = enemyWeapon.GetComponent<Weapon>();
        if (weapon.canFire)
        {
            gameObject.transform.LookAt(player.transform);
            anim.SetTrigger("Fire");
            weapon.Use(gameObject);
        }
    }
}


