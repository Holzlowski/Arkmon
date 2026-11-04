using UnityEditor;
public static class ConfigureCourtyardCharacterStyle
{
    [MenuItem("Arkmon/Apply shared courtyard character style")]
    public static void Run()
    {
        ConfigureHannesStable.ImportArt();
        ConfigureIdaWorkshop.ImportArt();
        ConfigureMartaEmotions.Run();
        ConfigureIntroCourtyard.Run();
    }
}