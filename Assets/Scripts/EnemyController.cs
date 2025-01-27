using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyController : MonoBehaviour
{
    public Animator anim;
    public BoxCollider collisionBox;
    public List<Transform> targets; // 타겟 = 플레이어들
    public Transform hpBarPos;
    public Canvas canvas;

    NavMeshAgent nav;
    Vector2 moveVec;
    float fovAngle = 110f; // 시야각
    bool playerInSight = false; // 플레이어가 시야 내에 있는지 여부
    Vector3 originalPos;
    Transform closeTarget;
    Vector3 dirToCloseTarget;
    Vector3 targetPos;
    float sightDistance = 10f; // 시야 내에서 탐지할 수 있는 거리
    float detectDistance = 2.4f; // 시야 밖에서 탐지할 수 있는 거리

    int maxHp = 100;
    float curHp;
    int damage = 10;

    public Slider hpBar;
    public TextMeshProUGUI hpTxt;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        originalPos = transform.position;
        curHp = maxHp;
    }

    void Start()
    {
        // hpBar = Instantiate(UIManager.instance.hpBarPrefab, canvas.transform);
        StartCoroutine(TrackingTarget()); // 타겟 추적
    }

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

        // 체력바 위치 업데이트
        // Vector3 screenPos = GameManager.instance.mainCamera.WorldToScreenPoint(hpBarPos.position);
        // hpBar.transform.position = screenPos;
        // Vector3 dirToCamera = GameManager.instance.mainCamera.transform.position - hpBar.transform.position;
        // dirToCamera.y = 0;
        // hpBar.transform.rotation = Quaternion.LookRotation(dirToCamera);
        // Debug.Log(dirToCamera);

        canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);
    }

    IEnumerator TrackingTarget()
    {
        targetPos = originalPos;

        while (true)
        {
            // 변수 초기화
            closeTarget = null; // 가장 가까운 타겟
            dirToCloseTarget = targets[0].position - transform.position; // 가장 가까운 타겟의 방향

            // 추적 조건에 맞는 가장 가까운 타겟 탐색
            foreach (Transform target in targets)
            {
                Vector3 dirToTarget = target.position - transform.position; // 타겟과 적 간의 방향 벡터 계산

                float angleToTarget = Vector3.Angle(transform.forward, dirToTarget); // 앞을 바라보는 방향 벡터와 타겟과 적 간의 방향 벡터 사이의 각도 계산

                // Debug.Log(target.name + " | " + (angleToTarget < fovAngle / 2f) + "," + (dirToTarget.magnitude <= sightDistance) + "," + (dirToTarget.magnitude <= detectDistance) + " , " + dirToTarget.magnitude);

                if ((angleToTarget < fovAngle / 2f && dirToTarget.magnitude <= sightDistance) || dirToTarget.magnitude <= detectDistance) // 타겟이 추적 조건에 맞는지 비교(각도는 시야 각의 절반과 비교해야 함)
                {
                    if (dirToTarget.magnitude <= dirToCloseTarget.magnitude) // 더 가까운 타겟이면
                    {
                        closeTarget = target; // 가장 가까운 타겟 변경
                        dirToCloseTarget = dirToTarget; // 가장 가까운 타겟의 방향 변경
                    }
                }
            }

            // 적의 이동
            if (closeTarget) // 추적할 타겟이 있으면
            {
                // 타겟 추적
                targetPos = closeTarget.position;
                nav.SetDestination(targetPos);
            }
            else // 추적할 타겟이 없으면
            {
                // 목적지가 원래 위치가 아닌 경우에 한번만 원래 위치로 초기화
                if (targetPos != originalPos)
                {
                    // 원래 위치로 이동
                    targetPos = originalPos;
                    nav.SetDestination(targetPos);
                }
            }
            // 움직이는 방향에 맞는 애니메이션 설정
            SetAnimMoveVec();

            // Debug.Log(closeTarget);

            yield return new WaitForSeconds(0.5f);
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

    public void Damage(int amount)
    {
        if (curHp - amount < 0)
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

    void OnCollisionEnter(Collision collision)
    {
        Attack(collision.gameObject);
    }

    void Attack(GameObject player)
    {
        Debug.Log("적이 플레이어를 공격합니다.");
        player.GetComponent<PlayerController>().Damage(damage);
    }
}


