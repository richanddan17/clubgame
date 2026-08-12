using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Health 컴포넌트와 UI를 연결하여 8단계 HP 아이콘(05_0~05_7)과 숫자를 갱신함
/// [0]=풀피, [7]=사망 (최대체력 7, 피격 1데미지 → 피격횟수 = 인덱스)
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("연결 설정 (비어있으면 자동 검색)")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Image hpStateIcon;
    [SerializeField] private Sprite[] hpStateSprites; // 05_0(풀피) ~ 05_7(사망)
    [SerializeField] private TextMeshProUGUI hpText;

    private void Start()
    {
        // 1. 대상(플레이어) 찾기
        if (targetHealth == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetHealth = player.GetComponent<Health>();

            // 여전히 없다면 씬 전체에서 Health 찾기
            if (targetHealth == null) targetHealth = Object.FindAnyObjectByType<Health>();
        }

        // 2. 아이콘 및 텍스트 자동 연결
        if (hpStateIcon == null)
        {
            var icon = transform.Find("HP_Icon");
            if (icon != null) hpStateIcon = icon.GetComponent<Image>();
            else hpStateIcon = GetComponentInChildren<Image>(true);
        }
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (targetHealth != null)
        {
            // 이벤트 연결
            targetHealth.OnHealthChanged.AddListener(UpdateBar);

            // 초기 수치 반영
            UpdateBar(targetHealth.CurrentHealth, targetHealth.MaxHealth);
            Debug.Log($"[HealthBar.cs] Connected to {targetHealth.gameObject.name}");
        }
        else
        {
            Debug.LogError("[HealthBar.cs] Target Health를 찾을 수 없습니다!");
        }
    }

    private void UpdateBar(float current, float max)
    {
        // 숫자 갱신
        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)} / {max}";
        }

        // 8단계 아이콘 갱신 (0=풀피, 7=사망)
        if (hpStateIcon != null && hpStateSprites != null && hpStateSprites.Length > 0)
        {
            int state = Mathf.Clamp(Mathf.RoundToInt(max - current), 0, 7);
            if (state < hpStateSprites.Length)
            {
                hpStateIcon.sprite = hpStateSprites[state];
            }
        }
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged.RemoveListener(UpdateBar);
        }
    }
}
