using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Random = UnityEngine.Random;

public class BallController : MonoBehaviourPun
{
    private Rigidbody rb;
    private float speed = 150f;
    private Renderer rend;
    
    [Header("속도 설정")]
    public float initialSpeed = 5.0f;
    public float minSpeed = 3.0f;
    public float maxSpeed = 15.0f;
    
    [Header("VR 상호작용 설정")]
    public float baseForce = 10.0f;
    public float velocityMultiplier = 0.5f;
    
    [Header("디버그")]
    public bool applyInitialForceOnStart = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rend = GetComponent<Renderer>();
    }
    
    void ApplyInitialForce()
    {
        if (rb == null) return;
        
        // 속도가 없는 경우에만 초기 속도 부여
        if (rb.velocity.magnitude < 0.1f)
        {
            // 랜덤한 방향 (오른쪽 또는 왼쪽으로 약간의 랜덤성 추가)
            float randomAngle = Random.Range(-30f, 30f); // -30도 ~ 30도 사이의 각도
            Vector3 direction = Quaternion.Euler(0, randomAngle, 0) * Vector3.right;
            
            // 속도 적용
            rb.velocity = direction * initialSpeed;
            
            Debug.Log($"공에 초기 속도 적용: {rb.velocity}, 속도: {rb.velocity.magnitude}");
        }
    }
    
    // 직접 공에 힘을 가하는 메서드 (외부에서 호출 가능)
    public void ApplyForce(Vector3 direction, float force)
    {
        if (rb == null) return;
        
        // 힘 적용
        rb.velocity = direction.normalized * Mathf.Clamp(force, minSpeed, maxSpeed);
        
        // 네트워크 환경에서 동기화를 위해 소유권 요청
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            photonView.RequestOwnership();
        }
    }
    
    private void Update()
    {
        // 속도 감시 및 보정 (너무 느리거나 빠르지 않도록)
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            // 속도가 너무 느릴 경우 최소 속도로 보정
            if (rb.velocity.magnitude < minSpeed)
            {
                rb.velocity = rb.velocity.normalized * minSpeed;
            }
            // 속도가 너무 빠를 경우 최대 속도로 제한
            else if (rb.velocity.magnitude > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
        
        // 디버그 - 키보드 입력으로 공에 힘 적용 (테스트용)
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ApplyInitialForce();
            
            // 네트워크에서 공의 소유권 요청
            if (PhotonNetwork.IsConnected && photonView != null)
            {
                photonView.RequestOwnership();
                
                // 마스터 클라이언트에서 실행 중이고 모든 클라이언트에게 공 속도 적용 호출
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("RPC_ApplyInitialForce", RpcTarget.Others);
                }
            }
        }
        #endif
    }
    
    [PunRPC]
    void RPC_ApplyInitialForce()
    {
        ApplyInitialForce();
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];
        Vector3 hitDirection = (collision.transform.position - contact.point).normalized;
        Vector3 forceDirection = hitDirection;
        float forceMagnitude = baseForce;
        
        // VR 컨트롤러 검출
        if (collision.gameObject.CompareTag("VRController"))
        {
            // VR 컨트롤러의 속도 정보 가져오기
            Rigidbody controllerRb = collision.gameObject.GetComponent<Rigidbody>();
            if (controllerRb != null)
            {
                // 컨트롤러의 속도를 기본 힘에 더함
                Vector3 controllerVelocity = controllerRb.velocity;
                forceDirection = (hitDirection + controllerVelocity.normalized) / 2f;
                forceMagnitude = baseForce + (controllerVelocity.magnitude * velocityMultiplier);
                rb.velocity = forceDirection * forceMagnitude;
                
                Debug.Log($"컨트롤러 속도: {controllerVelocity.magnitude}, 적용된 힘: {forceMagnitude}");
            }
        }
        else
        {
            // 일반 충돌일 경우 반사 방향으로 약간의 속도 유지
            Vector3 reflectDir = Vector3.Reflect(rb.velocity.normalized, contact.normal);
            float currentSpeed = rb.velocity.magnitude;
            
            // 속도가 감소하지 않도록 유지 (약간의 가속 적용)
            rb.velocity = reflectDir * Mathf.Max(currentSpeed, minSpeed) * 1.05f;
        }
        
        // 색상 변경
        Color newColor = new Color(Random.value, Random.value, Random.value);
        rend.material.color = newColor;
    }
}
