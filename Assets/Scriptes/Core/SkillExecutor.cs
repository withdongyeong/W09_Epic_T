
using System.Collections;

public class SkillExecutor
{
    private BattleManager _battleManager;
    
    public SkillExecutor(BattleManager battleManager)
    {
        _battleManager = battleManager;
    }
    
    public IEnumerator ExecuteSkill(Character caster, Skill skill)
    {
        BattleManager.Instance.isSomeoneActing = true;
        yield return caster.StartCoroutine(skill.Activate(caster, _battleManager.playerTeam, _battleManager.enemyTeam));
        BattleManager.Instance.isSomeoneActing = false;
    }
    
    public IEnumerator ExecuteBasicAttack(Character attacker, Character target)
    {
        BattleManager.Instance.isSomeoneActing = true;
        yield return attacker.StartCoroutine(attacker.BasicAttack(target));
        BattleManager.Instance.isSomeoneActing = false;
    }
}