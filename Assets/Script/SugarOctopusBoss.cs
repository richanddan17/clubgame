using UnityEngine;
using System.Collections;

/// <summary>
/// 첫 번째 보스: 설탕 문어 (Sugar Octopus)
/// </summary>
public class SugarOctopusBoss : MonoBehaviour
{
    public enum BossState { Idle, TentacleAttack, SugarSpray, PhaseTransition, Dead }

    [Header("능력치")]
    public float maxHP = 500f;
    public float attackInterval = 3f;

    [Header("공격 설정")]
    public GameObject sugarProjectilePrefab;
    public Transform[] tentacleSpawnPoints;
    public GameObject tentaclePrefab;

    private BossState _currentState = BossState.Idle;
    private Health _health;
    private Transform _player;

    void Awake()
    {
        _health = GetComponent<Health>();
        if (_health != null)
        {
            _health.Initialize(maxHP);
            _health.OnDie.AddListener(OnBossDie);
        }
    }

    void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        StartCoroutine(BossLogicRoutine());
    }

    IEnumerator BossLogicRoutine()
    {
        yield return new WaitForSeconds(2f); // 등장 대기

        while (_currentState != BossState.Dead)
        {
            // 패턴 선택
            float rand = Random.value;
            if (rand < 0.5f)
                yield return PerformTentacleAttack();
            else
                yield return PerformSugarSpray();

            yield return new WaitForSeconds(attackInterval);
        }
    }

    IEnumerator PerformTentacleAttack()
    {
        _currentState = BossState.TentacleAttack;
        Debug.Log("Sugar Octopus: Tentacle Attack!");
        
        if (tentacleSpawnPoints.Length > 0 && tentaclePrefab != null)
        {
            // 플레이어 근처 또는 고정 위치에 촉수 생성
            foreach (var sp in tentacleSpawnPoints)
            {
                Instantiate(tentaclePrefab, sp.position, Quaternion.identity);
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(1f);
        _currentState = BossState.Idle;
    }

    IEnumerator PerformSugarSpray()
    {
        _currentState = BossState.SugarSpray;
        Debug.Log("Sugar Octopus: Sugar Spray!");

        if (sugarProjectilePrefab != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                Instantiate(sugarProjectilePrefab, transform.position, rotation);
            }
        }

        yield return new WaitForSeconds(1.5f);
        _currentState = BossState.Idle;
    }

    private void OnBossDie()
    {
        _currentState = BossState.Dead;
        StopAllCoroutines();
        Debug.Log("Sugar Octopus Defeated! Clue dropped.");
        // 여기서 중요한 단서(Clue) 아이템 드랍 로직 추가 가능
    }
}
