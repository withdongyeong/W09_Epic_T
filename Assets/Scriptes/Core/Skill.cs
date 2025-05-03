using System.Collections.Generic;

public class Skill
{
    public string skillName;
    public SkillType skillType; // Active or Passive
    public int cooldown;
    public int currentCooldown;

    public void Activate(Character self, List<Character> allies, List<Character> enemies)
    {
        
    }
}