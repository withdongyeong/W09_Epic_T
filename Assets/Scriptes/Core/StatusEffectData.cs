[System.Serializable]
public class StatusEffectData
{
    public StatusEffectType type;
    public int potency;        // 위력
    public int duration;       // 지속 턴수
    public StatusEffectTickType tickType;  // 언제 효과 발생? (EndOfTurn 등)
    public bool isBuff;         // 버프(true) / 디버프(false)

    // 필요시 추가 필드
}