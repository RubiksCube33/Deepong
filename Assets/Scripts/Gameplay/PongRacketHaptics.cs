using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

public class PongRacketHaptics : MonoBehaviour
{
    [Header("컨트롤러 설정")]
    [SerializeField] private HapticImpulsePlayer hapticPlayer;
    [SerializeField] private bool isLeftHand = true;

    [Header("진동 설정")]
    [SerializeField, Range(0, 1)] private float baseHapticIntensity = 0.3f;
    [SerializeField, Range(0, 0.5f)] private float baseHapticDuration = 0.1f;
    [SerializeField] private float maxVelocityScale = 5f; // 최대 속도 기준값

    // 핑퐁 볼과 라켓의 충돌 속도를 기반으로 진동 강도 조절
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Game_Ball"))
        {
            // 충돌 속도 계산
            float impactVelocity = collision.relativeVelocity.magnitude;
            
            // 속도에 따른 진동 강도 계산 (0.3 ~ 1.0)
            float hapticIntensity = Mathf.Clamp(baseHapticIntensity + (impactVelocity / maxVelocityScale), 0.3f, 1.0f);
            
            // 속도에 따른 진동 지속 시간 계산 (0.05 ~ 0.2)
            float hapticDuration = Mathf.Clamp(baseHapticDuration + (impactVelocity / maxVelocityScale * 0.1f), 0.05f, 0.2f);
            
            // 진동 발생
            if (hapticPlayer != null)
            {
                hapticPlayer.SendHapticImpulse(hapticIntensity, hapticDuration);
                Debug.Log($"볼 충돌: 속도={impactVelocity}, 강도={hapticIntensity}, 지속시간={hapticDuration}");
            }
            else
            {
                Debug.LogWarning("HapticImpulsePlayer가 설정되지 않았습니다!");
            }
        }
    }
}