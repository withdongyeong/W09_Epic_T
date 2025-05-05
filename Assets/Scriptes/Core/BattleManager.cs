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
    private bool isChangingDeck = false;
    
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
    private int activeDamageTexts = 0;
    private Queue<(Character, Skill)> skillRequestQueue = new Queue<(Character, Skill)>();
    public List<CharacterUI> characterUIs; 
    private List<Character> allCharacters = new List<Character>();
        
    [Header("협공 QTE 설정")]
    [SerializeField] private float followUpAttackChance = 0.1f; //
    [SerializeField] private Vector3 followUpAttackOffset = new Vector3(0, 2f, 0); // 협공 시 위치 오프셋
    
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
    
     public bool ShouldTriggerFollowUpQTE()
    {
        return Random.value <= followUpAttackChance;
    }
// 대기 아군 협공 메서드
    public IEnumerator PerformWaitingAllyAssault(Character main, Character target)
    {
        List<Character> waitingAllies = new List<Character>();
    
        // 대기 중인 아군 찾기
        foreach (var ally in playerTeam)
        {
            if (ally != main && ally.isAlive && ally.waitingForAssault)
                waitingAllies.Add(ally);
        }
    
        if (waitingAllies.Count == 0)
            yield break;
    
        // 랜덤 선택
        Character assaulter = waitingAllies[Random.Range(0, waitingAllies.Count)];
    
        // 협공 위치로 이동
        Vector3 assaultPos = main.transform.position;
        Vector3 originalPos = assaulter.transform.position;
    
        // 이동
        float moveTime = 0.2f;
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            assaulter.transform.position = Vector3.Lerp(originalPos, assaultPos, elapsed / moveTime);
            yield return null;
        }
    
        // 공격
        Vector3 attackImpulse = (target.transform.position - assaulter.transform.position).normalized * 0.3f;
        yield return assaulter.MoveToImpulse(attackImpulse, 0.2f);
    
        assaulter.DealDamage(target);
    
        yield return new WaitForSeconds(0.3f);
    
        // 대기 위치로 복귀
        elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            assaulter.transform.position = Vector3.Lerp(assaultPos, originalPos, elapsed / moveTime);
            yield return null;
        }
    
        assaulter.transform.position = originalPos;
    }
    
// 아군 대기 위치 배치
    public void PrepareAssaultPositions(Character main, List<Character> allies)
    {
        // 협공 위치 계산
        Vector3 mainAttackPos = main.isEnemy ? enemyAdvancePoint.position : playerAdvancePoint.position;
    
        // 대기 위치 계산 (협공 위치보다 왼쪽, 세로로 배치)
        Vector3 waitingBasePos = mainAttackPos + new Vector3(-2f, 0f, 0f);
    
        // 대기 아군 배치
        for (int i = 0; i < allies.Count; i++)
        {
            allies[i].transform.position = waitingBasePos + new Vector3(0f, -0.7f * i, 0f);
        }
    }
