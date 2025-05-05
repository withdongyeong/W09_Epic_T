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

    public List<Skill> skills = new List<Skill>();
    public List<StatusEffectData> activeStatusEffects = new List<StatusEffectData>();
    
    public RectTransform atbIconTransform;
    public bool isAlive => hp > 0;
    public Vector3 originalPosition;

    public TextMeshProUGUI atbText;
    public float atbGauge = 0f;
    public float atbSpeedMultiplier = 1f;
    public bool isEnemy;

    private bool isActing = false;
    private bool isAliveActionLoop = true;
    public Queue<Func<IEnumerator>> actionQueue = new Queue<Func<IEnumerator>>();
    [SerializeField] private TMP_Text statusEffectText; // 상태이상 표시용
    
    private static readonly Dictionary<StatusEffectType, (string emoji, string colorHex)> StatusEffectVisuals = new()
    {
        { StatusEffectType.Poison, ("☠️", "#80FF80") },
        { StatusEffectType.Bleed, ("🩸", "#FF4040") },
        { StatusEffectType.Burn, ("🔥", "#FFA500") },
        { StatusEffectType.Shock, ("⚡", "#8080FF") },
        { StatusEffectType.HealOverTime, ("💚", "#40FF40") },
        { StatusEffectType.Shield, ("🛡️", "#00FFFF") }
    };

    public void UpdateStatusEffectUI()
    {
        if (statusEffectText == null) return;

        string result = "";

        foreach (var effect in activeStatusEffects)
        {
            if (effect.duration <= 0) continue;

            if (StatusEffectVisuals.TryGetValue(effect.type, out var visual))
            {
                result += $"<color={visual.colorHex}>{visual.emoji} {effect.potency}/{effect.duration}</color>\n";

            }
            else
            {
                // 정의 안된 경우
                result += $"{effect.type} {effect.potency}/{effect.duration}\n";
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

        yield return StartCoroutine(WaitForAllDamageTexts()); // ⭐️ 여기 추가

        transform.position = originalPosition;
        target.transform.position = target.originalPosition;

        foreach (var skill in skills)
        {
            if (skill.currentCooldown > 0)
                skill.currentCooldown--;
        }

        BattleManager.Instance.UpdateAllCharacterUIs();
        CameraManager.Instance.ZoomOut(0.3f);
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
            existing.potency += statusEffect.potency;
            existing.duration += statusEffect.duration;
        }
        else
        {
            activeStatusEffects.Add(new StatusEffectData
            {
                type = statusEffect.type,
                potency = statusEffect.potency,
                duration = statusEffect.duration,
                tickType = statusEffect.tickType
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
            effect.duration--;
            if (effect.duration <= 0)
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

    private IEnumerator ApplyStatusEffectImpact(StatusEffectData effect)
    {
        switch (effect.type)
        {
            case StatusEffectType.Poison:
            case StatusEffectType.Bleed:
            case StatusEffectType.Burn:
            case StatusEffectType.Shock:
                ApplyDamage(effect.potency, StatusEffectSource.StatusDamage, effect.type);
                yield return new WaitForSeconds(0.3f);
                break;

            case StatusEffectType.HealOverTime:
                ApplyHeal(effect.potency, effect.type);
                yield return new WaitForSeconds(0.3f);
                break;

            case StatusEffectType.Shield:
                ApplyShield(effect.potency);
                yield return new WaitForSeconds(0.3f);
                break;
        }
    }



    public void RemoveStatusEffect(StatusEffectType type)
    {
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

    private void DealDamage(Character target)
    {
        int damage = Random.Range(1, 5);
        target.ApplyDamage(damage);
    }

    public void ApplyDamage(int amount, StatusEffectSource source = StatusEffectSource.DirectAttack, StatusEffectType effectType = StatusEffectType.None)
    {
        // 감전 추가피해 체크
        if (HasStatusEffect(StatusEffectType.Shock))
        {
            StatusEffectData shock = GetStatusEffect(StatusEffectType.Shock);
            if (shock != null && shock.duration > 0)
            {
                int shockDamage = shock.potency;
                hp -= shockDamage;
                hp = Mathf.Max(hp, 0);

                var spawner = GetComponentInChildren<DamageTextSpawner>();
                if (spawner != null)
                {
                    spawner.ShowStatusEffectDamage(shockDamage, StatusEffectType.Shock, 0.7f);
                }

                shock.duration -= 1;
                if (shock.duration <= 0)
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
