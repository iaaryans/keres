using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class KERESNetworkManager : MonoBehaviourPunCallbacks
{
    private const string RoomName = "KERES_TDM_TEST";
    private const string PlayerPrefabName = "Player"; // Dhyan rakhein, Resources folder mein prefab ka naam exact 'Player' hona chahiye.
    private const byte MaxPlayers = 4;

    private bool playerSpawned;

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

        if (playerSpawned)
            return;

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("KERES: Photon is not ready to spawn the player.");
            return;
        }

        playerSpawned = true;
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
        
        // JADOO: Dono players ko bilkul paas (0 aur 5 ki doori par) spawn kara rahe hain. Height thodi upar rakhi hai.
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
        SpawnLocalPlayer();
    }
}