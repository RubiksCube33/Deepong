using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WheelRotationController : MonoBehaviour
{
    [Tooltip("회전 속도 계수")]
    public float rotationSpeed = 15f;
    
    [Tooltip("바퀴 회전 축 (로컬 공간)")]
    public Vector3 wheelAxis = Vector3.right; // X축을 바퀴 회전 축으로 설정
    
    [Tooltip("방향 전환 속도")]
    public float orientationSpeed = 8f;
    
    [Tooltip("최소 속도 (이 값 이하에서는 회전하지 않음)")]
    public float minVelocityThreshold = 0.1f;
    
    private Rigidbody rb;
    private Quaternion targetOrientation;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        targetOrientation = transform.rotation;
    }
    
    void FixedUpdate()
    {
        // 속도가 임계값보다 큰 경우에만 회전 적용
        if (rb.velocity.magnitude > minVelocityThreshold)
        {
            // 이동 방향 계산
            Vector3 movementDirection = rb.velocity.normalized;
            
            // 1. 바퀴 회전 - 속도에 비례하여 wheelAxis 축을 중심으로 회전
            float rotationAmount = rb.velocity.magnitude * rotationSpeed;
            
            // 로컬 회전 축을 월드 공간으로 변환
            Vector3 worldWheelAxis = transform.TransformDirection(wheelAxis);
            
            // 회전 적용 (바퀴 회전 축을 중심으로)
            rb.AddTorque(worldWheelAxis * rotationAmount, ForceMode.Acceleration);
            
            // 2. 방향 전환 - 바퀴가 이동 방향을 향하도록 함
            if (Vector3.Dot(movementDirection, Vector3.up) < 0.9f) // 거의 수직으로 움직이는 경우는 제외
            {
                // 바퀴의 앞쪽 방향 (wheelAxis와 수직인 방향)
                Vector3 wheelForward = Vector3.Cross(Vector3.up, wheelAxis).normalized;
                
                // 이동 방향을 XZ 평면에 투영하여 바퀴가 수평으로만 회전하도록 함
                Vector3 flatDirection = new Vector3(movementDirection.x, 0, movementDirection.z).normalized;
                
                if (flatDirection.magnitude > 0.1f) // 수평 방향 속도가 있는 경우만
                {
                    // 목표 회전 - 바퀴 축은 유지하면서 앞쪽이 이동 방향을 향하도록
                    targetOrientation = Quaternion.LookRotation(flatDirection, Vector3.up);
                    
                    // 현재 회전에서 목표 회전으로 부드럽게 보간
                    Quaternion newRotation = Quaternion.Slerp(
                        transform.rotation, 
                        targetOrientation, 
                        Time.fixedDeltaTime * orientationSpeed
                    );
                    
                    // 회전 적용 (Rigidbody의 회전을 직접 설정)
                    rb.MoveRotation(newRotation);
                }
            }
        }
    }
}