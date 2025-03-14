using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum Direction { UP, DOWN, LEFT, RIGHT }

public class PlayerAnimationStateController : AnimationStateController
{
    [SerializeField] private Transform cameraFocus;

    Vector2 oldMousePos;
    float mousePosTimer;
    Vector3 currentMovement, slopeMoveDirection;
    RaycastHit hit, slopeHit;
    bool forwardBlocked, backBlocked, rightBlocked, leftBlocked;
    private float g = 0, gtimer;

    public override void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        anim = GetComponent<Animator>();
        VelocityXHash = Animator.StringToHash("Velocity X");
        VelocityZHash = Animator.StringToHash("Velocity Z");
        blockingHash = Animator.StringToHash("isBlocking");
        hittingHash = Animator.StringToHash("isHitting");
        AttackDirectionHash = Animator.StringToHash("AttackDirection");
        hitHash = Animator.StringToHash("isHit");
        HitTypeHash = Animator.StringToHash("HitType");
    }

    public override void Update()
    {
        InputMovementController();
        InputActionController();
        HandleDirection();
        HandleRotation();
        HandleMovement();
    }

    private void OnGUI()
    {
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.normal.textColor = Color.black;
        GUI.skin.label.fontSize = 60;
        GUI.Label(new Rect(Screen.width / 2 - 150, 50, 300, 150), direction.ToString());
    }

    private void HandleDirection()
    {
        mousePosTimer -= Time.deltaTime * 30;
        if (mousePosTimer < 0)
        {
            oldMousePos = Input.mousePosition;
            mousePosTimer = 1;
        }
        if (isBlocking || isAttacking)
        {
            return;
        }
        if (Vector2.Distance(Input.mousePosition, oldMousePos) > 15)
        {
            var mouseX = Input.mousePosition.x;
            var mouseY = Input.mousePosition.y;
            if (mouseX > oldMousePos.x)
            {
                if (Mathf.Abs(mouseY - oldMousePos.y) > 10)
                {
                    if (mouseY > oldMousePos.y)
                    {
                        direction = Direction.UP;
                    }
                    else
                    {
                        direction = Direction.DOWN;
                    }
                }
                else
                {
                    direction = Direction.RIGHT;
                }
            }
            else
            {
                if (Mathf.Abs(mouseY - oldMousePos.y) > 15)
                {
                    if (mouseY > oldMousePos.y)
                    {
                        direction = Direction.UP;
                    }
                    else
                    {
                        direction = Direction.DOWN;
                    }
                }
                else
                {
                    direction = Direction.LEFT;
                }
            }
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out slopeHit, 1))
        {
            if (slopeHit.normal != Vector3.up)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsGrounded ()
    {
        Debug.DrawRay(transform.position + Vector3.up, transform.TransformDirection(Vector3.down), Color.yellow);
        if (Physics.Raycast(transform.position + Vector3.up, transform.TransformDirection(Vector3.down), out hit, 1f))
        {
            return true;
        }
        return false;
    }

    void FixedUpdate()
    {
        forwardBlocked = false;
        backBlocked = false;
        leftBlocked = false;
        rightBlocked = false;
        /*if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 10))
        {
            if (Vector3.Distance(transform.position, hit.point) < 0.5f)
            {
                forwardBlocked = true;
            }
        }
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.back), out hit, 10))
        {
            if (Vector3.Distance(transform.position, hit.point) < 0.5f)
            {
                backBlocked = true;
            }
        }
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.left), out hit, 10))
        {
            if (Vector3.Distance(transform.position, hit.point) < 0.5f)
            {
                leftBlocked = true;
            }
        }
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out hit, 10))
        {
            if (Vector3.Distance(transform.position, hit.point) < 0.5f)
            {
                rightBlocked = true;
            }
        }*/
        HandleRotation();
    }

    private void HandleRotation()
    {
        //for mounts

        /*cameraFocus.eulerAngles += new Vector3(0, Input.GetAxis("Mouse X") * Time.deltaTime * 1000, 0); 
        if (cameraFocus.eulerAngles.y > 120 || cameraFocus.eulerAngles.y < -120)
        {
            cameraFocus.transform.DOLocalRotate(new Vector3(20, 0, 0), 3.5f).SetUpdate(true);
        }*/
        transform.eulerAngles += new Vector3(0, Input.GetAxisRaw("Mouse X") * Time.deltaTime * 1000, 0);
    }

    private void HandleMovement()
    {
        currentMovement = new Vector3(VelocityX, 0, VelocityZ);
        if (forwardBlocked && VelocityZ > 0)
        {
            VelocityZ = 0;
        }
        if (backBlocked && VelocityZ < 0)
        {
            VelocityZ = 0;
        }
        if (leftBlocked && VelocityX < 0)
        {
            VelocityX = 0;
        }
        if (rightBlocked && VelocityX > 0)
        {
            VelocityX = 0;
        }
        slopeMoveDirection = Vector3.ProjectOnPlane(currentMovement, slopeHit.normal);
        if (OnSlope())
        {
            transform.Translate(slopeMoveDirection * Time.deltaTime * movementSpeed);
        }
        else
        {
            transform.Translate(currentMovement * Time.deltaTime * movementSpeed);
        }
    }

    private void InputActionController()
    {
        if (isAttacking || isHit)
        {
            return;
        }
        if (Input.GetMouseButton(1))
        {
            MeleeBlock();
        }
        else
        {
            if (isBlocking)
            {
                StopBlocking();
            }
        }
        if (Input.GetMouseButtonDown(0) && !isBlocking)
        {
            MeleeAttack();
        }
    }

    public override void MeleeBlock(HumanEntity enemy = null)
    {
        isBlocking = true;
        isAttacking = false;
        thisEntity.DeactivateWeapons();
        switch (direction)
        {
            case Direction.UP:
                anim.SetInteger(AttackDirectionHash, 0);
                break;
            case Direction.DOWN:
                anim.SetInteger(AttackDirectionHash, 2);
                break;
            case Direction.LEFT:
                anim.SetInteger(AttackDirectionHash, -1);
                break;
            case Direction.RIGHT:
                anim.SetInteger(AttackDirectionHash, 1);
                break;
        }
        anim.SetBool(blockingHash, true);
    }

    public override void MeleeAttack()
    {
        StopBlocking();
        isAttacking = true;
        thisEntity.ActivateWeapons(direction);
        switch (direction)
        {
            case Direction.UP:
                anim.SetInteger(AttackDirectionHash, 0);
                break;
            case Direction.DOWN:
                anim.SetInteger(AttackDirectionHash, 2);
                break;
            case Direction.LEFT:
                anim.SetInteger(AttackDirectionHash, -1);
                break;
            case Direction.RIGHT:
                anim.SetInteger(AttackDirectionHash, 1);
                break;
        }
        anim.SetTrigger(hittingHash);
    }

    private void InputMovementController()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }
        if (Input.GetKey(KeyCode.W))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                VelocityZ += Time.deltaTime * GetAcceleration(acceleration);
            }
            else
            {
                if (VelocityZ > 0.5f)
                {
                    VelocityZ -= Time.deltaTime * deceleration;
                }
                else
                {
                    VelocityZ += Time.deltaTime * GetAcceleration(acceleration);
                }
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.S))
            {
                if (VelocityZ > -0.5f)
                {
                    VelocityZ -= Time.deltaTime * GetAcceleration(acceleration / 2);
                }
                else
                {
                    VelocityZ = -0.5f;
                }
            }
            else
            {
                if (VelocityZ > 0)
                {
                    VelocityZ -= Time.deltaTime * deceleration;
                }
                if (VelocityZ < 0)
                {
                    VelocityZ += Time.deltaTime * deceleration;
                }
            }
        }
        if (Input.GetKey(KeyCode.A))
        {
            if (VelocityX > 0)
            {
                VelocityX -= Time.deltaTime * GetAcceleration(acceleration);
            }
            else
            {
                VelocityX -= Time.deltaTime * GetAcceleration(acceleration / 2);
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.D))
            {
                if (VelocityX < 0)
                {
                    VelocityX += Time.deltaTime * GetAcceleration(acceleration);
                }
                else
                {
                    VelocityX += Time.deltaTime * GetAcceleration(acceleration / 2);
                }
            }
            else
            {
                if (VelocityX > 0)
                {
                    VelocityX -= Time.deltaTime * deceleration;
                }
                if (VelocityX < 0)
                {
                    VelocityX += Time.deltaTime * deceleration;
                }
            }
        }
    }

    public override void ToggleHit()
    {
        isHit = false;
    }
}