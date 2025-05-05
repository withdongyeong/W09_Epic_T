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
    // 속도 증가 버프 스킬
// 속도 증가 버프 스킬 수정 - 아군에게 적용되도록
public static Skill CreateSpeedBuffSkill(string name, int damage, int speedBoost, int duration, int cooldownTurns, bool targetAllAllies = false)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>(),
        isAreaAttack = targetAllAllies
    };

    // 첫 번째 페이즈: 적에게 데미지
    skill.attackPhases.Add(new AttackPhase
    {
        damage = damage,
        requiresQTE = false,
        delayAfterHit = 0.5f
    });
    
    // 두 번째 페이즈: 아군에게 버프
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = false,
        delayAfterHit = 0.5f,
        customEffect = (target) => {
            // 아군 캐릭터 선택
            Character ally = BattleManager.Instance.playerTeam[0]; // 기본은 첫 캐릭터
            
            if (targetAllAllies)
            {
                // 전체 아군에게 적용
                foreach(var allyCharacter in BattleManager.Instance.playerTeam)
                {
                    if (!allyCharacter.isAlive) continue;
                    
                    allyCharacter.ApplyStatusEffect(new StatusEffectData
                    {
                        type = StatusEffectType.SpeedUp,
                        power = speedBoost,
                        stack = duration,
                        tickType = StatusEffectTickType.EndOfTurn,
                        isBuff = true
                    });
                    
                    allyCharacter.atbSpeedMultiplier += speedBoost / 100f;
                    LogManager.Instance.Log($"{allyCharacter.characterName}의 속도가 {speedBoost}% 증가합니다!");
                }
            }
            else
            {
                // 스킬 사용자에게만 적용
                Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
                if (caster != null && caster.isAlive)
                {
                    caster.ApplyStatusEffect(new StatusEffectData
                    {
                        type = StatusEffectType.SpeedUp,
                        power = speedBoost,
                        stack = duration,
                        tickType = StatusEffectTickType.EndOfTurn,
                        isBuff = true
                    });
                    
                    caster.atbSpeedMultiplier += speedBoost / 100f;
                    LogManager.Instance.Log($"{caster.characterName}의 속도가 {speedBoost}% 증가합니다!");
                }
            }
        }
    });

    return skill;
}

// 협공 확률 증가 버프 스킬 수정 - 전체 효과 적용
public static Skill CreateTeamworkBuffSkill(string name, int damage, int teamworkBoost, int duration, int cooldownTurns, bool isAreaAttack = false)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>(),
        isAreaAttack = isAreaAttack
    };

    // 첫 번째 페이즈: 적에게 데미지
    skill.attackPhases.Add(new AttackPhase
    {
        damage = damage,
        requiresQTE = false,
        delayAfterHit = 0.5f
    });
    
    // 두 번째 페이즈: 전체 협공 확률 증가
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = false,
        customEffect = (target) => {
            // 팀 전체에 버프 효과 적용
            BattleManager.Instance.IncreaseFollowUpChance(teamworkBoost);
            
            // 모든 아군에게 버프 상태이상 표시
            foreach (var ally in BattleManager.Instance.playerTeam)
            {
                if (ally.isAlive)
                {
                    ally.ApplyStatusEffect(new StatusEffectData
                    {
                        type = StatusEffectType.TeamworkUp,
                        power = 100,
                        stack = duration,
                        tickType = StatusEffectTickType.EndOfTurn,
                        isBuff = true
                    });
                }
            }
        }
    });

    return skill;
}

