# VR 컨트롤러 네트워크 동기화 시스템

## 개요

VR 컨트롤러의 움직임과 버튼 상태를 네트워크로 실시간 동기화하는 시스템입니다. Photon PUN2를 사용하여 멀티플레이어 VR 탁구 게임에서 양손 컨트롤러의 위치, 회전, 버튼 입력, 햅틱 피드백을 동기화합니다.

## 주요 기능

### 🎮 컨트롤러 동기화
- 양손 컨트롤러의 실시간 위치/회전 동기화
- 부드러운 보간을 통한 자연스러운 움직임
- 순간이동 방지 시스템

### 🔘 버튼 상태 동기화
- 트리거, 그립, A/B 버튼 상태 실시간 전송
- 비트 패킹으로 네트워크 대역폭 최적화
- RPC를 통한 버튼 이벤트 전송

### 📳 햅틱 피드백 동기화
- 햅틱 피드백 강도와 지속시간 네트워크 전송
- 원격 플레이어용 시각적 피드백 효과
- 실시간 햅틱 이벤트 공유

### 🎯 패들 상태 동기화
- 패들 변경 상태 자동 동기화
- PaddleChangeController와 연동

## 파일 구조

```
Assets/Scripts/
├── VR/
│   ├── VRControllerNetworkSync.cs     # 메인 동기화 스크립트
│   └── VRControllerNetworkSetup.md    # 이 문서
├── Examples/
│   └── VRControllerSyncExample.cs     # 사용 예제
└── Player/
    └── PlayerNetworkSync.cs           # 기존 플레이어 동기화 (통합됨)
```

## 설정 방법

### 1. Player_Origin 프리팹 설정

```csharp
// Player_Origin 프리팹에 다음 컴포넌트들이 필요합니다:
- PhotonView
- VRHumanoidController
- VRControllerNetworkSync (새로 추가)
- PaddleChangeController (선택적)
```

### 2. PhotonView 설정

PhotonView의 Observed Components에 `VRControllerNetworkSync`를 추가:

1. Player_Origin 프리팹 선택
2. PhotonView 컴포넌트의 "Observed Components" 섹션
3. "+ Add Component" 클릭
4. `VRControllerNetworkSync` 선택
5. Send Rate: 20 Hz (권장)

### 3. 컴포넌트 자동 참조

`VRControllerNetworkSync`는 자동으로 다음 컴포넌트들을 참조합니다:

```csharp
// 자동으로 참조되는 컴포넌트들
- VRHumanoidController      // 컨트롤러 Transform 참조
- PaddleChangeController    // 패들 상태 동기화
- XRBaseController         // XR 입력 시스템
```

## 코드 사용법

### 기본 사용

```csharp
// VRControllerNetworkSync 컴포넌트 가져오기
VRControllerNetworkSync controllerSync = GetComponent<VRControllerNetworkSync>();

// 원격 플레이어의 컨트롤러 위치 가져오기
Vector3 leftHandPos = controllerSync.GetRemoteControllerPosition(true);   // 왼손
Vector3 rightHandPos = controllerSync.GetRemoteControllerPosition(false); // 오른손

// 원격 플레이어의 버튼 상태 확인
bool rightTrigger = controllerSync.GetRemoteButtonState("RightTrigger");
bool leftGrip = controllerSync.GetRemoteButtonState("LeftGrip");
```

### 햅틱 피드백 전송

```csharp
// 로컬 플레이어가 햅틱 피드백을 다른 플레이어에게 전송
controllerSync.SendHapticFeedback("RightController", 0.7f, 0.2f);
controllerSync.SendHapticFeedback("LeftController", 0.5f, 0.15f);
```

### 커스텀 이벤트 처리

```csharp
public class MyVRController : MonoBehaviourPunCallbacks
{
    private VRControllerNetworkSync controllerSync;
    
    void Start()
    {
        controllerSync = GetComponent<VRControllerNetworkSync>();
    }
    
    void Update()
    {
        // 원격 플레이어 데이터 처리
        if (!photonView.IsMine)
        {
            ProcessRemoteControllerData();
        }
    }
    
    void ProcessRemoteControllerData()
    {
        // 트리거가 눌렸을 때 특별한 효과 실행
        if (controllerSync.GetRemoteButtonState("RightTrigger"))
        {
            // 효과 실행 (예: 파티클, 사운드 등)
            PlayTriggerEffect();
        }
    }
}
```

