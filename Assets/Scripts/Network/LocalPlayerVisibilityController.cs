using UnityEngine;
using Photon.Pun;

/// <summary>
/// 로컬 플레이어의 로봇 아바타를 보이지 않게 하는 컨트롤러
/// 상대방의 로봇은 보이고, 자신의 로봇은 시야를 가리지 않도록 숨김
/// </summary>
public class LocalPlayerVisibilityController : MonoBehaviourPun
{
    [Header("Robot References")]
    [SerializeField] private GameObject robotObject; // Robot GameObject 참조
    
    [Header("Settings")]
    [SerializeField] private bool hideLocalPlayer = true; // 로컬 플레이어 숨김 여부
    [SerializeField] private bool debugMode = false; // 디버그 모드
    
    private Renderer[] robotRenderers; // Robot의 모든 렌더러들
    private bool isInitialized = false;

    void Start()
    {
        // 조금 지연시켜서 다른 컴포넌트들이 초기화될 시간을 줌
        Invoke(nameof(InitializeVisibility), 0.1f);
    }

    /// <summary>
    /// 가시성 초기화
    /// </summary>
    private void InitializeVisibility()
    {
        if (isInitialized) return;

        // Robot 오브젝트 자동 찾기
        if (robotObject == null)
        {
            robotObject = FindRobotObject();
        }

        if (robotObject == null)
        {
            if (debugMode)
                Debug.LogWarning($"[{gameObject.name}] Robot 오브젝트를 찾을 수 없습니다.");
            return;
        }

        // Robot의 모든 렌더러 찾기
        robotRenderers = robotObject.GetComponentsInChildren<Renderer>(true);
        
        if (debugMode)
        {
            Debug.Log($"[{gameObject.name}] Robot 렌더러 {robotRenderers.Length}개 발견");
            Debug.Log($"[{gameObject.name}] PhotonView.IsMine: {photonView.IsMine}");
        }

        // 로컬 플레이어인 경우 Robot 숨김
        if (photonView.IsMine && hideLocalPlayer)
        {
            SetRobotVisibility(false);
            
            if (debugMode)
                Debug.Log($"[{gameObject.name}] 로컬 플레이어의 Robot을 숨김처리했습니다.");
        }
        else
        {
            SetRobotVisibility(true);
            
            if (debugMode)
                Debug.Log($"[{gameObject.name}] 원격 플레이어의 Robot을 표시합니다.");
        }

        isInitialized = true;
    }

    /// <summary>
    /// Robot 오브젝트 자동 찾기
    /// </summary>
    private GameObject FindRobotObject()
    {
        // 하위에서 "Robot"이라는 이름의 GameObject 찾기
        Transform robotTransform = transform.Find("Robot");
        if (robotTransform != null)
        {
            return robotTransform.gameObject;
        }

        // 더 깊이 찾기
        robotTransform = GetComponentInChildren<Transform>().Find("Robot");
        if (robotTransform != null)
        {
            return robotTransform.gameObject;
        }

        // 이름에 "Robot"이 포함된 하위 오브젝트 찾기
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains("Robot"))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Robot의 가시성 설정
    /// </summary>
    /// <param name="visible">표시 여부</param>
    private void SetRobotVisibility(bool visible)
    {
        if (robotRenderers == null) return;

        foreach (Renderer renderer in robotRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }

    /// <summary>
    /// 런타임에서 가시성 토글 (디버그용)
    /// </summary>
    [ContextMenu("Toggle Robot Visibility")]
    public void ToggleRobotVisibility()
    {
        if (!isInitialized) return;

        bool currentVisibility = robotRenderers.Length > 0 && robotRenderers[0].enabled;
        SetRobotVisibility(!currentVisibility);
        
        if (debugMode)
            Debug.Log($"[{gameObject.name}] Robot 가시성 토글: {!currentVisibility}");
    }

    /// <summary>
    /// 수동으로 Robot 오브젝트 설정
    /// </summary>
    /// <param name="robot">Robot GameObject</param>
    public void SetRobotObject(GameObject robot)
    {
        robotObject = robot;
        isInitialized = false;
        InitializeVisibility();
    }

    // 에디터에서 컴포넌트 추가시 자동으로 설정
#if UNITY_EDITOR
    void Reset()
    {
        // Robot 오브젝트 자동 찾기 시도
        robotObject = FindRobotObject();
        
        if (robotObject != null)
        {
            Debug.Log($"Robot 오브젝트 자동 설정: {robotObject.name}");
        }
    }
#endif
} 