// 지연된 랜덤 아군 협공 메서드
    public IEnumerator TriggerForcedAllyAssist(Character attacker, Character target)
    {
        // 덱 변경 중이면 즉시 중단
        if (isChangingDeck) yield break;
    
        // null 체크
        if (attacker == null || target == null) yield break;
        
        // 잠시 대기
        yield return new WaitForSeconds(0.5f);
    
        Character ally = GetRandomAliveAllyExcept(attacker);
    
        if (ally != null && target.isAlive)
        {
            yield return StartCoroutine(PerformFollowUpAttack(ally, target));
        }
    }
    
    
    public IEnumerator PerformChainAssault(Character main, Character target, List<Character> availableAllies)
{
    if (!target.isAlive)
        yield break;
    
    // 원래 위치 저장
    Vector3 originalMainPos = main.transform.position;
    Vector3 originalTargetPos = target.transform.position;
    
    // 협공 위치 계산
    Vector3 mainAttackPos = main.isEnemy ? enemyAdvancePoint.position : playerAdvancePoint.position;
    Vector3 targetPos = main.isEnemy ? playerAdvancePoint.position : enemyAdvancePoint.position;
    
    // 대기 위치 계산 (협공 위치보다 왼쪽, 세로로 배치)
    Vector3 waitingBasePos = mainAttackPos + new Vector3(-2f, 0f, 0f);
    
    // 메인 캐릭터 위치 이동
    main.transform.position = mainAttackPos;
    target.transform.position = targetPos;
    
    // 대기 아군 배치
    for (int i = 0; i < availableAllies.Count; i++)
    {
        availableAllies[i].transform.position = waitingBasePos + new Vector3(0f, -0.7f * i, 0f);
    }
    
    // 공격 실행
    Vector3 attackImpulse = (targetPos - mainAttackPos).normalized * 0.3f;
    yield return main.MoveToImpulse(attackImpulse, 0.2f);
    
    // 데미지 적용
    main.DealDamage(target);
    
    yield return new WaitForSeconds(0.3f);
    
    // 원래 위치로 복귀
    main.transform.position = originalMainPos;
    
    // 원래는 여기서 아군 위치도 복귀시켰지만,
    // 연속 협공 중에는 대기 위치에 유지하는 것이 좋음
    
    // 타겟 위치는 고정 플래그로 관리하므로 여기서 변경하지 않음
}

    
// 랜덤 아군 선택해 협공시키는 메서드
public IEnumerator TriggerRandomAllyAssault(Character main, Character target)
{
    if (playerTeam.Count <= 1 || !target.isAlive)
        yield break;
    
    // 랜덤 아군 선택
    List<Character> allAllies = new List<Character>();
    
    foreach (var ally in playerTeam)
    {
        if (ally != main && ally.isAlive)
            allAllies.Add(ally);
    }
    
    if (allAllies.Count == 0)
        yield break;
        
    Character chosenAlly = allAllies[Random.Range(0, allAllies.Count)];
    
    // 원래 위치 저장
    Vector3 originalAllyPos = chosenAlly.transform.position;
    
    // 협공 위치 계산
    Vector3 mainPos = main.transform.position;
    Vector3 targetPos = target.transform.position;
    
    // 대기 위치 (왼쪽에 배치)
    Vector3 waitingPos = mainPos + new Vector3(-1.5f, 0f, 0f);
    chosenAlly.transform.position = waitingPos;
    
    yield return new WaitForSeconds(0.5f);
    
    // 협공 위치로 이동
    Vector3 assaultPos = mainPos;
    chosenAlly.transform.position = assaultPos;
    
    // 공격 실행
    Vector3 attackImpulse = (targetPos - assaultPos).normalized * 0.3f;
    yield return chosenAlly.MoveToImpulse(attackImpulse, 0.2f);
    
    // 데미지 적용
    chosenAlly.DealDamage(target);
    
    yield return new WaitForSeconds(0.3f);
    
    // 원래 위치로 복귀
    chosenAlly.transform.position = originalAllyPos;
}
    public IEnumerator TriggerFollowUpQTE(Character attacker, Character target)
    {
        bool qteResult = false;
        bool qteCompleted = false;
        
        // QTE 시작
        QTEManager.Instance.StartQTE(QTEType.TimingButton, (result) => {
            qteResult = result;
            qteCompleted = true;
        });
        
        // QTE 완료 대기
        while (!qteCompleted)
            yield return null;
        
        LogManager.Instance.Log($"QTE {(qteResult? "성공":"실패")}");
        
        // 성공 시 협공 실행
        if (qteResult && target.isAlive)
        {
            Character ally = GetRandomAliveAllyExcept(attacker);
            if (ally != null)
            {
                yield return StartCoroutine(PerformFollowUpAttack(ally, target));
            }
        }
    }
    // 팀 전체 협공 확률 조정 메서드
    public void IncreaseFollowUpChance(int percentage)
    {
        followUpAttackChance += percentage / 100f;
        followUpAttackChance = Mathf.Min(followUpAttackChance, 1.0f); // 최대 100%로 제한
    }
    
    public void DecreaseFollowUpChance(int percentage)
    {
        followUpAttackChance -= percentage / 100f;
        followUpAttackChance = Mathf.Max(followUpAttackChance, 0.0f); // 최소 0%
    }
    
    // 연속 협공 실행 메서드
