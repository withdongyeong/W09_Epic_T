using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Skill
{
    public string skillName;
    public SkillType skillType;
    public bool isBasicAttack;
    public List<AttackPhase> attackPhases = new List<AttackPhase>();
    public int cooldownTurns; 
    public int currentCooldown; 

    public bool CanUse()
    {
        return currentCooldown <= 0;
    }

    public IEnumerator Activate(Character caster, List<Character> allies, List<Character> enemies)
    {
        if (!CanUse())
        {
            LogManager.Instance.Log($"{skillName}은 아직 쿨타임!!. ({currentCooldown}턴 남음)");
            yield break;
        }

        yield return caster.StartCoroutine(ExecutePhases(caster, enemies));

        currentCooldown = cooldownTurns;
    }

    private IEnumerator<WaitForSeconds> ExecutePhases(Character caster, List<Character> enemies)
    {
        foreach (var phase in attackPhases)
        {
            Character target = BattleManager.Instance.GetFirstAliveEnemy();
            if (target == null)
                yield break;

            // ⭐ DirectAttack 으로 명시해서 데미지 주기
            target.ApplyDamage(phase.damage, StatusEffectSource.DirectAttack);

            // ⭐ 상태이상도 명확히 체크
            if (phase.statusEffect != null && phase.statusEffect.type != StatusEffectType.None)
            {
                target.ApplyStatusEffect(phase.statusEffect);
                LogManager.Instance.Log($"{target.characterName}에게 {phase.statusEffect.type} 부여!");
            }

            if (phase.requiresQTE)
            {
                bool success = SimulateQTE();
                if (!success)
                {
                    LogManager.Instance.Log("QTE 실패로 스킬 중단");
                    yield break;
                }
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private bool SimulateQTE()
    {
        return Random.value < 0.8f;
    }
}