// 기본 공격 스킬 생성
public static Skill CreateBasicAttackSkill(string name, int damage, int cooldownTurns)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>()
    };

    skill.attackPhases.Add(new AttackPhase
    {
        damage = damage,
        requiresQTE = false,
        delayAfterHit = 0.5f
    });

    return skill;
}
// 고급 연쇄 협공 스킬 구현 수정
public static Skill CreateAdvancedChainAssaultSkill(string name, int baseDamage, int finalDamage, int cooldownTurns)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>(),
        onSkillEnd = (success) => {
            // 스킬 종료 시 모든 캐릭터 위치 초기화
            foreach (var character in BattleManager.Instance.playerTeam)
            {
                character.isUnderAssault = false;
                character.waitingForAssault = false;
                character.transform.position = character.originalPosition;
            }
            
            // 적 위치 초기화
            foreach (var enemy in BattleManager.Instance.enemyTeam)
            {
                enemy.isUnderAssault = false;
                enemy.transform.position = enemy.originalPosition;
            }
        }
    };

    // 첫 번째 타격 - 위치 고정 및 대기 위치 설정
    skill.attackPhases.Add(new AttackPhase
    {
        damage = baseDamage,
        requiresQTE = true,
        qteType = QTEType.TimingButton,
        delayAfterHit = 0.7f,
        customEffect = (target) => {
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
            if (caster != null && target.isAlive)
            {
                // 위치 고정
                target.isUnderAssault = true;
                caster.isUnderAssault = true;
                
                // 배치
                List<Character> allAllies = new List<Character>();
                foreach (var ally in BattleManager.Instance.playerTeam)
                {
                    if (ally != caster && ally.isAlive)
                    {
                        ally.waitingForAssault = true; // 대기 상태
                        allAllies.Add(ally);
                    }
                }
                
                // 초기 배치
                BattleManager.Instance.PrepareAssaultPositions(caster, allAllies);
            }
        }
    });

    // 첫번째 5회 연속 공격 (일반 QTE)
    for (int i = 0; i < 5; i++)
    {
        skill.attackPhases.Add(new AttackPhase
        {
            damage = Mathf.RoundToInt(baseDamage * 0.5f),
            requiresQTE = true,
            qteType = QTEType.TimingButton,
            delayAfterHit = 0.5f,
            customEffect = (target) => {
                Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
                if (caster != null && target.isAlive)
                {
                    // 대기 아군 중 랜덤 선택
                    BattleManager.Instance.StartCoroutine(
                        BattleManager.Instance.PerformWaitingAllyAssault(caster, target)
                    );
                }
            }
        });
    }
    
    // 연타 QTE 구간 (성공 시 빠른 연속 협공 발동)
    skill.attackPhases.Add(new AttackPhase
    {
        damage = Mathf.RoundToInt(baseDamage * 0.5f),
        requiresQTE = true,
        qteType = QTEType.TapRapidly,  // 연타 QTE
        delayAfterHit = 0.3f,
        customEffect = (target) => {
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
            if (caster != null && target.isAlive)
            {
                // 연속 공격 트리거
                BattleManager.Instance.StartCoroutine(
                    BattleManager.Instance.PerformRapidTeamAssault(caster, target, 10)
                );
            }
        }
    });
    
    // 연타 마친 후 QTE
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = true,
        qteType = QTEType.TimingButton,
        delayAfterHit = 0.5f,
    });
    
    // 마무리 강력 공격 (QTE 성공 시에만 실행)
    skill.attackPhases.Add(new AttackPhase
    {
        damage = finalDamage,  // 강력한 피니시 데미지
        requiresQTE = false,   // QTE는 이미 앞에서 처리
        delayAfterHit = 2f,
        customEffect = (target) => {
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
        }
    });
    
    // 위치 초기화 페이즈
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = false,
        delayAfterHit = 0.1f,
        customEffect = (target) => {
            // 위치 초기화는 onSkillEnd에서 처리
        }
    });

    return skill;
}
// 랜덤 협공 스킬 수정 - 협공 완료 기다리기
public static Skill CreateRandomAllyAssistSkill(string name, int damage, int cooldownTurns)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>()
    };

    // 공격 페이즈
    skill.attackPhases.Add(new AttackPhase
    {
        damage = damage,
        requiresQTE = false,
        delayAfterHit = 1.5f, // 길게 대기
        customEffect = (target) => {
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
            if (caster != null && target.isAlive)
            {
                // 위치 고정 플래그
                target.isUnderAssault = true;
                caster.isUnderAssault = true;
                
                // 협공 트리거
                BattleManager.Instance.StartCoroutine(
                    BattleManager.Instance.TriggerForcedAllyAssist(caster, target)
                );
            }
        }
    });
    
    // 위치 초기화 페이즈
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = false,
        delayAfterHit = 0.1f,
        customEffect = (target) => {
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
            target.isUnderAssault = false;
            if (caster != null) caster.isUnderAssault = false;
        }
    });

    return skill;
}
// 자신 속도 대폭 증가 + 아군 협공 확률 증가 스킬
public static Skill CreateSelfSpeedTeamworkBuffSkill(string name, int speedBoost, int teamworkBoost, int duration, int cooldownTurns)
{
    var skill = new Skill
    {
        skillName = name,
        skillType = SkillType.Active,
        cooldownTurns = cooldownTurns,
        attackPhases = new List<AttackPhase>()
    };

    // 효과 적용 페이즈
    skill.attackPhases.Add(new AttackPhase
    {
        damage = 0,
        requiresQTE = false,
        customEffect = (target) => {
            // 스킬 사용자 찾기
            Character caster = BattleManager.Instance.playerTeam.Find(c => c.skills.Contains(skill));
            if (caster != null && caster.isAlive)
            {
                // 1. 자신 속도 대폭 증가
                caster.ApplyStatusEffect(new StatusEffectData
                {
                    type = StatusEffectType.SpeedUp,
                    power = speedBoost,
                    stack = duration,
                    tickType = StatusEffectTickType.EndOfTurn,
                    isBuff = true
                });
                
                caster.atbSpeedMultiplier += speedBoost / 100f;
                
                // 2. 전체 팀 협공 확률 증가
                BattleManager.Instance.IncreaseFollowUpChance(teamworkBoost);
                
                // 모든 아군에게 상태이상 표시
                foreach (var ally in BattleManager.Instance.playerTeam)
                {
                    if (ally != caster && ally.isAlive)
                    {
                        ally.ApplyStatusEffect(new StatusEffectData
                        {
                            type = StatusEffectType.TeamworkUp,
                            power = teamworkBoost,
                            stack = duration,
                            tickType = StatusEffectTickType.EndOfTurn,
                            isBuff = true
                        });
                    }
                }
            }
        },
        delayAfterHit = 0.5f
    });

    return skill;
}
}
