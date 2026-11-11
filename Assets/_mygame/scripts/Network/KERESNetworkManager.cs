using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class KERESNetworkManager : MonoBehaviourPunCallbacks
{
    private const string PlayerPrefabName = "Player"; 
    private bool playerSpawned;

    private void Awake() { DontDestroyOnLoad(this.gameObject); }

    public override void OnJoinedRoom()
    {
        if (SceneManager.GetActiveScene().name == "SampleScene") AttemptSpawnPlayer();
    }

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
        if (scene.name == "SampleScene") AttemptSpawnPlayer();
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

        Vector3 spawnPosition = new Vector3(0f, 2f, 0f); 
        Quaternion spawnRotation = Quaternion.identity;

        int myTeam = (PhotonNetwork.LocalPlayer.ActorNumber % 2 == 0) ? 1 : 0; 

        string containerName = (myTeam == 0) ? "BlueSpawns" : "RedSpawns";
        GameObject teamContainer = GameObject.Find(containerName);

        if (teamContainer != null)
        {
            Transform[] points = teamContainer.GetComponentsInChildren<Transform>();
            if (points.Length > 1) 
            {
                int randomIndex = Random.Range(1, points.Length);
                
                // JADOO FIX: Sirf X aur Z axis mein thoda idhar-udhar hoga, Y hamesha original point jaisa rahega!
                Vector3 randomOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                
                spawnPosition = points[randomIndex].position + randomOffset;
                spawnRotation = points[randomIndex].rotation;
            }
        }
        else
        {
            Debug.LogWarning($"[KERES] {containerName} nahi mila! Check karo.");
        }

        GameObject player = PhotonNetwork.Instantiate(PlayerPrefabName, spawnPosition, spawnRotation);

        if (player != null)
        {
            playerSpawned = true;
            KeresBattleHUD hud = FindFirstObjectByType<KeresBattleHUD>();
            if (hud != null && hud.respawnPanel != null) hud.respawnPanel.SetActive(false);
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
        
        KeresBattleHUD hud = FindFirstObjectByType<KeresBattleHUD>();
        if (hud != null) hud.StartRespawnCountdown(3f);
        
        Invoke("SpawnLocalPlayer", 3f); 
    }
}