using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using Photon.Pun;

/// <summary>
/// 핑퐁 라켓 햅틱 피드백 컨트롤러
/// 로컬 플레이어(자신)의 패들에서만 진동이 발생하고, 상대방 패들에서는 진동이 발생하지 않습니다.
/// </summary>
public class PongRacketHaptics : MonoBehaviour
{
    [Header("컨트롤러 설정")]
    [SerializeField] private HapticImpulsePlayer hapticPlayer;

    [Header("진동 설정")]
    [SerializeField, Range(0, 1)] private float baseHapticIntensity = 0.3f;
    [SerializeField, Range(0, 0.5f)] private float baseHapticDuration = 0.1f;
    [SerializeField] private float maxVelocityScale = 5f; // 최대 속도 기준값

    [Header("네트워크 설정")]
    [SerializeField] private bool enableLocalPlayerOnlyHaptics = true; // 로컬 플레이어만 진동 허용

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;

    // 캐시된 PhotonView 참조
    private PhotonView parentPhotonView;
    private bool hasCheckedPhotonView = false;

    void Start()
    {
        // PhotonView 체크는 Start에서 한 번만 수행
        CheckPhotonView();
    }

    /// <summary>
    /// 부모 오브젝트에서 PhotonView를 찾아서 캐시
    /// </summary>
    private void CheckPhotonView()
    {
        if (hasCheckedPhotonView) return;

        // 부모 계층에서 PhotonView 찾기
        parentPhotonView = GetComponentInParent<PhotonView>();
        
        if (parentPhotonView == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[PongRacketHaptics] {gameObject.name}: PhotonView를 찾을 수 없습니다. 로컬 전용 모드로 동작합니다.");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[PongRacketHaptics] {gameObject.name}: PhotonView 발견 - 로컬 플레이어: {parentPhotonView.IsMine}");
        }

        hasCheckedPhotonView = true;
    }

    /// <summary>
    /// 현재 패들이 로컬 플레이어의 것인지 확인
    /// </summary>
    private bool IsLocalPlayerPaddle()
    {
        if (!enableLocalPlayerOnlyHaptics)
            return true; // 네트워크 제한이 비활성화된 경우 항상 진동 허용

        if (parentPhotonView == null)
            return true; // PhotonView가 없으면 로컬 전용으로 간주

        return parentPhotonView.IsMine; // Photon 네트워크에서 내 것인지 확인
    }

    // 핑퐁 볼과 라켓의 충돌 속도를 기반으로 진동 강도 조절
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Game_Ball"))
        {
            // 로컬 플레이어의 패들인지 확인
            if (!IsLocalPlayerPaddle())
            {
                if (showDebugLogs)
                    Debug.Log($"[PongRacketHaptics] {gameObject.name}: 상대방 패들이므로 진동 건너뜀");
                return; // 상대방 패들이면 진동하지 않음
            }

            // 충돌 속도 계산
            float impactVelocity = collision.relativeVelocity.magnitude;
            
            // 속도에 따른 진동 강도 계산 (0.3 ~ 1.0)
            float hapticIntensity = Mathf.Clamp(baseHapticIntensity + (impactVelocity / maxVelocityScale), 0.3f, 1.0f);
            
            // 속도에 따른 진동 지속 시간 계산 (0.05 ~ 0.2)
            float hapticDuration = Mathf.Clamp(baseHapticDuration + (impactVelocity / maxVelocityScale * 0.1f), 0.05f, 0.2f);
            
            // 진동 발생 (로컬 플레이어만)
            if (hapticPlayer != null)
            {
                hapticPlayer.SendHapticImpulse(hapticIntensity, hapticDuration);
                
                if (showDebugLogs)
                    Debug.Log($"[PongRacketHaptics] {gameObject.name}: 로컬 볼 충돌 진동 - 속도={impactVelocity:F2}, 강도={hapticIntensity:F2}, 지속시간={hapticDuration:F2}");
            }
            else
            {
                Debug.LogWarning($"[PongRacketHaptics] {gameObject.name}: HapticImpulsePlayer가 설정되지 않았습니다!");
            }
        }
    }

    /// <summary>
    /// 런타임에서 햅틱 활성화/비활성화 토글
    /// </summary>
    public void SetLocalPlayerOnlyHaptics(bool enabled)
    {
        enableLocalPlayerOnlyHaptics = enabled;
        if (showDebugLogs)
            Debug.Log($"[PongRacketHaptics] {gameObject.name}: 로컬 플레이어 전용 햅틱 = {enabled}");
    }

    /// <summary>
    /// 디버그 로그 토글
    /// </summary>
    public void SetDebugLogs(bool enabled)
    {
        showDebugLogs = enabled;
    }
}