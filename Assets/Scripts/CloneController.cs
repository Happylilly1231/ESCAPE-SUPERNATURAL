using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class CloneController : MonoBehaviour
{
    public Animator anim; // 분신 애니메이터
    public GameObject[] weapons; // 무기 배열
    public GameObject cloningAbilityCharacter; // 분신술 캐릭터

    public Canvas canvas;

    // 체력바
    public Slider hpBar;
    public TextMeshProUGUI hpTxt;
    public int cloneNum; // 1부터 시작

    NavMeshAgent nav;
    Vector2 moveVec;
    float detectDistance = 5f; // 적 탐지 거리(시야각 고려 X)
    Vector3 targetPos; // 목표 위치

    int maxHp = 100; // 최대 체력
    float curHp; // 현재 체력

    bool isMovingToTargetPos; // 목표 위치로 이동하고 있는지 여부
    float stopDistance = 0.1f; // nav가 이동을 멈추는 거리

    GameObject holdingWeapon; // 들고 있는 무기(분신술 캐릭터가 가지고 있는 무기 중 앞 순서부터 선택됨)

    bool isCurFollow; // 현재 분신이 분신술 캐릭터를 따라가고 있는지 여부
    bool isFollow; // 분신이 분신술 캐릭터 따라오기 선택 여부

    public bool IsFollow { get => isFollow; set => isFollow = value; }

    GameObject triggerMachine;

    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        nav.isStopped = true;
    }

    // 분신을 생성할 때(분신이 활성화될 때) 실행되는 함수
    void OnEnable()
    {
        // 초기화
        nav.enabled = true; // NavMeshAgent 다시 활성화
        curHp = maxHp; // 현재 체력 최대 체력으로 설정
        isMovingToTargetPos = false;
        isCurFollow = false;
        IsFollow = false;

        // 모든 무기 비활성화
        foreach (GameObject weapon in weapons)
        {
            weapon.SetActive(false);
        }

        StartCoroutine(FindClosestEnemy()); // 주변에 있는 적들 중 가장 가까운 적을 찾는 코루틴 실행
        StartCoroutine(FollowCloningAbilityCharacter());
    }

    // 분신술 캐릭터가 설정한 목표 위치로 이동하는 함수
    public void MoveToTargetPos(Vector3 pos)
    {
        isMovingToTargetPos = true; // 
        targetPos = pos; // 목표 위치 설정
        nav.SetDestination(targetPos); // NavMeshAgent로 목표 위치로 이동
        stopDistance = 0.1f; // nav가 목표 위치에 도착해야 멈추는 것으로 설정
    }

    // 분신이 분신술 캐릭터를 따라가는 함수
    IEnumerator FollowCloningAbilityCharacter()
    {
        while (IsFollow) // 
        {
            Debug.Log("분신이 따라가는 중...");
            targetPos = cloningAbilityCharacter.transform.position; // 목표 위치를 분신술 캐릭터 위치로 설정
            nav.SetDestination(targetPos); // NacMeshAgent로 목표 위치로 이동
            stopDistance = 3f; // 일정 거리만큼 가까워지면 멈추는 것으로 설정

            yield return new WaitForSeconds(0.5f);
        }
        isCurFollow = false; // 현재 분신이 분신술 캐릭터를 따라가고 있지 않음
    }

    void Update()
    {
        // 체력바 따라다니고 플레이어 화면 바라보게 하기
        canvas.gameObject.SetActive(true);
        canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);

        if (!isCurFollow && IsFollow) // 현재 따라가고 있지 않고, 따라오도록 선택했을 때(1번만 실행되기 위해 이렇게 조건 설정)
        {
            isCurFollow = true; // 현재 따라가고 있는 것으로 변경
            StartCoroutine(FollowCloningAbilityCharacter()); // 분신술 캐릭터를 따라가는 코루틴 실행

            // 멈춤 해제
            nav.isStopped = false;
            anim.SetBool("Moving", true);
        }
        else if (isCurFollow && !IsFollow) // 현재 따라가고 있고, 따라오지 않도록 선택했을 때
        {
            isCurFollow = false; // 현재 따라가고 있지 않은 것으로 변경

            // 멈춤
            nav.isStopped = true;
            anim.SetBool("Moving", false);
        }

        if (isMovingToTargetPos || IsFollow) // 현재 움직이고 있을 때(멈춰있어도 따라가고 있는 중이면 해당)
        {
            // 거리에 따라 멈춤 설정
            float distanceToTarget = Vector3.Distance(transform.position, targetPos); // 목표 위치까지 거리
            if (distanceToTarget < stopDistance)
            {
                nav.isStopped = true;
                anim.SetBool("Moving", false);
                if (isMovingToTargetPos) // 목표 위치로 가는 중이었으면
                    isMovingToTargetPos = false; // 목표 위치에 도달했다고 설정
            }
            else
            {
                nav.isStopped = false;
                anim.SetBool("Moving", true);
            }
        }

        // 움직이는 방향에 맞는 애니메이션 설정
        SetAnimMoveVec();

        // 총 각도 변경
        if (holdingWeapon != null) // 총이 선택되어있을 때(총을 들고 있을 때)
        {
            Weapon weapon = holdingWeapon.GetComponent<Weapon>();

            bool isPlayingFireAnimation = anim.GetCurrentAnimatorStateInfo(0).IsName("Fire");
            bool isDoingFireTranstion = anim.GetAnimatorTransitionInfo(0).IsName("Fire -> Idle") || anim.GetAnimatorTransitionInfo(0).IsName("Idle -> Fire");

            if (!isDoingFireTranstion)
            {
                if (isPlayingFireAnimation)
                {
                    weapon.canFireBullet = true;
                    holdingWeapon.transform.localRotation = Quaternion.Euler(weapon.fireRotation); // 총 각도 변경(발사 각도)
                }
                else
                {
                    weapon.canFireBullet = false;
                    holdingWeapon.transform.localRotation = Quaternion.Euler(weapon.originalRotation);
                }
            }
            else
            {
                weapon.canFireBullet = false;
                holdingWeapon.transform.localRotation = Quaternion.Euler(weapon.originalRotation);
            }
        }
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

    // 주변에 있는 적들 중 가장 가까운 적을 찾는 코루틴
    IEnumerator FindClosestEnemy()
    {
        while (gameObject.activeSelf)
        {
            if (nav.isStopped)
            {
                // 적 레이어에서 감지 거리 안에 있는 적 콜라이더들 가져오기
                Collider[] hits = Physics.OverlapSphere(transform.position, detectDistance, GameManager.instance.enemyLayerMask);

                // 가장 가까운 적 찾기
                GameObject closestEnemy = null;
                float minDistance = detectDistance + 1f; // 최소 거리는 감지 거리에 1 더한 것으로 초기화
                foreach (Collider hit in hits)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position); // 적과의 거리 계산
                    if (distance < minDistance) // 적과의 거리가 최소 거리보다 작을 때
                    {
                        minDistance = distance; // 최소 거리 갱신
                        closestEnemy = hit.gameObject; // 가장 가까운 적 설정
                    }
                }
                Debug.Log("closestEnemy: " + closestEnemy);

                if (closestEnemy != null) // 가장 가까운 적이 존재한다면
                {
                    // 0.5초마다 가장 가까운 적 공격
                    Attack(closestEnemy);
                    float curTime = 0.5f;
                    while (curTime > 0)
                    {
                        curTime -= Time.deltaTime;
                        yield return null;
                    }
                }
                else // 존재하지 않는다면
                {
                    if (holdingWeapon != null) // 무기를 들고 있을 때
                    {
                        // 무기 넣기
                        holdingWeapon.SetActive(false); // 들고 있는 무기 비활성화
                        holdingWeapon = null; // 들고 있는 무기 초기화
                    }
                }
            }

            yield return null;
        }
    }

    // 피해 함수
    public void Damage(int amount)
    {
        if (curHp - amount <= 0)
        {
            curHp = 0;
            hpBar.value = 0;
            hpTxt.text = "0 / " + maxHp.ToString();
            Debug.Log("분신이 죽었습니다.");
            gameObject.SetActive(false);
        }
        else
        {
            curHp -= amount;
            hpBar.value = curHp / maxHp;
            hpTxt.text = curHp.ToString() + " / " + maxHp.ToString();
        }
    }

    // 공격 함수
    void Attack(GameObject enemy)
    {
        Debug.Log("분신이 적을 공격합니다.");

        PlayerController cloningAbilityCharacterController = cloningAbilityCharacter.GetComponent<PlayerController>();

        // 무기로 공격
        for (int i = 0; i < 3; i++)
        {
            if (cloningAbilityCharacterController.equipWeapons[i] != null)
            {
                int id = cloningAbilityCharacterController.equipWeapons[i].GetComponent<Weapon>().weaponId;
                holdingWeapon = weapons[id];
                holdingWeapon.SetActive(true);
                gameObject.transform.LookAt(enemy.transform);

                Weapon weapon = holdingWeapon.GetComponent<Weapon>();
                if (weapon.canFire)
                {
                    anim.SetTrigger("Fire");
                    weapon.Use(gameObject);
                }
                break;
            }
        }
    }

    // 분신을 해제할 때(분신이 비활성화 할 때) 실행되는 함수
    void OnDisable()
    {
        if (triggerMachine != null)
            triggerMachine.GetComponent<DocumentCollectObject>().isExists[cloneNum + 2] = false;
        nav.enabled = false; // 사용하지 않으므로 NavMeshAgent 비활성화
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Machine")
        {
            triggerMachine = other.gameObject;
            if (other.GetComponent<DocumentCollectObject>().interactiveCharacterId == 1)
            {
                other.GetComponent<DocumentCollectObject>().isExists[cloneNum + 2] = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Machine")
        {
            triggerMachine = null;
            if (other.GetComponent<DocumentCollectObject>().interactiveCharacterId == 1)
            {
                other.GetComponent<DocumentCollectObject>().isExists[cloneNum + 2] = false;
            }
        }
    }
}
