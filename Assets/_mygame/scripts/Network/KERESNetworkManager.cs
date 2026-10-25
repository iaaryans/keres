using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement; // NAYA ADD KIYA

public class KERESNetworkManager : MonoBehaviourPunCallbacks
{
    private const string RoomName = "KERES_TDM_TEST";
    private const string PlayerPrefabName = "Player";
    private const byte MaxPlayers = 4;

    private bool playerSpawned;

    private void Awake()
    {
        // JADOO: Scene change hone par is script ko destroy hone se bachao
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.IsConnectedAndReady)
        {
            JoinTestRoom();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("KERES: Connected to Photon Master Server.");
        JoinTestRoom();
    }

    private void JoinTestRoom()
    {
        if (PhotonNetwork.InRoom)
            return;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = MaxPlayers,
            BroadcastPropsChangeToAll = true
        };

        PhotonNetwork.JoinOrCreateRoom(RoomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("KERES: Joined room " + PhotonNetwork.CurrentRoom.Name + ". Total Players in room: " + PhotonNetwork.CurrentRoom.PlayerCount);

        // Agar hum already SampleScene mein hain tabhi spawn karo
        if (SceneManager.GetActiveScene().name == "SampleScene")
        {
             AttemptSpawnPlayer();
        }
    }

    // JADOO: Naya event listener jo tab trigger hoga jab scene badlega (Loader -> Map)
    public override void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
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
        
        float spawnX = actorNumber == 1 ? 0f : 5f; 
        Vector3 spawnPosition = new Vector3(spawnX, 2f, 0f);

        GameObject player = PhotonNetwork.Instantiate(PlayerPrefabName, spawnPosition, Quaternion.identity);

        if (player == null)
        {
            playerSpawned = false;
            Debug.LogError("KERES: Spawn failed. PLEASE CHECK THAT 'Player' prefab is inside 'Assets/Resources' folder.");
            return null;
        }

        playerSpawned = true;
        Debug.Log("KERES: Successfully Spawned network player for actor " + actorNumber + " at position " + spawnPosition);
        return player;
    }

    public void RespawnLocalPlayer(GameObject deadPlayerRoot)
    {
        if (!PhotonNetwork.InRoom || deadPlayerRoot == null)
            return;

        PhotonView view = deadPlayerRoot.GetComponent<PhotonView>();
        if (view == null || !view.IsMine)
            return;

        playerSpawned = false;
        PhotonNetwork.Destroy(deadPlayerRoot);
        
        // Chota sa delay daalo respawn karne se pehle
        Invoke("SpawnLocalPlayer", 1f); 
    }
}