using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Object Reference
    private Rigidbody2D rb;
    private Animator anim;
    //private BoxCollider2D collider;

    //Horizontal Movements
    [Header("Horizontal Movement Setting:")]
    [SerializeField] private float walk_speed = 0;
    private bool canDash = true;
    private bool dashed;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [Space(5)]

    //Vertical Movements
    [Header("Vertical Movement Settings:")]
    [SerializeField] private float jump_force = 0;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter = 0;
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

    //Movement Reference
    private float xAxis;
    private float gravity;

    //Player Reference
    public static PlayerController Instance;
    PlayerStateList pState;

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
        //collider = GetComponent<BoxCollider2D>();
        anim = GetComponent<Animator>();
        pState = GetComponent<PlayerStateList>();
        gravity = rb.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateJump();
        if (pState.dashing) return;
        Movement();
        Jump();
        StartDash();
        
    }

    //Movement Manager
    private void Movement()
    {

        xAxis = Input.GetAxisRaw("Horizontal");

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

}
