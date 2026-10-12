using UnityEngine;

[CreateAssetMenu(menuName = "Arkmon/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    [Tooltip("Matches the action name, e.g. Move, Krallenhieb, Flammenschwert or Rammstoß.")]
    public string actionName = "Shoot";
    [Min(0)] public int actionPointCost = 1;
    [Min(0)] public int damage = 40;
    [Min(1)] public int range = 6;
}
