using System;
using UnityEngine;

[System.Serializable]
public class AttackPhase
{
    public int damage;                         // 기본 데미지
    public StatusEffectData statusEffect;      // 상태이상 부여
    public bool requiresQTE;                   // QTE 필요 여부
    public Action<Character> customEffect;     // ⭐️ 특수 행동 추가
    public float delayAfterHit = 0.3f;
}