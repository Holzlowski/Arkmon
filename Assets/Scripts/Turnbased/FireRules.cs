using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Fire Rules")]
public class FireRules : ScriptableObject
{
    [Min(1)] public int durationRounds = 3;
    [Min(1)] public int damagePerRound = 5;
    [Range(0f, 1f)] public float monsterHitChance = 0.25f;
    [Range(0f, 1f)] public float monsterSpreadChance = 0.2f;
    [Range(0f, 1f)] public float vegetationSpreadChance = 0.5f;
}
