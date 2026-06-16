using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI keyText;

    private SkillData currentSkill;
    private float lastUseTime;
    private float cooldownDuration;

    public void Setup(string key)
    {
        if (keyText != null) keyText.text = key;
        SetSkill(null);
    }

    public void SetSkill(SkillData data)
    {
        currentSkill = data;
        if (data != null && iconImage != null)
        {
            iconImage.sprite = data.Icon;
            iconImage.enabled = true;
            cooldownDuration = data.Cooldown;
        }
        else if (iconImage != null)
        {
            iconImage.enabled = false;
        }
        
        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0;
    }

    public void StartCooldown()
    {
        lastUseTime = Time.time;
    }

    private void Update()
    {
        if (currentSkill == null || cooldownOverlay == null) return;

        float elapsed = Time.time - lastUseTime;
        if (elapsed < cooldownDuration)
        {
            cooldownOverlay.fillAmount = 1f - (elapsed / cooldownDuration);
        }
        else
        {
            cooldownOverlay.fillAmount = 0;
        }
    }
}
