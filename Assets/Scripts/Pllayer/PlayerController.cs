using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings:")]
    [SerializeField] private float walkSpeed = 1;
    [Space(5)]


    [Header("Vertical Movement Settings")]
    [SerializeField] private float jumpForce = 45f;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;
    [Space(5)]


    [Header("Ground Check Settings:")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;
    [Space(5)]


    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    [Space(5)]

    private bool isWallSliding;
    private float wallSlidingSpeed = 2f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask wallLayer;

    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(8f, 16f);
    private bool isFacingRight = true;

    public PlayerStateList pState;
    private Rigidbody2D rb;
    private float xAxis, yAxis;
    private float gravity;
    Animator anim;
    private bool canDash = true;
    private bool dashed;


    [Header("Attack Settings:")]
    [SerializeField] private Transform SideAttackTransform; 
    [SerializeField] private Vector2 SideAttackArea; 

    [SerializeField] private Transform UpAttackTransform; 
    [SerializeField] private Vector2 UpAttackArea; 

    [SerializeField] private Transform DownAttackTransform; 
    [SerializeField] private Vector2 DownAttackArea; 
    [SerializeField] private float timeBetweenAttack;
    [SerializeField] private LayerMask attackableLayer;
    [SerializeField] private GameObject slashEffect;
    [SerializeField] private float damage;
    [Space(5)]

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
    bool restoreTime;
    float restoreTimeSpeed;
    [SerializeField] GameObject bloodSpurt;
    [SerializeField] float hitFlashSpeed;
    private SpriteRenderer sr;
    public delegate void OnHealthChangedDelegate();
    [HideInInspector] public OnHealthChangedDelegate onHealthChangedCallback;
    float healTimer;
    [SerializeField] float timeToHeal;
    [Space(5)]

    [Header("Mana Settings")]
    [SerializeField] float mana;
    [SerializeField] float manaDrainSpeed;
    [SerializeField] float manaGain;
    [SerializeField] UnityEngine.UI.Image manaStorage;
    [Space(5)]

    [Header("Spell Settings")]
    [SerializeField] float manaSpellCost = 0.3f;
    [SerializeField] float timeBetweenCast = 0.5f;
    float timeSinceCast;
    [SerializeField] float spellDamage;
    [SerializeField] float downSpellForce;
    [SerializeField] GameObject sideSpellFireball;
    [SerializeField] GameObject upSpellExplosion;
    [SerializeField] GameObject downSpellFireball;
    [Space(5)]

    [Header("Sound Effects:")]
    [SerializeField] private AudioClip AttackSound;
    [SerializeField] private AudioClip JumpSound;
    [SerializeField] private AudioClip DeadSound;
    [SerializeField] private AudioClip HurtSound;
    [SerializeField] private AudioClip FireballSound;
    [SerializeField] private AudioClip DashSound;
    [SerializeField] private AudioClip ExplosionSound;
    [SerializeField] private AudioClip RespawnSound;
    [SerializeField] private AudioClip CheckpointActivateSound;
    [Space(5)]

    private UIManager uiManager;
    private float timeSinceAttack;
    private bool attack = false;
    private Transform currentCheckpoint;
    public static PlayerController Instance;

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
        uiManager = FindObjectOfType<UIManager>();
    }
   
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        gravity = rb.gravityScale;
        Mana = mana;
        pState.alive = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    void Update()
    {
        RestoreTimeScale();
        if (pState.alive)
            GetInputs();
        if (pState.dashing || pState.healing || !pState.alive) return;
        if (pState.casting)
        {
            CastSpell();
            return;
        }
        UpdateJumpVariables();
        FlashWhileInvincible();
        if (pState.alive)
        {
            CastSpell();
            Jump();
            StartDash();
            WallSlide();
            WallJump();
            Flip();
            Attack();
        }
    }
    private void FixedUpdate()
    {
        if (pState.dashing) return;
        Move();
        Recoil();
        Heal();
    }



    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        attack = Input.GetButtonDown("Attack");
        yAxis = Input.GetAxisRaw("Vertical");
    }

    private void Flip()
    {
        if (isFacingRight && xAxis < 0f || !isFacingRight && xAxis > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
        if (xAxis < 0)
            pState.lookingRight = false;

        else if (xAxis > 0)
            pState.lookingRight = true;

    }

    private void Move()
    {
        if (pState.healing)
        {
            rb.velocity = new Vector2(0, 0);
        }
        
        rb.velocity = new Vector2(walkSpeed * xAxis, rb.velocity.y);
        anim.SetBool("Walking", rb.velocity.x != 0 && Grounded());
    }

    void StartDash()
    {
        if (Input.GetButtonDown("Dash") && canDash && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (Grounded() || IsWalled())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        pState.dashing = true;
        anim.SetTrigger("Dashing");
        AudioManager.instance.PlaySound(DashSound);
        rb.gravityScale = 0;
        rb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        rb.gravityScale = gravity;
        pState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
            return true;
        else
            return false;
    }

    private bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }


    private void WallSlide()
    {
        if (IsWalled() && !Grounded() && xAxis != 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
            isWallSliding = false;
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
            wallJumpingCounter -= Time.deltaTime;
        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(-Mathf.Sign(transform.localScale.x) * 20, 5);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    void Jump()
    {
        if (Input.GetButtonDown("Jump"))
            AudioManager.instance.PlaySound(JumpSound);
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 3)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            pState.jumping = false;
        }
        if (!pState.jumping && jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce);
            pState.jumping = true;
        }
        else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
        {
            pState.jumping = true;
            airJumpCounter++;
            rb.velocity = new Vector3(rb.velocity.x, jumpForce);
        }
        anim.SetBool("Jumping", !Grounded());
        
    }

    void UpdateJumpVariables()
    {
        if (Grounded() || IsWalled())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
            coyoteTimeCounter -= Time.deltaTime;
        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferFrames;
        else
            jumpBufferCounter--;
    }

    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if (attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            anim.SetTrigger("Attacking");
            AudioManager.instance.PlaySound(AttackSound);
            if (yAxis == 0 || yAxis < 0 && Grounded())
            {
                var recoilLeftOrRight = isFacingRight ? 1 : -1;
                Hit(SideAttackTransform, SideAttackArea, ref pState.recoilingX, Vector2.right * recoilLeftOrRight, recoilXSpeed);
                Instantiate(slashEffect, SideAttackTransform);
            }
            else if (yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea, ref pState.recoilingY, Vector2.up, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, 80, UpAttackTransform);
            }
            else if (yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea, ref pState.recoilingY, Vector2.down, recoilYSpeed);
                SlashEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }

    void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilBool, Vector2 _recoilDir, float _recoilStrength)
    {
        Collider2D[] objectsToHit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, attackableLayer);
        List<Enemy> hitEnemies = new List<Enemy>();
        if (objectsToHit.Length > 0)
            _recoilBool = true;
        for (int i = 0; i < objectsToHit.Length; i++)
        {
            Enemy e = objectsToHit[i].GetComponent<Enemy>();
            if (e && !hitEnemies.Contains(e))
            {
                e.EnemyHit(damage, _recoilDir, _recoilStrength);
                hitEnemies.Add(e);
                if (objectsToHit[i].CompareTag("Enemy"))
                    Mana += manaGain;
            }
        }
    }
    void SlashEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        if (isFacingRight)
            _slashEffect.transform.localScale = new Vector2(0.6f, 0.6f);
        else
            _slashEffect.transform.localScale = new Vector2(-0.6f, -0.6f);
    }
    void Recoil()
    {
        if (pState.recoilingX)
        {
            if (pState.lookingRight)
                rb.velocity = new Vector2(-recoilXSpeed, rb.velocity.y);
            else
                rb.velocity = new Vector2(recoilXSpeed, rb.velocity.y);
        }

        if (pState.recoilingY)
        {

            if (yAxis < 0)
                rb.velocity = new Vector2(rb.velocity.x, recoilYSpeed);
            else
                rb.velocity = new Vector2(rb.velocity.x, -recoilYSpeed);
            airJumpCounter = 0;
        }
        else
            rb.gravityScale = gravity;

        if (pState.recoilingX && stepsXRecoiled < recoilXSteps)
            stepsXRecoiled++;
        else
            StopRecoilX();
        if (pState.recoilingY && stepsYRecoiled < recoilYSteps)
            stepsYRecoiled++;
        else
            StopRecoilY();
        if (Grounded())
            StopRecoilY();
    }
    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        pState.recoilingX = false;
    }
    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        pState.recoilingY = false;
    }

    public void TakeDamage(float _damage)
    {
        Health -= Mathf.RoundToInt(_damage);
        if (Health <= 0)
        {
            Health = 0;
            Death();
        }
        else
            StartCoroutine(StopTakingDamage());
    }
    IEnumerator StopTakingDamage()
    {
        pState.invincible = true;
        anim.SetTrigger("TakeDamage");
        AudioManager.instance.PlaySound(HurtSound);
        GameObject _bloodSpurtParticles = Instantiate(bloodSpurt, transform.position, Quaternion.identity);
        Destroy(_bloodSpurtParticles, 1.5f);
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
                if (onHealthChangedCallback != null)
                    onHealthChangedCallback.Invoke();
            }

        }
    }
    void RestoreTimeScale()
    {
        if (restoreTime)
        {
            if (Time.timeScale < 1)
                Time.timeScale += Time.unscaledDeltaTime * restoreTimeSpeed;
            else
            {
                Time.timeScale = 1;
                restoreTime = false;
            }
        }
    }

    public void HitStopTime(float _newTimeScale, int _restoreSpeed, float _delay)
    {
        restoreTimeSpeed = _restoreSpeed;
        if (_delay > 0)
        {
            StopCoroutine(StartTimeAgain(_delay));
            StartCoroutine(StartTimeAgain(_delay));
        }
        else
        {
            restoreTime = true;
        }
          
        Time.timeScale = _newTimeScale;
    }
    IEnumerator StartTimeAgain(float _delay)
    {
        yield return new WaitForSecondsRealtime(_delay);
        restoreTime = true;
    }

    void FlashWhileInvincible()
    {
        sr.material.color = pState.invincible && pState.alive ? Color.Lerp(Color.white, Color.black, Mathf.PingPong(Time.time * hitFlashSpeed, 1.0f)) : Color.white;
    }

    void Heal()
    {
        if (Input.GetButton("Healing") && Health < maxHealth && Mana > 0 && Grounded() && !pState.dashing)
        {
            pState.healing = true;
            anim.SetBool("Healing", true);
            healTimer += Time.deltaTime;
            if (healTimer >= timeToHeal)
            {
                Health++;
                healTimer = 0;
            }
            Mana -= Time.deltaTime * manaDrainSpeed;
        }
        else
        {
            pState.healing = false;
            anim.SetBool("Healing", false);
            healTimer = 0;
        }
    }

    float Mana
    {
        get { return mana; }
        set
        {
            if (mana != value)
            {
                mana = Mathf.Clamp(value, 0, 1);
                manaStorage.fillAmount = Mana;
            }
        }
    }

    void CastSpell()
    {
        if (Input.GetButtonDown("CastSpell") && timeSinceCast >= timeBetweenCast && Mana >= manaSpellCost)
        {
            pState.casting = true;
            timeSinceCast = 0;
            StartCoroutine(CastCoroutine());
        }
        else
            timeSinceCast += Time.deltaTime;
        if (Grounded() && downSpellFireball.activeInHierarchy)
        {
            downSpellFireball.SetActive(false);
            pState.invincible = false;
        }
        if (downSpellFireball.activeInHierarchy)
            rb.velocity += downSpellForce * Vector2.down;
    }

    IEnumerator CastCoroutine()
    {
        anim.SetBool("Casting", true);
        yield return new WaitForSeconds(0.15f);
        if (yAxis == 0 || (yAxis < 0 && Grounded()))
        {
            GameObject _fireBall = Instantiate(sideSpellFireball, new Vector3(wallCheck.position.x - 0.01f, wallCheck.position.y), Quaternion.identity);
            AudioManager.instance.PlaySound(FireballSound);
            if (pState.lookingRight)
                _fireBall.transform.eulerAngles = Vector3.zero;
            else
                _fireBall.transform.eulerAngles = new Vector2(_fireBall.transform.eulerAngles.x, 180);
            pState.recoilingX = true;
        }
        else if (yAxis > 0)
        {
            Instantiate(upSpellExplosion, transform);
            AudioManager.instance.PlaySound(ExplosionSound);
            rb.velocity = Vector2.zero;
        }
        else if (yAxis < 0 && !Grounded())
        {
            pState.invincible = true;
            downSpellFireball.SetActive(true);
            AudioManager.instance.PlaySound(FireballSound);
        }
        Mana -= manaSpellCost;
        yield return new WaitForSeconds(0.35f);
        anim.SetBool("Casting", false);
        pState.casting = false;
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (_other.GetComponent<Enemy>() != null && pState.casting)
            _other.GetComponent<Enemy>().EnemyHit(spellDamage, (_other.transform.position - transform.position).normalized, -recoilYSpeed);
        if (_other.gameObject.CompareTag("Checkpoint"))
        {
            currentCheckpoint = _other.transform;
            _other.GetComponent<Collider2D>().enabled = false;
            _other.GetComponent<Animator>().SetTrigger("appear");
            AudioManager.instance.PlaySound(CheckpointActivateSound);
        }
    }

    void Death()
    {
        if (downSpellFireball.activeInHierarchy)
        {
            downSpellFireball.SetActive(false);
            anim.SetBool("Casting", false);
            rb.velocity = Vector2.down;
        }
        pState.alive = false;
        pState.invincible = true;
        pState.dashing = false;
        Time.timeScale = 0;
        GameObject _bloodSpurtParticles = Instantiate(bloodSpurt, transform.position, Quaternion.identity);
        Destroy(_bloodSpurtParticles, 1.5f);
        anim.SetTrigger("Death");
        AudioManager.instance.PlaySound(DeadSound);
    }

    public void Respawn()
    {
        Health = maxHealth;
        Mana = 1;
        anim.ResetTrigger("Death");
        anim.Play("Player-idle");
        AudioManager.instance.PlaySound(RespawnSound);
        transform.position = currentCheckpoint.position;
        pState.alive = true;
        pState.invincible = false;
    }

    void GoToDieMenu() => uiManager.GameOver();
}

