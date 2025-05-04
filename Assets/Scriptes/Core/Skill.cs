using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    public string skillName;
    public SkillType skillType;
    public bool isBasicAttack;
    public List<AttackPhase> attackPhases = new List<AttackPhase>();
    public int cooldownTurns;
    public int currentCooldown;
    public bool isAreaAttack; // ⭐ 전체공격 여부 추가
    public float hitInterval = 0.3f;

    public bool CanUse()
    {
        return currentCooldown <= 0;
    }

    public IEnumerator Activate(Character caster, List<Character> allies, List<Character> enemies)
    {
        if (!CanUse())
        {
            LogManager.Instance.Log($"{skillName} 쿨다운 : ({currentCooldown})");
            yield break;
        }

        yield return caster.StartCoroutine(ExecutePhases(caster, allies, enemies));

        currentCooldown = cooldownTurns;
        BattleManager.Instance.UpdateAllCharacterUIs();
    }

private IEnumerator ExecutePhases(Character caster, List<Character> allies, List<Character> enemies)
{
    if (isAreaAttack)
    {
        List<Character> targets = (caster.isEnemy ? allies : enemies).FindAll(c => c.isAlive);

        Vector3 casterAdvancePos = caster.isEnemy ? BattleManager.Instance.enemyAdvancePoint.position : BattleManager.Instance.playerAdvancePoint.position;
        Vector3 targetAdvancePos = caster.isEnemy ? BattleManager.Instance.playerAdvancePoint.position : BattleManager.Instance.enemyAdvancePoint.position;

        caster.transform.position = casterAdvancePos;
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].transform.position = targetAdvancePos + new Vector3(0, i * -0.5f, 0);
        }

        CameraManager.Instance.FocusBetweenPoints(casterAdvancePos, targetAdvancePos, 0.1f, 3.5f);
        yield return new WaitForSeconds(0.1f);

        Vector3 attackImpulse = (targetAdvancePos - casterAdvancePos).normalized * 0.5f;
        yield return caster.StartCoroutine(caster.MoveToImpulse(attackImpulse, 0.3f));

        foreach (var phase in attackPhases)
        {
            foreach (var target in targets)
            {
                if (!target.isAlive) continue;

                target.ApplyDamage(phase.damage, StatusEffectSource.DirectAttack);

                if (phase.statusEffect != null && phase.statusEffect.type != StatusEffectType.None)
                {
                    target.ApplyStatusEffect(phase.statusEffect);
                }

                if (phase.customEffect != null)
                {
                    phase.customEffect(target);
                }
            }

            if (phase.requiresQTE)
            {
                bool success = SimulateQTE();
                if (!success)
                {
                    LogManager.Instance.Log("QTE 실패로 스킬 중단");
                    break;
                }
            }

            yield return new WaitForSeconds(phase.delayAfterHit);
        }

        yield return new WaitForSeconds(0.7f);
        yield return caster.StartCoroutine(caster.WaitForAllDamageTexts());

        caster.transform.position = caster.originalPosition;
        foreach (var target in targets)
        {
            target.transform.position = target.originalPosition;
        }

        CameraManager.Instance.ZoomOut(0.3f);
    }
    else
    {
        Character target = BattleManager.Instance.currentTargetEnemy;
        if (target == null || !target.isAlive)
            target = BattleManager.Instance.GetFirstAliveEnemy();

        if (target == null)
            yield break;

        Vector3 casterAdvancePos = caster.isEnemy ? BattleManager.Instance.enemyAdvancePoint.position : BattleManager.Instance.playerAdvancePoint.position;
        Vector3 targetAdvancePos = caster.isEnemy ? BattleManager.Instance.playerAdvancePoint.position : BattleManager.Instance.enemyAdvancePoint.position;

        caster.transform.position = casterAdvancePos;
        target.transform.position = targetAdvancePos;

        CameraManager.Instance.FocusBetweenPoints(casterAdvancePos, targetAdvancePos, 0.1f, 3.5f);
        yield return new WaitForSeconds(0.1f);

        Vector3 attackImpulse = (targetAdvancePos - casterAdvancePos).normalized * 0.5f;
        yield return caster.StartCoroutine(caster.MoveToImpulse(attackImpulse, 0.3f));

        foreach (var phase in attackPhases)
        {
            target.ApplyDamage(phase.damage, StatusEffectSource.DirectAttack);

            if (phase.statusEffect != null && phase.statusEffect.type != StatusEffectType.None)
            {
                target.ApplyStatusEffect(phase.statusEffect);
            }

            if (phase.customEffect != null)
            {
                phase.customEffect(target);
            }

            if (phase.requiresQTE)
            {
                bool success = SimulateQTE();
                if (!success)
                {
                    LogManager.Instance.Log("QTE 실패로 스킬 중단");
                    break;
                }
            }

            yield return new WaitForSeconds(phase.delayAfterHit);
        }

        yield return new WaitForSeconds(0.7f);
        yield return caster.StartCoroutine(caster.WaitForAllDamageTexts());

        caster.transform.position = caster.originalPosition;
        target.transform.position = target.originalPosition;

        CameraManager.Instance.ZoomOut(0.3f);
    }
}


    private IEnumerator MoveToPosition(Character character, Vector3 targetPosition, float duration)
    {
        Vector3 startPos = character.transform.position;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            character.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        character.transform.position = targetPosition;
    }


    private bool SimulateQTE()
    {
        return Random.value < 0.8f;
    }
}
