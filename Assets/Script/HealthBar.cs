using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Health 컴포넌트와 UI를 연결하여 숫자와 바 수치를 갱신함
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("연결 설정 (비어있으면 자동 검색)")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider hpSlider;
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

        // 2. 슬라이더 및 텍스트 자동 연결
        if (hpSlider == null) hpSlider = GetComponent<Slider>() ?? GetComponentInChildren<Slider>();
        if (hpText == null) hpText = GetComponentInChildren<TextMeshProUGUI>();

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
        // 숫자 갱신 (사용자 최우선 요청 사항)
        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)} / {max}";
        }

        // 슬라이더 바 갱신
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
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
