[System.Serializable]
public struct CharacterData
{
    public string characterName;
    public int hp;
    public int speed;

    public CharacterData(string name, int hp, int speed)
    {
        characterName = name;
        this.hp = hp;
        this.speed = speed;
    }
}