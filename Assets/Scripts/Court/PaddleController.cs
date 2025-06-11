using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PaddleController : MonoBehaviour
{
    private AudioSource sfxSource;
    private ScoreManager scoreManager;
    private VisualEffect vfxComponent;
    
    [Header("파티클 효과 설정")]
    [SerializeField] private VisualEffectAsset particleVFX;
    [SerializeField] private float minCollisionForce = 1.0f; // 파티클이 시작되는 최소 충돌 세기
    [SerializeField] private float maxCollisionForce = 10.0f; // 최대 파티클 강도를 위한 충돌 세기
    [SerializeField] private float particleIntensityMultiplier = 1.0f; // 파티클 강도 배수
    
    [Header("파티클 색상 설정")]
    [SerializeField] private Color weakCollisionColor = Color.yellow; // 약한 충돌 색상
    [SerializeField] private Color strongCollisionColor = Color.red; // 강한 충돌 색상
        
    // Start is called before the first frame update
    void Start()
    {
        sfxSource = GetComponent<AudioSource>();
        
        // VFX 컴포넌트 찾기 또는 추가
        vfxComponent = GetComponent<VisualEffect>();
        if (vfxComponent == null)
        {
            vfxComponent = gameObject.AddComponent<VisualEffect>();
        }
        
        // VFX 에셋 설정
        if (particleVFX != null)
        {
            vfxComponent.visualEffectAsset = particleVFX;
        }
        else
        {
            // 기본 VFX 에셋 로드 시도
            VisualEffectAsset defaultVFX = Resources.Load<VisualEffectAsset>("Trail_Particle");
            
#if UNITY_EDITOR
            if (defaultVFX == null)
            {
                // Assets/Shaders/Shaders/ 경로에서 로드 시도 (에디터에서만)
                defaultVFX = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/Shaders/Shaders/Trail_Particle.vfx");
            }
#endif
            
            if (defaultVFX != null)
            {
                vfxComponent.visualEffectAsset = defaultVFX;
                particleVFX = defaultVFX;
                Debug.Log("기본 Trail_Particle VFX를 로드했습니다.");
            }
            else
            {
                Debug.LogWarning("Trail_Particle.vfx 파일을 찾을 수 없습니다. Inspector에서 수동으로 할당해주세요.");
            }
        }
        
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager를 찾을 수 없습니다!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 사운드 재생
        if (sfxSource != null)
        {
            sfxSource.Play();
        }
        
        // 공과의 충돌인지 확인
        if (collision.gameObject.CompareTag("Game_Ball"))
        {
            // 점수 추가
            if (scoreManager != null)
            {
                scoreManager.AddScore();
            }
            
            // 충돌 세기 계산
            float collisionForce = collision.relativeVelocity.magnitude;
            
            // 파티클 효과 재생
            PlayCollisionParticles(collision, collisionForce);
            
            Debug.Log($"공과 충돌! 충돌 세기: {collisionForce:F2}");
        }
    }
    
    /// <summary>
    /// 충돌 시 파티클 효과를 재생합니다.
    /// </summary>
    /// <param name="collision">충돌 정보</param>
    /// <param name="collisionForce">충돌 세기</param>
    private void PlayCollisionParticles(Collision collision, float collisionForce)
    {
        if (vfxComponent == null || particleVFX == null)
        {
            return;
        }
        
        // 충돌 세기가 최소값보다 작으면 파티클을 재생하지 않음
        if (collisionForce < minCollisionForce)
        {
            return;
        }
        
        // 충돌 세기를 0~1 범위로 정규화
        float normalizedForce = Mathf.Clamp01((collisionForce - minCollisionForce) / (maxCollisionForce - minCollisionForce));
        
        // 충돌 지점 계산
        Vector3 collisionPoint = collision.contacts[0].point;
        Vector3 collisionNormal = collision.contacts[0].normal;
        
        // VFX 위치를 충돌 지점으로 설정
        vfxComponent.transform.position = collisionPoint;
        
        // 파티클 강도 설정 (VFX Graph의 파라미터에 따라 조정 필요)
        float particleIntensity = normalizedForce * particleIntensityMultiplier;
        
        // 충돌 세기에 따른 색상 계산
        Color particleColor = Color.Lerp(weakCollisionColor, strongCollisionColor, normalizedForce);
        
        // VFX Graph 파라미터 설정
        try
        {
            // Trail_Particle.vfx의 파라미터에 맞춰 설정
            if (vfxComponent.HasVector4("New Color"))
            {
                vfxComponent.SetVector4("New Color", new Vector4(particleColor.r, particleColor.g, particleColor.b, particleColor.a));
            }
            
            if (vfxComponent.HasVector4("ParticleColor"))
            {
                vfxComponent.SetVector4("ParticleColor", new Vector4(particleColor.r, particleColor.g, particleColor.b, particleColor.a));
            }
            
            // 파티클 수량 조절 (파라미터가 있다면)
            if (vfxComponent.HasFloat("Rate"))
            {
                vfxComponent.SetFloat("Rate", 100 + (particleIntensity * 200)); // 100~300 범위
            }
            
            // 파티클 속도 조절 (파라미터가 있다면)
            if (vfxComponent.HasFloat("Velocity"))
            {
                vfxComponent.SetFloat("Velocity", 1.0f + particleIntensity * 2.0f); // 1~3 범위
            }
            
            // 파티클 크기 조절 (파라미터가 있다면)
            if (vfxComponent.HasFloat("Size"))
            {
                vfxComponent.SetFloat("Size", 0.5f + particleIntensity * 1.0f); // 0.5~1.5 범위
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VFX 파라미터 설정 중 오류: {e.Message}");
        }
        
        // 파티클 재생
        vfxComponent.Play();
        
        Debug.Log($"파티클 재생 - 세기: {normalizedForce:F2}, 색상: {particleColor}, 위치: {collisionPoint}");
    }
    
    /// <summary>
    /// Inspector에서 VFX 에셋이 변경될 때 호출됩니다.
    /// </summary>
    private void OnValidate()
    {
        if (vfxComponent != null && particleVFX != null)
        {
            vfxComponent.visualEffectAsset = particleVFX;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
