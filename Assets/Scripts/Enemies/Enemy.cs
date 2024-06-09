using System.Collections;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

public class Enemy : MonoBehaviour
{
    [SerializeField] public float health;
    [SerializeField] protected float recoilLength;
    [SerializeField] protected float recoilFactor;
    [SerializeField] protected bool isRecoiling = false;
    [SerializeField] protected float speed;
    [SerializeField] protected float damage;
    [SerializeField] GameObject orangeBlood;
    [SerializeField] GameObject blackBlood;
    [SerializeField] protected AudioClip HurtSound;
    [SerializeField] protected AudioClip DeadSound;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    protected SpriteRenderer sr;
    protected Animator anim;

    protected enum EnemyStates
    {
        Crawler_Idle,
        Crawler_Flip,
        Bat_Idle,
        Bat_Chase,
        Bat_Stunned,
        Bat_Death,
        Charger_Idle,
        Charger_Surprised,
        Charger_Charge,
    }

    protected EnemyStates currentEnemyState;

    protected virtual EnemyStates GetCurrentEnemyState
    {
        get { return currentEnemyState; }
        set
        {
            if (currentEnemyState != value)
            {
                currentEnemyState = value;
                ChangeCurrentAnimation();
            }
        }
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    
    protected virtual void Update()
    {
        

        
        if (isRecoiling)
        {
            if (recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0;
            }
        }
        else
        {
            UpdateEnemyStates();
        }
        if (!PlayerController.Instance.pState.alive)
        {
            Death(0.5f);
        }
        
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        health -= _damageDone;
        if (!isRecoiling)
        {
            if (health > 0)
            {
                GameObject _orangeBlood = Instantiate(orangeBlood, transform.position, Quaternion.identity);
                Destroy(_orangeBlood, 5.5f);
                AudioManager.instance.PlaySound(HurtSound);
                rb.velocity = _hitForce * recoilFactor * _hitDirection;
                isRecoiling = true;
            }
            else
            {
                AudioManager.instance.PlaySound(DeadSound);
                GameObject _blackBlood = Instantiate(blackBlood, transform.position, Quaternion.identity);
                Destroy(_blackBlood, 5.5f);
            }
        }
        
        
    }

    protected virtual void Death(float destroyTime)
    {
        Destroy(gameObject, destroyTime);
    }

    protected void OnCollisionStay2D(Collision2D _other)
    {
        if (_other.gameObject.CompareTag("Player") && !PlayerController.Instance.pState.invincible && health > 0)
        {
            Attack();
            PlayerController.Instance.HitStopTime(0, 5, 0.2f);
        }
    }
    protected virtual void Attack()
    {
        PlayerController.Instance.TakeDamage(damage);
    }

    protected virtual void UpdateEnemyStates()
    {

    }
    protected virtual void ChangeCurrentAnimation()
    {

    }

    protected void ChangeState(EnemyStates _newState)
    {
        GetCurrentEnemyState = _newState;
    }
}
