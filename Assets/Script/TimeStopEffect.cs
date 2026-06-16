using UnityEngine;

/// <summary>
/// 5초간 적을 정지시키는 시간 정지 스킬 효과 (플레이어를 따라다님)
/// </summary>
public class TimeStopEffect : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float radius = 5f;
    [SerializeField] private float stunDuration = 5f;
    [SerializeField] private float lifeTime = 1f; // VFX가 사라지는 시간

    private Transform _player;

    private void Start()
    {
        // 플레이어 찾기
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = GameObject.Find("player");
        if (playerObj != null) _player = playerObj.transform;

        ApplyEffect();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // 애니메이션 재생 중 플레이어 위치 따라가기
        if (_player != null)
        {
            transform.position = _player.position;
        }
    }

    private void ApplyEffect()
    {
        Debug.Log("<color=cyan>[TimeStop]</color> Time Stop Skill Activated!");

        // 범위 내의 모든 적 찾기
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (var col in hitEnemies)
        {
            // 1. 기본 적
            if (col.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.ApplyStun(stunDuration);
            }
            // 2. 원거리/비행 적
            else if (col.TryGetComponent<RangedEnemy>(out var ranged))
            {
                ranged.ApplyStun(stunDuration);
            }
            // 3. 하리보
            else if (col.TryGetComponent<MeltingHaribo>(out var haribo))
            {
                haribo.ApplyStun(stunDuration);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
