using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;

    private Vector3 moveDirection;
    private float moveSpeed = 1f;
    private float lifetime = 1f;
    private float elapsed = 0f;

    private void Start()
    {
        BattleManager.Instance?.RegisterDamageText();
    }

    public void Setup(int amount, DamageStrength strength, DamageVisualType visualType, StatusEffectType effectType, float customLifetime = 1f)
    {
        tmpText.text = amount.ToString();
        lifetime = customLifetime;

        SetVisual(strength, visualType, effectType);
    }

    private void SetVisual(DamageStrength strength, DamageVisualType visualType, StatusEffectType effectType)
    {
        Color color = Color.white;
        int fontSize = 16;

        if (visualType == DamageVisualType.Normal)
        {
            (color, fontSize) = strength switch
            {
                DamageStrength.Low => (Color.white, 16),
                DamageStrength.Medium => (Color.yellow, 24),
                DamageStrength.High => (new Color(0.5f, 0, 0), 32),
                _ => (Color.white, 16)
            };
        }
        else if (visualType == DamageVisualType.StatusEffect)
        {
            (color, fontSize) = effectType switch
            {
                StatusEffectType.Poison => (new Color(0.6f, 1f, 0.6f), 16),
                StatusEffectType.Bleed => (new Color(1f, 0.2f, 0.2f), 20),
                StatusEffectType.Burn => (new Color(1f, 0.5f, 0f), 20),
                StatusEffectType.Shock => (new Color(0.5f, 0.5f, 1f), 20),
                _ => (Color.white, 16)
            };
        }
        else if (visualType == DamageVisualType.Heal)
        {
            color = new Color(0.2f, 1f, 0.2f);
            fontSize = 18;
        }

        tmpText.color = color;
        tmpText.fontSize = fontSize;

        moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1.0f, 0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.localPosition += moveDirection * moveSpeed * Time.deltaTime;

        Color c = tmpText.color;
        c.a = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        tmpText.color = c;

        if (elapsed >= lifetime)
        {
            BattleManager.Instance?.UnregisterDamageText();
            Destroy(gameObject);
        }
    }
}
