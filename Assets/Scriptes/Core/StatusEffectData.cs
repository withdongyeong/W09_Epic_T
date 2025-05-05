[System.Serializable]
public class StatusEffectData
{
    public StatusEffectType type;
    public int power;         // 위력
    public int stack;         // 남은 스택 수 (ex: 남은 발동 가능 횟수)
    public StatusEffectTickType tickType;  // 언제 효과 발생? (EndOfTurn, OnHit 등)
    public bool isBuff;      // 버프(true) / 디버프(false)

    // 필요시 추가 필드
}