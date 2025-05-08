using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DeckInitializer
{
    public static void InitializeDeck(BattleManager manager, int deckId)
    {
        manager.ClearField();

        switch (deckId)
        {
            case 0:
                InitializePoisonDeck(manager);
                break;
            case 1:
                InitializeShockDeck(manager);
                break;
            case 2:
                InitializeAssistDeck(manager);
                break;
            case 3:
                InitializeBurnDeck(manager);
                break;
            default:
                Debug.LogError($"잘못된 덱 ID: {deckId}");
                return;
        }

        manager.StartCoroutine(manager.InitializeCharacters());
        manager.ValidateTarget();
        manager.UpdateTargetUI();
    }

    private static void InitializePoisonDeck(BattleManager manager)
    {
        CreatePlayerCharacters(manager, new string[] { "중독빌드1", "중독빌드2", "중독빌드3", "중독피니시" }, "Poison");
        CreateEnemies(manager);
    }

    private static void InitializeShockDeck(BattleManager manager)
    {
        CreatePlayerCharacters(manager, new string[] { "감전빌드1", "감전빌드2", "감전빌드3", "감전피니시" }, "Shock");
        CreateEnemies(manager);
    }

    private static void InitializeAssistDeck(BattleManager manager)
    {
        CreatePlayerCharacters(manager, new string[] { "협공빌드1", "협공빌드2", "협공빌드3", "협공피니시" }, "Teamwork");
        CreateEnemies(manager);
    }

    private static void InitializeBurnDeck(BattleManager manager)
    {
        CreatePlayerCharacters(manager, new string[] { "화상빌드1", "화상빌드2", "화상빌드3", "화상피니시" }, "Burn");
        CreateEnemies(manager);
    }

    private static void CreatePlayerCharacters(BattleManager manager, string[] names, string deckType)
    {
        for (int i = 0; i < names.Length; i++)
        {
            GameObject go = GameObject.Instantiate(manager.characterPrefab, manager.playerSpawnRoot);
            Character c = go.GetComponent<Character>();
            c.characterName = names[i];
            c.hp = 999999;
            c.maxHp = 999999;
            c.speed = Random.Range(5, 20);
            c.atbIconTransform = manager.atbIcons[i];
            c.isEnemy = false;
            manager.playerTeam.Add(c);

            manager._characterSetupManager.SetupCharacterIcon(c, manager.playerColors[i]);
            manager._characterSetupManager.SetupATBIcon(c, manager.playerColors[i]);

            AssignSkills(c, deckType, i);
        }
    }

    private static void AssignSkills(Character c, string deckType, int index)
    {
        // 임시 리스트를 생성하여 기존 스킬을 저장할 준비
        List<Skill> originalSkills = new List<Skill>();
        
        // 기본 스킬 (쿨타임 0) 먼저 추가
        Skill basicSkill = SkillDatabase.CreateBasicAttackSkill("기본 공격", 3, 0);
        
        if (deckType == "Poison")
        {
            if (index == 0)
            {
                originalSkills.Add(SkillDatabase.CreatePoisonSkill("단일중독(약)", 5, 3, 3, 2));
                originalSkills.Add(SkillDatabase.CreatePoisonSkill("단일중독(강)", 8, 5, 1, 3));
            }
            else if (index == 1)
            {
                originalSkills.Add(SkillDatabase.CreatePoisonSkill("단일중독(약)", 4, 2, 4, 2));
                originalSkills.Add(SkillDatabase.CreatePoisonSkill("단일중독(강)", 6, 4, 2, 3));
            }
            else if (index == 2)
            {
                var skill1 = SkillDatabase.CreatePoisonSkill("광역중독(약)", 4, 2, 3, 3);
                skill1.isAreaAttack = true;
                var skill2 = SkillDatabase.CreatePoisonSkill("광역중독(강)", 6, 5, 2, 4);
                skill2.isAreaAttack = true;
                originalSkills.Add(skill1);
                originalSkills.Add(skill2);
            }
            else if (index == 3)
            {
                var skill1 = SkillDatabase.CreatePoisonSkill("광역중독(약)", 4, 3, 2, 2);
                skill1.isAreaAttack = true;
                originalSkills.Add(skill1);
                originalSkills.Add(SkillDatabase.CreatePoisonFinishSkill("독 압축 폭발", 7, 15, 4));
            }
        }
        else if (deckType == "Shock")
        {
            if (index == 0)
            {
                originalSkills.Add(SkillDatabase.CreateShockSkill("단일감전(약)", 3, 2, 1, 1, 2));
                originalSkills.Add(SkillDatabase.CreateShockSkill("단일감전(강)", 4, 2, 1, 2, 3));
            }
            else if (index == 1)
            {
                var skill1 = SkillDatabase.CreateShockSkill("광역감전(약)", 2, 2, 2, 2, 3);
                skill1.isAreaAttack = true;
                var skill2 = SkillDatabase.CreateShockSkill("광역감전(강)", 3, 2, 2, 3, 4);
                skill2.isAreaAttack = true;
                originalSkills.Add(skill1);
                originalSkills.Add(skill2);
            }
            else if (index == 2)
            {
                originalSkills.Add(SkillDatabase.CreateShockSkill("단일감전(다타)", 2, 3, 3, 2, 2));
                var skill2 = SkillDatabase.CreateShockSkill("광역감전(다타)", 2, 3, 3, 3, 4);
                skill2.isAreaAttack = true;
                originalSkills.Add(skill2);
            }
            else if (index == 3)
            {
                var skill1 = SkillDatabase.CreateShockSkill("광역감전 연격", 2, 5, 1, 5, 3);
                skill1.isAreaAttack = true;
                originalSkills.Add(skill1);
                originalSkills.Add(SkillDatabase.CreateShockFinishSkill("감전 30연격", 3, 1, 29, 1, 5, 4));
            }
        }
        else if (deckType == "Burn")
        {
            if (index < 3)
            {
                originalSkills.Add(SkillDatabase.CreateBurnSkill("단일화상", 4, 2, 3, 2));
                originalSkills.Add(SkillDatabase.CreateBurnSkill("광역화상", 3, 1, 4, 3, 1, true));
            }
            else
            {
                originalSkills.Add(SkillDatabase.CreateBurnSkill("광역화상", 3, 2, 3, 3, 1,true));
                originalSkills.Add(SkillDatabase.CreateBurnFinishSkill("화상폭발", 5, 2, 5));
            }
        }
        else if (deckType == "Teamwork")
        {
            if (index == 0) // 첫 번째 캐릭터: 강력한 평타 + 랜덤 아군 협공
            {
                // 패시브 스킬: 평타 공격력 대폭 강화
                originalSkills.Add(PassiveSkillDatabase.CreateAttackBoostPassive("P:평타 50%강화", 50));
        
                // 액티브 스킬: 랜덤 아군 협공
                originalSkills.Add(SkillDatabase.CreateRandomAllyAssistSkill("랜덤 협공", 7, 3));
            }
            else if (index == 1) // 두 번째 캐릭터: 강력한 평타 + 랜덤 아군 협공
            {
                // 패시브 스킬: 평타 공격력 대폭 강화
                originalSkills.Add(PassiveSkillDatabase.CreateAttackBoostPassive("P:평타 70%강화", 60));
        
                // 액티브 스킬: 랜덤 아군 협공
                originalSkills.Add(SkillDatabase.CreateRandomAllyAssistSkill("랜덤 협공", 8, 3));
            }
            else if (index == 2) // 세 번째 캐릭터: 강력한 평타 + 팀 속도 증가
            {
                // 패시브 스킬: 평타 공격력 강화
                originalSkills.Add(PassiveSkillDatabase.CreateDoubleAttackPassive("P:평타 100% 2대", 100));
        
                // 액티브 스킬: 랜덤 아군 협공
                originalSkills.Add(SkillDatabase.CreateRandomAllyAssistSkill("랜덤 협공", 10, 3));
            }
            else // 피니시 캐릭터: 1번 속도/협공 버프, 2번 QTE 연속 협공
            {
                // 1번 스킬: 자신 속도 대폭 증가 + 아군 협공 확률 증가
                originalSkills.Add(SkillDatabase.CreateSelfSpeedTeamworkBuffSkill("선봉 돌격", 200, 100, 5, 3));
        
                // 2번 스킬: QTE 연속 협공
                originalSkills.Add(SkillDatabase.CreateAdvancedChainAssaultSkill("연쇄 협공", 10, 10, 4));
            }
        }

        // 1번 스킬에 기본 스킬 추가
        c.skills.Add(basicSkill);
        
        // 기존 스킬들을 2번, 3번 슬롯으로 추가
        foreach (var skill in originalSkills)
        {
            c.skills.Add(skill);
        }
    }

    private static void CreateEnemies(BattleManager manager)
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = GameObject.Instantiate(manager.characterPrefab, manager.enemySpawnRoot);
            Character e = go.GetComponent<Character>();
            e.characterName = $"적_{i + 1}";
            e.hp = 999999;
            e.maxHp = 999999;
            e.speed = Random.Range(5, 20);
            e.atbIconTransform = manager.atbIcons[i + 4];
            e.isEnemy = true;
            manager.enemyTeam.Add(e);

            manager._characterSetupManager.SetupCharacterIcon(e, manager.enemyColors[i]);
            manager._characterSetupManager.SetupATBIcon(e, manager.enemyColors[i]);
        }
    }
}