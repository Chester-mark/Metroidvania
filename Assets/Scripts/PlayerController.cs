using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Object Reference
    private Rigidbody2D rb;
    private Animator anim;

    //Horizontal Movements
    [Header("Horizontal Movement Setting:")]

    [SerializeField] private float walk_speed = 0;

    private bool canDash = true, dashed;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dash_effect;
    [Space(5)]

    //Vertical Movements
    [Header("Vertical Movement Settings:")]

    private float gravity;
    [SerializeField] private float jump_force = 0f;

    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;

    private float coyoteTimeCounter = 0f;
    [SerializeField] private float coyoteTime;

    private int airJumpCounter = 0;
    [SerializeField] private int maxJumps;
    [Space(5)]

    //Ground Check
    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask IsGround;
    [Space(5)]

    //Attack Setting
    [Header("Attack Settings:")]
    [SerializeField] private Transform SideAttackTransfrom;
    [SerializeField] private Vector2 SideAttackArea;

    [SerializeField] private Transform UpAttackTransfrom;
    [SerializeField] private Vector2 UpAttackArea;

    [SerializeField] private Transform DownAttackTransfrom;
    [SerializeField] private Vector2 DownAttackArea;

    [SerializeField] private float timeBetweenAttack;
    private float timeSinceAttack;
    [Space(5)]

    //Input Reference
    private float xAxis;
    private bool attack =false;
    
    //Player Reference
    public static PlayerController Instance;
    private PlayerStateList pState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        pState = GetComponent<PlayerStateList>();
        gravity = rb.gravityScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransfrom.position, SideAttackArea);
        Gizmos.DrawWireCube(DownAttackTransfrom.position, UpAttackArea);
        Gizmos.DrawWireCube(UpAttackTransfrom.position, DownAttackArea);
    }



    // Update is called once per frame
    void Update()
    {
        GetInput();
        UpdateJump();

        if (pState.dashing) return;
        Movement();
        Jump();
        StartDash();
        Attack();
    }

    private void GetInput() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        attack = Input.GetButtonDown("Attack");
    }

    //Movement Manager
    private void Movement()
    {

        rb.velocity = new Vector2 (walk_speed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && IsGrounded());
        Flip();

    }

    //Dashing
    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (IsGrounded())
        {
            dashed = false;
        }
    }

    //Dash Coroutine
    IEnumerator Dash() 
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (IsGrounded()) Instantiate(dash_effect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    //Flip Sprite while moving
    private void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
        }
    }

    //Check if Player is grounded
    public bool IsGrounded() 
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, IsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, IsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, IsGround))
        {
            return true;
            
        } else return false;
    }

    //Jump Mechanics
    private void Jump()
    {
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
        }

        if (!pState.jumping) 
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, jump_force);
                pState.jumping = true;
            }
            else if (!IsGrounded() && airJumpCounter < maxJumps && Input.GetButtonDown("Jump")) {

                pState.jumping = true;
                airJumpCounter++;
                rb.velocity = new Vector3(rb.velocity.x, jump_force);
            }
        }
        anim.SetBool("Jumping", !IsGrounded());
    }
    private void UpdateJump()
    {
        if (IsGrounded())
        {
            coyoteTimeCounter = coyoteTime;
            pState.jumping = false;
            airJumpCounter = 0;

        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
        }


        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }

    }

    private void Attack() 
    {
        timeSinceAttack += Time.deltaTime;

        if (attack && timeSinceAttack >= timeBetweenAttack) {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");
        
        }

    }

}
