using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    public int characterIndex; // 0~3
    public int skillIndex;     // 0~1

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnSkillButtonClicked);
    }

    private void OnSkillButtonClicked()
    {
        BattleManager.Instance.RequestSkillUse(characterIndex, skillIndex);
    }
}