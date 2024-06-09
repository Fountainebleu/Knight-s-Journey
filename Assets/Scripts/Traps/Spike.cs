using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !PlayerController.Instance.pState.invincible)
        {
            PlayerController.Instance.TakeDamage(damage);
            PlayerController.Instance.HitStopTime(0, 5, 0.2f);
        }
        if (collision.gameObject.CompareTag("Enemy"))
            collision.GetComponent<Enemy>().EnemyHit(10f, new Vector2 (0, 0), 0f);
    }
}
