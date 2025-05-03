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
            damageText.Setup(amount, strength, lifetime); // 여기!
        }

        // 카메라 흔들기
        switch (strength)
        {
            case DamageStrength.Low:
                CameraManager.Instance.Shake(0.05f, 0.15f);
                break;
            case DamageStrength.Medium:
                CameraManager.Instance.Shake(0.15f, 0.25f);
                break;
            case DamageStrength.High:
                CameraManager.Instance.Shake(0.3f, 0.4f);
                break;
        }
    }


    private DamageStrength GetDamageStrength(int amount)
    {
        if (amount <= 3333)
            return DamageStrength.Low;
        else if (amount <= 6666)
            return DamageStrength.Medium;
        else
            return DamageStrength.High;
    }

}