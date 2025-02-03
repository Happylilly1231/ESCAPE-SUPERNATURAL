using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

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
    public bool aim;
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
        aim = false;
    }
}

public class PlayerController : MonoBehaviour
{
    public Animator anim;
    public CapsuleCollider col; //CHARACTER COLLIDER WITH DEFAULT VALUES (DEFAULT = STAND UP)
    public CapsuleCollider zeroFrictionCol;
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
    public GameObject[] equipWeapons; // 장착 무기 배열(주무기 : 0, 1 / 보조무기 : 2) ###저장 필요###

    // public float supernaturalCoolDown; // 초능력 쿨타임 (초 단위)

    public GameObject characterCamera; // 캐릭터 카메라

    // 체력바
    public Canvas canvas;
    public Slider hpBar;
    public TextMeshProUGUI hpTxt;


    // bool isSupernaturalReady = true; // 초능력 사용 가능 여부

    Rigidbody rigid;
    InputSent inputs;
    float inputX;
    float inputZ;
    public bool isGrounded;
    float movementInputSpeed = 6f;

    // 기본 Collider 정보
    Vector3 defaultColCenter;
    float defaultColHeight;

    bool isHoldingWeapon;

    ISupernatural supernatural;

    GameObject nearObj;
    int selectWeaponId = -1;
    int curWeaponId = -1;

    bool isPlayingCharacter;

    public NavMeshAgent nav;
    public Transform targetCharacter = null;

    int maxHp = 100;
    float curHp;

    public int MaxHp { get => maxHp; set => maxHp = value; }
    public float CurHp { get => curHp; set => curHp = value; }

    bool isAiming;

    // float cooldownRemainTime;

    bool canUIUpdate;

    bool haveCorrectKeyCard;
    public bool[] havingKeyCardLevel; // 카드키 레밸에 따른 소유 여부 ###저장 필요###

    public bool isWalking;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        inputs = new InputSent();

        defaultColCenter = col.center;
        defaultColHeight = col.height;

        supernatural = GetComponent<ISupernatural>(); // 초능력 인터페이스

        equipWeapons = new GameObject[3];

        nav = GetComponent<NavMeshAgent>();

        CurHp = MaxHp;

        havingKeyCardLevel = new bool[4];

