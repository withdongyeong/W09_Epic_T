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

        // 첫 번째 타격 (약한 중독 걸기)
        skill.attackPhases.Add(new AttackPhase
        {
            damage = firstHitDamage,
            statusEffect = new StatusEffectData
            {
                type = StatusEffectType.Poison,
                power = 3, // 약한 중독
                stack = 2
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

        // 나머지 연타
        for (int i = 0; i < repeatCount; i++)
        {
            float delay;
            if (i < 10)
            {
                delay = Mathf.Lerp(0.6f, 0.3f, (i + 1) / 4f); 
            }
            else if (i == 27)
            {
                delay = 0.8f;
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

    // 첫 번째 타격: 약한 대미지 + 화상 부여
    skill.attackPhases.Add(new AttackPhase
    {
        damage = firstHitDamage,
        statusEffect = new StatusEffectData
        {
            type = StatusEffectType.Burn,
            power = burnPower,
            stack = 10 // 회수 10회를 1타에 더함
        },
        requiresQTE = false,
        delayAfterHit = 1f // ⭐ 약간 기다리기
    });

    // 두 번째 타격: 화상 스택을 전부 소모하고 추가 데미지
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0, // 추가 데미지는 customEffect에서 처리, 0은 데미지 표기 안됨
        requiresQTE = false,
        delayAfterHit = 1f,
        customEffect = (target) =>
        {
            if (target.HasStatusEffect(StatusEffectType.Burn))
            {
                var burn = target.GetStatusEffect(StatusEffectType.Burn);
                int currentBurnPower = burn.power;
                int currentBurnStack = burn.stack;
                int totalDamage = currentBurnPower * currentBurnStack;

                target.ApplyDamage(totalDamage, StatusEffectSource.SpecialSkill);
                
                target.RemoveStatusEffect(StatusEffectType.Burn);
                target.UpdateStatusEffectUI();
            }
        }
    });

    return skill;
}



}
