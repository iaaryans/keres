using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // TextMeshPro ke liye zaroori

// JADOO 1: MonoBehaviour ki jagah MonoBehaviourPunCallbacks lagaya taaki Photon ke events sun sakein
public class KeresLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Screens (Canvas Groups)")]
    public CanvasGroup splashScreen;
    public CanvasGroup loaderScreen;
    public GameObject mainMenuPanel; // Fix: Ye variable ab declare ho gaya hai

    [Header("Main Menu UI")]
    public TMP_InputField playerNameInput; // Naam daalne wala dabba
    public TMP_InputField roomCodeInput;   // Code daalne wala dabba
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
        if (splashScreen != null)
        {
            splashScreen.alpha = 1f;
            splashScreen.gameObject.SetActive(true);
        }
        
        if (loaderScreen != null)
        {
            loaderScreen.alpha = 0f;
            loaderScreen.gameObject.SetActive(true); 
        }

        if (loadingSlider != null) loadingSlider.value = 0f;
        
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    private void Start()
    {
        // Shuru mein buttons band rakho jab tak Photon server se connect na ho jaye
        if (createRoomBtn != null) createRoomBtn.interactable = false;
        if (joinRoomBtn != null) joinRoomBtn.interactable = false;

        // JADOO 2: Background mein chupke se Photon Master Server se connect karo
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
        splashScreen.alpha = 0f;
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
        PhotonNetwork.JoinLobby(); // Lobby join karo taaki rooms bana/dhoondh sakein
        PhotonNetwork.AutomaticallySyncScene = true; 
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[KERES] Joined Matchmaking Lobby!");
        // Ab player buttons daba sakta hai
        if (createRoomBtn != null) createRoomBtn.interactable = true;
        if (joinRoomBtn != null) joinRoomBtn.interactable = true;
    }

    // Ye function "Create Room" button par lagayenge
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

    // Ye function "Join Room" button par lagayenge
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

    public override void OnJoinedRoom()
    {
        Debug.Log($"[KERES] Room '{PhotonNetwork.CurrentRoom.Name}' joined! Loading Map...");
        
        PhotonNetwork.LoadLevel("MatchLoaderScene"); 
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[KERES] Room nahi mila ya full hai: " + message);
    }
}