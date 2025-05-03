using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpText;

    private Vector3 moveDirection;
    private float moveSpeed = 1f;
    private float lifetime = 1f;
    private float elapsed = 0f;

    public void Setup(int amount, DamageStrength strength, float customLifetime = 1f)
    {
        tmpText.text = amount.ToString();
        lifetime = customLifetime; // 여기!

        switch (strength)
        {
            case DamageStrength.Low:
                tmpText.color = Color.white;
                tmpText.fontSize = 12;
                moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1, 0);
                break;
            case DamageStrength.Medium:
                tmpText.color = Color.yellow;
                tmpText.fontSize = 20;
                moveDirection = new Vector3(Random.Range(-0.3f, 0.3f), 1.2f, 0);
                break;
            case DamageStrength.High:
                tmpText.color = new Color(0.5f, 0, 0);
                tmpText.fontSize = 32;
                moveDirection = new Vector3(Random.Range(-0.5f, 0.5f), 1.5f, 0);
                break;
        }
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
            Destroy(gameObject);
        }
    }
}