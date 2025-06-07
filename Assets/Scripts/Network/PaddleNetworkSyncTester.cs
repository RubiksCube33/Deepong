using UnityEngine;
using Photon.Pun;

/// <summary>
/// PaddleNetworkSync 컴포넌트의 기능을 테스트하기 위한 유틸리티 클래스
/// </summary>
public class PaddleNetworkSyncTester : MonoBehaviour
{
    [Header("테스트 대상")]
    [SerializeField] private PaddleNetworkSync paddleNetworkSync;
    [SerializeField] private bool autoFindPaddleSync = true;
    
    [Header("테스트 설정")]
    [SerializeField] private bool enableRuntimeTesting = true;
    [SerializeField] private KeyCode toggleSyncKey = KeyCode.P;
    [SerializeField] private KeyCode debugInfoKey = KeyCode.I;
    
    void Start()
    {
        if (autoFindPaddleSync && paddleNetworkSync == null)
        {
            paddleNetworkSync = GetComponent<PaddleNetworkSync>();
            if (paddleNetworkSync == null)
            {
                paddleNetworkSync = FindObjectOfType<PaddleNetworkSync>();
            }
        }
    }
    
    void Update()
    {
        if (!enableRuntimeTesting || paddleNetworkSync == null) return;
        
        // P키로 패들 동기화 토글
        if (Input.GetKeyDown(toggleSyncKey))
        {
            bool currentState = paddleNetworkSync.IsPaddleSyncEnabled;
            paddleNetworkSync.SetPaddleSyncEnabled(!currentState);
            Debug.Log($"[PaddleNetworkSyncTester] 패들 동기화 {(!currentState ? "활성화" : "비활성화")}");
        }
        
        // I키로 디버그 정보 출력
        if (Input.GetKeyDown(debugInfoKey))
        {
            PrintDebugInfo();
        }
    }
    
    [ContextMenu("Print Debug Info")]
    void PrintDebugInfo()
    {
        if (paddleNetworkSync == null)
        {
            Debug.LogWarning("[PaddleNetworkSyncTester] PaddleNetworkSync 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("=== PaddleNetworkSync 디버그 정보 ===");
        Debug.Log($"패들 동기화 활성화: {paddleNetworkSync.IsPaddleSyncEnabled}");
        Debug.Log($"네트워크 데이터 수신됨: {paddleNetworkSync.HasReceivedNetworkData}");
        Debug.Log($"Photon 연결 상태: {PhotonNetwork.IsConnected}");
        Debug.Log($"로컬 플레이어: {(paddleNetworkSync.photonView != null ? paddleNetworkSync.photonView.IsMine : "PhotonView 없음")}");
        
        // PaddleChangeController 정보
        var paddleController = paddleNetworkSync.GetComponent<DeepongVR.Court.PaddleChangeController>();
        if (paddleController != null)
        {
            Debug.Log($"현재 패들 타입: {paddleController.CurrentPaddleIndex} ({paddleController.CurrentPaddleName})");
        }
        else
        {
            Debug.LogWarning("PaddleChangeController를 찾을 수 없습니다!");
        }
        
        // VRHumanoidController 정보
        var vrController = paddleNetworkSync.GetComponent<VRHumanoidController>();
        if (vrController != null)
        {
            Debug.Log($"VR 컨트롤러 연결:");
            Debug.Log($"  왼손: {(vrController.LeftHandController != null ? vrController.LeftHandController.name : "null")}");
            Debug.Log($"  오른손: {(vrController.RightHandController != null ? vrController.RightHandController.name : "null")}");
            Debug.Log($"  헤드셋: {(vrController.Headset != null ? vrController.Headset.name : "null")}");
        }
        else
        {
            Debug.LogWarning("VRHumanoidController를 찾을 수 없습니다!");
            
            // 대체 VR 컨트롤러 찾기 시도
            Debug.Log("대체 VR 컴포넌트 검색:");
            var allVRControllers = FindObjectsOfType<VRHumanoidController>();
            Debug.Log($"씬에 있는 VRHumanoidController 수: {allVRControllers.Length}");
            
            for (int i = 0; i < allVRControllers.Length; i++)
            {
                var controller = allVRControllers[i];
                Debug.Log($"  VRController {i}: {controller.name}");
                Debug.Log($"    왼손: {(controller.LeftHandController != null ? controller.LeftHandController.name : "null")}");
                Debug.Log($"    오른손: {(controller.RightHandController != null ? controller.RightHandController.name : "null")}");
            }
        }
        
        // AudioSource 관련 진단
        Debug.Log("=== AudioSource 진단 ===");
        var paddleControllers = FindObjectsOfType<PaddleController>();
        Debug.Log($"씬에 있는 PaddleController 수: {paddleControllers.Length}");
        
        for (int i = 0; i < paddleControllers.Length; i++)
        {
            var paddle = paddleControllers[i];
            var audioSource = paddle.GetComponent<AudioSource>();
            Debug.Log($"PaddleController {i} ({paddle.name}):");
            Debug.Log($"  AudioSource: {(audioSource != null ? "있음" : "없음")}");
            if (audioSource != null)
            {
                Debug.Log($"  AudioClip: {(audioSource.clip != null ? audioSource.clip.name : "없음")}");
            }
        }
        
        Debug.Log("=====================================");
    }
    
    [ContextMenu("Toggle Paddle Sync")]
    void TogglePaddleSync()
    {
        if (paddleNetworkSync != null)
        {
            bool currentState = paddleNetworkSync.IsPaddleSyncEnabled;
            paddleNetworkSync.SetPaddleSyncEnabled(!currentState);
            Debug.Log($"[PaddleNetworkSyncTester] 패들 동기화 {(!currentState ? "활성화" : "비활성화")}");
        }
    }
    
    [ContextMenu("Test Network Connection")]
    void TestNetworkConnection()
    {
        Debug.Log("=== 네트워크 연결 테스트 ===");
        Debug.Log($"Photon 연결됨: {PhotonNetwork.IsConnected}");
        Debug.Log($"방에 참가됨: {PhotonNetwork.InRoom}");
        Debug.Log($"플레이어 수: {PhotonNetwork.PlayerList.Length}");
        Debug.Log($"로컬 플레이어 ID: {PhotonNetwork.LocalPlayer.ActorNumber}");
        
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("방의 플레이어들:");
            foreach (var player in PhotonNetwork.PlayerList)
            {
                Debug.Log($"  - {player.NickName} (ID: {player.ActorNumber})");
            }
        }
        Debug.Log("=========================");
    }
    
    void OnGUI()
    {
        if (!enableRuntimeTesting || paddleNetworkSync == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("PaddleNetworkSync 테스터", GUI.skin.box);
        
        GUILayout.Label($"패들 동기화: {paddleNetworkSync.IsPaddleSyncEnabled}");
        GUILayout.Label($"네트워크 데이터: {paddleNetworkSync.HasReceivedNetworkData}");
        GUILayout.Label($"Photon 연결: {PhotonNetwork.IsConnected}");
        
        if (GUILayout.Button($"{toggleSyncKey} - 동기화 토글"))
        {
            TogglePaddleSync();
        }
        
        if (GUILayout.Button($"{debugInfoKey} - 디버그 정보"))
        {
            PrintDebugInfo();
        }
        
        GUILayout.EndArea();
    }
} 