using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager instance;
    // Hier könnten alle Daten gespeichert werden, die zwischen den Szenen ausgetauscht werden sollen
    [SerializeField] Vector3 playerPosition;
    [SerializeField] int sceneIndex;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SavePlayerTransform(Vector3 playerPosition)
    {
        this.playerPosition = playerPosition;
        sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }

    public void SetPlayertransform(Transform playerTransform)
    {
        playerTransform.position = this.playerPosition;
    }

    public int GetSceneIndex()
    {
        return sceneIndex;
    }


    public void LoadSceneBeforeBattle()
    {
        SceneManager.LoadSceneAsync(sceneIndex).completed += OnSceneLoaded;
    }

    private void OnSceneLoaded(AsyncOperation asyncOperation)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerPosition;
        }
        else
        {
            Debug.LogWarning("Player not found in the loaded scene.");
        }
    }

    //[SerializeField] int sceneToLoad = -1;
    //[SerializeField] Transform spawnpoint;
    //[SerializeField] DestinationIdentifier destination;
    //[SerializeField] float fadeOutTime = 2f;
    //[SerializeField] float fadeInTime = 1f;
    //[SerializeField] float fadeWaitTime = 0.5f;

    //private IEnumerator Transition()
    //{
    //    if (sceneToLoad < 0)
    //    {
    //        Debug.LogError("Scene to load not set.");
    //        yield break;
    //    }

    //    Fader fader = FindObjectOfType<Fader>();
    //    PlayerController playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();
    //    playerController.enabled = false;

    //    DontDestroyOnLoad(gameObject);

    //    yield return fader.FadeOut(fadeOutTime);

    //    SavingWrapper wrapper = FindObjectOfType<SavingWrapper>();
    //    wrapper.Save();

    //    yield return SceneManager.LoadSceneAsync(sceneToLoad);
    //    PlayerController newPlayerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>(); // ein neues Playerobject nach dem Laden einer Szene
    //    newPlayerController.enabled = false;

    //    wrapper.Load();

    //    Portal otherPortal = GetOtherPortal();
    //    UpdatePlayer(otherPortal);

    //    wrapper.Save();

    //    yield return new WaitForSeconds(fadeWaitTime);
    //    fader.FadeIn(fadeInTime);

    //    newPlayerController.enabled = true;
    //    Destroy(gameObject);
    //}

    //private Portal GetOtherPortal()
    //{
    //    foreach (Portal portal in FindObjectsOfType<Portal>())
    //    {
    //        if (portal == this) continue;
    //        if (portal.destination != destination) continue;
    //        return portal;
    //    }

    //    return null;
    //}

    //private void UpdatePlayer(Portal otherPortal)
    //{
    //    GameObject player = GameObject.FindWithTag("Player");
    //    player.GetComponent<NavMeshAgent>().Warp(otherPortal.spawnpoint.position);
    //    player.transform.rotation = otherPortal.spawnpoint.rotation;
    //}
}
