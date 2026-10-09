using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    [Min(1)] public int maxHealth = 100;
    [Min(1)] public int maxActionPoints = 3;
    public AbilityDefinition[] abilities = new AbilityDefinition[0];

    public AbilityDefinition GetAbility(string actionName)
    {
        foreach (AbilityDefinition ability in abilities)
            if (ability != null && ability.actionName == actionName) return ability;
        return null;
    }
}
