public enum StatusEffectType
{
    None,
    Poison,     // 중독 데미지
    Bleed,      // 출혈 데미지
    Burn,       // 화상 데미지
    Shock,      // 감전 데미지
    HealOverTime,   // 턴마다 회복
    Shield,     // 턴마다 보호막
    SpeedUp,      // 속도 증가
    TeamworkUp    // 협공 확률 증가
}