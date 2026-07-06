using System.Collections;
using UnityEngine;

public class PoppingCandyBat : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 100f)] // 인스펙터에서 슬라이더로 조절 가능하게 추가
    public float detectionRange = 40f; 
    
    public float attackCooldown = 2f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("References")]
    private Animator _anim;
    private Transform _player;
    private float _lastAttackTime = -100f; 
    private bool _isDead = false;

    void Awake()
    {
        _anim = GetComponent<Animator>();
        if (_anim == null) _anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        FindPlayer();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) 
        {
            _player = playerObj.transform;
        }
    }

    void Update()
    {
        if (_isDead) return;
        if (_player == null) 
        {
            if (Time.frameCount % 100 == 0) FindPlayer();
            return;
        }

        // 2D 거리 계산
        float distance = Vector2.Distance(transform.position, _player.position);

        if (distance <= detectionRange)
        {
            if (Time.time >= _lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
    }

    public void Attack()
    {
        _lastAttackTime = Time.time;
        if (_anim != null) _anim.SetTrigger("Attack");
        StartCoroutine(FireRoutine(0.2f));
    }

    IEnumerator FireRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (bulletPrefab != null && firePoint != null && _player != null)
        {
            GameObject bullet = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.SpawnFromPool("PoppingBullet", firePoint.position, Quaternion.identity)
                : Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Vector2 direction = ((Vector2)_player.position - (Vector2)firePoint.position).normalized;
            
            var projectile = bullet.GetComponent<PoppingBullet>();
            if (projectile != null)
            {
                projectile.Launch(direction);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        if (_player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _player.position);
        }
    }

    public void Die()
    {
        _isDead = true;
        if (_anim != null) _anim.SetTrigger("Die");
        Destroy(gameObject, 1f);
    }
}
