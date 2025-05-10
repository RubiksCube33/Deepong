using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallRotationController : MonoBehaviour
{
    [Tooltip("회전 속도 계수")]
    public float rotationSpeed = 10f;
    
    [Tooltip("기울임 강도")]
    public float tiltStrength = 15f;
    
    [Tooltip("최소 속도 (이 값 이하에서는 회전하지 않음)")]
    public float minVelocityThreshold = 0.1f;
    
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 movementDirection;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
    }
    
    void FixedUpdate()
    {
        // 속도가 임계값보다 큰 경우에만 회전 적용
        if (rb.velocity.magnitude > minVelocityThreshold)
        {
            // 이동 방향 계산
            movementDirection = rb.velocity.normalized;
            
            // 1. 이동 방향에 따른 회전 축 계산
            Vector3 rotationAxis = Vector3.Cross(Vector3.up, movementDirection);
            
            // 2. 속도에 비례한 회전 각도 계산
            float rotationAmount = rb.velocity.magnitude * rotationSpeed;
            
            // 3. 회전 적용
            rb.AddTorque(rotationAxis * rotationAmount, ForceMode.Acceleration);
            
            // 4. 이동 방향으로 약간 기울임 적용
            ApplyTilt(movementDirection);
        }
    }
    
    // 이동 방향으로 공을 약간 기울이는 함수
    private void ApplyTilt(Vector3 direction)
    {
        // 목표 기울기 계산 (이동 방향으로 약간 기울임)
        Quaternion targetTilt = Quaternion.FromToRotation(Vector3.up, Vector3.up + direction * 0.2f);
        
        // 현재 회전에서 목표 기울기로 부드럽게 보간
        Quaternion targetRotation = targetTilt * transform.rotation;
        
        // 토크를 사용하여 자연스러운 기울임 적용
        Quaternion rotationDifference = targetRotation * Quaternion.Inverse(transform.rotation);
        
        // 회전 차이를 오일러 각으로 변환하고 토크로 적용
        Vector3 rotationAxis;
        float rotationAngle;
        rotationDifference.ToAngleAxis(out rotationAngle, out rotationAxis);
        
        // 각도가 180도를 넘으면 반대 방향으로 회전 (최단 경로)
        if (rotationAngle > 180f)
        {
            rotationAngle -= 360f;
        }
        
        // 토크 적용 (각도와 방향에 비례)
        if (rotationAngle != 0f) // NaN 방지
        {
            Vector3 torque = rotationAxis.normalized * rotationAngle * tiltStrength;
            rb.AddTorque(torque, ForceMode.Acceleration);
        }
    }
}