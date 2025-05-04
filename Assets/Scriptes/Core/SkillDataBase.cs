using System.Collections.Generic;
using UnityEngine;

public static class SkillDatabase
{
    public static Skill CreatePoisonSkill(string name, int damage, int poisonPower, int poisonDuration, int cooldownTurns, int hitCount = 1, bool[] qtePhases = null)
    {
        var skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Active,
            cooldownTurns = cooldownTurns,
            attackPhases = new List<AttackPhase>()
        };

        for (int i = 0; i < hitCount; i++)
        {
            skill.attackPhases.Add(new AttackPhase
            {
                damage = damage,
                statusEffect = new StatusEffectData
                {
                    type = StatusEffectType.Poison,
                    potency = poisonPower,
                    duration = poisonDuration
                },
                requiresQTE = (qtePhases != null && i < qtePhases.Length) ? qtePhases[i] : false
            });
        }

        return skill;
    }

    public static Skill CreatePoisonFinishSkill(string name, int firstHitDamage, int compressDamage, int cooldownTurns)
    {
        var skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Active,
            cooldownTurns = cooldownTurns,
            attackPhases = new List<AttackPhase>()
        };

        // 첫 번째 타격 (약한 중독 걸기)
        skill.attackPhases.Add(new AttackPhase
        {
            damage = firstHitDamage,
            statusEffect = new StatusEffectData
            {
                type = StatusEffectType.Poison,
                potency = 3, // 약한 중독
                duration = 2
            },
            requiresQTE = false
        });

        // 두 번째 타격 (중독 압축 - customEffect)
        skill.attackPhases.Add(new AttackPhase
        {
            damage = compressDamage,
            requiresQTE = false,
            customEffect = (target) =>
            {
                if (target.HasStatusEffect(StatusEffectType.Poison))
                {
                    var poison = target.GetStatusEffect(StatusEffectType.Poison);
                    poison.potency *= 2;
                    poison.duration = Mathf.Max(1, poison.duration / 2);
                    target.UpdateStatusEffectUI();
                }
            }
        });

        return skill;
    }
    public static Skill CreateShockSkill(string name, int damagePerHit, int hitCount, int shockPower, int shockDuration, int cooldownTurns)
    {
        Skill skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Active,
            cooldownTurns = cooldownTurns,
            currentCooldown = 0
        };

        for (int i = 0; i < hitCount; i++)
        {
            skill.attackPhases.Add(new AttackPhase
            {
                damage = damagePerHit,
                statusEffect = new StatusEffectData
                {
                    type = StatusEffectType.Shock,
                    potency = shockPower,
                    duration = shockDuration,
                    tickType = StatusEffectTickType.OnHitTaken // ⭐ 피격당할때마다 발동
                },
                customEffect = null,
                requiresQTE = false
            });
        }

        return skill;
    }
    public static Skill CreateShockFinishSkill(string name, int firstHitDamage, int repeatDamage, int repeatCount, int shockPower, int shockDuration, int cooldownTurns)
    {
        Skill skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Active,
            cooldownTurns = cooldownTurns,
            currentCooldown = 0
        };

        // 첫 타: 감전 부여
        skill.attackPhases.Add(new AttackPhase
        {
            damage = firstHitDamage,
            statusEffect = new StatusEffectData
            {
                type = StatusEffectType.Shock,
                potency = shockPower,
                duration = shockDuration,
                tickType = StatusEffectTickType.OnHitTaken
            },
            requiresQTE = false,
            delayAfterHit = 0.6f
        });

        // 나머지 연타
        for (int i = 0; i < repeatCount; i++)
        {
            float delay;
            if (i < 10)
            {
                delay = Mathf.Lerp(0.6f, 0.15f, (i + 1) / 4f); 
            }
            else
            {
                delay = 0.1f; // 5타부터 고정
            }

            skill.attackPhases.Add(new AttackPhase
            {
                damage = repeatDamage,
                statusEffect = null,
                customEffect = null,
                requiresQTE = false,
                delayAfterHit = delay
            });
        }

        return skill;
    }


}
