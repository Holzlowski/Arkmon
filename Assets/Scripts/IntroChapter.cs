using UnityEngine;

// This scene profile separates the new introduction from the archived demo flow.
[DefaultExecutionOrder(-1000)]
public class IntroChapter : MonoBehaviour
{
    public static bool Active { get; private set; }
    public static bool HasFamilyOutfit { get; private set; }
    private static bool roomReturn;
    public static void MarkRoomReturn()=>roomReturn=true;
    public static bool ConsumeRoomReturn(){bool result=roomReturn;roomReturn=false;return result;}
    public const string PlayerName = "Elian";
    public const string FamilyName = "Aurenfeld";
    public static string TowerScene { get; private set; } = "TowerRoom";
    private void Awake()
    {
        Active = true;
        if (gameObject.scene.name == "TowerRoomPaper") TowerScene = "TowerRoomPaper";
    }
    private void Start()
    {
        if (gameObject.scene.name == "StartScene")
        {
            CourtyardScenicMorning.Install();
            CourtyardGardenPlace.PrepareBeds();
            CourtyardEastGroundStyle.PrepareTerrain();
            CourtyardResidentPlaces.Install();
            CourtyardGarden.Install();
            CourtyardStableCare.Install();
            CourtyardWorkshopDetails.Install();
            CourtyardLife.Install();
            CourtyardGardenDetails.Install();
            CourtyardVegetableBed.Install();
            CourtyardGardenStyle.Install();
            CourtyardGardenPlace.Install();
            CourtyardCastleEntranceStyle.Install();
            CourtyardCastleFacadeStyle.Install();
            CourtyardBreachStyle.Install();
            CourtyardLookoutLandscape.Install();
            CourtyardDistantFields.Install();
            CourtyardFoothills.Install();
            CourtyardFieldMargins.Install();
            CourtyardDistantFarm.Install();
            CourtyardLookout.Install();
            CourtyardFieldAtmosphere.Install();
            CourtyardFieldParticles.Install();
            CourtyardCloudSunrays.Install();
            CourtyardCellarEntranceStyle.Install();
            CourtyardEastCottageStyle.Install();
            CourtyardGuard.Install();
            CourtyardVergePolish.Install();
            CourtyardEastGroundStyle.Install();
            CourtyardGateStyle.Install();
            CourtyardBridgeThreshold.Install();
            CourtyardEastFieldStyle.Install();
            CourtyardGateForecourt.Install();
            CourtyardWallFootStyle.Install();
            CourtyardWallRelief.Install();
            CourtyardWestGroundStyle.Install();
            CourtyardStableGardenStyle.Install();
            CourtyardResidenceGround.Install();
            CourtyardTreeStyle.Install();
            CourtyardTreeShadows.Install();
            CourtyardPlantWind.Install();
            CourtyardFurnitureShadows.Install();
            CourtyardArchitecturePalette.Install();
            CourtyardMorningAccents.Install();
            CourtyardAmbientDetails.Install();
            CourtyardWayfinding.Install();
            CourtyardBreachSunrays.Install();
            CourtyardNearView.Install();
            CourtyardFeedbackPolish.Install();
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetProgress() { Active = false; HasFamilyOutfit = false;roomReturn=false; TowerScene="TowerRoom"; CourtyardGarden.ResetProgress(); }
    public static void EquipFamilyOutfit() => HasFamilyOutfit = true;
}
