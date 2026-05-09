using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Health 컴포넌트와 UI Slider를 연결해주는 스크립트
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("연결 설정")]
    [SerializeField] private Health targetHealth;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText; // 숫자 표시용 텍스트 (선택사항)

    private void Start()
    {
        // 대상이 설정되지 않았다면 부모나 자신에게서 찾음
        if (targetHealth == null) targetHealth = GetComponentInParent<Health>();
        if (hpSlider == null) hpSlider = GetComponent<Slider>();

        if (targetHealth != null)
        {
            // 체력 변경 이벤트 구독
            targetHealth.OnHealthChanged.AddListener(UpdateBar);
            
            // 초기값 설정
            UpdateBar(targetHealth.CurrentHealth, targetHealth.MaxHealth);
        }
    }

    private void UpdateBar(float current, float max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }

        if (hpText != null)
        {
            hpText.text = $"{Mathf.CeilToInt(current)} / {max}";
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged.RemoveListener(UpdateBar);
        }
    }
}
