using System.Collections.Generic;
using UnityEngine;

// 패시브 스킬용 데이터베이스 확장
public static class PassiveSkillDatabase
{
    // 기본 공격력 증가 패시브
// 공격력 증가 패시브 수정 - 확실하게 데미지 증가되도록
    public static Skill CreateAttackBoostPassive(string name, int boostPercentage)
    {
        var skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Passive,
            attackPhases = new List<AttackPhase>(),
            cooldownTurns = 0,
            isBasicAttack = false
        };

        // 캐릭터에 패시브 스킬 효과 적용
        skill.onEquip = (character) => {
            character.basicAttackBonus += boostPercentage;
        };

        return skill;
    }

    // 일정 확률로 2번 공격하는 패시브
    public static Skill CreateDoubleAttackPassive(string name, int doubleAttackChance)
    {
        var skill = new Skill
        {
            skillName = name,
            skillType = SkillType.Passive,
            attackPhases = new List<AttackPhase>(),
            cooldownTurns = 0,
            isBasicAttack = false
        };

        // 캐릭터에 패시브 스킬 효과 적용
        skill.onEquip = (character) => {
            // Character 클래스에 doubleAttackChance 필드 추가 필요
            character.doubleAttackChance = doubleAttackChance;
        };

        return skill;
    }
}