using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    [Header("기본 설정")]
    public EnemyData data;
    public GameObject projectilePrefab;
    public Transform firePoint;

    private Rigidbody2D _rb;
    private Animator _animator;
    private Health _health;
    private Transform _player;
    private bool _isDead = false;
    private float _nextAttackTime;
    private Vector3 _initialScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<Health>();
        _initialScale = transform.localScale;
        
        if (firePoint == null) firePoint = transform;
        if (_health != null) _health.OnDie.AddListener(Die);
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (data != null && _health != null) _health.Initialize(data.HP);
    }

    private void FixedUpdate()
    {
        if (_isDead || _player == null || data == null) return;

        float distance = Vector2.Distance(transform.position, _player.position);

        // 1. 공격 사거리(5f) 안에 있으면 멈추고 공격
        if (distance <= 5f) 
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            if (_animator != null) _animator.SetBool("Walk", false);
            TryAttack();
        }
        // 2. 공격 사거리 밖이지만 탐지 범위(data.DetectionRange) 안이면 추격
        else if (distance <= data.DetectionRange)
        {
            Vector2 direction = (_player.position - transform.position).normalized;
            _rb.linearVelocity = new Vector2(direction.x * data.Speed, _rb.linearVelocity.y);
            if (_animator != null) _animator.SetBool("Walk", true);
            ApplyFlip(direction.x);
        }
        // 3. 둘 다 아니면 정지
        else
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            if (_animator != null) _animator.SetBool("Walk", false);
        }
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime) return;

        // 플레이어 방향 보기
        float directionX = _player.position.x - transform.position.x;
        ApplyFlip(directionX);

        if (_animator != null)
        {
            _animator.SetTrigger("Attack");
            _nextAttackTime = Time.time + data.AttackInterval;
        }
    }

    // 애니메이션 이벤트에서 호출할 함수
    public void Shoot()
    {
        if (_isDead || projectilePrefab == null || firePoint == null || _player == null) return;

        Vector2 direction = (_player.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.Euler(0, 0, angle));
        if (projectile.TryGetComponent<Projectile>(out var proj))
        {
            // 현재 스케일에 따른 방향 판정 (음수면 오른쪽, 양수면 왼쪽을 바라보는 리소스 특성 반영)
            bool facingRight = transform.localScale.x < 0;
            proj.Initialize(data.Damage, facingRight);
        }
    }

    private void ApplyFlip(float x)
    {
        // 리소스가 반대로 보고 있을 경우를 대비해 조건을 반전시킴
        if (x > 0.1f) transform.localScale = new Vector3(Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
        else if (x < -0.1f) transform.localScale = new Vector3(-Mathf.Abs(_initialScale.x), _initialScale.y, _initialScale.z);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _rb.linearVelocity = Vector2.zero;
        if (_animator != null) _animator.SetTrigger("Die");
        Destroy(gameObject, 1.5f);
    }
}
