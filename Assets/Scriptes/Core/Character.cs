using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class Character : MonoBehaviour
{
    public string characterName;
    public int hp;
    public int maxHp;
    public int speed;
    
    public List<Skill> skills; // active + passive 혼합 스킬
    public int poisonStacks;
    public int bleedStacks;
    public RectTransform atbIconTransform;
    public bool isAlive => hp > 0;
    public Vector3 originalPosition; // 처음 자기 자리

    public TextMeshProUGUI atbText;
    public float atbGauge = 0f;
    public float atbSpeedMultiplier = 1f;
    public bool isEnemy;



    private void Update()
    {
        UpdateATBIcon();
    }

    public void UpdateATBIcon()
    {
        if (atbIconTransform != null)
        {
            float percent = atbGauge / 100f;
            float moveRange = 500f; // 예시 이동 거리

            atbIconTransform.anchoredPosition = new Vector2(
                atbIconTransform.anchoredPosition.x,
                -percent * moveRange
            );
        }
    }
    private void OnMouseDown()
    {
        if (isEnemy && BattleManager.Instance != null)
        {
            BattleManager.Instance.SetCurrentTarget(this);
        }
    }




    
    public void UseSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
            return;

        Skill skill = skills[skillIndex];
        if (skill != null && skill.skillType == SkillType.Active)
        {
            skill.Activate(this, BattleManager.Instance.playerTeam, BattleManager.Instance.enemyTeam);
        }
    }

    public void BasicAttack(Character target)
    {
        
    }

    public void ApplyDamage(int amount, bool isStatusDamage = false)
    {
        
        // TODO 죽음처리는 나중에
        // if (!isAlive)
        //     return;
        //
        // hp -= amount;
        // if (hp < 0)
        //     hp = 0;

        var spawner = GetComponentInChildren<DamageTextSpawner>();
        if (spawner != null)
        {
            spawner.ShowDamage(amount, 0.7f); // <- 여기
        }

        // 추가로 사망처리 등 나중에
    }



    public void ApplyBuff(BuffType buff, int value, int duration)
    {
        
    }

    public void ApplyDebuff(DebuffType debuff, int value, int duration)
    {
        
    }
    


    public IEnumerator AttackTarget(Character target)
    {
        if (target == null || !target.isAlive)
            yield break;

        BattleManager.Instance.isSomeoneActing = true;

        bool isPlayer = BattleManager.Instance.playerTeam.Contains(this);
        Vector3 myAdvancePos = BattleManager.Instance.playerAdvancePoint.position;
        Vector3 enemyAdvancePos = BattleManager.Instance.enemyAdvancePoint.position;

        // 1. 공격자와 피공격자 AdvancePoint로 순간이동
        if (this == null || target == null) yield break;
        transform.position = isPlayer ? myAdvancePos : enemyAdvancePos;
        target.transform.position = isPlayer ? enemyAdvancePos : myAdvancePos;

        // 2. 카메라 연출
        CameraManager.Instance.FocusBetweenPoints(transform.position, target.transform.position, 0.1f, 3.5f);

        // 3. 움찔
        yield return new WaitForSeconds(0.1f);
        if (this == null || target == null) yield break;
        Vector3 attackImpulse = (target.transform.position - transform.position).normalized * 0.5f;
        yield return MoveToImpulse(attackImpulse, 0.3f);

        // 4. 데미지
        if (this == null || target == null) yield break;
        DealDamage(target);

        // 5. 데미지 기다림
        yield return new WaitForSeconds(0.7f);

        // 6. 복귀
        if (this == null || target == null) yield break;
        transform.position = originalPosition;
        target.transform.position = target.originalPosition;

        // 7. 카메라 복귀
        CameraManager.Instance.ZoomOut(0.3f);

        BattleManager.Instance.isSomeoneActing = false;
    }


    private IEnumerator MoveToImpulse(Vector3 impulse, float duration)
    {
        if (this == null)
            yield break;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + impulse;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (this == null)
                yield break;

            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }

        if (this == null)
            yield break;

        transform.position = endPos;
    }





    private IEnumerator MoveTo(Vector3 targetPos)
    {
        float moveSpeed = 10f;
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }



    private void DealDamage(Character target)
    {
        int damage = Random.Range(10, 10000); 

        target.ApplyDamage(damage);
        LogManager.Instance.Log($"{characterName}이 {target.characterName}에게 {damage} 데미지!");
    }
}