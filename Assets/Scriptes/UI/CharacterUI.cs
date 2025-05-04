using TMPro;
using UnityEngine;

public class CharacterUI : MonoBehaviour
{
    [SerializeField] private TMP_Text characterNameText;

    [Header("Skill 1")]
    [SerializeField] private TMP_Text skill1NameText;
    [SerializeField] private TMP_Text skill1CooldownText;

    [Header("Skill 2")]
    [SerializeField] private TMP_Text skill2NameText;
    [SerializeField] private TMP_Text skill2CooldownText;

    private Character targetCharacter;

    public void Bind(Character character)
    {
        targetCharacter = character;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (targetCharacter == null) return;

        if (characterNameText != null)
            characterNameText.text = targetCharacter.characterName;

        if (targetCharacter.skills.Count > 0)
        {
            if (skill1NameText != null)
                skill1NameText.text = targetCharacter.skills[0].skillName;
            if (skill1CooldownText != null)
                skill1CooldownText.text = targetCharacter.skills[0].currentCooldown > 0 ? $"CD: {targetCharacter.skills[0].currentCooldown}" : "";
        }

        if (targetCharacter.skills.Count > 1)
        {
            if (skill2NameText != null)
                skill2NameText.text = targetCharacter.skills[1].skillName;
            if (skill2CooldownText != null)
                skill2CooldownText.text = targetCharacter.skills[1].currentCooldown > 0 ? $"CD: {targetCharacter.skills[1].currentCooldown}" : "";
        }
    }
}