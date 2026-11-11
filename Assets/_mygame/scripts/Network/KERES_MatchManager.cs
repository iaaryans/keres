using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class KERES_MatchManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static KERES_MatchManager Instance;

    private int blueScore = 0;
    private int redScore = 0;
    private KeresBattleHUD hud;

    private Dictionary<int, int> playerTeams = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        
        hud = FindFirstObjectByType<KeresBattleHUD>();
        if (hud != null) hud.UpdateScore(blueScore, redScore);

        if (PhotonNetwork.IsConnectedAndReady)
        {
            AssignTeam(PhotonNetwork.LocalPlayer);
        }
    }

    private void AssignTeam(Player player)
    {
        int team = (player.ActorNumber % 2 == 0) ? 1 : 0; 
        
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["Team"] = team;
        player.SetCustomProperties(props);
        playerTeams[player.ActorNumber] = team;
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Team"))
        {
            playerTeams[targetPlayer.ActorNumber] = (int)changedProps["Team"];
        }
    }

    public int GetLocalPlayerTeam()
    {
        int actorNr = PhotonNetwork.LocalPlayer.ActorNumber;
        if (playerTeams.ContainsKey(actorNr)) return playerTeams[actorNr];
        return (actorNr % 2 == 0) ? 1 : 0;
    }

    // Kill milne par Score endless badhta rahega
    [PunRPC]
    public void RPC_RegisterKill(int killerActorNumber, int victimActorNumber)
    {
        int killerTeam = (killerActorNumber % 2 == 0) ? 1 : 0;

        if (PhotonNetwork.IsMasterClient)
        {
            if (killerTeam == 0) blueScore++;
            else redScore++;
        }

        if (hud != null)
        {
            hud.UpdateScore(blueScore, redScore);
            string killerName = GetPlayerName(killerActorNumber);
            string victimName = GetPlayerName(victimActorNumber);
            hud.ShowKill(killerName, victimName);
        }
    }

    private string GetPlayerName(int actorNumber)
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == actorNumber) return p.NickName;
        }
        return "Unknown";
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(blueScore);
            stream.SendNext(redScore);
        }
        else
        {
            blueScore = (int)stream.ReceiveNext();
            redScore = (int)stream.ReceiveNext();
            if (hud != null) hud.UpdateScore(blueScore, redScore);
        }
    }
}