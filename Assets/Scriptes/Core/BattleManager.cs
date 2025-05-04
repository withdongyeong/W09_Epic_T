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
    private Queue<(Character, Skill)> skillRequestQueue = new Queue<(Character, Skill)>();
    [SerializeField] private List<CharacterUI> characterUIs; 
    private List<Character> allCharacters = new List<Character>();
    
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
        CreatePoisonDeckCharacters(); // 초기화 시 중독 덱 세팅
        ValidateTarget();
        UpdateTargetUI();
    }

    private void CreatePoisonDeckCharacters()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, playerSpawnRoot);
            Character c = go.GetComponent<Character>();
            switch (i)
            {
                case 0:
                    c.characterName = "중독빌드1";
                    break;
                case 1:
                    c.characterName = "중독빌드2";
                    break;
                case 2:
                    c.characterName = "중독빌드1";
                    break;
                case 3:
                    c.characterName = "중독피니시";
                    break;
                default:
                    c.characterName = $"아군_{i+1}";
                    break;
            }

            c.hp = 999999;
            c.maxHp = 100;
            c.speed = Random.Range(5, 20);
            c.atbIconTransform = atbIcons[i];
            c.isEnemy = false;
            playerTeam.Add(c);

            SetupCharacterIcon(c, playerColors[i]);
            SetupATBIcon(c, playerColors[i]);

            // ⭐ 캐릭터별 스킬 세팅
            if (i == 0) // 빌드업1
            {
                var skill1 = SkillDatabase.CreatePoisonSkill(
                    name: "단일중독(약)",
                    damage: 5,
                    poisonPower: 3,
                    poisonDuration: 3, // 3회
                    cooldownTurns: 2
                );
                var skill2 = SkillDatabase.CreatePoisonSkill(
                    name: "단일중독(강)",
                    damage: 8,
                    poisonPower: 5,
                    poisonDuration: 1, // 1회
                    cooldownTurns: 3
                );
                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }
            else if (i == 1) // 빌드업2
            {
                var skill1 = SkillDatabase.CreatePoisonSkill(
                    name: "단일중독(약)",
                    damage: 4,
                    poisonPower: 2,
                    poisonDuration: 4, // 4회
                    cooldownTurns: 2
                );
                var skill2 = SkillDatabase.CreatePoisonSkill(
                    name: "단일중독(강)",
                    damage: 6,
                    poisonPower: 4,
                    poisonDuration: 2, // 2회
                    cooldownTurns: 3
                );
                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }
            else if (i == 2) // 빌드업3
            {
                var skill1 = SkillDatabase.CreatePoisonSkill(
                    name: "광역중독(약)",
                    damage: 4,
                    poisonPower: 2,
                    poisonDuration: 3, // 3회
                    cooldownTurns: 3
                );
                skill1.isAreaAttack = true; // ⭐ 전체 공격 설정

                var skill2 = SkillDatabase.CreatePoisonSkill(
                    name: "광역중독(강)",
                    damage: 6,
                    poisonPower: 5,
                    poisonDuration: 2, // 2회
                    cooldownTurns: 4
                );
                skill2.isAreaAttack = true; // ⭐ 전체 공격 설정

                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }
            else if (i == 3) // 피니시 캐릭터
            {
                var skill1 = SkillDatabase.CreatePoisonSkill(
                    name: "광역중독(약)",
                    damage: 4,
                    poisonPower: 3,
                    poisonDuration: 2, // 2회
                    cooldownTurns: 2
                );
                skill1.isAreaAttack = true; // ⭐ 전체 공격 설정

                var skill2 = SkillDatabase.CreatePoisonFinishSkill(
                    name: "독 압축 폭발",
                    firstHitDamage: 7,
                    compressDamage: 15,
                    cooldownTurns: 4
                );

                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }

        }

        // (적군 생성은 기존 코드 유지)
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

