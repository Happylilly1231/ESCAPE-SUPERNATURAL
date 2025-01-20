using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

//CLASS FOR HANDLING PLAYER INPUTS
public class InputSent
{
    public Vector2 movement;
    public float turn;
    public bool jump;
    public bool walk;
    public bool crouch;
    public bool sprint;
    public bool fire;
    public bool interact;
    // public bool holdWeapon1;
    // public bool holdWeapon2;
    // public bool holdSecondaryWeapon;

    public void Clear()
    {
        movement = Vector2.zero;
        turn = 0f;
        jump = false;
        walk = false;
        crouch = false;
        sprint = false;
        fire = false;
    }
}

public class PlayerController : MonoBehaviour
{
    public Animator anim;
    public CapsuleCollider col; //CHARACTER COLLIDER WITH DEFAULT VALUES (DEFAULT = STAND UP)
    public int mapLayer = 7;
    public float jumpForce = 5f;
    public Transform bottomTransform;
    public GameObject interactionUI; // 상호작용 UI
    public float interactionRange = 2.5f; // 상호작용 가능한 범위(OverlapSphere 반지름)

    // 속도
    public float moveSpeed = 4.4f;
    public float runSpeed = 4.4f; //SPEED WHEN RUNNING
    public float walkSpeed = 2f; //SPEED WHEN WALKING
    public float crouchSpeed = 2f; //SPEED WHEN CROUCHING
    public float sprintSpeed = 7.5f; //SPEED WHEN SPRINTING
    public float turnSpeed = 150f; //SPEED FOR TURNING THE CHARACTERs

    // 애니메이션 레이어
    public int walkLayer = 1;
    public float walkLayerWeight = 0f;
    public float walkTransitionSpeed = 10f;
    public int sprintLayer = 2;
    public float sprintLayerWeight = 0f;
    public float sprintTransitionSpeed = 6f;
    public int crouchLayer = 3;
    public float crouchLayerWeight = 0f;
    public float crouchTransitionSpeed = 10f;

    // 웅크려 앉았을 때 Collider 정보
    public Vector3 crouchColCenter;
    public float crouchColHeight;

    public int characterId; // 캐릭터 아이디(캐릭터 식별 정보 - 0 : 데릭 / 1 : 소피아 / 2 : 에단 / 3 : 분신)

    // 무기
    public GameObject[] weapons; // 무기 배열
    public GameObject[] equipWeapons; // 장착 무기 배열(주무기 : 1, 2 / 보조무기 : 3)


    Rigidbody rigid;
    InputSent inputs;
    float inputX;
    float inputZ;
    bool isGrounded;
    float movementInputSpeed = 6f;
    Animator interactionAnim; // 상호작용하는 물체의 애니메이터
    LayerMask interactiveLayerMask; // 상호작용 가능한 오브젝트 레이어 마스크
    bool interactive; // 상호작용 가능 여부
    // bool interact; // 상호작용 여부
    bool openingDoor;
    float currentSpeed;

    // 기본 Collider 정보
    Vector3 defaultColCenter;
    float defaultColHeight;

    bool isHoldingWeapon;

    ISupernatural supernatural;

    GameObject nearObj;
    int selectWeaponId = -1;
    int curWeaponId = -1;

    bool isPlayingCharacter;

    NavMeshAgent nav;
    public Transform targetCharacter = null;

    int maxHp = 100;
    float curHp;

    public int MaxHp { get => maxHp; set => maxHp = value; }
    public float CurHp { get => curHp; set => curHp = value; }

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        inputs = new InputSent();
        interactiveLayerMask = LayerMask.GetMask("InteractiveObject"); // 상호작용 가능한 오브젝트 레이어 마스크 초기화

        defaultColCenter = col.center;
        defaultColHeight = col.height;

        supernatural = GetComponent<ISupernatural>(); // 초능력 인터페이스

        equipWeapons = new GameObject[3];

        nav = GetComponent<NavMeshAgent>();

