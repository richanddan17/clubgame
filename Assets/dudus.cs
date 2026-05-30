using System.Collections;
using UnityEngine;

public class MoleMonster : MonoBehaviour
{
    public enum State
    {
        Idle,
        Detected,
        Burrowed,
        EmergeWarning,
        EmergeAttack,
        StiffVulnerable
    }

    [Header("Settings")]
    public float detectionRange = 10f;
    public float burrowSpeed = 5f;
    public float burrowDepth = 2f;
    public float attackRange = 3f;
    public float attackDamage = 30f;
    public float knockbackForce = 10f;

    [Header("References")]
    public Transform playerTransform;
    public GameObject omenEffect; // 징조 이펙트 (땅 먼지 등)
    public GameObject modelObject; // 몬스터 모델 (땅 들어갈 때 숨기기 위함)

    private State currentState = State.Idle;
    private Vector3 initialPosition;
    private bool isDead = false;

    void Start()
    {
        initialPosition = transform.position;
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        switch (currentState)
        {
            case State.Idle:
                CheckForPlayer();
                break;
        }
    }

    private void CheckForPlayer()
    {
        if (playerTransform == null) return;

        if (Vector3.Distance(transform.position, playerTransform.position) <= detectionRange)
        {
            StartCoroutine(BehaviorRoutine());
        }
    }

    IEnumerator BehaviorRoutine()
    {
        // 1. 플레이어 발견 - 1초 경직
        currentState = State.Detected;
        Debug.Log("Player Detected! Stiffening for 1s...");
        yield return new WaitForSeconds(1f);

        // 2. 땅속으로 들어가기
        currentState = State.Burrowed;
        Debug.Log("Burrowing underground...");
        if (modelObject != null) modelObject.SetActive(false);

        float burrowTime = Random.Range(3f, 5f);
        float elapsed = 0f;

        while (elapsed < burrowTime)
        {
            // 플레이어 추적 (X, Z 좌표만)
            Vector3 targetPos = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, burrowSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 튀어나오기 전 징조 (잠시 멈추고 징조 표시)
        currentState = State.EmergeWarning;
        Debug.Log("Showing Omen...");
        if (omenEffect != null) omenEffect.SetActive(true);
        yield return new WaitForSeconds(1f); // 징조 지속 시간

        // 4. 튀어나오기 + 공격
        currentState = State.EmergeAttack;
        Debug.Log("Emerging and Attacking!");
        if (omenEffect != null) omenEffect.SetActive(false);
        if (modelObject != null) modelObject.SetActive(true);

        // 공격 로직 (범위 내 플레이어 체크)
        ApplyAreaDamage();
        yield return new WaitForSeconds(0.5f);

        // 5. 튀어나온 후 4초간 경직 (취약 상태)
        currentState = State.StiffVulnerable;
        Debug.Log("Vulnerable for 4s!");
        yield return new WaitForSeconds(4f);

        // 다시 Idle 상태로 복귀
        currentState = State.Idle;
    }

    private void ApplyAreaDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Player"))
            {
                var playerHealth = hitCollider.GetComponent<IPlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);

                    Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 direction = (hitCollider.transform.position - transform.position).normalized;
                        direction.y = 0.5f;
                        rb.AddForce(direction * knockbackForce, ForceMode.Impulse);
                    }
                }
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentState == State.StiffVulnerable)
        {
            Debug.Log("Mole Monster hit while vulnerable!");
        }
    }
}

public interface IPlayerHealth
{
    void TakeDamage(float amount);
}
