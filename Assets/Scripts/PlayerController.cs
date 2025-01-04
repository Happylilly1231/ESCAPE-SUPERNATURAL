using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//CLASS FOR HANDLING PLAYER INPUTS
public class InputSent
{
    public Vector2 movement;
    public float turn;
    public bool jump;
    public bool walk;
    // public bool runSlide;
    // public bool roll;
    public bool crouch;
    public bool sprint;

    public void Clear()
    {
        movement = Vector2.zero;
        turn = 0f;
        jump = false;
        walk = false;
        // runSlide = false;
        // roll = false;
        crouch = false;
        sprint = false;
    }
}

public class PlayerController : MonoBehaviour
{
    public Animator anim;
    public CapsuleCollider col; //CHARACTER COLLIDER WITH DEFAULT VALUES (DEFAULT = STAND UP)
    public int mapLayer = 7;
    public float jumpForce = 5f;
    public Transform bottomTransform;
    public GameObject controlsWindow;
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


    Rigidbody rigid;
    InputSent inputs;
    float inputX;
    float inputZ;
    bool isGrounded;
    float movementInputSpeed = 6f;
    bool isJumping;
    Animator interactionAnim; // 상호작용하는 물체의 애니메이터
    LayerMask interactiveLayerMask; // 상호작용 가능한 오브젝트 레이어 마스크
    bool interactive; // 상호작용 가능 여부
    bool interact; // 상호작용 여부
    bool openingDoor;
    float currentSpeed;

    // 기본 Collider 정보
    Vector3 defaultColCenter;
    float defaultColHeight;


    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        inputs = new InputSent();
        interactiveLayerMask = LayerMask.GetMask("InteractiveObject"); // 상호작용 가능한 오브젝트 레이어 마스크 초기화

        defaultColCenter = col.center;
        defaultColHeight = col.height;
    }

    void Start()
    {
        // 커서를 숨기고 화면 중앙에 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 입력 받기
        GetInputs();

        ChangeColliderSize();

        // 캐릭터 움직임 제어
        ControlCharacter();
    }

    void GetInputs()
    {
        // 매 프레임마다 inputs 전부 false로 클리어
        inputs.Clear();

        // 8방향 이동
        float targetInputX = Input.GetAxisRaw("Horizontal");
        float targetInputZ = Input.GetAxisRaw("Vertical");
        // inputs.movement = new Vector2(targetInputX, targetInputY);
        // 부드럽게 변화하도록 X, Z 값 조정
        inputX = Mathf.MoveTowards(inputX, targetInputX, movementInputSpeed * Time.deltaTime);
        inputZ = Mathf.MoveTowards(inputZ, targetInputZ, movementInputSpeed * Time.deltaTime);

        anim.SetBool("Moving", inputX != 0 || inputZ != 0);

        anim.SetFloat("InputX", inputX);
        anim.SetFloat("InputY", inputZ);

        // 회전
        inputs.turn = 0f;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            inputs.turn = Input.GetAxisRaw("Mouse X");
        }

        // 걷기
        if (Input.GetKey(KeyCode.LeftControl))
        {
            inputs.walk = true;
        }

        // 빠르게 달리기
        if (Input.GetKey(KeyCode.LeftShift))
        {
            inputs.sprint = true;
        }

        // 점프
        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputs.jump = true;
        }


        // 웅크려 앉기
        if (Input.GetKey(KeyCode.C))
        {
            inputs.crouch = true;
        }

        // G키를 누르면 상호작용
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (interactive) // 상호작용 가능하면(=상호작용 범위 내에 가능한 물체가 있으면)
            {
                interact = true; // 상호작용하기
            }
        }

        // 컨트롤 UI 켜기/끄기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            controlsWindow.SetActive(!controlsWindow.activeInHierarchy);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None; // 커서 해제
                Cursor.visible = true; // 커서 보이기
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; // 커서 고정
                Cursor.visible = false; // 커서 숨기기
            }
        }
    }

    void ControlCharacter()
    {
        //BY DEFAULT USE RUN SPEED
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
    }

    void FixedUpdate()
    {
        // 땅에 닿아있는지 검사
        CheckGround();

        // 8방향 이동
        Vector3 inputDir = new Vector3(inputX, 0, inputZ).normalized;
        Vector3 moveDir = transform.TransformDirection(inputDir); // 로컬 방향으로 변환
        rigid.MovePosition(rigid.position + moveDir * moveSpeed * Time.fixedDeltaTime);

        // 회전
        Vector3 rotation = new Vector3(0, inputs.turn, 0) * turnSpeed * Time.deltaTime;
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
        CheckCanInteraction();
    }

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

    // 상호작용 가능한 물체가 범위 내에 있는지 체크
    private void CheckCanInteraction()
    {
        Debug.DrawRay(transform.position + col.center + Vector3.up * 0.5f, transform.forward * interactionRange, Color.yellow);

        if (Physics.Raycast(transform.position + col.center + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, interactionRange, interactiveLayerMask)) // 상호작용 가능한 물체가 범위 내에 있으면
        {
            interactive = true; // 상호작용 가능
            interactionUI.SetActive(true); // 상호작용 UI 활성화

            if (interact) // 상호작용하는 상태면
            {
                // *현재는 문 열고 닫는 상호작용만 구현

                if (!openingDoor) // 문 열고 있는 중이 아닐 때
                {
                    GameObject hitObj = hit.collider.gameObject;
                    interactionAnim = hitObj.transform.parent.gameObject.GetComponent<Animator>();
                    interactionAnim.SetBool("open", true);
                    openingDoor = true;
                    StartCoroutine(CloseDoorCoroutine()); // 문을 연 후 2초 뒤에 문을 닫는 코루틴 실행
                }

                interact = false; // 상호작용 끝
            }
        }
        else // 상호작용 가능한 물체가 범위 내에 없으면
        {
            interactive = false; // 상호작용 불가능
            interactionUI.SetActive(false); // 상호작용 UI 비활성화
        }
    }

    // 문을 연 후 2초 뒤에 문을 닫는 코루틴
    private IEnumerator CloseDoorCoroutine()
    {
        yield return new WaitForSeconds(2f); // 2초 대기
        interactionAnim.SetBool("open", false);  // 문 닫기
        openingDoor = false;
    }

    // 웅크려 앉았던 상태에서 천장에 닿지 않고 일어날 수 있는지 여부 체크
    private bool IsCeilingAbove()
    {
        bool obstacleDetected = false;
        // RaycastHit[] hits = Physics.BoxCastAll(transform.position + defaultColCenter, defaultBoxSize * 0.45f, Vector3.up, transform.rotation, 0.01f);

        // foreach (RaycastHit hit in hits)
        // {
        //     //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
        //     if (!IsMap(hit.collider))
        //     {
        //         continue;
        //     }
        //     obstacleDetected = true;
        // }
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
    private bool IsMap(Collider collider)
    {
        if (collider.transform.parent != null)
        {
            return collider.transform.parent.gameObject.layer == mapLayer;
        }
        return false;
    }

    void ChangeColliderSize() //CHANGE COLLIDER CENTER AND HEIGHT WHEN CROUCH
    {
        if (inputs.crouch)
        {
            col.center = crouchColCenter;
            col.height = crouchColHeight;
        }
        else
        {
            col.center = defaultColCenter;
            col.height = defaultColHeight;
        }
    }
}
