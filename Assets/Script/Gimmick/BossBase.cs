using UnityEngine;
using System.Collections;

/// <summary>
/// 보스 페이즈 전환 베이스 클래스.
/// Health 이벤트를 구독해서 HP%에 따라 자동으로 페이즈를 전환합니다.
/// 기존 SugarOctopusBoss의 BossState + 코루틴 패턴을 추상화합니다.
/// </summary>
public abstract class BossBase : MonoBehaviour
{
    [Header("Boss Base")]
    [SerializeField] protected float maxHP = 500f;
    [SerializeField] protected float[] phaseThresholds = { 0.75f, 0.5f, 0.25f };

    protected Health _health;
    protected Transform _player;
    protected int _currentPhase;
    protected bool _isDead;

    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
        if (_health != null)
        {
            _health.Initialize(maxHP);
            _health.OnDie.AddListener(OnBossDie);
            _health.OnHealthChanged.AddListener(OnHealthChanged);
        }
    }

    protected virtual void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _currentPhase = 0;
        StartCoroutine(BossBehaviorRoutine());
    }

    /// <summary>
    /// 보스의 메인 행동 루프. 하위 클래스에서 구현합니다.
    /// </summary>
    protected abstract IEnumerator BossBehaviorRoutine();

    /// <summary>
    /// 페이즈 전환 시 호출됩니다. 하위 클래스에서 페이즈별 보너스 로직을 구현합니다.
    /// </summary>
    protected abstract void OnPhaseTransition(int newPhase);

    private void OnHealthChanged(float current, float max)
    {
        if (_isDead || max <= 0f) return;
        float hpPercent = current / max;

        for (int i = _currentPhase; i < phaseThresholds.Length; i++)
        {
            if (hpPercent <= phaseThresholds[i])
            {
                _currentPhase = i + 1;
                OnPhaseTransition(_currentPhase);
                break;
            }
        }
    }

    protected virtual void OnBossDie()
    {
        _isDead = true;
        StopAllCoroutines();
        Debug.Log($"{gameObject.name} Defeated!");
    }
}
