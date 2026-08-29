using UnityEngine;


[CreateAssetMenu(fileName = "Item", menuName = "Game/Character Stats")]
public class CharacterStatsSO : ScriptableObject
{
    [SerializeField] private int maxHealth;
    [SerializeField] private int maxMana;
    [SerializeField] private int maxStamina;
    [SerializeField] private int maxPoise;

    public const int INFINITE_STAT_VALUE = -1;

    public static bool HasInfinite(int stat) { return stat > INFINITE_STAT_VALUE; }
    public bool HasInfiniteHealth() { return maxHealth < 0; }
    public bool HasInfiniteMana() { return maxMana < 0; }
    public bool HasInfiniteStamina() { return maxStamina < 0; }
}