private void CreateShockDeckCharacters()
{
    for (int i = 0; i < 4; i++)
    {
        GameObject go = Instantiate(characterPrefab, playerSpawnRoot);
        Character c = go.GetComponent<Character>();
        switch (i)
        {
            case 0:
                c.characterName = "감전빌드1";
                break;
            case 1:
                c.characterName = "감전빌드2";
                break;
            case 2:
                c.characterName = "감전빌드3";
                break;
            case 3:
                c.characterName = "감전피니시";
                break;
            default:
                c.characterName = $"아군_{i + 1}";
                break;
        }

        c.hp = 999999;
        c.maxHp = 999999;
        c.speed = Random.Range(5, 20);
        c.atbIconTransform = atbIcons[i];
        c.isEnemy = false;
        playerTeam.Add(c);

        SetupCharacterIcon(c, playerColors[i]);
        SetupATBIcon(c, playerColors[i]);

        // ⭐ 캐릭터별 스킬 세팅
        if (i == 0) // 빌드업1
        {
            var skill1 = SkillDatabase.CreateShockSkill(
                name: "단일감전(약)",
                damagePerHit: 3,
                hitCount: 2,
                shockPower: 1,
                shockDuration: 1,
                cooldownTurns: 2
            );
            var skill2 = SkillDatabase.CreateShockSkill(
                name: "단일감전(강)",
                damagePerHit: 4,
                hitCount: 2,
                shockPower: 1,
                shockDuration: 2,
                cooldownTurns: 3
            );
            c.skills.Add(skill1);
            c.skills.Add(skill2);
        }
        else if (i == 1) // 빌드업2
        {
            var skill1 = SkillDatabase.CreateShockSkill(
                name: "광역감전(약)",
                damagePerHit: 2,
                hitCount: 2,
                shockPower: 2,
                shockDuration: 2,
                cooldownTurns: 3
            );
            skill1.isAreaAttack = true;

            var skill2 = SkillDatabase.CreateShockSkill(
                name: "광역감전(강)",
                damagePerHit: 3,
                hitCount: 2,
                shockPower: 2,
                shockDuration: 3,
                cooldownTurns: 4
            );
            skill2.isAreaAttack = true;

            c.skills.Add(skill1);
            c.skills.Add(skill2);
        }
        else if (i == 2) // 빌드업3
        {
            var skill1 = SkillDatabase.CreateShockSkill(
                name: "단일감전(다타)",
                damagePerHit: 2,
                hitCount: 3,
                shockPower: 3,
                shockDuration: 2,
                cooldownTurns: 2
            );
            var skill2 = SkillDatabase.CreateShockSkill(
                name: "광역감전(다타)",
                damagePerHit: 2,
                hitCount: 3,
                shockPower: 3,
                shockDuration: 3,
                cooldownTurns: 4
            );
            skill2.isAreaAttack = true;

            c.skills.Add(skill1);
            c.skills.Add(skill2);
        }
        else if (i == 3) // 감전 피니시 캐릭터
        {
            var skill1 = SkillDatabase.CreateShockSkill(
                name: "광역감전 연격",
                damagePerHit: 2,
                hitCount: 5,
                shockPower: 1,
                shockDuration: 5,
                cooldownTurns: 3
            );
            skill1.isAreaAttack = true;

            var skill2 = SkillDatabase.CreateShockFinishSkill(
                name: "감전 30연격",
                firstHitDamage: 3,
                repeatDamage: 1,
                repeatCount: 29,
                shockPower: 1,
                shockDuration: 5,
                cooldownTurns: 5
            );
            skill2.isAreaAttack = true;

            c.skills.Add(skill1);
            c.skills.Add(skill2);
        }
    }

    // 적군 생성
    for (int i = 0; i < 4; i++)
    {
        GameObject go = Instantiate(characterPrefab, enemySpawnRoot);
        Character e = go.GetComponent<Character>();
        e.characterName = $"적_{i + 1}";
        e.hp = 999999;
        e.maxHp = 999999;
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
        SetupCharacters(playerTeam);
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
    public void SwitchDeck(int deckId) { }
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

        if (deckId == 0)
        {
            CreatePoisonDeckCharacters(); // 0번 덱은 중독덱 전용
            ValidateTarget();
            UpdateTargetUI();
            return;
        }
        if (deckId == 1)
        {
            CreateShockDeckCharacters();
            ValidateTarget();
            UpdateTargetUI();
            return;
        }
        if (deckId == 3)
        {
            CreateBurnDeckCharacters();
            ValidateTarget();
            UpdateTargetUI();
            return;
        }


        CharacterData[] selectedDeck = deckId switch
        {
            2 => deck3,
            _ => null
        };

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

        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, enemySpawnRoot);
            Character e = go.GetComponent<Character>();
            e.characterName = $"적_{i+1}";
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
    
    private void CreateBurnDeckCharacters()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, playerSpawnRoot);
            Character c = go.GetComponent<Character>();

            c.characterName = i switch
            {
                0 => "화상빌드1",
                1 => "화상빌드2",
                2 => "화상빌드3",
                3 => "화상피니시",
                _ => $"아군_{i + 1}"
            };

            c.hp = 999999;
            c.maxHp = 999999;
            c.speed = Random.Range(5, 20);
            c.atbIconTransform = atbIcons[i];
            c.isEnemy = false;
            playerTeam.Add(c);

            SetupCharacterIcon(c, playerColors[i]);
            SetupATBIcon(c, playerColors[i]);

            if (i < 3) // 빌드업용
            {
                var skill1 = SkillDatabase.CreateBurnSkill(
                    name: "단일화상",
                    damage: 4,
                    burnPower: 2,
                    burnDuration: 3,
                    cooldownTurns: 2
                );

                var skill2 = SkillDatabase.CreateBurnSkill(
                    name: "광역화상",
                    damage: 3,
                    burnPower: 1,
                    burnDuration: 4,
                    cooldownTurns: 3,
                    isAreaAttack: true
                );

                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }
            else // 피니시용
            {
                var skill1 = SkillDatabase.CreateBurnSkill(
                    name: "광역화상",
                    damage: 3,
                    burnPower: 2,
                    burnDuration: 3,
                    cooldownTurns: 3,
                    isAreaAttack: true
                );

                var skill2 = SkillDatabase.CreateBurnFinishSkill(
                    name: "화상폭발",
                    firstHitDamage: 5,
                    burnPower: 2,
                    cooldownTurns: 5
                );

                c.skills.Add(skill1);
                c.skills.Add(skill2);
            }
        }

        // 적 생성은 기존 코드와 동일
        for (int i = 0; i < 4; i++)
        {
            GameObject go = Instantiate(characterPrefab, enemySpawnRoot);
            Character e = go.GetComponent<Character>();
            e.characterName = $"적_{i + 1}";
            e.hp = 999999;
            e.maxHp = 999999;
            e.speed = Random.Range(5, 20);
            e.atbIconTransform = atbIcons[i + 4];
            e.isEnemy = true;
            enemyTeam.Add(e);

            SetupCharacterIcon(e, enemyColors[i]);
            SetupATBIcon(e, enemyColors[i]);
        }

        StartCoroutine(InitializeCharacters());
    }

}
