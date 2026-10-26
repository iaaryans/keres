using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class KERESNetworkManager : MonoBehaviourPunCallbacks
{
    private const string PlayerPrefabName = "Player"; 
    private bool playerSpawned;

    private void Awake()
    {
        // JADOO: Scene change hone par is script ko destroy hone se bachao
        DontDestroyOnLoad(this.gameObject);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("KERES: Master logic detected room join.");
        
        // Agar hum already SampleScene mein hain tabhi spawn karo
        if (SceneManager.GetActiveScene().name == "SampleScene")
        {
             AttemptSpawnPlayer();
        }
    }

    // Warning fix: override aur base use kiya gaya hai
    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            AttemptSpawnPlayer();
        }
    }

    private void AttemptSpawnPlayer()
    {
        if (playerSpawned) return;
        SpawnLocalPlayer();
    }

    private GameObject SpawnLocalPlayer()
    {
        if (!PhotonNetwork.InRoom || !PhotonNetwork.IsConnectedAndReady)
        {
            playerSpawned = false;
            return null;
        }

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        
        // Spawn positions
        float spawnX = actorNumber == 1 ? 0f : 5f; 
        Vector3 spawnPosition = new Vector3(spawnX, 2f, 0f);

        GameObject player = PhotonNetwork.Instantiate(PlayerPrefabName, spawnPosition, Quaternion.identity);

        if (player != null)
        {
            playerSpawned = true;
            Debug.Log($"KERES: Player {actorNumber} spawned at {spawnPosition}");
        }
        else
        {
            Debug.LogError("KERES: Spawn failed. PLEASE CHECK THAT 'Player' prefab is inside 'Assets/Resources' folder.");
        }
        
        return player;
    }

    public void RespawnLocalPlayer(GameObject deadPlayerRoot)
    {
        if (!PhotonNetwork.InRoom || deadPlayerRoot == null) return;

        PhotonView view = deadPlayerRoot.GetComponent<PhotonView>();
        if (view == null || !view.IsMine) return;

        playerSpawned = false;
        PhotonNetwork.Destroy(deadPlayerRoot);
        
        // Chota sa delay daalo respawn karne se pehle
        Invoke("SpawnLocalPlayer", 1f); 
    }
}