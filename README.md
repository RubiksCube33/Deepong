# Deepong - VR 멀티플레이어 탁구 게임

![Unity](https://img.shields.io/badge/Unity-2022.3.x-blue)
![XR](https://img.shields.io/badge/XR-Meta%20Quest%20Series-green)
![Photon](https://img.shields.io/badge/Photon-PUN2-orange)
![Platform](https://img.shields.io/badge/Platform-Android%20VR-red)

![ChatGPT Image 2025년 6월 18일 오후 11_52_37](https://github.com/user-attachments/assets/246bcbd5-c6f9-456d-a812-d857e5482456)

Deepong은 Unity와 Meta XR SDK를 기반으로 개발된 실시간 멀티플레이어 VR 탁구 게임입니다. Photon PUN2 네트워킹을 통해 최대 2명의 플레이어가 VR 환경에서 실시간으로 탁구를 즐길 수 있습니다.

## 팀 소개

| 팀원 | 역할 | 주요 담당 업무 |
|------|------|----------------|
| **권혜능** | **팀 리더 (PM)** | • 팀 일정 관리 및 기획<br>• 기기 관리<br>• 물리 기반 인터렉션 구현 |
| **권익주** | **에셋 담당** | • SFX, VFX 담당<br>• 프리팹 구성<br>• 게임 에셋 관리 |
| **서주은** | **UI 담당** | • 각 씬간의 이동 시스템<br>• 메인, 스코어, 팀 매칭 등의 UI<br>• VR UI/UX 디자인 |
| **안대성** | **네트워크 담당** | • 포톤 멀티플레이어 구현<br>• 공, 캐릭터, 패들, 점수 동기화<br>• 실시간 네트워크 코드 최적화 |

## 주요 특징

### **몰입형 VR 경험**
- **Meta Quest 시리즈 완벽 지원** (Quest, Quest 2, Quest Pro, Quest 3, Quest 3S)
- **자연스러운 VR 상호작용** - 실제 탁구와 같은 패들 조작
- **정확한 물리 시뮬레이션** - 현실적인 공의 움직임과 충돌
- **햅틱 피드백** - 공과 패들 충돌 시 진동 피드백

### **실시간 멀티플레이어**
- **Photon PUN2 기반** - 안정적인 클라우드 네트워킹
- **최대 2명 동시 플레이** - 1:1 탁구 매치
- **실시간 동기화** - 플레이어 움직임, 공 물리, 점수 동기화
- **지연 보정 시스템** - 네트워크 지연을 보정하는 예측 알고리즘

### **다양한 게임플레이**
- **3가지 패들 타입** - 탁구 라켓, 사이버 검, 복싱 글러브
- **실시간 패들 변경** - A 버튼으로 게임 중 패들 교체
- **점수 시스템** - 실시간 점수 추적 및 승부 판정
- **사운드 시스템** - 패들별 차별화된 타격음과 배경음악

### **직관적 UI/UX**
- **VR 최적화 UI** - 3D 공간에서 자연스러운 메뉴 조작
- **로비 시스템** - 방 생성, 참가, 랜덤 매칭 지원
- **실시간 상태 표시** - 네트워크 상태, 플레이어 정보 표시

## 기술 스택

### **게임 엔진 & 플랫폼**
- **Unity 2022.3.x** (LTS)
- **Universal Render Pipeline (URP) 14.0.12**
- **Android API Level 32+** (Meta Quest 지원)

### **VR & XR 기술**
- **Meta XR SDK 74.0.2** - Meta Quest 플랫폼 지원
- **Unity XR Interaction Toolkit 3.0.3** - VR 상호작용 시스템
- **XR Management 4.4.0** - 크로스 플랫폼 XR 관리
- **OpenXR 1.14.1** - 표준 XR API 지원

### **네트워킹**
- **Photon PUN2** - 실시간 멀티플레이어 네트워킹
- **Photon Realtime** - 로비 및 방 관리
- **Custom RPC System** - 게임 이벤트 동기화

### **물리 & 렌더링**
- **Unity Physics** - 3D 물리 시뮬레이션
- **Custom Ball Physics** - 정밀한 공 물리 동기화
- **Haptic Feedback System** - VR 컨트롤러 진동

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Network/           # 네트워킹 시스템
│   │   ├── NetworkManager.cs          # 네트워크 매니저 (싱글톤)
│   │   ├── Launcher.cs                # 게임 씬 네트워크 상태 관리
│   │   ├── LobbyManager.cs            # 로비 입장/퇴장 관리
│   │   ├── MatchMakingManager.cs      # 방 생성/참가/매칭
│   │   ├── RoomManager.cs             # 방 내 플레이어 관리
│   │   ├── NetworkPlayerSpawner.cs    # 플레이어 스폰 시스템
│   │   └── PlayerPrefabSetup.cs       # 플레이어 프리팹 설정
│   ├── Player/            # 플레이어 관련
│   │   ├── PlayerNetworkSync.cs       # 플레이어 위치/회전 동기화
│   │   ├── PlayerAnimationSync.cs     # 애니메이션 동기화
│   │   ├── PlayerSetup.cs             # VR 플레이어 초기 설정
│   │   └── VRMovementController.cs    # VR 이동 제어
│   ├── VR/                # VR 전용 시스템
│   │   ├── VRControllerNetworkSync.cs # VR 컨트롤러 동기화
│   │   ├── VRHumanoidController.cs    # VR 휴머노이드 제어
│   │   └── XRPlayerArmatureSetup.cs   # XR 플레이어 아바타 설정
│   ├── Gameplay/          # 게임플레이 로직
│   │   ├── BallSync.cs                # 공 물리 동기화 (핵심)
│   │   ├── GameManager.cs             # 게임 상태 관리
│   │   └── PongRacketHaptics.cs       # 패들 햅틱 피드백
│   ├── Court/             # 탁구장 관련
│   │   ├── BallController.cs          # 공 제어 로직
│   │   ├── CourtManager.cs            # 탁구장 관리
│   │   └── PaddleChangeController.cs  # 패들 변경 시스템
│   ├── UI/                # 사용자 인터페이스
│   │   ├── ChoosingRoomUI.cs          # 방 선택 UI
│   │   ├── MakingRoomUI.cs            # 방 생성 UI
│   │   └── ScoreManager.cs            # 점수 관리 UI
│   └── Managers/          # 매니저 클래스들
│       └── SoundManager.cs            # 사운드 시스템
├── Scenes/                # 게임 씬들
│   ├── MainMenuScene.unity            # 메인 메뉴
│   ├── BasicScene.unity               # 기본 게임 씬
│   └── CourtSceneAds.unity           # 탁구장 씬
├── Prefabs/               # 프리팹들
│   ├── Player_Origin.prefab           # VR 플레이어 프리팹
│   ├── GameBall.prefab               # 게임 공 프리팹
│   └── SoundManager.prefab           # 사운드 매니저
├── Models/                # 3D 모델들
├── Materials/             # 머티리얼들
├── Sounds/                # 오디오 파일들
└── Photon/                # Photon 네트워킹 에셋
```

## 설치 및 설정

### **시스템 요구사항**
- **Unity 2022.3.x LTS** 이상
- **Meta Quest Development** 환경
- **Android Build Support** 모듈
- **Git** (버전 관리)

### **1. 프로젝트 클론**
```bash
git clone <repository-url>
cd Deepong
```

### **2. Unity에서 프로젝트 열기**
1. Unity Hub에서 "Open" 클릭
2. 클론한 Deepong 폴더 선택
3. Unity 에디터에서 프로젝트 로드 대기

### **3. 패키지 설정 확인**
프로젝트가 열리면 다음 패키지들이 자동으로 설치됩니다:
- Meta XR SDK All (74.0.2)
- XR Interaction Toolkit (3.0.3)
- Universal Render Pipeline (14.0.12)
- Photon PUN2 (포함됨)

### **4. Meta Quest 개발 환경 설정**
```bash
# Android SDK 경로 설정 (Unity Preferences)
# Meta Quest Developer Mode 활성화
# USB 디버깅 허용
```

### **5. Photon 설정**
1. [Photon Dashboard](https://dashboard.photonengine.com)에서 계정 생성
2. PUN2 앱 생성 및 App ID 복사
3. Unity에서 `Window > Photon Unity Networking > PUN Wizard`
4. App ID 입력 및 설정 완료

## 게임플레이 가이드

### **기본 조작법**
![image](https://github.com/user-attachments/assets/56386f02-7ac8-48b9-9ebd-5d316b02cd11)

- **이동**: 왼쪽 컨트롤러 스틱을 움직여 이동
- **시점 이동** 오른쪽 컨트롤러 스틱을 움직여 이동
- **패들 조작**: VR 컨트롤러로 직관적인 패들 스윙
- **패들 변경**: A 버튼 (Primary Button) 눌러서 패들 타입 변경
- **메뉴 조작**: VR 컨트롤러로 3D UI 요소 선택

### **게임 진행**
![image](https://github.com/user-attachments/assets/3601dcc7-1073-4950-a3fb-e3ab7b256b42)

1. **메인 메뉴**에서 멀티플레이어 선택
2. **방 생성** 또는 **기존 방 참가**
3. **상대방 입장 대기** (최대 2명)
4. **게임 자동 시작** - 탁구장으로 이동
5. **실시간 탁구 게임** 진행
6. **점수 달성** 시 승부 결정

### **패들 타입별 특징**
- **탁구 라켓**: 기본 패들, 균형잡힌 성능
- **사이버 검**: 특수 효과음과 시각 효과, 긴 사거리
- **복싱 글러브**: 양손 사용, 강력한 햅틱 피드백

## 네트워크 아키텍처

### **핵심 동기화 시스템**

#### **1. BallSync.cs - 공 물리 동기화**
```csharp
// 네트워크 지연 보정
Vector3 targetPos = networkPos + (networkVel * syncDelay * delayCompensation);

// 거리 기반 보간
float interpSpeed = Mathf.Lerp(minSyncSpeed, maxSyncSpeed, distance / distanceMultiplier);
```

#### **2. PlayerNetworkSync.cs - 플레이어 동기화**
- VR 헤드셋 위치/회전 동기화
- 양손 컨트롤러 위치/회전 동기화
- 부드러운 보간을 통한 자연스러운 움직임

#### **3. VRControllerNetworkSync.cs - 컨트롤러 동기화**
- 버튼 상태 실시간 전송
- 햅틱 피드백 네트워크 공유
- 패들 변경 상태 동기화

### **매칭 시스템 플로우**
![KakaoTalk_20250618_213046635_05](https://github.com/user-attachments/assets/ea2afc6d-ca3a-435f-9a56-039ce64bbfd5)

## 주요 기술적 특징

### **1. 정밀한 물리 동기화**
- **마스터 클라이언트** 기반 공 소유권 관리
- **지연 보정 알고리즘** - 네트워크 지연 예측 및 보정
- **순간이동 방지** - 임계값 기반 텔레포트 처리

### **2. 최적화된 VR 성능**
- **프레임률 최적화** - 90FPS 유지를 위한 렌더링 최적화
- **배터리 효율성** - 불필요한 연산 최소화
- **메모리 관리** - 동적 로딩 및 언로딩

### **3. 확장 가능한 아키텍처**
- **모듈화된 구조** - 각 시스템의 독립성 보장
- **이벤트 기반 통신** - 느슨한 결합을 통한 유지보수성
- **플러그인 지원** - 새로운 패들 타입 쉽게 추가 가능

## 빌드 및 배포

### **Meta Quest 빌드**
```bash
# Unity Build Settings
1. Platform: Android 선택
2. XR Settings 확인
3. Player Settings 구성
4. Build 실행
```

### **개발자 모드 설치**
```bash
adb install Deepong.apk
```
