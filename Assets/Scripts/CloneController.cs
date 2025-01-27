using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CloneController : MonoBehaviour
{
    public Animator anim;
    Vector3 targetPos;
    public GameObject[] weapons; // 무기 배열
    public GameObject cloningAbilityCharacter;

    NavMeshAgent nav;
    Vector2 moveVec;
    float fovAngle = 110f; // 시야각
    bool playerInSight = false; // 플레이어가 시야 내에 있는지 여부
    Transform closeTarget;
    Vector3 dirToCloseTarget;
    float sightDistance = 10f; // 시야 내에서 탐지할 수 있는 거리
    float detectDistance = 5f; // 시야 밖에서 탐지할 수 있는 거리

    int maxHp = 100;
    float curHp;
    int damage = 10;

    public bool isFollow;
    bool isCurFollow;
    bool isMovingToTargetPos;
    float stopDistance = 0.1f;

    bool isCheckingFireAnimationEnd;
    GameObject selectedWeapon;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        curHp = maxHp;
    }

    void OnEnable()
    {
        // isMovingToTargetPos = false;
        isFollow = false;
        isCurFollow = false;
        nav.isStopped = true;

        anim.SetBool("Moving", false);

        nav.isStopped = true;

        StartCoroutine(TrackingTarget()); // 타겟 추적
        StartCoroutine(FollowCloningAbilityCharacter());

        foreach (GameObject weapon in weapons)
        {
            weapon.SetActive(false);
        }
    }

    public void MoveToTargetPos(Vector3 pos)
    {
        isMovingToTargetPos = true;
        targetPos = pos;
        nav.SetDestination(targetPos);
        stopDistance = 0.1f;
    }

    IEnumerator FollowCloningAbilityCharacter()
    {
        while (isFollow)
        {
            Debug.Log("분신이 따라가는 중...");
            targetPos = cloningAbilityCharacter.transform.position;
            nav.SetDestination(targetPos);
            stopDistance = 3f;

            yield return new WaitForSeconds(0.5f);
        }
        isCurFollow = false;
    }

    void Update()
    {
        if (!isCurFollow && isFollow)
        {
            isCurFollow = true;
            StartCoroutine(FollowCloningAbilityCharacter());
            nav.isStopped = false;
            anim.SetBool("Moving", true);
        }
        else if (isCurFollow && !isFollow)
        {
            isCurFollow = false;
            nav.isStopped = true;
            anim.SetBool("Moving", false);
        }

        if (isMovingToTargetPos || isFollow)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);
            // 속력에 따라 Moving 상태 설정
            if (distanceToTarget < stopDistance)
            {
                nav.isStopped = true;
                anim.SetBool("Moving", false);
                isMovingToTargetPos = false;
            }
            else
            {
                nav.isStopped = false;
                anim.SetBool("Moving", true);
            }
        }

        // 움직이는 방향에 맞는 애니메이션 설정
        SetAnimMoveVec();

        // 총이 선택되어있을 때(총을 들고 있을 때)
        if (selectedWeapon != null)
        {
            // 총 각도 원래대로 변경
            SetWeaponToOrignalRotation(selectedWeapon.GetComponent<Weapon>());
        }
    }

    // 총 각도 원래대로 변경하는 함수
    void SetWeaponToOrignalRotation(Weapon weapon)
    {
        // 총 각도 원래대로 변경
        if (isCheckingFireAnimationEnd && weapon.canFire)
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Fire"))
            {
                selectedWeapon.transform.localRotation = Quaternion.Euler(weapon.originalRotation); // 총 각도 원래대로 변경(들고 있는 각도)
                isCheckingFireAnimationEnd = false;
            }
        }
    }

    IEnumerator TrackingTarget()
    {
        while (gameObject.activeSelf)
        {
            if (nav.isStopped)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, detectDistance, GameManager.instance.enemyLayerMask);

                GameObject closestEnemy = null;
                float minDistance = detectDistance + 1f;

                foreach (Collider hit in hits)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestEnemy = hit.gameObject;
                    }
                }

                Debug.Log("closestEnemy: " + closestEnemy);
                if (closestEnemy != null)
                {
                    Attack(closestEnemy);
                    yield return new WaitForSeconds(1f);
                }
                else
                {
                    if (selectedWeapon != null)
                    {
                        selectedWeapon.SetActive(false);
                        selectedWeapon = null;
                    }
                }
            }

            yield return null; // 매 프레임마다가 아니라 0.5초 주기로 반복됨(최적화를 위해서)
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
            Debug.Log("분신이 죽었습니다.");
            Destroy(gameObject);
        }
        else
        {
            curHp -= amount;
        }
    }

    void Attack(GameObject enemy)
    {
        Debug.Log("분신이 적을 공격합니다.");

        PlayerController cloningAbilityCharacterController = cloningAbilityCharacter.GetComponent<PlayerController>();

        // 총으로 공격 구현
        for (int i = 0; i < 3; i++)
        {
            if (cloningAbilityCharacterController.equipWeapons[i] != null)
            {
                int id = cloningAbilityCharacterController.equipWeapons[i].GetComponent<Weapon>().weaponId;
                selectedWeapon = weapons[id];
                selectedWeapon.SetActive(true);
                gameObject.transform.LookAt(enemy.transform);

                Weapon weapon = selectedWeapon.GetComponent<Weapon>();
                // Debug.Log("weapon.canFire: " + weapon.canFire);
                if (weapon.canFire)
                {
                    anim.SetTrigger("Fire");
                    weapon.Use(gameObject);
                }
                break;
            }
        }
    }
}