public IEnumerator PerformRapidTeamAssault(Character main, Character target, int hitCount)
{
    List<Character> allies = new List<Character>();
    foreach (var ally in playerTeam)
    {
        if (ally != main && ally.isAlive && ally.waitingForAssault)
            allies.Add(ally);
    }
    
    if (allies.Count == 0)
    {
        yield break;
    }
    
    // 협공 위치
    Vector3 mainPos = main.transform.position;
    Vector3 targetPos = target.transform.position;
    
    // 각 캐릭터별 공격 위치 계산 (부채꼴 형태)
    List<Vector3> attackPositions = new List<Vector3>();
    float radius = 1.0f;
    float angleStep = 180f / (allies.Count + 1);
    
    for (int i = 0; i < allies.Count; i++)
    {
        float angle = -90f + (i + 1) * angleStep;
        float x = mainPos.x + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = mainPos.y + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        attackPositions.Add(new Vector3(x, y, mainPos.z));
    }
    
    // 연타 공격 모션
    for (int hit = 0; hit < hitCount; hit++)
    {
        // 공격자 선택
        Character attacker = allies[Random.Range(0, allies.Count)];
        Vector3 attackPos = attackPositions[allies.IndexOf(attacker)];
        Vector3 originalPos = attacker.transform.position;
        
        // 공격 위치로 빠르게 이동
        float moveTime = 0.05f;
        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            attacker.transform.position = Vector3.Lerp(originalPos, attackPos, elapsed / moveTime);
            yield return null;
        }
        
        // 공격 실행
        Vector3 attackDir = (target.transform.position - attackPos).normalized * 0.2f;
        yield return attacker.MoveToImpulse(attackDir, 0.05f);
        
        attacker.DealDamage(target);
        
        // 원래 위치로 빠르게 복귀
        elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;
            attacker.transform.position = Vector3.Lerp(attackPos, originalPos, elapsed / moveTime);
            yield return null;
        }
        
        attacker.transform.position = originalPos;
        
        // 짧은 대기
        yield return new WaitForSeconds(0.02f);
    }
    
    yield return new WaitForSeconds(0.5f);
}

    public IEnumerator TriggerTeamAssault(Character attacker, Character target)
    {
        LogManager.Instance.Log($"{attacker.characterName}의 전체 협공 발동!");
    
        // 전체 협공 배치용 위치 오프셋
        Vector3[] positionOffsets = new Vector3[] {
            new Vector3(0, 1.5f, 0),
            new Vector3(0.5f, 1.0f, 0),
            new Vector3(-0.5f, 1.0f, 0)
        };
    
        int offsetIndex = 0;
        List<Character> assisters = new List<Character>();
    
        // 공격자를 제외한 모든 살아있는 아군 추가
        foreach (Character ally in playerTeam)
        {
            if (ally != attacker && ally.isAlive)
            {
                assisters.Add(ally);
                if (assisters.Count >= 3) break; // 최대 3명까지만
            }
        }
    
        if (assisters.Count == 0)
        {
            LogManager.Instance.Log("협공할 아군이 없습니다.");
            yield break;
        }
    
        // 각 아군마다 협공 실행
        foreach (Character assister in assisters)
        {
            // 원래 위치 저장
            Vector3 originalAssisterPos = assister.transform.position;
        
            // 협공 위치 계산 (공격자 기준 오프셋)
            Vector3 assaultPos = attacker.transform.position + positionOffsets[offsetIndex];
            offsetIndex = (offsetIndex + 1) % positionOffsets.Length;
        
            // 위치 이동
            assister.transform.position = assaultPos;
        
            // 공격 애니메이션 및 데미지 적용
            Vector3 attackImpulse = (target.transform.position - assister.transform.position).normalized * 0.3f;
            yield return assister.MoveToImpulse(attackImpulse, 0.2f);
        
            assister.DealDamage(target);
        
            yield return new WaitForSeconds(0.3f);
        
            // 원래 위치로 복귀
            assister.transform.position = originalAssisterPos;
        }
    
        yield return new WaitForSeconds(0.5f);
    }
    
    private IEnumerator PerformFollowUpAttack(Character attacker, Character target)
    {
        // null 체크
        if (attacker == null || target == null || !attacker.isAlive || !target.isAlive)
            yield break;
    
        // 원래 위치 저장
        Vector3 originalAttackerPos = attacker.transform.position;
        Vector3 originalTargetPos = target.transform.position;
    
        // 협공 위치 계산
        Vector3 myAdvancePos = playerAdvancePoint.position + followUpAttackOffset;
        Vector3 enemyAdvancePos = enemyAdvancePoint.position + followUpAttackOffset;
    
        // 이동 전 null 체크
        if (attacker == null || target == null) yield break;
    
        // 위치 이동
        attacker.transform.position = attacker.isEnemy ? enemyAdvancePos : myAdvancePos;
        target.transform.position = attacker.isEnemy ? myAdvancePos : enemyAdvancePos;
    
        CameraManager.Instance.FocusBetweenPoints(attacker.transform.position, target.transform.position, 0.1f, 3.5f);
    
        yield return new WaitForSeconds(0.1f);
    
        // 공격 전 null 체크
        if (attacker == null || target == null) yield break;
    
        // 공격 모션
        Vector3 attackImpulse = (target.transform.position - attacker.transform.position).normalized * 0.5f;
        yield return attacker.MoveToImpulse(attackImpulse, 0.3f);
    
        // 데미지 전 null 체크
        if (attacker == null || target == null) yield break;
    
        // 데미지 처리
        attacker.DealDamage(target);
    
        yield return new WaitForSeconds(0.7f);
    
        // 마무리 전 null 체크
        if (attacker == null) yield break;
    
        attacker.ReduceSkillCooldowns();
    
        // 원위치 전 null 체크
        if (attacker != null && attacker.transform != null)
            attacker.transform.position = originalAttackerPos;
        
        if (target != null && target.transform != null)
            target.transform.position = originalTargetPos;
    
        CameraManager.Instance.ZoomOut(0.3f);
    }

    
    private Character GetRandomAliveAllyExcept(Character attacker)
    {
        List<Character> aliveAllies = new List<Character>();
    
        foreach (Character ally in playerTeam)
        {
            if (ally.isAlive && ally != attacker)
                aliveAllies.Add(ally);
        }
    
        if (aliveAllies.Count > 0)
            return aliveAllies[Random.Range(0, aliveAllies.Count)];
        
        return null;
    }
    
    
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
    
    // Character 초기화 시 패시브 스킬 적용
    public IEnumerator InitializeCharacters()
    {
        yield return null; // 한 프레임 쉬어야 GridLayoutGroup 적용됨

        foreach (Character c in playerTeam)
        {
            c.originalPosition = c.transform.position;
            c.ApplyPassiveSkills(); // 패시브 스킬 적용
        }

        foreach (Character e in enemyTeam)
        {
            e.originalPosition = e.transform.position;
            e.ApplyPassiveSkills(); // 패시브 스킬 적용
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
        // 덱 변경 중 표시
        isChangingDeck = true;
    
        // 진행 중인 모든 코루틴 정지
        StopAllCoroutines();
    
        // 액션 중 표시 초기화
        isSomeoneActing = false;
    
        // 덱 초기화
        DeckInitializer.InitializeDeck(this, deckId);
    
        // 덱 변경 완료
        isChangingDeck = false;
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
    
    
    
    // BattleManager의 ClearField 메서드 수정
    public void ClearField()
    {
        // 진행 중인 모든 코루틴 정지
        StopAllCoroutines();
    
        // 액션 중 표시 초기화
        isSomeoneActing = false;
    
        // 나머지 로직
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
                c.isUnderAssault = false;
                c.waitingForAssault = false;
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
                e.isUnderAssault = false;
                e.waitingForAssault = false;
            }
            Destroy(e.gameObject);
        }

        playerTeam.Clear();
        enemyTeam.Clear();
        activeDamageTexts = 0;
    }
}
