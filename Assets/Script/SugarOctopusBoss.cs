using UnityEngine;
using System.Collections;

/// <summary>
/// 첫 번째 보스: 설탕 문어 (Sugar Octopus)
/// BossBase를 상속하여 페이즈 전환 시스템을 자동으로 사용합니다.
/// </summary>
public class SugarOctopusBoss : BossBase
{
    private enum SugarState { Idle, TentacleAttack, SugarSpray }

    [Header("Sugar Octopus Specific")]
    public float attackInterval = 3f;
    public GameObject sugarProjectilePrefab;
    public Transform[] tentacleSpawnPoints;
    public GameObject tentaclePrefab;

    private SugarState _currentState = SugarState.Idle;

    protected override IEnumerator BossBehaviorRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (!_isDead)
        {
            float rand = Random.value;
            if (rand < 0.5f)
                yield return PerformTentacleAttack();
            else
                yield return PerformSugarSpray();

            yield return new WaitForSeconds(attackInterval);
        }
    }

    protected override void OnPhaseTransition(int newPhase)
    {
        Debug.Log($"Sugar Octopus: Phase {newPhase}!");
        // 페이즈마다 공격 속도 증가
        attackInterval = Mathf.Max(1f, attackInterval - 0.5f);
    }

    private IEnumerator PerformTentacleAttack()
    {
        _currentState = SugarState.TentacleAttack;
        Debug.Log("Sugar Octopus: Tentacle Attack!");

        if (tentacleSpawnPoints.Length > 0 && tentaclePrefab != null)
        {
            foreach (var sp in tentacleSpawnPoints)
            {
                Instantiate(tentaclePrefab, sp.position, Quaternion.identity);
                yield return new WaitForSeconds(0.2f);
            }
        }

        yield return new WaitForSeconds(1f);
        _currentState = SugarState.Idle;
    }

    private IEnumerator PerformSugarSpray()
    {
        _currentState = SugarState.SugarSpray;
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
        _currentState = SugarState.Idle;
    }
}
