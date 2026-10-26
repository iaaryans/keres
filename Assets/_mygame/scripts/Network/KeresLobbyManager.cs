using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; 

public class KeresLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Screens (Canvas Groups)")]
    public CanvasGroup splashScreen;
    public CanvasGroup loaderScreen;
    public GameObject mainMenuPanel; 

    [Header("Main Menu UI")]
    public TMP_InputField playerNameInput; 
    public TMP_InputField roomCodeInput;   
    public Button createRoomBtn;
    public Button joinRoomBtn;

    [Header("Loading UI")]
    public Slider loadingSlider;

    [Header("Timings")]
    public float splashDuration = 2.5f; 
    public float fadeSpeed = 1.5f;      
    public float fakeLoadTime = 4f;     

    private void Awake()
    {
        if (splashScreen != null) { splashScreen.alpha = 1f; splashScreen.gameObject.SetActive(true); }
        if (loaderScreen != null) { loaderScreen.alpha = 0f; loaderScreen.gameObject.SetActive(true); }
        if (loadingSlider != null) loadingSlider.value = 0f;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    private void Start()
    {
        if (createRoomBtn != null) createRoomBtn.interactable = false;
        if (joinRoomBtn != null) joinRoomBtn.interactable = false;

        PhotonNetwork.ConnectUsingSettings();
        StartCoroutine(LobbyFlowRoutine());
    }

    private IEnumerator LobbyFlowRoutine()
    {
        yield return new WaitForSeconds(splashDuration);

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            splashScreen.alpha = Mathf.Lerp(1f, 0f, t);
            loaderScreen.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        
        splashScreen.gameObject.SetActive(false);
        loaderScreen.alpha = 1f;

        float loadProgress = 0f;
        while (loadProgress < 1f)
        {
            loadProgress += Time.deltaTime / fakeLoadTime;
            if (loadingSlider != null) loadingSlider.value = loadProgress;
            yield return null;
        }

        loaderScreen.gameObject.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // --- PHOTON MULTIPLAYER LOGIC ---
    public override void OnConnectedToMaster()
    {
        Debug.Log("[KERES] Connected to Photon Server!");
        PhotonNetwork.JoinLobby(); 
        PhotonNetwork.AutomaticallySyncScene = true; 
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[KERES] Joined Matchmaking Lobby!");
        if (createRoomBtn != null) createRoomBtn.interactable = true;
        if (joinRoomBtn != null) joinRoomBtn.interactable = true;
    }

    public void CreateGameRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || string.IsNullOrEmpty(roomCodeInput.text))
        {
            Debug.LogWarning("[KERES] Naam aur Room Code dono daalna zaroori hai!");
            return;
        }

        PhotonNetwork.NickName = playerNameInput.text;
        RoomOptions options = new RoomOptions { MaxPlayers = 4, BroadcastPropsChangeToAll = true };
        PhotonNetwork.CreateRoom(roomCodeInput.text, options);
    }

    public void JoinGameRoom()
    {
        if (string.IsNullOrEmpty(playerNameInput.text) || string.IsNullOrEmpty(roomCodeInput.text))
        {
            Debug.LogWarning("[KERES] Naam aur Room Code dono daalna zaroori hai!");
            return;
        }

        PhotonNetwork.NickName = playerNameInput.text;
        PhotonNetwork.JoinRoom(roomCodeInput.text);
    }

    // JADOO: Agar same code se dusra player create karega, to Photon mana kar dega aur ye error aayega
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[KERES] Room Create Failed! Sayad is code ({roomCodeInput.text}) se room pehle hi bana hua hai. Join dabao.");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[KERES] Room nahi mila! Pehle kisi ko Create karna padega.");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[KERES] Room '{PhotonNetwork.CurrentRoom.Name}' joined! Loading Map...");
        PhotonNetwork.LoadLevel("MatchLoaderScene"); 
    }
}