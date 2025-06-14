using UnityEngine;

public class DamageTextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject damageTextPrefab;

    public void ShowDamage(int amount, float lifetime = 0.8f)
    {
        if (damageTextPrefab == null) return;

        DamageStrength strength = GetDamageStrength(amount);

        GameObject obj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity, transform);
        DamageText damageText = obj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Setup(amount, strength, DamageVisualType.Normal, StatusEffectType.None, lifetime);
        }

        ApplyShake(strength);
    }

    public void ShowStatusEffectDamage(int amount, StatusEffectType effectType, float lifetime = 0.8f)
    {
        if (damageTextPrefab == null) return;

        DamageStrength strength = GetDamageStrength(amount);

        Vector3 offset = new Vector3(0.5f, 0.5f, 0f); // 살짝 오른쪽 위로
        GameObject obj = Instantiate(damageTextPrefab, transform.position + offset, Quaternion.identity, transform);

        DamageText damageText = obj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Setup(amount, strength, DamageVisualType.StatusEffect, effectType, lifetime);
        }

        ApplyShake(strength);
    }


    public void ShowHeal(int amount, StatusEffectType effectType, float lifetime = 0.8f)
    {
        if (damageTextPrefab == null) return;

        GameObject obj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity, transform);
        DamageText damageText = obj.GetComponent<DamageText>();
        if (damageText != null)
        {
            damageText.Setup(amount, DamageStrength.Low, DamageVisualType.Heal, effectType, lifetime);
        }

        // 회복은 따로 카메라 흔들 필요 없음
    }

    private DamageStrength GetDamageStrength(int amount)
    {
        if (amount <= 10)
            return DamageStrength.Low;
        else if (amount <= 50)
            return DamageStrength.Medium;
        else
            return DamageStrength.High;
    }

    private void ApplyShake(DamageStrength strength)
    {
        switch (strength)
        {
            case DamageStrength.Low:
                CameraManager.Instance.Shake(0.05f, 0.15f);
                break;
            case DamageStrength.Medium:
                CameraManager.Instance.Shake(0.1f, 0.25f);
                break;
            case DamageStrength.High:
                CameraManager.Instance.Shake(0.2f, 0.4f);
                break;
        }
    }
}
