
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSetupManager
{
    private BattleManager _battleManager;
    
    public CharacterSetupManager(BattleManager battleManager)
    {
        _battleManager = battleManager;
    }

    public void SetupCharacters(List<Character> characters)
    {
        _battleManager.allCharacters = characters;

        for (int i = 0; i < characters.Count && i < _battleManager.characterUIs.Count; i++)
        {
            _battleManager.characterUIs[i].Bind(characters[i]);
        }

        UpdateAllCharacterUIs();
    }
    
    public void SetupATBIcon(Character c, Color color)
    {
        if (c.atbIconTransform != null)
        {
            Image iconImage = c.atbIconTransform.GetComponentInChildren<Image>();

            if (iconImage != null)
            {
                iconImage.color = color;
            }

            TMP_Text atbText = c.atbIconTransform.GetComponentInChildren<TMP_Text>();
            if (atbText != null)
            {
                atbText.text = c.characterName;
            }
        }
    }
    
    public void SetupCharacterIcon(Character c, Color color)
    {
        SpriteRenderer iconSprite = c.GetComponentInChildren<SpriteRenderer>();

        if (iconSprite != null)
        {
            iconSprite.color = color;
        }
        Transform nameTransform = c.transform.Find("Canvas/Name");
        if (nameTransform != null)
        {
            TMP_Text nameText = nameTransform.GetComponent<TMP_Text>();
            nameText.text = c.characterName;
        }
    }
    
    public void UpdateAllCharacterUIs()
    {
        foreach (var ui in _battleManager.characterUIs)
        {
            ui.UpdateUI();
        }
    }

}