## 설정 파라미터

### 동기화 설정

```csharp
[Header("동기화 설정")]
public float positionLerpRate = 15f;        // 위치 보간 속도
public float rotationLerpRate = 15f;        // 회전 보간 속도
public float teleportThreshold = 2f;        // 순간이동 방지 임계값
public bool syncButtonStates = true;        // 버튼 상태 동기화 여부
public bool syncHapticFeedback = true;      // 햅틱 피드백 동기화 여부
```

### 최적화 설정

```csharp
[Header("최적화 설정")]
public float sendRate = 20f;                // 초당 전송 횟수
public float positionThreshold = 0.01f;     // 위치 변화 감지 임계값
public float rotationThreshold = 1f;        // 회전 변화 감지 임계값 (도)
```

## 네트워크 최적화

### 데이터 압축

- **버튼 상태**: 8개 버튼을 1바이트로 압축 (비트 패킹)
- **위치/회전**: Unity의 기본 Vector3/Quaternion 압축 사용
- **변화 감지**: 임계값 이하의 변화는 전송하지 않음

### 대역폭 사용량

```
기본 설정 (20Hz):
- 위치 데이터: 24 bytes × 2 (양손) = 48 bytes
- 회전 데이터: 16 bytes × 2 (양손) = 32 bytes  
- 버튼 상태: 1 byte
- 패들 인덱스: 4 bytes
- 총합: 85 bytes × 20Hz = 1.7 KB/s per player
```

## 디버깅

### 로그 확인

```csharp
// 콘솔에서 다음 로그들을 확인할 수 있습니다:
[VRControllerNetworkSync] 입력 액션 설정 완료
[VRControllerNetworkSync] 로컬 플레이어 초기화 완료
[VRControllerNetworkSync] Player1의 컨트롤러 시각화 오브젝트 생성 완료
[VRControllerNetworkSync] 원격 플레이어 버튼 이벤트: RightTrigger = True
```

### 시각적 디버깅

원격 플레이어의 컨트롤러는 자동으로 시각화됩니다:
- **왼손**: 빨간색 캡슐
- **오른손**: 파란색 캡슐
- **버튼 이벤트**: 컨트롤러가 흰색으로 깜빡임

### 예제 스크립트 사용

`VRControllerSyncExample.cs`를 Player_Origin에 추가하면:
- GUI로 실시간 버튼 상태 확인
- 키보드로 햅틱 피드백 테스트 (Space, Left Shift)
- Context Menu에서 전체 동기화 테스트

## 문제 해결

### 컨트롤러가 동기화되지 않는 경우

1. PhotonView에 VRControllerNetworkSync가 추가되었는지 확인
2. VRHumanoidController가 올바르게 설정되었는지 확인
3. XR Origin 구조가 올바른지 확인

### 성능 문제

1. Send Rate를 낮춰보세요 (20Hz → 15Hz)
2. Position/Rotation Threshold를 높여보세요
3. 불필요한 버튼 동기화를 비활성화하세요

### 네트워크 지연

1. Photon 서버 지역을 확인하세요
2. Lerp Rate를 조정하세요 (높일수록 빠른 반응)
3. Teleport Threshold를 조정하세요

## 확장 가능성

### 커스텀 버튼 추가

```csharp
// VRControllerNetworkSync.cs의 SendControllerData 메서드에 추가
if (myCustomButton) buttonStates |= 256; // 새로운 비트 추가
```

### 추가 센서 데이터

```csharp
// 예: 컨트롤러 가속도 동기화
stream.SendNext(leftControllerAcceleration);
stream.SendNext(rightControllerAcceleration);
```

### AI 플레이어 지원

```csharp
// AI 플레이어의 가상 컨트롤러 데이터 생성
if (isAIPlayer)
{
    GenerateAIControllerData();
}
```

## 라이센스

이 코드는 프로젝트의 라이센스 정책을 따릅니다. 