        CurHp = MaxHp;
    }

    void Start()
    {
        // 커서를 숨기고 화면 중앙에 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Debug.Log(targetCharacter);

        if (GameManager.instance.selectCharacterId == characterId)
        {
            if (!isPlayingCharacter)
            {
                nav.enabled = false;
                isPlayingCharacter = true;
            }

            // 입력 받기
            GetInputs();

            ChangeColliderSize();

            // 상호작용
            Interaction();
        }
        else
        {
            if (isPlayingCharacter)
            {
                nav.enabled = true;
                isPlayingCharacter = false;
            }

            // 따라갈 캐릭터가 있다면 추적
            if (targetCharacter != null)
            {
                nav.SetDestination(targetCharacter.position);

                // 목표 타겟까지의 거리 계산
                float distanceToTarget = Vector3.Distance(transform.position, targetCharacter.position);

                // 일정 거리 안으로 가까워지면 멈춤
                if (distanceToTarget <= 3f)
                {
                    nav.isStopped = true;
                    anim.SetBool("Moving", false);
                }
                else
                {
                    nav.isStopped = false;
                    anim.SetBool("Moving", true);
                    // 움직이는 방향에 맞는 애니메이션 설정
                    SetAnimMoveVec();
                }
            }
        }

        // 캐릭터 움직임 제어
        ControlCharacter();
    }

    // // 캐릭터 변경했을 때 기본 설정 함수
    // void PlayingCharacterSetting()
    // {
    //     // 현재 선택한 플레이어의 무기로 이미지 변경
    //     for (int i = 0; i < 3; i++)
    //     {
    //         if (equipWeapons[i] == null)
    //             UIManager.instance.ChangeWeaponImg(i, -1);
    //         else
    //             UIManager.instance.ChangeWeaponImg(i, equipWeapons[i].GetComponent<Weapon>().weaponId);
    //     }

    //     UIManager.instance.characterName.text = gameObject.name;
    // }

    // 움직이는 방향에 맞는 애니메이션 설정
    void SetAnimMoveVec()
    {
        Vector3 velocity = nav.velocity; // NavMeshAgent의 속도 벡터 가져오기
        Vector3 localVelocity = transform.InverseTransformDirection(velocity); // 속도 벡터를 로컬 공간으로 변환 (캐릭터의 기준으로 방향 설정)

        // 8방향 이동
        // X와 Y를 블렌드 트리에 전달할 값으로 설정
        float targetInputX = localVelocity.x;
        float targetInputZ = localVelocity.z; // NavMeshAgent는 z축을 앞으로 사용
        inputs.movement = new Vector2(targetInputX, targetInputZ);
        // 부드럽게 변화하도록 X, Z 값 조정
        inputX = Mathf.MoveTowards(inputX, targetInputX, movementInputSpeed * Time.deltaTime);
        inputZ = Mathf.MoveTowards(inputZ, targetInputZ, movementInputSpeed * Time.deltaTime);

        // 애니메이터에 파라미터 전달
        anim.SetFloat("InputX", inputX);
        anim.SetFloat("InputY", inputZ);
    }

    void GetInputs()
    {
        // 매 프레임마다 inputs 전부 false로 클리어
        inputs.Clear();

        // 8방향 이동
        float targetInputX = Input.GetAxisRaw("Horizontal");
        float targetInputZ = Input.GetAxisRaw("Vertical");
        inputs.movement = new Vector2(targetInputX, targetInputZ);
        // 부드럽게 변화하도록 X, Z 값 조정
        inputX = Mathf.MoveTowards(inputX, targetInputX, movementInputSpeed * Time.deltaTime);
        inputZ = Mathf.MoveTowards(inputZ, targetInputZ, movementInputSpeed * Time.deltaTime);

        anim.SetBool("Moving", inputX != 0 || inputZ != 0);

        anim.SetFloat("InputX", inputX);
        anim.SetFloat("InputY", inputZ);

        // 회전
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            inputs.turn = Input.GetAxisRaw("Mouse X");
        }

        inputs.walk = Input.GetKey(KeyCode.LeftControl); // 걷기
        inputs.sprint = Input.GetKey(KeyCode.LeftShift); // 빠르게 달리기
        inputs.jump = Input.GetKeyDown(KeyCode.Space); // 점프
        inputs.crouch = Input.GetKey(KeyCode.C); // 웅크려 앉기
        inputs.interact = Input.GetKeyDown(KeyCode.F); // 상호작용
        inputs.fire = Input.GetMouseButtonDown(0); // 공격

        if (Input.GetKeyDown(KeyCode.Alpha1)) // 주무기 1번
        {
            if (selectWeaponId == 0) selectWeaponId = -1;
            else selectWeaponId = 0;
            SelectWeapon();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // 주무기 2번
        {
            if (selectWeaponId == 1) selectWeaponId = -1;
            else selectWeaponId = 1;
            SelectWeapon();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) // 주무기 3번
        {
            if (selectWeaponId == 2) selectWeaponId = -1;
            else selectWeaponId = 2;
            SelectWeapon();
        }

        // 인벤토리 현재 클릭된 오브젝트 버리기
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (curWeaponId != -1)
            {
                equipWeapons[curWeaponId].SetActive(false);
                int id = equipWeapons[curWeaponId].GetComponent<Weapon>().weaponId;
                Instantiate(GameManager.instance.weaponItems[id], transform.position + transform.forward * 2f, Quaternion.identity);
                equipWeapons[curWeaponId] = null;
                UIManager.instance.ChangeWeaponImg(curWeaponId, -1);
                curWeaponId = -1;
            }
        }

        // 초능력
        if (Input.GetKeyDown(KeyCode.R))
        {
            supernatural.Activate();
        }

        // 첫번째 캐릭터 따라오게 하기
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            int id = 0;
            if (characterId == 0) id = 1;
            GameManager.instance.SetFollowPlayingCharacter(id);
        }

        // 두번째 캐릭터 따라오게 하기
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            int id = 2;
            if (characterId == 2) id = 1;
            GameManager.instance.SetFollowPlayingCharacter(id);
        }

        // // 상호작용
        // if (Input.GetKeyDown(KeyCode.F))
        // {
        //     if (interactive) // 상호작용 가능하면(=상호작용 범위 내에 가능한 물체가 있으면)
        //     {
        //         interact = true; // 상호작용하기
        //     }
        // }

        // // 인벤토리 1번 꺼내기/숨기기
        // if (Input.GetKeyDown(KeyCode.Alpha1))
        // {
        //     // 인벤토리 확인 코드 추가

        //     if (!isHoldingGun)
        //     {
        //         isHoldingGun = true;
        //         handgun.SetActive(true);
        //         handgun.transform.Rotate(new Vector3(-67f, 79f, -97f));
        //     }
        //     else
        //     {
        //         isHoldingGun = false;
        //         handgun.SetActive(false);
        //         handgun.transform.Rotate(Vector3.zero);
        //     }

        //     Debug.Log("isHoldingGun : " + isHoldingGun);
        // }
    }

    void ControlCharacter()
    {
        // 기본 속도로 초기화
        float currentSpeed = runSpeed;

        // 회전 애니메이션 적용
        anim.SetFloat("Turn", inputs.turn);

        // 걷기 애니메이션 적용
        if (inputs.walk)
        {
            if (anim.GetBool("Grounded"))
            {
                walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 1f, Time.deltaTime * walkTransitionSpeed);
                currentSpeed = walkSpeed;
            }
        }
        else
        {
            walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 0f, Time.deltaTime * walkTransitionSpeed);
        }
        anim.SetLayerWeight(walkLayer, walkLayerWeight);

        // 달리기 애니메이션 적용
        if (inputs.sprint)
        {
            if (anim.GetBool("Grounded") && anim.GetBool("Moving") && !inputs.crouch)
            {
                sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 1f, Time.deltaTime * sprintTransitionSpeed);

                //SPRINT ONLY WORKS FOR FORWARD, FORWARD-LEFT AND FORWARD-RIGHT DIRECTION
                if (anim.GetFloat("InputY") > 0)
                {
                    currentSpeed = sprintSpeed;
                }
            }
        }
        else
        {
            sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 0f, Time.deltaTime * sprintTransitionSpeed);
        }
        anim.SetLayerWeight(sprintLayer, sprintLayerWeight);

        // 웅크려 앉기 애니메이션 적용
        if (inputs.crouch)
        {
            if (anim.GetBool("Grounded"))
            {
                crouchLayerWeight = Mathf.MoveTowards(crouchLayerWeight, 1f, Time.deltaTime * crouchTransitionSpeed);
                currentSpeed = crouchSpeed;
            }
        }
        else
        {
            if (!IsCeilingAbove())
            {
                crouchLayerWeight = Mathf.MoveTowards(crouchLayerWeight, 0f, Time.deltaTime * crouchTransitionSpeed);
            }
            else
            {
                currentSpeed = crouchSpeed;
            }
        }
        anim.SetLayerWeight(crouchLayer, crouchLayerWeight);

        // 속도 적용
        if (anim.GetBool("Grounded"))
        {
            moveSpeed = currentSpeed;
        }

        // 점프 애니메이션 적용
        if (inputs.jump && isGrounded)
        {
            Debug.Log("점프!");
            anim.SetBool("Jump", true);
        }

        // 총 쏘기
        Debug.Log(inputs.fire + " , " + isHoldingWeapon);
        if (inputs.fire && isHoldingWeapon)
        {
            Debug.Log("사격!");
            anim.SetTrigger("Fire");
            equipWeapons[selectWeaponId].GetComponent<Weapon>().Use();
        }
    }

    void FixedUpdate()
    {
        // 땅에 닿아있는지 검사
        CheckGround();

        if (isPlayingCharacter)
        {
            // 8방향 이동
            Vector3 inputDir = new Vector3(inputX, 0, inputZ).normalized;
            Vector3 moveDir = transform.TransformDirection(inputDir); // 로컬 방향으로 변환
            rigid.MovePosition(rigid.position + moveDir * moveSpeed * Time.fixedDeltaTime);

            // 회전
            Vector3 rotation = new Vector3(0, inputs.turn, 0) * turnSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(rotation);
            rigid.MoveRotation(rigid.rotation * deltaRotation);

            // 점프
            if (anim.GetBool("Jump"))
            {
                rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 점프
                CheckGround(); // 땅에서 떨어졌으므로 체크
                anim.SetBool("Jump", false); // 점프 변수 false로 다시 초기화
            }

            // 상호작용 가능한 물체가 범위 내에 있는지 체크
            // CheckCanInteraction();
        }
    }

    // 땅에 닿아있는지 검사
    void CheckGround()
    {
        Debug.DrawRay(bottomTransform.position + Vector3.up * 0.1f, Vector3.down * 0.2f, Color.red);

        if (Physics.Raycast(bottomTransform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.2f))
        {
            anim.SetBool("Grounded", true);
            isGrounded = true;
        }
        else
        {
            anim.SetBool("Grounded", false);
            isGrounded = false;
        }
    }

    // 상호작용
    void Interaction()
    {
        if (inputs.interact && nearObj != null)
        {
            if (nearObj.tag == "Weapon") // 무기
            {
                Equip(nearObj); // 무기 장착
            }
        }
    }

    // 무기 장착
    void Equip(GameObject weaponObj)
    {
        Item item = weaponObj.GetComponent<Item>();
        int weaponId = item.id; // 무기 종류 식별

        int equipId = -1;
        if (weaponId >= 2) // 주 무기
        {
            if (equipWeapons[0] == null) equipId = 0;
            else if (equipWeapons[1] == null) equipId = 1;
        }
        else // 보조 무기
        {
            if (equipWeapons[2] == null) equipId = 2;
        }

        if (equipId != -1) // 무기 장착
        {
            Debug.Log("무기 장착 완료!");
            equipWeapons[equipId] = weapons[weaponId];
            UIManager.instance.ChangeWeaponImg(equipId, weaponId);
            Destroy(weaponObj); // 입수한 무기 아이템 파괴
        }
        else // 무기 장착 실패
        {
            Debug.Log("무기를 더 장착할 수 없습니다!");
        }
    }

    // 무기 선택
    void SelectWeapon()
    {
        if (selectWeaponId != -1) // 무기 선택한 경우
        {
            if (curWeaponId != -1) // 현재 선택한 무기가 있는 경우
            {
                equipWeapons[curWeaponId].transform.localRotation = Quaternion.Euler(equipWeapons[curWeaponId].GetComponent<Weapon>().originalRotation); // 총 각도 원래대로 변경(들고 있는 각도)
                equipWeapons[curWeaponId].SetActive(false); // 그 무기 선택 해제(비활성화)
                curWeaponId = -1; // 현재 선택된 무기 없음
                isHoldingWeapon = false; // 현재 무기 들고 있지 않음
            }

            if (equipWeapons[selectWeaponId] != null && curWeaponId != selectWeaponId) // 선택하려는 무기를 가지고 있고, 선택이 바뀔 때만
            {
                equipWeapons[selectWeaponId].SetActive(true); // 그 무기 선택(활성화)
                curWeaponId = selectWeaponId; // 현재 선택된 무기 변경

                isHoldingWeapon = true; // 현재 총 들고 있음
                equipWeapons[curWeaponId].transform.localRotation = Quaternion.Euler(equipWeapons[curWeaponId].GetComponent<Weapon>().fireRotation); // 총 각도 변경(발사 각도)
            }
        }
        else // 무기 선택 해제 경우
        {
            if (curWeaponId != -1) // 현재 선택한 무기가 있는 경우
            {
                equipWeapons[curWeaponId].transform.localRotation = Quaternion.Euler(equipWeapons[curWeaponId].GetComponent<Weapon>().originalRotation); // 총 각도 원래대로 변경(들고 있는 각도)
                equipWeapons[curWeaponId].SetActive(false); // 그 무기 선택 해제(비활성화)
                curWeaponId = -1; // 현재 선택된 무기 없음
                isHoldingWeapon = false; // 현재 무기 들고 있지 않음
            }
        }
    }

    // // 상호작용 가능한 물체가 범위 내에 있는지 체크
    // private void CheckCanInteraction()
    // {
    //     Debug.DrawRay(transform.position + col.center + Vector3.up * 0.5f, transform.forward * interactionRange, Color.yellow);

    //     if (Physics.Raycast(transform.position + col.center + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, interactionRange, interactiveLayerMask)) // 상호작용 가능한 물체가 범위 내에 있으면
    //     {
    //         interactive = true; // 상호작용 가능
    //         if (GameManager.instance.selectCharacterId == characterId) // 현재 선택된 캐릭터가 상호작용 가능하다면(나머지 캐릭터에 의해 상호작용 UI가 제어되면 안됨)
    //         {
    //             UIManager.instance.ShowInteractionUI(); // 상호작용 UI 활성화
    //         }

    //         Debug.Log(gameObject.name);

    //         if (interact) // 상호작용하는 상태면
    //         {
    //             // *현재는 문 열고 닫는 상호작용만 구현

    //             if (!openingDoor) // 문 열고 있는 중이 아닐 때
    //             {
    //                 GameObject hitObj = hit.collider.gameObject;
    //                 interactionAnim = hitObj.transform.parent.gameObject.GetComponent<Animator>();
    //                 interactionAnim.SetBool("open", true);
    //                 openingDoor = true;
    //                 StartCoroutine(CloseDoorCoroutine()); // 문을 연 후 2초 뒤에 문을 닫는 코루틴 실행
    //             }

    //             interact = false; // 상호작용 끝
    //         }
    //     }
    //     else // 상호작용 가능한 물체가 범위 내에 없으면
    //     {
    //         interactive = false; // 상호작용 불가능
    //         if (GameManager.instance.selectCharacterId == characterId) // 현재 선택된 캐릭터가 상호작용 가능하다면(나머지 캐릭터에 의해 상호작용 UI가 제어되면 안됨)
    //         {
    //             UIManager.instance.HideInteractionUI(); // 상호작용 UI 비활성화
    //         }
    //     }
    // }

    // 문을 연 후 2초 뒤에 문을 닫는 코루틴
    private IEnumerator CloseDoorCoroutine()
    {
        yield return new WaitForSeconds(2f); // 2초 대기
        interactionAnim.SetBool("open", false);  // 문 닫기
        openingDoor = false;
    }

    // 웅크려 앉았던 상태에서 천장에 닿지 않고 일어날 수 있는지 여부 체크
    bool IsCeilingAbove()
    {
        bool obstacleDetected = false;
        if (Physics.Raycast(transform.position + defaultColCenter + Vector3.down * 0.1f, Vector3.up, out RaycastHit hit, 0.2f))
        {
            if (IsMap(hit.collider))
            {
                obstacleDetected = true;
            }
        }

        return obstacleDetected;
    }

    // 해당 콜라이더가 맵 오브젝트인지 확인하는 함수
    bool IsMap(Collider collider)
    {
        if (collider.transform.parent != null)
        {
            return collider.transform.parent.gameObject.layer == mapLayer;
        }
        return false;
    }

    // 앉거나 일어날 때 콜라이더 사이즈 변경하는 함수
    void ChangeColliderSize()
    {
        if (inputs.crouch) // 앉을 때
        {
            col.center = crouchColCenter;
            col.height = crouchColHeight;
        }
        else // 일어날 때
        {
            col.center = defaultColCenter;
            col.height = defaultColHeight;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon")
        {
            nearObj = other.gameObject;
        }

        Debug.Log(nearObj);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon")
        {
            nearObj = null;
        }
    }

    public void Damage(int amount)
    {
        if (CurHp - amount < 0)
        {
            CurHp = 0;
            UIManager.instance.SetHpBar(0);
            Debug.Log(gameObject.name + "이(가) 죽었습니다.");
            Destroy(gameObject);
        }
        else
        {
            CurHp -= amount;
            UIManager.instance.SetHpBar(CurHp / MaxHp);
        }
    }
}
