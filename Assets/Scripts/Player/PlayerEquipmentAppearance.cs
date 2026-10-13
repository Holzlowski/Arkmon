using UnityEngine;

// One mapping for world sprites, walking sheets and dialogue portraits.
public static class PlayerEquipmentAppearance
{
    public static string SpriteName => "Prince-" + (!DemoOpening.Started || DemoOpening.HasMantle ? "Mantle" : "Room")
        + (DemoOpening.Started && !DemoOpening.HasLamp ? "-NoLamp" : "");
    public static Sprite RestingSprite => Resources.Load<Sprite>(SpriteName);
    public static Sprite Portrait => Resources.Load<Sprite>(SpriteName + "-Portrait");
}
