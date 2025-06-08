using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static NetworkManager Instance { get; private set; }
    
    [Header("Photon 설정")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 2;
    
    [Header("씬 이름들")]
    public string mainMenuScene = "MainMenuScene";
    public string makingRoomScene = "MakingRoomScene";
    public string choosingRoomScene = "ChoosingRoomScene";
    public string courtScene = "CourtScene";
    
    private bool isInitialized = false;
    
    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePhoton();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePhoton()
    {
        if (isInitialized) return;
        
        // Photon 기본 설정
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = true;
        
        // 닉네임 설정
        if (string.IsNullOrEmpty(PhotonNetwork.NickName))
        {
            PhotonNetwork.NickName = "Player_" + Random.Range(1000, 10000);
        }
        
        isInitialized = true;
        Debug.Log("NetworkManager 초기화 완료");
    }
    
    public void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Photon 서버에 연결 중...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("이미 Photon에 연결되어 있습니다.");
        }
    }
    
    public void JoinLobbyIfNeeded()
    {
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InLobby)
        {
            Debug.Log("로비에 참가 중...");
            PhotonNetwork.JoinLobby();
        }
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon 마스터 서버에 연결되었습니다.");
    }
    
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"Photon 연결이 끊어졌습니다: {cause}");
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 네트워크 동기화가 필요한 경우 구현
    }
} 