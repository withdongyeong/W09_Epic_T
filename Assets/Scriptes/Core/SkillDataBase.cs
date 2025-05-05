using System.Collections.Generic;
using UnityEngine;

public static class SkillDatabase
{
    public static Skill CreatePoisonSkill(string name, int damage, int poisonPower, int poisonStack, int cooldownTurns, int hitCount = 1, bool[] qtePhases = null)
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
                    power = poisonPower,
                    stack = poisonStack
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

        // 첫 번째 타격 (약한 중독 걸기) - QTE 추가
        skill.attackPhases.Add(new AttackPhase
        {
            damage = firstHitDamage,
            statusEffect = new StatusEffectData
            {
                type = StatusEffectType.Poison,
                power = 3, 
                stack = 2
            },
            requiresQTE = true,
            qteType = QTEType.TimingButton
        });

        // 두 번째 타격 (중독 압축)
        skill.attackPhases.Add(new AttackPhase
        {
            damage = compressDamage,
            requiresQTE = false,
            customEffect = (target) =>
            {
                if (target.HasStatusEffect(StatusEffectType.Poison))
                {
                    var poison = target.GetStatusEffect(StatusEffectType.Poison);
                    poison.power *= 2;
                    poison.stack = Mathf.Max(1, poison.stack / 2);
                    target.UpdateStatusEffectUI();
                }
            }
        });

        return skill;
    }

    public static Skill CreateShockSkill(string name, int damagePerHit, int hitCount, int shockPower, int shockStack, int cooldownTurns)
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
                    power = shockPower,
                    stack = shockStack,
                    tickType = StatusEffectTickType.OnHitTaken // ⭐ 피격당할때마다 발동
                },
                customEffect = null,
                requiresQTE = false
            });
        }

        return skill;
    }
    public static Skill CreateShockFinishSkill(string name, int firstHitDamage, int repeatDamage, int repeatCount, int shockPower, int shockStack, int cooldownTurns)
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
                power = shockPower,
                stack = shockStack,
                tickType = StatusEffectTickType.OnHitTaken
            },
            requiresQTE = false,
            delayAfterHit = 0.6f
        });

        // 서서히 빨라지는 연타 (2~10타)
        for (int i = 0; i < 9; i++)
        {
            float delay = Mathf.Lerp(0.6f, 0.3f, (i + 1) / 4f);
        
            skill.attackPhases.Add(new AttackPhase
            {
                damage = repeatDamage,
                statusEffect = null,
                requiresQTE = (i == 8), // 마지막 타격 후 QTE
                qteType = (i == 8) ? QTEType.TapRapidly : QTEType.TimingButton,
                delayAfterHit = delay
            });
        }

        // 일정한 속도 빠른 연타 (11~27타)
        for (int i = 0; i < 17; i++)
        {
            skill.attackPhases.Add(new AttackPhase
            {
                damage = repeatDamage,
                statusEffect = null,
                requiresQTE = (i == 16), // 마지막 타격 후 QTE
                qteType = (i == 16) ? QTEType.TimingButton : QTEType.TimingButton,
                delayAfterHit = 0.1f
            });
        }

        // 마지막 타격
        skill.attackPhases.Add(new AttackPhase
        {
            damage = repeatDamage * 2,
            statusEffect = null,
            requiresQTE = false,
            delayAfterHit = 0.8f
        });

        return skill;
    }
    
    public static Skill CreateBurnSkill(string name, int damage, int burnPower, int burnStack, int cooldownTurns, int hitCount = 1, bool isAreaAttack = false)
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
                type = StatusEffectType.Burn, // ⭐ 화상
                power = burnPower,
                stack = burnStack
            },
            requiresQTE = false
        });
    }

    skill.isAreaAttack = isAreaAttack;
    return skill;
}

    public static Skill CreateBurnFinishSkill(string name, int firstHitDamage, int burnPower, int cooldownTurns)
    {
        var skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Active,
            cooldownTurns = cooldownTurns,
            attackPhases = new List<AttackPhase>()
        };

        // 첫 번째 타격
        skill.attackPhases.Add(new AttackPhase
        {
            damage = firstHitDamage,
            statusEffect = new StatusEffectData
            {
                type = StatusEffectType.Burn,
                power = burnPower,
                stack = 10
            },
            requiresQTE = true,
            qteType = QTEType.TimingButton,
            delayAfterHit = 1f
        });

        // 두 번째 타격
        skill.attackPhases.Add(new AttackPhase
        {
            damage = 0,
            requiresQTE = false,
            delayAfterHit = 1f,
            customEffect = (target) =>
            {
                if (target.HasStatusEffect(StatusEffectType.Burn))
                {
                    var burn = target.GetStatusEffect(StatusEffectType.Burn);
                    int totalDamage = burn.power * burn.stack;
                    target.ApplyDamage(totalDamage, StatusEffectSource.SpecialSkill);
                    target.RemoveStatusEffect(StatusEffectType.Burn);
                    target.UpdateStatusEffectUI();
                }
            }
        });

        return skill;
    }

}
