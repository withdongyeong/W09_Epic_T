
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefenseManager
{
    private BattleManager _battleManager;
    
    public DefenseManager(BattleManager battleManager)
    {
        _battleManager = battleManager;
    }

    public IEnumerator TriggerDefenseQTE(Character attacker, Character target)
    {
        bool qteResult = false;
        bool qteCompleted = false;
    
        // QTE 시작
        QTEManager.Instance.StartQTE(QTEType.TimingButton, (result) => {
            qteResult = result;
            qteCompleted = true;
        });
    
        // QTE 완료 대기
        while (!qteCompleted)
            yield return null;
    
        LogManager.Instance.Log($"방어 QTE {(qteResult ? "성공" : "실패")}");
    
        if (qteResult)
        {
            // 방어 성공 시 카운터 공격
            yield return _battleManager.StartCoroutine(PerformCounterAttack(target, attacker));
        }
        else
        {
            // 방어 실패 시 원래 공격 진행
            LogManager.Instance.Log($"{target.characterName}의 방어 실패!");
            
            Vector3 attackImpulse = (target.transform.position - attacker.transform.position).normalized * 0.5f;
            yield return attacker.MoveToImpulse(attackImpulse, 0.2f);
        
            // 데미지 적용
            attacker.DealDamage(target);
        }
    }    
    


    private IEnumerator PerformCounterAttack(Character defender, Character attacker)
    {
        LogManager.Instance.Log($"{defender.characterName}의 카운터 공격!");
    
        // 원래 위치 저장
        Vector3 originalDefenderPos = defender.transform.position;
        Vector3 originalAttackerPos = attacker.transform.position;
    
        // 카메라 흔들림 효과 (약간의 지연 후)
        yield return new WaitForSeconds(0.1f);
        CameraManager.Instance.Shake(0.2f, 0.1f);
    
        // 적을 튕겨내는 효과 표현
        Vector3 knockbackDirection = (attacker.transform.position - defender.transform.position).normalized;
        Vector3 knockbackTarget = attacker.transform.position + knockbackDirection * 1.5f;
    
        // 공격자 넉백
        float knockbackDuration = 0.2f;
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            attacker.transform.position = Vector3.Lerp(originalAttackerPos, knockbackTarget, elapsed / knockbackDuration);
            yield return null;
        }
    
        // 데미지 적용 (방어자의 공격력 기준으로 데미지 계산)
        int counterDamage = UnityEngine.Random.Range(3, 8); // 기본 카운터 데미지
        attacker.ApplyDamage(counterDamage);
    
        // 효과 표시를 위한 대기
        yield return new WaitForSeconds(0.5f);
    
        // 카운터 공격 후 방어자만 원래 위치로 복귀
        defender.transform.position = originalDefenderPos;
    
        // 공격자는 넉백된 위치에 그대로 유지 (원래 위치로 돌아오지 않음)
        // attacker.transform.position = originalAttackerPos; // 이 코드 제거
    }
}