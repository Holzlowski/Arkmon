using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
    [Min(1)] public int maxHealth = 100;
    [Min(1)] public int maxActionPoints = 3;
    public Color summonColor = new Color(1f, 0.42f, 0.1f, 1f);
    public AbilityDefinition[] abilities = new AbilityDefinition[0];

    public AbilityDefinition GetAbility(string actionName)
    {
        foreach (AbilityDefinition ability in abilities)
            if (ability != null && ability.actionName == actionName) return ability;
        return null;
    }
}
