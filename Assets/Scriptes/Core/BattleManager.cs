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

    [SerializeField] private GameObject characterPrefab; // 빈 캐릭터 프리팹
    [SerializeField] private Transform playerSpawnRoot; // 플레이어 표시할 부모
    [SerializeField] private Transform enemySpawnRoot; // 적 표시할 부모
    [SerializeField] private List<RectTransform> atbIcons; // ATB 아이콘 리스트
    [SerializeField] private TextMeshProUGUI targetDisplayText;
    
    public Character currentTargetEnemy;
    
    public Transform playerAdvancePoint;
    public Transform enemyAdvancePoint;
    private int activeDamageTexts = 0; // ⭐ 추가
    
    private readonly CharacterData[] deck1 = {
        new CharacterData("중독빌드1", 100, 10),
        new CharacterData("중독빌드2", 90, 15),
        new CharacterData("중독빌드3", 110, 8),
        new CharacterData("중독피니시", 80, 20)
    };

    private readonly CharacterData[] deck2 = {
        new CharacterData("감전빌드1", 100, 12),
        new CharacterData("감전빌드2", 95, 14),
        new CharacterData("감전빌드3", 105, 11),
        new CharacterData("감전피니시", 85, 18)
    };

    private readonly CharacterData[] deck3 = {
        new CharacterData("협공빌드1", 110, 9),
        new CharacterData("협공빌드2", 115, 7),
        new CharacterData("협공빌드3", 100, 10),
        new CharacterData("협공피니시", 90, 17)
    };

    private readonly CharacterData[] deck4 = {
        new CharacterData("화상빌드1", 95, 13),
        new CharacterData("화상빌드2", 100, 12),
        new CharacterData("화상빌드3", 105, 11),
        new CharacterData("화상피니시", 85, 19)
    };
    
    public void SetTargetEnemy(int enemyIndex)
    {
        if (enemyIndex >= 0 && enemyIndex < enemyTeam.Count)
        {
            currentTargetEnemy = enemyTeam[enemyIndex];
            UpdateTargetUI();
        }
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


    private void UpdateTargetUI()
    {
        if (targetDisplayText == null) return;

        if (currentTargetEnemy != null && currentTargetEnemy.isAlive)
            targetDisplayText.text = $"현재 대상: {currentTargetEnemy.characterName}";
        else
            targetDisplayText.text = "대상 없음";
    }

    
    private readonly Color[] playerColors = {
        new Color(0.5f, 0.7f, 1f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.2f, 0.5f, 0.9f),
        new Color(0.4f, 0.8f, 1f)
    };
    
    private readonly Color[] enemyColors = {
        new Color(1f, 0.5f, 0.5f),
        new Color(1f, 0.3f, 0.3f),
        new Color(0.9f, 0.2f, 0.2f),
        new Color(1f, 0.6f, 0.6f)
    };

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
        CreateCharacters();
        
        ValidateTarget();
        UpdateTargetUI();
    }

    private void CreateCharacters()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, playerSpawnRoot);
            Character c = go.GetComponent<Character>();
            c.characterName = $"아군_{i+1}";
            c.hp = 999999;
            c.maxHp = 100;
            c.speed = Random.Range(5, 20);
            c.atbIconTransform = atbIcons[i]; // 플레이어 0~3
            c.isEnemy = false;
            playerTeam.Add(c);

            SetupCharacterIcon(c, playerColors[i]);
            SetupATBIcon(c, playerColors[i]);

            // ⭐ 테스트용 중독 상태이상 추가
            c.ApplyStatusEffect(new StatusEffectData
            {
                type = StatusEffectType.Poison,
                potency = 5,    // 1턴당 5 데미지
                duration = 3,   // 3턴 지속
                tickType = StatusEffectTickType.EndOfTurn
            });
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, enemySpawnRoot);
            Character e = go.GetComponent<Character>();
            e.characterName = $"적_{i+1}";
            e.hp = 999999;
            e.maxHp = 100;
            e.speed = Random.Range(5, 20);
            e.atbIconTransform = atbIcons[i + 4];
            e.isEnemy = true;
            enemyTeam.Add(e);

            SetupCharacterIcon(e, enemyColors[i]);
            SetupATBIcon(e, enemyColors[i]);
        }

        StartCoroutine(InitializeCharacters());
    }

    
    private IEnumerator InitializeCharacters()
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
    }
    private void SetupATBIcon(Character c, Color color)
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
    
    private void SetupCharacterIcon(Character c, Color color)
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

    private void Update()
    {
        if (isSomeoneActing)
            return; // ⭐️ 누군가 행동 중이면 ATB 멈춤

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
                    nextActor.EnqueueBasicAttack(target);
            }
            else
            {
                if (currentTargetEnemy != null)
                    nextActor.EnqueueBasicAttack(currentTargetEnemy);
            }
        }
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
    public void SwitchDeck(int deckId) { }
    public void ToggleATBMode() { }
    public void ResetBattle() { }

    public void RequestSkillUse(int characterIndex, int skillIndex)
    {
        LogManager.Instance.Log($"{characterIndex + 1}번 캐릭터 스킬 {skillIndex + 1} 사용 요청");

        if (characterIndex >= 0 && characterIndex < playerTeam.Count)
        {
            Character c = playerTeam[characterIndex];
            if (c != null && c.isAlive && skillIndex >= 0 && skillIndex < c.skills.Count)
            {
                Skill skill = c.skills[skillIndex];
                if (skill != null && skill.skillType == SkillType.Active)
                {
                    // 스킬을 "큐에 삽입"한다
                    c.InsertSkillToQueue(() => skill.Activate(c, playerTeam, enemyTeam));
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
    
    
    
    private void ClearField()
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
        isSomeoneActing = false;
    }


    public void ChangeDeck(int deckId)
    {
        ClearField();

        CharacterData[] selectedDeck = null;

        switch (deckId)
        {
            case 0:
                selectedDeck = deck1;
                break;
            case 1:
                selectedDeck = deck2;
                break;
            case 2:
                selectedDeck = deck3;
                break;
            case 3:
                selectedDeck = deck4;
                break;
        }

        if (selectedDeck == null)
        {
            Debug.LogError("잘못된 덱 ID!");
            return;
        }

        for (int i = 0; i < selectedDeck.Length; i++)
        {
            GameObject go = Instantiate(characterPrefab, playerSpawnRoot);
            Character c = go.GetComponent<Character>();
            c.characterName = selectedDeck[i].characterName;
            c.hp = selectedDeck[i].hp;
            c.maxHp = selectedDeck[i].hp;
            c.speed = selectedDeck[i].speed;
            c.atbIconTransform = atbIcons[i];
            c.isEnemy = false;
            playerTeam.Add(c);

            SetupCharacterIcon(c, playerColors[i]);
            SetupATBIcon(c, playerColors[i]);
        }

        // 적도 4명 새로 소환 (간단하게 기본적)
        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, enemySpawnRoot);
            Character e = go.GetComponent<Character>();
            e.characterName = $"적_{i + 1}";
            e.hp = 100;
            e.maxHp = 100;
            e.speed = Random.Range(5, 20);
            e.atbIconTransform = atbIcons[i + 4];
            e.isEnemy = true;
            enemyTeam.Add(e);

            SetupCharacterIcon(e, enemyColors[i]);
            SetupATBIcon(e, enemyColors[i]);
        }

        StartCoroutine(InitializeCharacters());
        ValidateTarget();
        UpdateTargetUI();
    }
}
