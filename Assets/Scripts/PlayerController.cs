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
    //[SerializeField] private float attack_walk_speed = 3;

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

    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private float timeBetweenAttack;
    private float timeSinceAttack;

    [SerializeField] private float attackDamage;
    [SerializeField] private GameObject swordSlashEffect;
    [Space(5)]


    //Recoil Setting
    [Header("Recoil Settings:")]
    [SerializeField] private int recoilXSteps = 5;
    [SerializeField] private int recoilYSteps = 5;
    [SerializeField] private float recoilXSpeed = 100;
    [SerializeField] private float recoilYSpeed = 100;
    private int stepsXRecoiled, stepsYRecoiled;
    [Space(5)]

    [Header("Health Settings")]
    public int health;
    public int maxHealth;
    [Space(5)]

    //Input Reference
    private float xAxis;
    private float yAxis;
    private bool attack =false;
    
    //Player Reference
    public static PlayerController Instance;
    [HideInInspector]public PlayerStateList pState;

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
        Health = maxHealth;
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

    private void FixedUpdate()
    {
        if (pState.dashing) return;
        Recoil();
    }

    private void GetInput() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
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
            pState.lookingRight = false;
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            pState.lookingRight = true;
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

    //Damage Player
    public void TakeDamage(float _damage)
    {
        Health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }

    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;
        anim.SetTrigger("TakeDamage");
        yield return new WaitForSeconds(1f);
        pState.invincible = false;
    }

    public int Health
    {
        get { return health; }
        set
        {
            if (health != value)
            {
                health = Mathf.Clamp(value, 0, maxHealth);
            }
        }
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
            //walk_speed = attack_walk_speed;

            if (yAxis == 0 || yAxis < 0 && IsGrounded())
            {
                Hit(SideAttackTransfrom, SideAttackArea,  ref pState.recoilingX, recoilXSpeed);
                Instantiate(swordSlashEffect, SideAttackTransfrom);
            }
            else if (yAxis > 0)
            {
                Hit(UpAttackTransfrom, UpAttackArea, ref pState.recoilingY, recoilYSpeed);
                SlashEffectAtAngle(swordSlashEffect, 90, UpAttackTransfrom);
            }
            else if (yAxis < 0 && !IsGrounded())
            {
                Hit(DownAttackTransfrom, DownAttackArea, ref pState.recoilingX, recoilXSpeed);
                SlashEffectAtAngle(swordSlashEffect, -90, DownAttackTransfrom);
            }
        }
        

    }

    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength) 
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
        List<Enemy> hitEnemies = new List<Enemy>();

        if (objectsToHit.Length > 0)
        {
            _recoilDir = true;
        }

        for (int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy e = objectsToHit[i].GetComponent<Enemy>();
            if (e && !hitEnemies.Contains(e))
            {
                e.EnemyHit(attackDamage, (transform.position - objectsToHit[i].transform.position).normalized, _recoilStrength);
                hitEnemies.Add(e);
            }
        }
    }

    private void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }

    //Recoil Functions
    private void Recoil()
    {
        if (pState.recoilingX)
        {
            if (pState.lookingRight)
            {
                rb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                rb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }

        if (pState.recoilingY)
        {
            rb.gravityScale = 0;
            if (yAxis < 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else
        {
            rb.gravityScale = gravity;
        }

        //Stop Recoil
        if (pState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else StopRecoilX();
        
        if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else StopRecoilY();
        
        if (IsGrounded())
        {
            StopRecoilY();
        }
    }
    private void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }
    private void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

}
