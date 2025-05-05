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
    public bool isAreaAttack;
    public float hitInterval = 0.3f;
    // 패시브 스킬 장착 시 효과
    public System.Action<Character> onEquip;
    public System.Action<bool> onSkillEnd;

    public bool CanUse()
    {
        return currentCooldown <= 0;
    }

    public IEnumerator Activate(Character caster, List<Character> allies, List<Character> enemies)
    {
        bool skillSuccess = true;
        
        if (!CanUse())
        {
            LogManager.Instance.Log($"{skillName} 쿨다운 : ({currentCooldown})");
            yield break;
        }

        if (isAreaAttack)
        {
            yield return caster.StartCoroutine(ExecuteAreaAttack(caster, allies, enemies));
        }
        else
        {
            yield return caster.StartCoroutine(ExecuteSingleTargetAttack(caster, enemies));
        }

        currentCooldown = cooldownTurns;
        BattleManager.Instance.UpdateAllCharacterUIs();
        
        // 스킬 종료 콜백 실행
        if (onSkillEnd != null)
        {
            onSkillEnd(skillSuccess);
        }
    }

    private IEnumerator ExecuteAreaAttack(Character caster, List<Character> allies, List<Character> enemies)
    {
        List<Character> targets = (caster.isEnemy ? allies : enemies).FindAll(c => c.isAlive);
        if (targets.Count == 0) yield break;

        Vector3 casterAdvancePos = caster.isEnemy ? BattleManager.Instance.enemyAdvancePoint.position : BattleManager.Instance.playerAdvancePoint.position;
        Vector3 targetAdvancePos = caster.isEnemy ? BattleManager.Instance.playerAdvancePoint.position : BattleManager.Instance.enemyAdvancePoint.position;

        // 포지셔닝
        caster.transform.position = casterAdvancePos;
        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].transform.position = targetAdvancePos + new Vector3(0, i * -0.5f, 0);
        }

        // 카메라 및 공격 모션
        CameraManager.Instance.FocusBetweenPoints(casterAdvancePos, targetAdvancePos, 0.1f, 3.5f);
        yield return new WaitForSeconds(0.1f);

        Vector3 attackImpulse = (targetAdvancePos - casterAdvancePos).normalized * 0.5f;
        yield return caster.StartCoroutine(caster.MoveToImpulse(attackImpulse, 0.3f));

        // 공격 페이즈 실행
        yield return ExecuteAttackPhases(caster, targets);
        
        // 정리
        caster.transform.position = caster.originalPosition;
        foreach (var target in targets)
        {
            target.transform.position = target.originalPosition;
        }

        CameraManager.Instance.ZoomOut(0.3f);
    }

    private IEnumerator ExecuteSingleTargetAttack(Character caster, List<Character> enemies)
    {
        Character target = BattleManager.Instance.currentTargetEnemy;
        if (target == null || !target.isAlive)
            target = BattleManager.Instance.GetFirstAliveEnemy();

        if (target == null) yield break;

        Vector3 casterAdvancePos = caster.isEnemy ? BattleManager.Instance.enemyAdvancePoint.position : BattleManager.Instance.playerAdvancePoint.position;
        Vector3 targetAdvancePos = caster.isEnemy ? BattleManager.Instance.playerAdvancePoint.position : BattleManager.Instance.enemyAdvancePoint.position;

        // 포지셔닝
        caster.transform.position = casterAdvancePos;
        target.transform.position = targetAdvancePos;

        // 카메라 및 공격 모션
        CameraManager.Instance.FocusBetweenPoints(casterAdvancePos, targetAdvancePos, 0.1f, 3.5f);
        yield return new WaitForSeconds(0.1f);

        Vector3 attackImpulse = (targetAdvancePos - casterAdvancePos).normalized * 0.5f;
        yield return caster.StartCoroutine(caster.MoveToImpulse(attackImpulse, 0.3f));

        // 공격 페이즈 실행
        List<Character> singleTarget = new List<Character> { target };
        yield return ExecuteAttackPhases(caster, singleTarget);
        
        // 정리
        caster.transform.position = caster.originalPosition;
        target.transform.position = target.originalPosition;

        CameraManager.Instance.ZoomOut(0.3f);
    }

    // ExecuteAttackPhases 메서드 수정
    private IEnumerator ExecuteAttackPhases(Character caster, List<Character> targets)
    {
        foreach (var phase in attackPhases)
        {
            // 각 타겟에 데미지와 효과 적용
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

            // QTE 처리 (타입 지정)
            if (phase.requiresQTE)
            {
                bool qteSuccess = false;
                bool qteCompleted = false;
        
                QTEManager.Instance.StartQTE(phase.qteType, (result) => {
                    qteSuccess = result;
                    qteCompleted = true;
                });
        
                while (!qteCompleted)
                    yield return null;
        
                if (!qteSuccess)
                {
                    LogManager.Instance.Log("QTE 실패로 스킬 중단");
                
                    // QTE 실패 즉시 초기화를 위한 콜백 호출
                    if (onSkillEnd != null)
                    {
                        onSkillEnd(false);
                    }
                
                    break;
                }
                else
                {
                    LogManager.Instance.Log("QTE 성공! 스킬 계속 진행");
                }
            }

            yield return new WaitForSeconds(phase.delayAfterHit);
        }
    
        yield return caster.StartCoroutine(caster.WaitForAllDamageTexts());
    }
    private bool SimulateQTE()
    {
        return Random.value < 0.8f;
    }
}