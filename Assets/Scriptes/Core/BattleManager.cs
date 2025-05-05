using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }
    public bool isATBMode = true;
    public bool isSomeoneActing = false;
    
    public List<Character> playerTeam = new List<Character>();
    public List<Character> enemyTeam = new List<Character>();

    public GameObject characterPrefab; // 빈 캐릭터 프리팹
    public Transform playerSpawnRoot; // 플레이어 표시할 부모
    public Transform enemySpawnRoot; // 적 표시할 부모
    public List<RectTransform> atbIcons; // ATB 아이콘 리스트
    public TextMeshProUGUI targetDisplayText;
    
    public Character currentTargetEnemy;
    
    public Transform playerAdvancePoint;
    public Transform enemyAdvancePoint;
    private int activeDamageTexts = 0; // ⭐ 추가
    private Queue<(Character, Skill)> skillRequestQueue = new Queue<(Character, Skill)>();
    public List<CharacterUI> characterUIs; 
    private List<Character> allCharacters = new List<Character>();
    
    public readonly Color[] playerColors = {
        new Color(0.5f, 0.7f, 1f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.2f, 0.5f, 0.9f),
        new Color(0.4f, 0.8f, 1f)
    };
    
    public readonly Color[] enemyColors = {
        new Color(1f, 0.5f, 0.5f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.9f, 0.2f, 0.2f),
        new Color(1f, 0.6f, 0.6f)
    };
    
    public void UpdateAllCharacterUIs()
    {
        foreach (var ui in characterUIs)
        {
            ui.UpdateUI();
        }
    }
    public void SetupCharacters(List<Character> characters)
    {
        allCharacters = characters;

        for (int i = 0; i < characters.Count && i < characterUIs.Count; i++)
        {
            characterUIs[i].Bind(characters[i]);
        }

        UpdateAllCharacterUIs();
    }
    
    public void RegisterDamageText()
    {
        activeDamageTexts++;
    }

    public void UnregisterDamageText()
    {
        activeDamageTexts = Mathf.Max(0, activeDamageTexts - 1);
    }

    public bool IsAnyDamageTextActive()
    {
        return activeDamageTexts > 0;
    }


    public void UpdateTargetUI()
    {
        if (targetDisplayText == null) return;

        if (currentTargetEnemy != null && currentTargetEnemy.isAlive)
            targetDisplayText.text = $"현재 대상: {currentTargetEnemy.characterName}";
        else
            targetDisplayText.text = "대상 없음";
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        DeckInitializer.InitializeDeck(this, 0); // 초기화 시 중독 덱 세팅
        ValidateTarget();
        UpdateTargetUI();
    }
    
    public IEnumerator InitializeCharacters()
    {
        yield return null; // ⭐️ 한 프레임 쉬어야 GridLayoutGroup 적용됨

        foreach (Character c in playerTeam)
        {
            c.originalPosition = c.transform.position;
        }

        foreach (Character e in enemyTeam)
        {
            e.originalPosition = e.transform.position;
        }
        SetupCharacters(playerTeam);
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

    private IEnumerator ExecuteSkill(Character caster, Skill skill)
    {
        isSomeoneActing = true;
        yield return caster.StartCoroutine(skill.Activate(caster, playerTeam, enemyTeam));
        isSomeoneActing = false;
    }

    
    private void Update()
    {
        if (isSomeoneActing)
            return;

        // ⭐️ 스킬 요청이 존재하면 우선 발동
        if (skillRequestQueue.Count > 0)
        {
            var (character, skill) = skillRequestQueue.Dequeue();
            character.actionQueue.Enqueue(() => ExecuteSkill(character, skill));
            return;
        }


        Character nextActor = null;
        float highestATB = 0f;

        // 플레이어 검사
        foreach (Character c in playerTeam)
        {
            if (!c.isAlive) continue;
            c.atbGauge += c.speed * c.atbSpeedMultiplier * Time.deltaTime;
            if (c.atbGauge >= 100f && c.atbGauge > highestATB)
            {
                highestATB = c.atbGauge;
                nextActor = c;
            }
        }

        // 적 검사
        foreach (Character e in enemyTeam)
        {
            if (!e.isAlive) continue;
            e.atbGauge += e.speed * e.atbSpeedMultiplier * Time.deltaTime;
            if (e.atbGauge >= 100f && e.atbGauge > highestATB)
            {
                highestATB = e.atbGauge;
                nextActor = e;
            }
        }

        if (nextActor != null)
        {
            nextActor.atbGauge = 0f;

            if (nextActor.isEnemy)
            {
                Character target = GetRandomPlayerTarget();
                if (target != null)
                {
                    // ⭐ 바로 발동
                    StartCoroutine(ExecuteBasicAttack(nextActor, target));
                }
            }
            else
            {
                if (currentTargetEnemy != null)
                {
                    // ⭐ 바로 발동
                    StartCoroutine(ExecuteBasicAttack(nextActor, currentTargetEnemy));
                }
            }
        }

    }

    private IEnumerator ExecuteBasicAttack(Character attacker, Character target)
    {
        isSomeoneActing = true;
        yield return attacker.StartCoroutine(attacker.BasicAttack(target));
        isSomeoneActing = false;
    }


    
    public Character GetFirstAliveEnemy()
    {
        foreach (Character e in enemyTeam)
        {
            if (e.isAlive)
                return e;
        }
        return null;
    }


    public void StartBattle() { }
    public void EndBattle() { }
    public void PlayerTurn(Character character) { }
    public void EnemyTurn(Character enemy) { }
    public void SwitchDeck(int deckId)
    {
        DeckInitializer.InitializeDeck(this, deckId);
    }
    public void ToggleATBMode() { }
    public void ResetBattle() { }

    public void RequestSkillUse(int characterIndex, int skillIndex)
    {
        if (characterIndex >= 0 && characterIndex < playerTeam.Count)
        {
            Character c = playerTeam[characterIndex];
            if (c != null && c.isAlive && skillIndex >= 0 && skillIndex < c.skills.Count)
            {
                Skill skill = c.skills[skillIndex];
                if (skill != null && skill.skillType == SkillType.Active)
                {
                    skillRequestQueue.Enqueue((c, skill));
                }
            }
        }
    }


    public Character GetRandomPlayerTarget()
    {
        List<Character> alivePlayers = playerTeam.FindAll(p => p.isAlive);

        if (alivePlayers.Count == 0)
            return null;

        return alivePlayers[Random.Range(0, alivePlayers.Count)];
    }
    
    public void SetCurrentTarget(Character newTarget)
    {
        if (newTarget != null && newTarget.isAlive && newTarget.isEnemy)
        {
            currentTargetEnemy = newTarget;
            LogManager.Instance.Log($"공격 대상 변경: {newTarget.characterName}");
            UpdateTargetUI();
        }
    }

    
    public void ValidateTarget()
    {
        if (currentTargetEnemy == null || !currentTargetEnemy.isAlive)
        {
            currentTargetEnemy = GetFirstAliveEnemy();
            LogManager.Instance.Log($"{currentTargetEnemy.characterName}로 타겟 변경");
        }
    }
    
    
    
    public void ClearField()
    {
        foreach (Character c in playerTeam)
        {
            if (c != null)
            {
                c.skills?.Clear();
                c.atbIconTransform = null;
                c.hp = 0;
                c.maxHp = 0;
                c.speed = 0;
                c.atbGauge = 0;
                c.atbSpeedMultiplier = 1f;
                c.activeStatusEffects.Clear();
            }
            Destroy(c.gameObject);
        }
    
        foreach (Character e in enemyTeam)
        {
            if (e != null)
            {
                e.skills?.Clear();
                e.atbIconTransform = null;
                e.hp = 0;
                e.maxHp = 0;
                e.speed = 0;
                e.atbGauge = 0;
                e.atbSpeedMultiplier = 1f;
                e.activeStatusEffects.Clear();
            }
            Destroy(e.gameObject);
        }

        playerTeam.Clear();
        enemyTeam.Clear();
        activeDamageTexts = 0;
        isSomeoneActing = false;
    }
}
