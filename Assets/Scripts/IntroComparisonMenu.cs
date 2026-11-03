using UnityEngine;
using UnityEngine.SceneManagement;

// Included only in the comparison build. Neutral labels avoid priming the tester.
public sealed class IntroComparisonMenu : MonoBehaviour
{
    private GUIStyle title, body, button;
    private bool loading;

    private void OnGUI()
    {
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
            body = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 25 };
        }
        float scale = Mathf.Min(Screen.width / 900f, Screen.height / 580f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 900 * scale) / 2, (Screen.height - 580 * scale) / 2, 0),
            Quaternion.identity, Vector3.one * scale);
        GUI.Box(new Rect(130, 115, 640, 350), GUIContent.none);
        GUI.Label(new Rect(150, 145, 600, 60), "Arkmon · Intro-Vergleich", title);
        GUI.Label(new Rect(170, 220, 560, 65),
            "Spiel beide Fassungen bis in den Burghof. Starte das Spiel für den zweiten Durchlauf erneut.", body);
        if (!loading && GUI.Button(new Rect(180, 320, 255, 65), "Intro A")) Select("TowerRoom");
        if (!loading && GUI.Button(new Rect(465, 320, 255, 65), "Intro B")) Select("TowerRoomPaper");
        GUI.matrix = previous;
    }

    private void Select(string scene)
    {
        loading = true;
        SceneManager.LoadSceneAsync(scene);
    }
}