        // 커서를 숨기고 화면 중앙에 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameManager.instance.Characters[characterId] = gameObject; // 게임 매니저의 캐릭터 배열에 자신 할당
        GameManager.instance.CharacterCameras[characterId] = characterCamera; // 게임 매니저의 캐릭터 카메라 배열에 자신 할당
        if (characterId == GameManager.instance.selectCharacterId)
        {
            characterCamera.SetActive(true);
            GameManager.instance.mainCamera = characterCamera.GetComponent<Camera>();
        }
        else
        {
            characterCamera.SetActive(false);
        }
    }

    void Start()
    {
        // 이전 스테이지에서 들고 온 장착 무기가 있으면 장착
        for (int i = 0; i < 3; i++)
        {
            if (DataManager.instance.data.characterEquipWeapons.Count > 0)
            {
                equipWeapons[i] = DataManager.instance.data.characterEquipWeapons[characterId][i];
                if (equipWeapons[i] != null)
                {
                    equipWeapons[i].GetComponent<Weapon>().curBulletCnt = DataManager.instance.data.characterEquipWeaponCurBulletCnts[characterId][i];
                }
            }
        }
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
                supernatural.CanUIUpdate = true;
                canUIUpdate = true;
                canvas.gameObject.SetActive(false);
            }

            if (!GameManager.instance.isAllowOnlyUIInput)
            {
                // 입력 받기
                GetInputs();
            }

            ChangeColliderSize();

            // 상호작용
            Interaction();
        }
        else
        {
            if (isPlayingCharacter)
            {
                // nav.isStopped = false;
                // inputX = 0;
                // inputZ = 0;

                for (int i = 0; i < 3; i++)
                {
                    if (i != characterId)
                    {
                        Debug.Log("***추적 모드 해제" + i);
                        PlayerController playerController = GameManager.instance.Characters[i].GetComponent<PlayerController>();
                        playerController.targetCharacter = null;
                        UIManager.instance.isFollowImgs[i].SetActive(false);
                    }
                }

                nav.enabled = true;
                isPlayingCharacter = false;
                supernatural.Deactivate();
                canUIUpdate = false;
                canvas.gameObject.SetActive(true);
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
            else
            {
                nav.isStopped = true;
                anim.SetBool("Moving", false);
                inputX = 0;
                inputZ = 0;
            }

            // 체력바 따라다니고 플레이어 화면 바라보게 하기
            canvas.transform.LookAt(canvas.transform.position + GameManager.instance.mainCamera.transform.rotation * Vector3.forward, GameManager.instance.mainCamera.transform.rotation * Vector3.up);
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
        isWalking = inputs.walk;
        inputs.interact = Input.GetKeyDown(KeyCode.F); // 상호작용
        inputs.fire = Input.GetMouseButtonDown(0); // 공격
        inputs.aim = Input.GetMouseButtonDown(1); // 조준

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
                Weapon weapon = equipWeapons[curWeaponId].GetComponent<Weapon>();
                int id = weapon.weaponId;
                GameObject weaponItem = GameManager.instance.weaponItems[id];
                GameObject throwWeaponItem = Instantiate(weaponItem, transform.position + transform.forward * 2f, Quaternion.identity);
                throwWeaponItem.GetComponent<Item>().curBulletCnt = weapon.curBulletCnt;
                equipWeapons[curWeaponId] = null;
                UIManager.instance.ChangeWeaponImg(curWeaponId, -1);
                curWeaponId = -1;
                selectWeaponId = -1;
                isHoldingWeapon = false;
            }
        }

        // 초능력
        if (Input.GetKeyDown(KeyCode.R) && supernatural.IsSupernaturalReady)
        {
            supernatural.Activate();
            // StartCoroutine(UpdateSupernaturalCooldown()); // 쿨타임 업데이트 시작
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

    // // 초능력 쿨타임 업데이트 함수
    // IEnumerator UpdateSupernaturalCooldown()
    // {
    //     isSupernaturalReady = false;

    //     cooldownRemainTime = supernaturalCoolDown;
    //     while (cooldownRemainTime > 0)
    //     {
    //         cooldownRemainTime -= Time.deltaTime;

    //         if (canUIUpdate)
    //         {
    //             UIManager.instance.cooldownDisableImg.fillAmount = cooldownRemainTime / supernaturalCoolDown;
    //             UIManager.instance.cooldownRemainTimeText.text = cooldownRemainTime.ToString("F1") + "s";
    //         }
    //         yield return null;
    //     }
    //     if (canUIUpdate)
    //         UIManager.instance.cooldownRemainTimeText.text = "";

    //     isSupernaturalReady = true;
    // }

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

        // 점프
        if (inputs.jump && isGrounded && rigid.useGravity)
        {
            Debug.Log("점프!");
            anim.SetTrigger("Jump");
            rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 점프
            CheckGround(); // 땅에서 떨어졌으므로 체크
                           // anim.SetBool("Jump", false); // 점프 변수 false로 다시 초기화
        }

        // 총을 들고 있을 때
        if (isHoldingWeapon)
        {
            Weapon weapon = equipWeapons[selectWeaponId].GetComponent<Weapon>();

            if (!UIManager.instance.bulletCntUI.activeSelf && canUIUpdate)
                UIManager.instance.bulletCntUI.SetActive(true);

            // 총알 개수 UI에 표시 업데이트
            if (canUIUpdate)
            {
                UIManager.instance.bulletCntTxt.text = weapon.curBulletCnt.ToString() + " / " + weapon.maxBulletCnt.ToString();
            }

            // 총 각도 변경
            bool isPlayingFireAnimation = anim.GetCurrentAnimatorStateInfo(0).IsName("Fire");
            bool isDoingFireTranstion = anim.GetAnimatorTransitionInfo(0).IsName("Fire -> Idle") || anim.GetAnimatorTransitionInfo(0).IsName("Idle -> Fire");
            if (!isDoingFireTranstion)
            {
                if (isPlayingFireAnimation)
                {
                    weapon.canFireBullet = true;
                    equipWeapons[selectWeaponId].transform.localRotation = Quaternion.Euler(weapon.fireRotation); // 총 각도 변경(발사 각도)
                }
                else
                {
                    weapon.canFireBullet = false;
                    equipWeapons[selectWeaponId].transform.localRotation = Quaternion.Euler(weapon.originalRotation);
                }
            }
            else
            {
                weapon.canFireBullet = false;
                equipWeapons[selectWeaponId].transform.localRotation = Quaternion.Euler(weapon.originalRotation);
            }

            // 총 쏘기
            if (inputs.fire)
            {
                Debug.Log("사격!");
                if (weapon.canFire)
                {
                    anim.SetTrigger("Fire");
                    weapon.Use(gameObject);
                }
            }

            // 조준하기
            if (inputs.aim)
            {
                if (!isAiming)
                {
                    isAiming = true;
                    Debug.Log("조준");
                    StartCoroutine(Aim());
                    // anim.SetTrigger("Aim");
                    // GameManager.instance.mainCamera.transform.position += transform.forward * 1f;
                }
                else
                {
                    isAiming = false;
                    Debug.Log("조준 해제");
                    // GameManager.instance.mainCamera.transform.position -= transform.forward * 1f;
                }
            }
        }
        else
        {
            if (UIManager.instance.bulletCntUI.activeSelf && canUIUpdate)
                UIManager.instance.bulletCntUI.SetActive(false);
        }
    }

    // 조준 코루틴
    IEnumerator Aim()
    {
        while (isAiming)
        {
            // 카메라 중앙 방향 계산
            Ray ray = GameManager.instance.mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, GameManager.instance.enemyLayerMask))
            {
                if (hit.collider.gameObject.tag == "Enemy")
                {
                    UIManager.instance.crosshair.color = Color.red;
                    Debug.Log(hit.collider.gameObject.name + "을(를) 조준 중입니다.");
                    Debug.Log("적이 총알에 맞았습니다!");
                }
                else
                {
                    UIManager.instance.crosshair.color = Color.cyan;
                }
            }
            else
            {
                UIManager.instance.crosshair.color = Color.cyan;
            }
            yield return null;
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

            // // 점프
            // if (anim.GetBool("Jump"))
            // {
            //     rigid.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // 점프
            //     CheckGround(); // 땅에서 떨어졌으므로 체크
            //     anim.SetBool("Jump", false); // 점프 변수 false로 다시 초기화
            // }

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
            else if (nearObj.tag == "CardReader")
            {
                CardReader cardReader = nearObj.GetComponent<CardReader>();

                // 카드키 갖고 있는지 체크
                CheckHaveCorrectKeyCard(cardReader);

                if (haveCorrectKeyCard)
                {
                    haveCorrectKeyCard = false;
                    cardReader.OpenDoor(); // 해당 문 열기
                }
            }
            else if (nearObj.tag == "Researcher")
            {
                ResearcherController researcherController = nearObj.GetComponent<ResearcherController>();
                if (researcherController.havingKeyCard)
                {
                    researcherController.StealKeyCard(gameObject);
                    havingKeyCardLevel[researcherController.keyCardLevel] = true;

                    if (canUIUpdate)
                    {
                        UIManager.instance.ShowGuide(researcherController.keyCardLevel.ToString() + "급 카드키를 획득했습니다.");
                        UIManager.instance.keyCardLevelImgs[researcherController.keyCardLevel - 1].SetActive(true);
                    }

                    if (researcherController.keyCardLevel == 1)
                    {
                        QuestManager.instance.QuestClear(1, 0); // 스테이지 1 첫번째 퀘스트 완료
                    }
                }
            }
            else if (nearObj.tag == "Machine")
            {
                DocumentCollectObject documentCollectObject = nearObj.GetComponent<DocumentCollectObject>();
                if (documentCollectObject.interactiveCharacterId == characterId || documentCollectObject.interactiveCharacterId == -1)
                {
                    documentCollectObject.Activate();
                }
            }
        }
    }

    // 카드키 갖고 있는지 체크 함수
    void CheckHaveCorrectKeyCard(CardReader cardReader)
    {
        Debug.Log("카드키 갖고 있는지 체크");
        for (int i = 0; i < 3; i++)
        {
            if (havingKeyCardLevel[i] && i <= cardReader.keyCardLevel)
            {
                haveCorrectKeyCard = true;
                break;
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
            Weapon weapon = equipWeapons[equipId].GetComponent<Weapon>();
            weapon.curBulletCnt = item.curBulletCnt;
            UIManager.instance.ChangeWeaponImg(equipId, weaponId);
            UIManager.instance.bulletCntTxt.text = item.curBulletCnt.ToString() + " / " + weapon.maxBulletCnt.ToString();
            Destroy(weaponObj); // 입수한 무기 아이템 파괴
        }
        else // 무기 장착 실패
        {
            Debug.Log("무기를 더 장착할 수 없습니다!");
            UIManager.instance.ShowGuide("무기를 더 장착할 수 없습니다!");
        }
    }

    // 무기 선택
    void SelectWeapon()
    {
        if (selectWeaponId != -1) // 무기 선택한 경우
        {
            if (curWeaponId != -1) // 현재 선택한 무기가 있는 경우
            {
                equipWeapons[curWeaponId].SetActive(false); // 그 무기 선택 해제(비활성화)
                curWeaponId = -1; // 현재 선택된 무기 없음
                isHoldingWeapon = false; // 현재 무기 들고 있지 않음
            }

            if (equipWeapons[selectWeaponId] != null && curWeaponId != selectWeaponId) // 선택하려는 무기를 가지고 있고, 선택이 바뀔 때만
            {
                // UIManager.instance.bulletCntUI.SetActive(true);

                equipWeapons[selectWeaponId].SetActive(true); // 그 무기 선택(활성화)
                curWeaponId = selectWeaponId; // 현재 선택된 무기 변경
                isHoldingWeapon = true; // 현재 총 들고 있음
            }
        }
        else // 무기 선택 해제 경우
        {
            if (curWeaponId != -1) // 현재 선택한 무기가 있는 경우
            {
                // UIManager.instance.bulletCntUI.SetActive(false);
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

    // // 문을 연 후 2초 뒤에 문을 닫는 코루틴
    // private IEnumerator CloseDoorCoroutine()
    // {
    //     yield return new WaitForSeconds(2f); // 2초 대기
    //     interactionAnim.SetBool("open", false);  // 문 닫기
    //     openingDoor = false;
    // }

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
            return collider.transform.parent.gameObject.layer == GameManager.instance.mapLayerMask;
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

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "NextStagePoint") // 다음 층 포인트를 밟으면
        {
            // 스테이지 클리어
            Debug.Log(gameObject.name + "이(가) 스테이지를 클리어했습니다!");
            GameManager.instance.CharacterClear(characterId);

            gameObject.SetActive(false);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon" || other.tag == "CardReader" || other.tag == "Researcher" || other.tag == "Machine")
        {
            nearObj = other.gameObject;
            if (other.tag == "Machine")
            {
                DocumentCollectObject documentCollectObject = other.gameObject.GetComponent<DocumentCollectObject>();
                documentCollectObject.Exist(characterId, canUIUpdate);
            }
        }

        Debug.Log(nearObj);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon" || other.tag == "CardReader" || other.tag == "Researcher" || other.tag == "Machine")
        {
            nearObj = null;
            if (other.tag == "Machine")
            {
                other.gameObject.GetComponent<DocumentCollectObject>().isExists[characterId] = false;
            }
        }
    }

    public void Damage(int amount)
    {
        if (CurHp - amount <= 0)
        {
            CurHp = 0;
            UIManager.instance.SetHpBar(curHp, maxHp);
            hpBar.value = 0;
            hpTxt.text = "0 / " + maxHp.ToString();
            Debug.Log(gameObject.name + "이(가) 죽었습니다.");
            // Destroy(gameObject);
            GameManager.instance.GameOver(); // 죽었으니 게임 오버
        }
        else
        {
            CurHp -= amount;
            UIManager.instance.SetHpBar(curHp, maxHp);
            hpBar.value = curHp / maxHp;
            hpTxt.text = curHp.ToString() + " / " + maxHp.ToString();
        }
    }
}
