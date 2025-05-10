using UnityEngine;
using UnityEngine.UI;

public class NeonGlow : MonoBehaviour
{
    public Color32 glowColor = new Color32(191, 1, 7, 255); // 빨간색 네온
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float pulseSpeed = 2f;
    
    private Image image;
    private Material material;
    
    void Start()
    {
        image = GetComponent<Image>();
        material = new Material(image.material);
        image.material = material;
        
        // 기본 이미지 색상을 glowColor로 설정
        image.color = glowColor;
    }
    
    void Update()
    {
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, 
            Mathf.PingPong(Time.time * pulseSpeed, 1));
        
        // Color32를 Color로 변환하여 사용
        Color emissionColor = (Color)glowColor * intensity;
        material.SetColor("_EmissionColor", emissionColor);
    }
}