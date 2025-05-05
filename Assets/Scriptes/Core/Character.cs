using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Character : MonoBehaviour
{
    public string characterName;
    public int hp;
    public int maxHp;
    public int speed;
    public bool waitingForAssault = false;

    public List<Skill> skills = new List<Skill>();
    public List<StatusEffectData> activeStatusEffects = new List<StatusEffectData>();
    
    public RectTransform atbIconTransform;
    public bool isAlive => hp > 0;
    public Vector3 originalPosition;

    public TextMeshProUGUI atbText;
    public float atbGauge = 0f;
    public float atbSpeedMultiplier = 1f;
    public bool isEnemy;
    public bool isUnderAssault = false;

    private bool isActing = false;
    private bool isAliveActionLoop = true;
    public Queue<Func<IEnumerator>> actionQueue = new Queue<Func<IEnumerator>>();
    [SerializeField] private TMP_Text statusEffectText; // 상태이상 표시용
    
    public int basicAttackBonus = 0;     // 기본 공격력 증가 퍼센트
    public int doubleAttackChance = 0;   // 2번 공격 확률(%)
    
    private static readonly Dictionary<StatusEffectType, (string emoji, string colorHex)> StatusEffectVisuals = new()
    {
        { StatusEffectType.Poison, ("☠️", "#80FF80") },
        { StatusEffectType.Bleed, ("🩸", "#FF4040") },
        { StatusEffectType.Burn, ("🔥", "#FFA500") },
        { StatusEffectType.Shock, ("⚡", "#8080FF") },
        { StatusEffectType.HealOverTime, ("💚", "#40FF40") },
        { StatusEffectType.Shield, ("🛡️", "#00FFFF") },
        { StatusEffectType.SpeedUp, ("🪽", "#FFD700") },     // 속도 증가: 날개 + 금색
        { StatusEffectType.TeamworkUp, ("👊", "#FF69B4") }    // 협공 증가: 단검 + 핑크색
    };


    // 패시브 스킬 효과 적용 메서드
    public void ApplyPassiveSkills()
    {
        foreach (var skill in skills)
        {
            if (skill.skillType == SkillType.Passive && skill.onEquip != null)
            {
                skill.onEquip(this);
            }
        }
    }
    
    public void UpdateStatusEffectUI()
    {
        if (statusEffectText == null) return;

        string result = "";

        foreach (var effect in activeStatusEffects)
        {
            if (effect.stack <= 0) continue;

            if (StatusEffectVisuals.TryGetValue(effect.type, out var visual))
            {
                result += $"<color={visual.colorHex}>{visual.emoji} {effect.power}/{effect.stack}</color>\n";

            }
            else
            {
                // 정의 안된 경우
                result += $"{effect.type} {effect.power}/{effect.stack}\n";
            }
        }

        statusEffectText.text = result.TrimEnd(); // 마지막 개행 제거
    }

    private void Start()
    {
        StartCoroutine(ActionLoop());
    }

    private void Update()
    {
        UpdateATBIcon();
    }

    private void OnDestroy()
    {
        isAliveActionLoop = false;
    }

    public void UpdateATBIcon()
    {
        if (atbIconTransform != null)
        {
            float percent = atbGauge / 100f;
            float moveRange = 500f;

            atbIconTransform.anchoredPosition = new Vector2(
                atbIconTransform.anchoredPosition.x,
                -percent * moveRange
            );
        }
    }

    private void OnMouseDown()
    {
        if (isEnemy && BattleManager.Instance != null)
        {
            BattleManager.Instance.SetCurrentTarget(this);
        }
    }

    private IEnumerator ActionLoop()
    {
        while (isAliveActionLoop)
        {
            if (!isActing && actionQueue.Count > 0)
            {
                var action = actionQueue.Dequeue();
                isActing = true;
                BattleManager.Instance.isSomeoneActing = true;
                yield return StartCoroutine(action());
                BattleManager.Instance.isSomeoneActing = false;
                isActing = false;
            }
            else
            {
                yield return null;
            }
        }
    }

    public IEnumerator BasicAttack(Character target)
    {
        if (target == null || !target.isAlive)
            yield break;

        Vector3 myAdvancePos = BattleManager.Instance.playerAdvancePoint.position;
        Vector3 enemyAdvancePos = BattleManager.Instance.enemyAdvancePoint.position;

        transform.position = isEnemy ? enemyAdvancePos : myAdvancePos;
        target.transform.position = isEnemy ? myAdvancePos : enemyAdvancePos;

        CameraManager.Instance.FocusBetweenPoints(transform.position, target.transform.position, 0.1f, 3.5f);

        yield return new WaitForSeconds(0.1f);

        Vector3 attackImpulse = (target.transform.position - transform.position).normalized * 0.5f;
        yield return MoveToImpulse(attackImpulse, 0.3f);

        DealDamage(target);

        yield return new WaitForSeconds(0.7f);

        yield return StartCoroutine(ApplyStatusEffectsEndOfTurn());

        yield return StartCoroutine(WaitForAllDamageTexts());

        // 협공 QTE 발동 (플레이어 공격 후 + 타겟이 살아있을 때만)
        if (!isEnemy && target.isAlive && BattleManager.Instance.ShouldTriggerFollowUpQTE())
        {
            yield return StartCoroutine(BattleManager.Instance.TriggerFollowUpQTE(this, target));
        }
        
        if (!isUnderAssault)
            transform.position = originalPosition;
        
        if (!target.isUnderAssault)
            target.transform.position = target.originalPosition;

        ReduceSkillCooldowns();

        BattleManager.Instance.UpdateAllCharacterUIs();
        CameraManager.Instance.ZoomOut(0.3f);
    }
    
    public void ReduceSkillCooldowns()
    {
        foreach (var skill in skills)
        {
            if (skill.currentCooldown > 0)
                skill.currentCooldown--;
        }
    }
    public IEnumerator WaitForAllDamageTexts()
    {
        float timeout = 2f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            if (!BattleManager.Instance.IsAnyDamageTextActive())
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }


    public void InsertSkillToQueue(Func<IEnumerator> skillAction)
    {
        Queue<Func<IEnumerator>> newQueue = new Queue<Func<IEnumerator>>();
        newQueue.Enqueue(skillAction);

        while (actionQueue.Count > 0)
        {
            newQueue.Enqueue(actionQueue.Dequeue());
        }

        actionQueue = newQueue;
    }

    public void ApplyStatusEffect(StatusEffectData statusEffect)
    {
        var existing = activeStatusEffects.Find(e => e.type == statusEffect.type);
        if (existing != null)
        {
            existing.power += statusEffect.power;
            existing.stack += statusEffect.stack;
        
            // TeamworkUp 타입인 경우 power 값 100으로 제한
            if (existing.type == StatusEffectType.TeamworkUp)
            {
                existing.power = Mathf.Min(existing.power, 100);
            }
        }
        else
        {
            // 새로 추가하는 경우 TeamworkUp 타입이면 제한
            if (statusEffect.type == StatusEffectType.TeamworkUp)
            {
                statusEffect.power = Mathf.Min(statusEffect.power, 100);
            }
        
            activeStatusEffects.Add(new StatusEffectData
            {
                type = statusEffect.type,
                power = statusEffect.power,
                stack = statusEffect.stack,
                tickType = statusEffect.tickType,
                isBuff = statusEffect.isBuff
            });
        }

        UpdateStatusEffectUI();
    }

    public IEnumerator ApplyStatusEffectsEndOfTurn()
    {
        List<StatusEffectData> expiredEffects = new List<StatusEffectData>();

        foreach (var effect in activeStatusEffects)
        {
            if (effect.tickType != StatusEffectTickType.EndOfTurn)
                continue;

            // ⭐ 상태이상 효과를 적용
            yield return StartCoroutine(ApplyStatusEffectImpact(effect));

            // 지속시간 감소
            effect.stack--;
            if (effect.stack <= 0)
            {
                expiredEffects.Add(effect);
            }
        }
        UpdateStatusEffectUI();

        // 만료된 상태이상 제거
        foreach (var expired in expiredEffects)
        {
            RemoveStatusEffect(expired.type);
        }
    }

    // 상태이상 효과 적용 메서드 확장
    private IEnumerator ApplyStatusEffectImpact(StatusEffectData effect)
    {
        switch (effect.type)
        {
            case StatusEffectType.Poison:
            case StatusEffectType.Bleed:
            case StatusEffectType.Burn:
            case StatusEffectType.Shock:
                ApplyDamage(effect.power, StatusEffectSource.StatusDamage, effect.type);
                yield return new WaitForSeconds(0.3f);
                break;

            case StatusEffectType.HealOverTime:
                ApplyHeal(effect.power, effect.type);
                yield return new WaitForSeconds(0.3f);
                break;

            case StatusEffectType.Shield:
                ApplyShield(effect.power);
                yield return new WaitForSeconds(0.3f);
                break;
            
            case StatusEffectType.SpeedUp:
            case StatusEffectType.TeamworkUp:
                // 버프는 턴 종료 시 지속시간만 감소 (효과는 적용/제거 시점에 처리)
                yield return new WaitForSeconds(0.1f);
                break;
            
            default:
                yield return null;
                break;
        }
    }



    // Character 클래스에 버프 제거 로직 추가
    public void RemoveStatusEffect(StatusEffectType type)
    {
        var effect = GetStatusEffect(type);
    
        if (effect != null)
        {
            // 버프 효과 제거 처리
            if (effect.isBuff)
            {
                switch (type)
                {
                    case StatusEffectType.SpeedUp:
                        // 속도 증가 효과 제거
                        atbSpeedMultiplier = Mathf.Max(1.0f, atbSpeedMultiplier - (effect.power / 100f));
                        break;
                    
                    case StatusEffectType.TeamworkUp:
                        // 협공 확률 증가 효과 제거
                        BattleManager.Instance.DecreaseFollowUpChance(effect.power);
                        break;
                }
            }
        }
        
        activeStatusEffects.RemoveAll(e => e.type == type);
        UpdateStatusEffectUI();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return activeStatusEffects.Exists(e => e.type == type);
    }

    public StatusEffectData GetStatusEffect(StatusEffectType type)
    {
        return activeStatusEffects.Find(e => e.type == type);
    }

    public IEnumerator MoveToImpulse(Vector3 impulse, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + impulse;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        transform.position = endPos;
    }

// Character 클래스의 DealDamage 메서드 재정의 (더 강력하게)
    public void DealDamage(Character target)
    {
        // 기본 데미지 계산
        int baseDamage = Random.Range(5, 11);
    
        // 패시브 공격력 보너스 적용
        if (basicAttackBonus > 0)
        {
            float bonusMultiplier = 1 + (basicAttackBonus / 100f);
            baseDamage = Mathf.RoundToInt(baseDamage * bonusMultiplier);
        }
    
        // 데미지 적용
        target.ApplyDamage(baseDamage);
        
    
        // 2회 공격 확률 체크
        if (doubleAttackChance > 0)
        {
            int roll = Random.Range(1, 101);
        
            if (roll <= doubleAttackChance)
            {
                StartCoroutine(DelayedSecondAttack(target, baseDamage));
            }
        }
    }
    
    private IEnumerator DelayedSecondAttack(Character target, int firstDamage)
    {
        yield return new WaitForSeconds(0.3f);
        
        // 두 번째 공격은 첫 번째의 70-90% 데미지
        float damageMultiplier = Random.Range(0.7f, 0.9f);
        int secondDamage = Mathf.RoundToInt(firstDamage * damageMultiplier);
        
        target.ApplyDamage(secondDamage);
    }

    public void ApplyDamage(int amount, StatusEffectSource source = StatusEffectSource.DirectAttack, StatusEffectType effectType = StatusEffectType.None)
    {
        // 감전 추가피해 체크
        if (HasStatusEffect(StatusEffectType.Shock))
        {
            StatusEffectData shock = GetStatusEffect(StatusEffectType.Shock);
            if (shock != null && shock.stack > 0)
            {
                int shockDamage = shock.power;
                hp -= shockDamage;
                hp = Mathf.Max(hp, 0);

                var spawner = GetComponentInChildren<DamageTextSpawner>();
                if (spawner != null)
                {
                    spawner.ShowStatusEffectDamage(shockDamage, StatusEffectType.Shock, 0.7f);
                }

                shock.stack -= 1;
                if (shock.stack <= 0)
                {
                    RemoveStatusEffect(StatusEffectType.Shock);
                }
            }
            UpdateStatusEffectUI();
        }

        // 기본 데미지 처리
        var normalSpawner = GetComponentInChildren<DamageTextSpawner>();
        if (normalSpawner != null)
        {
            if (source == StatusEffectSource.StatusDamage)
                normalSpawner.ShowStatusEffectDamage(amount, effectType, 0.7f);
            else
            {
                // 0은 안 보여주기
                if (amount > 0)
                {
                    normalSpawner.ShowDamage(amount, 0.7f);    
                }
            }
        }

        hp -= amount;
        hp = Mathf.Max(hp, 0);
    }

    
    public void ApplyHeal(int amount, StatusEffectType effectType)
    {
        var spawner = GetComponentInChildren<DamageTextSpawner>();
        if (spawner != null)
        {
            spawner.ShowHeal(amount, effectType, 0.7f);
        }

        hp += amount;
        hp = Mathf.Min(hp, maxHp);
    }

    private int shieldAmount = 0;

    public void ApplyShield(int amount)
    {
        shieldAmount += amount;
    }

    

}
