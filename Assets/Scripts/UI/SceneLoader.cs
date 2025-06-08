using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// MakingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadMakingRoomScene()
    {
        Debug.Log("MakingRoomScene으로 이동합니다.");
        LoadSceneWithPhoton("MakingRoomScene");
    }
    
    /// <summary>
    /// ChoosingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadChoosingRoomScene()
    {
        Debug.Log("ChoosingRoomScene으로 이동합니다.");
        LoadSceneWithPhoton("ChoosingRoomScene");
    }
    
    /// <summary>
    /// WaitingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadWaitingRoomScene()
    {
        Debug.Log("WaitingRoomScene으로 이동합니다.");
        LoadSceneWithPhoton("WaitingRoomScene");
    }
    
    /// <summary>
    /// MainMenuScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadMainMenuScene()
    {
        Debug.Log("MainMenuScene으로 이동합니다.");
        LoadSceneWithPhoton("MainMenuScene");
    }
    
    /// <summary>
    /// 지정된 씬 이름으로 씬을 전환합니다.
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"{sceneName}으로 이동합니다.");
        LoadSceneWithPhoton(sceneName);
    }
    
    /// <summary>
    /// Photon 네트워크를 고려한 씬 전환 메서드
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    private void LoadSceneWithPhoton(string sceneName)
    {
        // Photon에 연결되어 있고 방에 있는 경우
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            // 마스터 클라이언트인 경우에만 PhotonNetwork.LoadLevel 사용
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"마스터 클라이언트로서 {sceneName}을 로드합니다. (방 유지)");
                PhotonNetwork.LoadLevel(sceneName);
            }
            else
            {
                Debug.Log("마스터 클라이언트가 아니므로 씬 로드를 기다립니다.");
                // 마스터 클라이언트가 아닌 경우 씬 전환을 마스터에게 요청하거나 대기
                // 자동 씬 동기화가 활성화되어 있다면 마스터가 씬을 바꾸면 자동으로 따라감
            }
        }
        // Photon에 연결되어 있지만 방에 없는 경우
        else if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
        {
            Debug.Log($"방에 없는 상태로 {sceneName}을 로드합니다.");
            SceneManager.LoadScene(sceneName);
        }
        // Photon에 연결되어 있지 않은 경우
        else
        {
            Debug.Log($"오프라인 상태로 {sceneName}을 로드합니다.");
            SceneManager.LoadScene(sceneName);
        }
    }
}