using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // 비동기 씬 로드 코루틴 시작
        StartCoroutine(LoadGameSceneAsync());
    }
    
    private IEnumerator LoadGameSceneAsync()
    {
        // 씬을 비동기적으로 로드
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync("CourtScene");
        
        // 씬이 백그라운드에서 로드되는 동안 사용자에게 보여지지 않도록 설정
        asyncOperation.allowSceneActivation = false;
        
        // 씬이 90% 로드될 때까지 대기
        while (asyncOperation.progress < 0.9f)
        {
            yield return null;
        }
        
        // 씬 활성화 허용
        asyncOperation.allowSceneActivation = true;
        
        // 씬이 완전히 로드될 때까지 대기
        while (!asyncOperation.isDone)
        {
            yield return null;
        }
        
        // 씬 로드 후 추가 초기화
        InitializeGameScene();
    }
    
    private void InitializeGameScene()
    {
        // 디렉셔널 라이트 설정
        GameObject directionalLight = GameObject.Find("Directional Light");
        if (directionalLight != null)
        {
            Light light = directionalLight.GetComponent<Light>();
            if (light != null)
            {
                // 필요한 경우 조명 설정 조정
                // light.intensity = 1.0f;
                // light.color = Color.white;
            }
        }
    }
    
    public void OpenSettings()
    {
        // Enable Settings UI panel (나중에 구현)
        Debug.Log("설정 메뉴 열기");
    }
    
    public void QuitGame()
    {
        // Quit Game
        Application.Quit();
        // Output message when testing in editor
        Debug.Log("게임 종료");
    }
}