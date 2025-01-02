/*
=============================================================================================
WARNING: This script is intended for demonstration purposes only. 
You may modify it as needed, but note that it was designed to work across multiple projects 
with different configurations. As a result, it is not optimized for performance (for example,
it does not use Layers or Tags for Physics queries in order to avoid conflicts or add
unnecessary Layers/Tags to your project).

To ensure this script works properly, please make sure the following scripts are also imported:
"BasicMotionsCamera.cs", "BasicMotionsAnimatorParameterRemover.cs", and 
"BasicMotionsAnimatorStateChanger.cs".

This is the Main Script of the Basic Motions playable demo scene and it contains the character
controller responsible for moving and rotating the character.

https://www.keviniglesias.com/
support@keviniglesias.com
=============================================================================================
*/

//- THIS IS THE FREE VERSION, SOME ACTIONS LIKE ROLL AND CROUCH ARE DISABLED -//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KevinIglesias
{
    //DEFINITION OF CHARACTER POSSIBLE STATES
    public enum CharacterState
    {
        Idle,
        Moving,
        Jump,
        Fall,
        Slide,
        Roll,
        Crouch,
    }

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

    ///MAIN CLASS//
    public class BasicMotionsCharacterController : MonoBehaviour
    {
        [Header("[CHARACTER STATE]")]
        public CharacterState characterState; //CURRENT STATE OF THE CHARACTER
        public void ChangeState(CharacterState newState) //FUNCTION TO MODIFY CHARACTER STATE
        {
            if (newState == CharacterState.Idle || newState == CharacterState.Moving)
            {
                if (crouchLayerWeight >= 0.5f) //THRESHOLD FOR TRIGGERING CROUCH STATE
                {
                    newState = CharacterState.Crouch;
                }
            }
            if (characterState == CharacterState.Slide) //FORCE CROUCH STATE WHILE SLIDING
            {
                crouchLayerWeight = 1f;
            }
            characterState = newState;
            ChangeColliderSize(newState); //CHANGE COLLIDER CENTER AND SIZE ACCORDING TO CURRENT STATE
        }

        [Header("[ANIMATOR]")]
        //ASSIGN HERE THE ANIMATOR FROM BOTH CHARACTERS
        //TO MAKE CHARACTER SWITCH POSSIBLE IN THE MIDDLE OF AN ANIMATION BOTH ANIMATORS ARE USED AT THE SAME TIME
        public Animator animator;
        private Animator interactionAnim; // 상호작용할 물체의 Animator

        //VARIABLES TO CONTROL ANIMATOR LAYERS
        public int walkLayer = 1;
        public float walkLayerWeight = 0f;
        public float walkTransitionSpeed = 10f;
        public int sprintLayer = 2;
        public float sprintLayerWeight = 0f;
        public float sprintTransitionSpeed = 6f;
        public int crouchLayer = 3;
        public float crouchLayerWeight = 0f;
        public float crouchTransitionSpeed = 10f;
        private bool openingDoor = false; // 문 열고 있는 중인지 여부


        [Header("[MOVEMENT]")]
        public float moveSpeed;               //CURRENT CHARACTER SPEED
        public float runSpeed = 4.4f;         //SPEED WHEN RUNNING
        public float walkSpeed = 2f;          //SPEED WHEN WALKING
        public float crouchSpeed = 2f;        //SPEED WHEN CROUCHING
        public float sprintSpeed = 7.5f;      //SPEED WHEN SPRINTING
        public float turnSpeed = 150f;        //SPEED FOR TURNING THE CHARACTER
        private Vector3 moveDirection = Vector3.zero; //CURRENT CHARACTER MOVEMENT DIRECTION

        private bool jump; //JUMP CHECK
        public float jumpForce = 4f; //VERTICAL VELOCITY APPLIED WHEN JUMPING
        public float verticalVelocity = 0f; //CURRENT VERTICAL VELOCITY

        //COROUTINE WHEN JUMPING (BYPASSES GROUND CHECK AT THE BEGINNING OF A JUMP)
        private IEnumerator jumpCheckGroundAvoider;
        private IEnumerator JumpCheckGroundAvoider()
        {
            jump = true;
            yield return new WaitForFixedUpdate();
            animator.SetBool("Jump", false);
            jump = false;
        }

        //JUMP COYOTE TIME
        private bool canJump = false;
        private IEnumerator canJumpTimer;
        private IEnumerator CanJumpTimer()
        {
            yield return new WaitForSeconds(0.1f);
            canJump = false;
            canJumpTimer = null;
        }

        [Header("[IMPULSES]")] //SLIDE AND ROLL ARE IMPULSES
        //SLIDE
        public float slideDistance = 3f;
        public float slideDuration = 0.5f;
        public AnimationCurve slideCurve;

        //ROLL
        public float rollDistance = 4.5f;
        public float rollDuration = 0.5f;
        public AnimationCurve rollCurve;

        //USE IMPULSE MOVEMENT INSTEAD OF INPUT MOVEMENT
        private bool useImpulseMovement = false;

        //COROUTINES FOR IMPULSE MOVEMENT AND ROTATION (CHARACTER FACES INPUT DIRECTION WHEN SLIDE OR ROLL)
        private IEnumerator impulseMovementCoroutine;
        private IEnumerator rotationCoroutine;

        //IMPULSE MOVEMENT DIRECTION
        private Vector3 impulseMovement = Vector3.zero;

        [Header("[INPUTS]")]
        private InputSent inputs;
        private bool blockControls = false; //USED FOR BLOCKING CONTROLS (FINISH LINE ANIMATION)
        private float movementInputSpeed = 6f; //SPEED FOR CHANGING MOVEMENT INPUTS (ANIMATOR PARAMETER)
        private float inputX = 0; //VARIABLE FOR X INPUT MOVEMENT FLOAT ANIMATOR PARAMATER
        private float inputY = 0; //VARIABLE FOR Y INPUT MOVEMENT FLOAT ANIMATOR PARAMATER
        private bool moving; //CHECK TO DETECT IF THERE IS MOVEMENT INPUT
        private float timeMoving; //AMOUNT OF TIME PLAYER MOVED

        private bool allowInputWhileJumping = false;
        public Vector2 lastMovementInputs = Vector2.zero;
        private bool interactive = false; // 상호작용 가능 여부
        private bool interact = false; // 상호작용 여부

        [Header("[PHYSICS]")]
        public BoxCollider collisionBox; //CHARACTER COLLIDER WITH DEFAULT VALUES (DEFAULT = STAND UP)
        private Vector3 defaultBoxCenter; //DEFAULT COLLIDER CENTER VALUES (LOADED FROM collisionBox AT Awake)
        private Vector3 defaultBoxSize; //DEFAULT COLLIDER SIZE VALUES (LOADED FROM collisionBox AT Awake)
        public Vector3 crouchBoxCenter; //CROUCH COLLIDER CENTER VALUES
        public Vector3 crouchBoxSize; //CROUCH COLLIDER SIZE VALUES
        public bool character_nearby;
        public float interactionRange = 2.5f; // 상호작용 가능한 범위(OverlapSphere 반지름)
        private LayerMask interactiveLayerMask; // 상호작용 가능한 오브젝트 레이어 마스크
        private void ChangeColliderSize(CharacterState newState) //CHANGE COLLIDER CENTER AND SIZE WHEN CROUCH
        {
            switch (newState)
            {
                case CharacterState.Crouch:
                case CharacterState.Roll:
                case CharacterState.Slide:
                    collisionBox.center = crouchBoxCenter;
                    collisionBox.size = crouchBoxSize;
                    break;

                default:
                    collisionBox.center = defaultBoxCenter;
                    collisionBox.size = defaultBoxSize;
                    break;
            }
        }

        //COLLISIONS ROOT (ONLY OBJECTS CHILDREN OF THIS TRANSFORM WILL BE USED FOR COLLISIONS)
        // public Transform collisionsRoot;
        public int mapLayer = 7; // 맵 레이어 번호

        //CUSTOM GRAVITY (NOT USING CURRENT UNITY PROJECT GRAVITY)
        public float gravity = -9.81f;

        //GROUND RAYS COLLISION DETECTION (FROM COLLIDER BOTTOM BASE TO SUPPOSED GROUND LOCATION)
        private float distanceToGround;
        private Vector3[] groundRayOrigin;
        private void LoadGroundRays(BoxCollider boxCollider)
        {
            Vector3 halfExtents = boxCollider.size * 0.5f;

            groundRayOrigin = new Vector3[16];

            //BOTTOM BASE CORNER ORIGIN POINTS
            groundRayOrigin[0] = new Vector3(-halfExtents.x, 0, -halfExtents.z);
            groundRayOrigin[1] = new Vector3(halfExtents.x, 0, -halfExtents.z);
            groundRayOrigin[2] = new Vector3(halfExtents.x, 0, halfExtents.z);
            groundRayOrigin[3] = new Vector3(-halfExtents.x, 0, halfExtents.z);

            //BOTTOM BASE SIDE ORIGIN POINTS
            groundRayOrigin[4] = new Vector3(0, 0, -halfExtents.z);
            groundRayOrigin[5] = new Vector3(halfExtents.x, 0, 0);
            groundRayOrigin[6] = new Vector3(0, 0, halfExtents.z);
            groundRayOrigin[7] = new Vector3(-halfExtents.x, 0, 0);

            //ORIGIN POINTS BETWEEN BASE CORNER AND BASE SIDE ORIGIN POINTS
            groundRayOrigin[8] = (groundRayOrigin[0] + groundRayOrigin[4]) * 0.5f;
            groundRayOrigin[9] = (groundRayOrigin[1] + groundRayOrigin[4]) * 0.5f;
            groundRayOrigin[10] = (groundRayOrigin[1] + groundRayOrigin[5]) * 0.5f;
            groundRayOrigin[11] = (groundRayOrigin[2] + groundRayOrigin[5]) * 0.5f;
            groundRayOrigin[12] = (groundRayOrigin[2] + groundRayOrigin[6]) * 0.5f;
            groundRayOrigin[13] = (groundRayOrigin[3] + groundRayOrigin[6]) * 0.5f;
            groundRayOrigin[14] = (groundRayOrigin[3] + groundRayOrigin[7]) * 0.5f;
            groundRayOrigin[15] = (groundRayOrigin[0] + groundRayOrigin[7]) * 0.5f;
        }

        // [Header("[CHARACTER SWITCH]")]
        // public GameObject characterMeshesRoot; //GAME OBJECT ROOT OF CHARACTER MESH (NOT WHOLE CHARACTER ROOT)
        // public GameObject characterChangeVFX; //PARTICLE EFFECT WHEN SWITCHING CHARACTERS
        // private int currentCharacter = 0; //CURRENT CHARACTER, BY DEFAULT 0

        [Header("[UI]")]
        public GameObject controlsWindow;
        public GameObject interactionUI; // 상호작용 UI

        ///INITIALIZE VARIABLES
        private void Awake()
        {
            //SET FRAME RATE LIMIT TO 60
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            //INITIALIZE INPUTS
            inputs = new InputSent();

            //LOAD DEFAULT COLLIDER SIZE (NEEDED WHEN CHANGING TO CROUCH COLLIDER SIZE)
            defaultBoxSize = collisionBox.size;
            defaultBoxCenter = collisionBox.center;

            //LOAD RAYS FOR GROUND COLLISION DETECTION FROM COLLIDER BASE TO FLOOR
            //(COLLIDER DOES NOT TOUCH GROUND, THIS IS INTENDED)
            LoadGroundRays(collisionBox);
            Vector3 origin = transform.position + (Vector3.up * collisionBox.center.y) + (-Vector3.up * (collisionBox.size.y * 0.5f));
            distanceToGround = (origin.y - transform.position.y) * 1.01f; //1.01f = MARGIN TO MAKE SURE RAYS TOUCH GROUND

            //LOAD DEFAULT MOVE SPEED (CHARACTER BY DEFAULT WILL RUN)
            moveSpeed = runSpeed;

            //RANDOM IDLE ANIMATION
            RandomIdle();

            // //INITIALIZE CHARACTER, HIDE BOTH ENABLE DEFAULT CHARACTER
            // ChangeCharacter(currentCharacter);

            interactiveLayerMask = LayerMask.GetMask("InteractiveObject"); // 상호작용 가능한 오브젝트 레이어 마스크 초기화
        }

        private void Start()
        {
            // 커서를 숨기고 화면 중앙에 고정
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ///CHANGE IDLE ANIMATION RANDOMLY
        private void RandomIdle()
        {
            int randomness = 6; //INCREASE THIS VALUE TO MAKE VARIANT IDLE LESS LIKELY TO APPEAR
            //IF randomValue IS 1 OR 2, THE CHARACTER WILL USE IDLE VARIANT INSTEAD OF DEFAULT IDLE
            int randomValue = Random.Range(0, randomness);
            animator.SetInteger("Idle Variant", randomValue);

            //RECURSIVELY CALL THIS FUNCTION AFTER 1 SECOND
            Invoke("RandomIdle", 1f);
        }

        ///DETECT PLAYER INPUTS AND MOVE CHARACTER
        private void Update()
        {
            //GET INPUTS
            GetInputs();

            //MOVE CHARACTER
            ControlCharacter();
        }

        ///INPUTS ARE READ DIRECTLY FROM KEYBOARD OR MOUSE (TO AVOID CONFLICTS WITH CURRENT PROJECT INPUT CONFIGURATION) 
        private void GetInputs()
        {
            //RESET INPUTS TO READ NEW ONES
            inputs.Clear();

            //AVOID NEW INPUTS IF blockControls IS ENABLED
            if (blockControls)
            {
                animator.SetBool("Moving", false);
                return;
            }

            //MOVEMENT
            float targetInputX = 0f;
            float targetInputY = 0f;

            if (Input.GetKey(KeyCode.D))
            {
                targetInputX = 1f;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                targetInputX = -1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                targetInputY = 1f;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                targetInputY = -1f;
            }

            // G키를 누르면 상호작용
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (interactive) // 상호작용 가능하면(=상호작용 범위 내에 가능한 물체가 있으면)
                {
                    interact = true; // 상호작용하기
                }
            }

            inputs.movement = new Vector2(targetInputX, targetInputY);

            animator.SetBool("Moving", targetInputX != 0 || targetInputY != 0);

            inputX = Mathf.MoveTowards(inputX, targetInputX, movementInputSpeed * Time.deltaTime);
            inputY = Mathf.MoveTowards(inputY, targetInputY, movementInputSpeed * Time.deltaTime);

            animator.SetFloat("InputX", inputX);
            animator.SetFloat("InputY", inputY);

            // //ROTATION
            // float turnInput = 0f;
            // if (Input.GetKey(KeyCode.E))
            // {
            //     turnInput = 1f;
            // }
            // else if (Input.GetKey(KeyCode.Q))
            // {
            //     turnInput = -1f;
            // }
            inputs.turn = 0f;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                inputs.turn = Input.GetAxisRaw("Mouse X");
            }


            //JUMP
            if (Input.GetKeyDown(KeyCode.Space))
            {
                inputs.jump = true;
            }

            //WALK
            if (Input.GetKey(KeyCode.LeftControl))
            {
                inputs.walk = true;
            }

            //SPRINT
            if (Input.GetKey(KeyCode.LeftShift))
            {
                inputs.sprint = true;
            }

            /* DISABLED ACTIONS, ANIMATIONS ONLY AVAILABLE IN THE FULL VERSION
            //RUN SLIDE
            if(Input.GetKeyDown(KeyCode.LeftControl))
            {
                inputs.runSlide = true;
            }
            
            //ROLL
            if(Input.GetKeyDown(KeyCode.LeftShift))
            {
                inputs.roll = true;
            }
            */

            //CROUCH
            if (Input.GetKey(KeyCode.C))
            {
                inputs.crouch = true;
            }

            // //SWITCH CHARACTER
            // if (Input.GetKeyDown(KeyCode.Tab))
            // {
            //     currentCharacter++;
            //     if (currentCharacter > characterMeshesRoot.Length - 1)
            //     {
            //         currentCharacter = 0;
            //     }
            //     ChangeCharacter(currentCharacter);
            //     characterChangeVFX.SetActive(false); //DISABLE PARTICLE EFFECT (JUST IN CASE THE PREVIOUS ONE DID NOT FINISH)
            //     characterChangeVFX.transform.localPosition = collisionBox.center; //SET TO BE AT CENTER OF collisionBox
            //     characterChangeVFX.SetActive(true); //SPAWN PARTICLE EFFECT
            // }

            //SHOW CONTROLS WINDOW
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

        ///MOVE CHARACTER BASED ON INPUTS
        private void ControlCharacter()
        {
            //BY DEFAULT USE RUN SPEED
            float currentSpeed = runSpeed;

            //WALK INPUT PRESSED
            if (inputs.walk)
            {
                if (animator.GetBool("Grounded"))
                {
                    walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 1f, Time.deltaTime * walkTransitionSpeed);
                    currentSpeed = walkSpeed;
                }
            }
            else
            {
                walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, 0f, Time.deltaTime * walkTransitionSpeed);
            }
            animator.SetLayerWeight(walkLayer, walkLayerWeight);

            //CROUCH INPUT PRESSED
            if (inputs.crouch)
            {
                if (animator.GetBool("Grounded"))
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
            animator.SetLayerWeight(crouchLayer, crouchLayerWeight);

            //SPRINT INPUT PRESSED
            if (inputs.sprint)
            {
                //MAKE SURE SLIDING IS POSSIBLE WHILE SPRINTING
                timeMoving = 1f;
                if (animator.GetBool("Grounded") && animator.GetBool("Moving") && characterState != CharacterState.Crouch)
                {
                    sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 1f, Time.deltaTime * sprintTransitionSpeed);

                    //SPRINT ONLY WORKS FOR FORWARD, FORWARD-LEFT AND FORWARD-RIGHT DIRECTION
                    if (animator.GetFloat("InputY") > 0)
                    {
                        currentSpeed = sprintSpeed;
                    }
                }

                //ADDITIONAL COLLISION CHECK TO AVOID TUNNELING WHEN SPRINTING
                CheckCollisions();
            }
            else
            {
                sprintLayerWeight = Mathf.MoveTowards(sprintLayerWeight, 0f, Time.deltaTime * sprintTransitionSpeed);
            }
            animator.SetLayerWeight(sprintLayer, sprintLayerWeight);

            //CHANGE SPEED ONLY WHEN CHARACTER IS GROUNDED
            if (animator.GetBool("Grounded"))
            {
                moveSpeed = currentSpeed;
            }

            // //SLIDE INPUT PRESSED (WHILE SLIDING CHARACTER MOVES USING SLIDE PROPERTIES INSTEAD OF USING moveDirection)
            // if (inputs.runSlide)
            // {
            //     if (characterState == CharacterState.Moving) //MAKE SURE SLIDE IS ONLY AVAILABLE WHILE MOVING
            //     {
            //         if (timeMoving >= 0.33f && !inputs.walk) //MAKE SURE SLIDE IS ONLY AVAILABLE AFTER MOVING WHILE A CERTAIN AMOUNT OF TIME AND NOT WALKING
            //         {
            //             if (animator.GetBool("Moving"))
            //             {
            //                 animator.SetBool("RunSlide", true);
            //                 if (impulseMovementCoroutine == null)
            //                 {
            //                     timeMoving = 0f; //RESET AMOUNT OF TIME MOVING TO AVOID CONSECUTIVE SLIDES
            //                     impulseMovementCoroutine = ImpulseMovementCoroutine(slideDistance, slideDuration, slideCurve, inputs.movement);
            //                     StartCoroutine(impulseMovementCoroutine);
            //                 }
            //             }
            //         }
            //     }
            // }

            // //ROLL INPUT PRESSED (WHILE ROLLING CHARACTER MOVES USING ROLL PROPERTIES INSTEAD OF USING moveDirection)
            // if (inputs.roll)
            // {
            //     if (characterState == CharacterState.Idle || characterState == CharacterState.Moving)
            //     {
            //         animator.SetBool("Roll", true);
            //         if (impulseMovementCoroutine == null)
            //         {
            //             impulseMovementCoroutine = ImpulseMovementCoroutine(rollDistance, rollDuration, rollCurve, inputs.movement);
            //             StartCoroutine(impulseMovementCoroutine);
            //         }
            //     }
            // }

            //JUMP INPUT PRESSED
            if (inputs.jump)
            {
                if (canJump && characterState != CharacterState.Slide && characterState != CharacterState.Roll)
                {
                    //STORE LAST INPUTS TO USE IF allowInputWhileJumping IS FALSE
                    //(WHEN JUMP WHILE RUN OR SPRINT CHARACTER WON'T BE ABLE TO CHANGE MOVEMENT DIRECTION)
                    lastMovementInputs = inputs.movement;

                    //FORCE LOW SPEED WHEN JUMPING IN PLACE AND ALLOWING TO CHANGE MOVEMENT DIRECTION WHILE JUMPING IN THIS CASE
                    if (timeMoving <= 0.025f)
                    {
                        moveSpeed = walkSpeed;
                        allowInputWhileJumping = true;
                    }

                    //AVOID DOUBLE JUMP
                    canJump = false;

                    //AVOID GROUND CHECK TO ALLOW CHARACTER MOVE UP
                    if (jumpCheckGroundAvoider != null)
                    {
                        StopCoroutine(jumpCheckGroundAvoider);
                    }
                    jumpCheckGroundAvoider = JumpCheckGroundAvoider();
                    StartCoroutine(jumpCheckGroundAvoider);

                    //FORCE CHARACTER OUT OF GROUND (BECAUSE WE DISABLED GROUND CHECK IN THIS FRAME)
                    animator.SetBool("Grounded", false);

                    //PLAY JUMP ANIMATION
                    animator.SetBool("Jump", true);

                    //MOVE CHARACTER UP AT ApplyGravity FUNCTION
                    verticalVelocity = jumpForce;
                }
            }

            //BLOCK MOVEMENT INPUTS WHILE SLIDING OR ROLLING
            if (!useImpulseMovement)
            {
                //GET CHARACTER FACING DIRECTION BASED ON MOVEMENT INPUTS
                if (!animator.GetBool("Grounded") && !allowInputWhileJumping)
                {
                    moveDirection = (animator.transform.forward * lastMovementInputs.y + animator.transform.right * lastMovementInputs.x).normalized;

                    //ROTATE CAMERA BUT KEEP CHARACTER LOOKING FORWARD
                    if (characterState != CharacterState.Slide && characterState != CharacterState.Roll)
                    {
                        transform.Rotate(Vector3.up, inputs.turn * turnSpeed * Time.deltaTime);
                        animator.transform.Rotate(Vector3.up, -inputs.turn * turnSpeed * Time.deltaTime);
                    }

                }
                else
                {
                    moveDirection = (transform.forward * inputs.movement.y + transform.right * inputs.movement.x).normalized;

                    //DEFAULT CHARACTER ROTATION (WITH CAMERA)
                    if (characterState != CharacterState.Slide && characterState != CharacterState.Roll)
                    {
                        animator.SetFloat("Turn", inputs.turn);
                        transform.Rotate(Vector3.up, inputs.turn * turnSpeed * Time.deltaTime);
                    }
                }
            }
            //moveDirection = (transform.forward * inputs.movement.y + transform.right * inputs.movement.x).normalized;

            //GET MOVE DIRECTION APPLYING SPEED
            moveDirection = moveDirection * moveSpeed * Time.deltaTime;

            //MOVE CHARACTER FROM INPUTS
            transform.position += moveDirection;

            //INCREASE AMOUNT OF TIME CHARACTER WAS MOVING FOR SLIDING
            if (characterState == CharacterState.Moving)
            {
                if (timeMoving < 10f) //LIMIT TO AVOID INFINITE VALUE GROW
                {
                    timeMoving += Time.deltaTime;
                }
            }

            //RESET AMOUNT OF TIME CHARACTER WAS MOVING WHEN CROUCHING OR NO MOVEMENT INPUTS DETECTED
            if (inputs.movement == Vector2.zero || characterState == CharacterState.Crouch)
            {
                timeMoving = 0f;
            }
        }

        ///PHYSICS///
        ///CHECK COLLISIONS AND PHYSICS (PHYSICS ARE INDEPENDENT FROM CURRENT PROJECT PHYSICS AND DO NOT USE ANY RIGIDBODY)
        private void FixedUpdate()
        {
            //CHECK COLLISION BASED ON CHARACTER BOX COLLIDER
            CheckCollisions();

            // 상호작용 가능한 물체가 범위 내에 있는지 체크하기
            CheckCanInteraction();

            //CHECK GROUND COLLISION BASED ON RAYS FROM CHARACTER BOX COLLIDER BASE
            if (!jump) //AVOID GROUND CHECKS THE INSTANT THE CHARACTER JUMPS
            {
                bool grounded = CheckGround();
                animator.SetBool("Grounded", grounded);
            }

            //APPLY GRAVITY WHEN CHARACTER IS NOT GROUNDED
            if (!animator.GetBool("Grounded"))
            {
                ApplyGravity();
                //lastMovementInputs = Vector2.zero;
            }
            else
            {
                //CALL LAND FUNCTION WHEN CHARACTER IS ON GROUND
                Land();
            }

            //MOVE CHARACTER FROM SLIDE AND ROLL MOVEMENT (IF ACTIVE)
            transform.position += impulseMovement * Time.fixedDeltaTime;
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

        ///CHECK FOR DETECTING IF CHARACTER CAN STAND UP FROM CROUCH OR NOT
        private bool IsCeilingAbove()
        {
            bool obstacleDetected = false;
            RaycastHit[] hits = Physics.BoxCastAll(transform.position + defaultBoxCenter, defaultBoxSize * 0.45f, Vector3.up, transform.rotation, 0.01f);

            foreach (RaycastHit hit in hits)
            {
                //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                if (!IsMap(hit.collider))
                {
                    continue;
                }
                obstacleDetected = true;
            }

            return obstacleDetected;
        }

        ///CHECK COLLISIONS TOUCHING CHARACTER BOX COLLIDER 
        private void CheckCollisions()
        {
            Vector3 penetrationDirection;
            float penetrationDistance;

            Collider[] colliders = Physics.OverlapBox(transform.position + collisionBox.center, collisionBox.size * 0.5f, transform.rotation);

            foreach (Collider collider in colliders)
            {
                //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                if (!IsMap(collider) && collider.gameObject.tag != "Door" && collider.gameObject.tag != "Enemy")
                {
                    continue;
                }

                // 적과 충돌했을 때
                if (collider.gameObject.tag == "Enemy")
                {
                    Debug.Log("적에게 피해를 받고 있습니다!");
                }

                bool insideCollision = Physics.ComputePenetration(collisionBox, collisionBox.transform.position, collisionBox.transform.rotation, collider, collider.transform.position, collider.transform.rotation, out penetrationDirection, out penetrationDistance);

                if (insideCollision)
                {
                    float angleWithDown = Vector3.Angle(penetrationDirection, Vector3.down);
                    if (angleWithDown < 10f) //DETECT IF COLLISION IS CEILING AND NOT A WALL
                    {
                        if (!animator.GetBool("Grounded"))
                        {
                            moveSpeed = walkSpeed; //REDUCE SPEED TO AVOID "FLYING EFFECT" UNDER CEILING
                        }

                        if (verticalVelocity > 0)
                        {
                            //RESET JUMP TO SIMULATE HIT WITH CEILING
                            verticalVelocity = 0f;
                            jump = false;
                        }
                    }

                    //MOVE CHARACTER OUTSIDE THE DETECTED COLLIDER WALL
                    transform.Translate(penetrationDirection * penetrationDistance, Space.World);
                }
            }
        }

        ///CHECK COLLISIONS MADE BY RAYS FROM BASE COLLIDER TO SUPPOSED GROUND LOCATION (COLLIDER DOES NOT COVER BOTTOM CHARACTER)
        private bool CheckGround()
        {
            Vector3 origin = transform.position + (Vector3.up * collisionBox.center.y) + (-Vector3.up * (collisionBox.size.y * 0.5f));

            bool groundHitDetected = false;

            //BASE COLLIDER CENTER RAY
            RaycastHit[] centerRayHits = Physics.RaycastAll(origin, Vector3.down, distanceToGround);
            foreach (RaycastHit centerRayHit in centerRayHits)
            {
                if (centerRayHit.collider.transform.parent != null)
                {
                    //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                    if (!IsMap(centerRayHit.collider))
                    {
                        continue;
                    }
                }

                transform.position = new Vector3(transform.position.x, centerRayHit.point.y, transform.position.z);
                groundHitDetected = true;
            }

            //CENTER RAY FROM COLLIDER TOP TO COLLIDER BASE (ENSURES CHARACTER IS NOT SUNKEN IN THE GROUND)
            Vector3 securityRayOrigin = transform.position + collisionBox.center + (Vector3.up * (collisionBox.size.y * 0.5f));
            float securityRayDistance = (securityRayOrigin.y - origin.y);

            RaycastHit[] securityHits = Physics.RaycastAll(securityRayOrigin, Vector3.down, securityRayDistance);
            foreach (RaycastHit securityHit in securityHits)
            {
                //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                if (!IsMap(securityHit.collider))
                {
                    continue;
                }

                transform.position = new Vector3(transform.position.x, securityHit.point.y, transform.position.z);
                groundHitDetected = true;
            }

            //BASE COLLIDER AROUND RAYS
            RaycastHit? closestHit = null; //NULLABLE RaycastHit
            for (int i = 0; i < groundRayOrigin.Length; i++)
            {
                Vector3 localRayOrigin = groundRayOrigin[i];
                Vector3 rotatedRayOrigin = transform.rotation * localRayOrigin;
                Vector3 aroundOrigin = origin + rotatedRayOrigin;

                RaycastHit[] groundHits = Physics.RaycastAll(aroundOrigin, Vector3.down, distanceToGround);
                foreach (RaycastHit groundHit in groundHits)
                {
                    //ONLY USE COLLIDERS THAT ARE CHILDREN OF collisionsRoot TRANSFORM
                    if (IsMap(groundHit.collider))
                    {
                        if (closestHit == null || groundHit.distance < closestHit.Value.distance) //CHECK SMALLER COLLISION DETECTED DISTANCE
                        {
                            closestHit = groundHit;
                        }
                    }
                }

                if (closestHit != null) //OBSTACLE DETECTED
                {
                    transform.position = new Vector3(transform.position.x, closestHit.Value.point.y, transform.position.z);
                    groundHitDetected = true;
                }
            }

            //IF FOR SOME REASON THE CHARACTER GOES DOWN THROUGH THE FLOOR TO THE VOID, MAKE IT COME BACK BY RESETTING ITS POSITION
            if (!groundHitDetected)
            {
                if (transform.position.y < -25)
                {
                    transform.position = Vector3.zero;
                }
            }

            //FIRST TIME IN AIR AFTER BEING GROUNDED (FALLING ONLY, NOT JUMPING)
            if (!groundHitDetected)
            {
                if (animator.GetBool("Grounded"))
                {
                    if (!useImpulseMovement)
                    {
                        lastMovementInputs = inputs.movement;
                    }
                }

            }
            else
            { //FIRST TIME ON GROUND AFTER BEING IN AIR
                if (!animator.GetBool("Grounded"))
                {
                    if (!allowInputWhileJumping) //CHECK IF JUMP/FALL ALLOWED DIRECTION CHANGE
                    {
                        //TURN PLAYER BACK TO INITIAL ROTATION
                        if (rotationCoroutine == null) //CHECK IF THERE IS NOT ANY ACTIVE COROUTINE
                        {
                            //START THE ROTATION COROUTINE
                            rotationCoroutine = RotationCoroutine();
                            StartCoroutine(rotationCoroutine);
                        }
                    }
                }
            }

            return groundHitDetected;
        }

        // 상호작용 가능한 물체가 범위 내에 있는지 체크하기
        private void CheckCanInteraction()
        {
            Debug.DrawRay(transform.position + collisionBox.center + Vector3.up * 0.5f, transform.forward * interactionRange, Color.red);

            if (Physics.Raycast(transform.position + collisionBox.center + Vector3.up * 0.5f, transform.forward, out RaycastHit hit, interactionRange, interactiveLayerMask)) // 상호작용 가능한 물체가 범위 내에 있으면
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

        // 충돌 범위 scene 뷰에 표시
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = Matrix4x4.TRS(transform.position + collisionBox.center, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, collisionBox.size * 0.5f);
            // Gizmos.matrix = Matrix4x4.identity; // Gizmo 매트릭스 초기화
        }

        ///APPLY CUSTOM GRAVITY BY MOVING CHARACTER DOWN
        private void ApplyGravity()
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
            transform.Translate(Vector3.up * verticalVelocity * Time.fixedDeltaTime);
        }

        ///FUNCTION CALLED WHEN CHARACTER IS GROUNDED, RESET GRAVITY MOVEMENT, MAKES PLAYER ABLE TO JUMP AND RESETS COYOTE TIME
        private void Land()
        {
            allowInputWhileJumping = false;
            verticalVelocity = 0f;
            canJump = true;
            if (canJumpTimer != null)
            {
                StopCoroutine(canJumpTimer);
            }
            canJumpTimer = null;
        }

        ///IMPULSES///
        //COROUTINE WHEN SLIDE OR ROLL IS ACTIVE
        private IEnumerator ImpulseMovementCoroutine(float impulseDistance, float impulseDuration, AnimationCurve impulseCurve, Vector2 impulseInputs)
        {
            //STORE LAST MOVEMENT INPUTS (TO USE IF PLAYER LEAVES GROUND WHILE SLIDE OR ROLL)
            lastMovementInputs = impulseInputs;

            Vector3 inputMoveDirection = (transform.forward * impulseInputs.y + transform.right * impulseInputs.x).normalized;

            //OVERRIDE INPUT MOVEMENT
            useImpulseMovement = true;

            //CHECK IF THERE IS ANY ACTIVE ROTATION COROUTINE AND STOP IT
            if (rotationCoroutine != null)
            {
                StopCoroutine(rotationCoroutine);
            }

            //TURN CHARACTER TO FACE INPUT DIRECTION
            if (inputMoveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputMoveDirection);

                animator.transform.rotation = targetRotation;
            }
            else
            {
                animator.transform.localRotation = Quaternion.identity;
            }

            //CHECK DIRECTION USED FOR IMPULSE MOVEMENT (CHARACTER FACE DIRECTION/INPUT DIRECTION)
            Vector3 forwardDirection = animator.transform.forward;

            yield return new WaitForFixedUpdate(); //THIS SEEMS TO ENSURE DISTANCE IS ALWAYS THE SAME REGARDLESS FPS (OR ANY OTHER RELATED ISSUE?)

            //IMPULSE TRAVELING impulseDistance UNITS IN impulseDuration SECONDS AT THE RHYTHM OF impulseCurve
            float distanceCovered = 0;
            float t = 0;
            while (t < 1)
            {
                t += Time.fixedDeltaTime / impulseDuration; //USING PHYSICS TIME (Time.fixedDeltaTime instead of Time.deltaTime)

                //CHECK TRAVEL DISTANCE ON CURVE BASED ON CURRENT TIME AND ALREADY COVERED DISTANCE
                float currentDistance = impulseDistance * impulseCurve.Evaluate(t);
                float distanceToCover = currentDistance - distanceCovered;
                distanceCovered = currentDistance;

                impulseMovement = forwardDirection * (distanceToCover / Time.fixedDeltaTime); //MOVEMENT DIRECTION APPLIED IN FixedUpdate

                //ADDITIONAL COLLISION CHECK TO AVOID TUNNELING WHEN SLIDE OR ROLL
                CheckCollisions();

                //CHECK IF IMPULSE GOT THE PLAYER OUT OF THE GROUND (WHEN SLIDE / ROLL NEAR EDGES OF PLATFORMS)
                if (!animator.GetBool("Grounded"))
                {
                    //EXIT COROUTINE WHILE LOOP
                    t = 1;
                }

                yield return new WaitForFixedUpdate(); //WAIT FOR PHYSICS TIME (WaitForFixedUpdate instead of yield return null or yield return 0)
            }

            //RESET IMPULSE MOVEMENT AND START USING INPUT MOVEMENT AGAIN
            impulseMovement = Vector3.zero;
            useImpulseMovement = false;

            //TURN PLAYER BACK TO INITIAL ROTATION (FOR SLIDES/ROLLS MADE TO OTHER THAN FORWARD DIRECTION)
            if (rotationCoroutine != null) //CHECK IF THERE IS ANY ACTIVE COROUTINE
            {
                StopCoroutine(rotationCoroutine); //ENSURE EXISTING COROUTINE STOPS BEFORE STARTING NEW ONE
            }
            //START THE ROTATION COROUTINE
            rotationCoroutine = RotationCoroutine();
            StartCoroutine(rotationCoroutine);

            //KEEP CHARACTER CROUCHED IF CEILING IS LOW IN ENDING IMPULSE TRAVEL POSITION
            if (IsCeilingAbove())
            {
                crouchLayerWeight = 1f;
            }

            //UNLOAD COROUTINE (FOR CHECKS LIKE impulseMovementCoroutine == null)
            impulseMovementCoroutine = null;
        }

        //COROUTINE FOR ROTATING CHARACTER AFTER ROLL / SLIDE ENDED
        private IEnumerator RotationCoroutine()
        {
            Quaternion initRotation = animator.transform.localRotation;

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 5f;
                animator.transform.localRotation = Quaternion.Slerp(initRotation, Quaternion.identity, t);
                yield return null;
            }

            animator.transform.localRotation = Quaternion.identity;

            //UNLOAD COROUTINE (FOR CHECKS LIKE rotationCoroutine == null)
            rotationCoroutine = null;
        }

        // ///CHARACTER SWITCH///
        // private void ChangeCharacter(int newCharacter)
        // {
        //     currentCharacter = newCharacter;
        //     for (int i = 0; i < characterMeshesRoot.Length; i++)
        //     {
        //         characterMeshesRoot[i].SetActive(false);
        //     }

        //     characterMeshesRoot[currentCharacter].SetActive(true);
        // }
    }
}



