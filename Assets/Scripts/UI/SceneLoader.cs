using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// MakingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadMakingRoomScene()
    {
        Debug.Log("MakingRoomScene으로 이동합니다.");
        SceneManager.LoadScene("MakingRoomScene");
    }
    
    /// <summary>
    /// ChoosingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadChoosingRoomScene()
    {
        Debug.Log("ChoosingRoomScene으로 이동합니다.");
        SceneManager.LoadScene("ChoosingRoomScene");
    }
    
    /// <summary>
    /// WaitingRoomScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadWaitingRoomScene()
    {
        Debug.Log("WaitingRoomScene으로 이동합니다.");
        SceneManager.LoadScene("WaitingRoomScene");
    }
    
    /// <summary>
    /// MainMenuScene으로 씬을 전환합니다.
    /// </summary>
    public void LoadMainMenuScene()
    {
        Debug.Log("MainMenuScene으로 이동합니다.");
        SceneManager.LoadScene("MainMenuScene");
    }
    
    /// <summary>
    /// 지정된 씬 이름으로 씬을 전환합니다.
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"{sceneName}으로 이동합니다.");
        SceneManager.LoadScene(sceneName);
    }
}