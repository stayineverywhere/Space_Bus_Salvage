# Space Bus Salvage - CODE Documentation

본 문서는 프로젝트의 코드 구조와 주요 시스템 설계를 설명합니다.

---

## 프로젝트 구조

```text
Assets/
├── Scripts/
├── Prefabs/
├── Scenes/
├── Settings/
├── InputSystem_Actions/
├── LB3D/
└── Realistic Hail Set/
```

---

## 시스템 아키텍처

```text
GameBootstrapper
        ↓
GameLoopManager
        ↓
 ┌───────────────┬──────────────┬─────────────┐
 ↓               ↓              ↓
Contract     Planet         Curse
Manager      Manager        Manager
```

---

## 시스템 매니저

### GameBootstrapper.cs

게임 실행 시 필수 매니저를 생성하고 초기화합니다.

주요 역할:

* Persistent Manager 생성
* 씬 독립 초기화
* 런타임 의존성 연결

---

### GameLoopManager.cs

게임의 전체 흐름을 관리합니다.

관리 상태:

* Garage Hub
* Planet Exploration
* Warp Escape
* Loot Settlement
* Upgrade Phase

---

### ContractManager.cs

계약 진행을 담당합니다.

관리 항목:

* 목표 크레딧
* 계약 기간
* 계약 성공 여부
* 패널티 계산

---

### PlanetManager.cs

행성 정보를 관리합니다.

관리 항목:

* 난이도
* 환경 프리셋
* 기후 설정
* 몬스터 구성

---

### CurseManager.cs

저주 시스템을 관리합니다.

관리 항목:

* 글로벌 저주 수치
* 난이도 배율
* 몬스터 스폰 보정
* 아노말리 발생

---

## 플레이어 시스템

### PlayerMovement.cs

담당 기능:

* 이동
* 점프
* 스태미나 관리
* 산소 관리
* 상호작용

---

### PlayerInputController.cs

담당 기능:

* Input Action 연결
* 입력 이벤트 처리

---

### Flashlight.cs

담당 기능:

* 손전등 On / Off
* 배터리 및 전력 소비

---

### AIDroneCompanion.cs

담당 기능:

* 플레이어 추적
* 위험 요소 탐지
* 탐사 보조

---

## 버스 시스템

### BusController.cs

담당 기능:

* 차량 이동
* 조향
* 가속 및 감속

---

### BusDoor.cs

담당 기능:

* 문 개폐 애니메이션
* 상태 관리

---

### BusDoorInteractable.cs

담당 기능:

* 내부 ↔ 외부 전환
* 플레이어 위치 이동

---

### BusPowerSystem.cs

담당 기능:

* 전력 소비 계산
* 저주 단계 연동

---

### DiegeticDashboard.cs

담당 기능:

* 대시보드 정보 출력
* 전력량 표시
* 저주 수치 표시

---

## 몬스터 시스템

### MonsterAI.cs

모든 몬스터의 기본 클래스입니다.

공통 기능:

* NavMesh 이동
* 타겟 탐색
* 상태 전환

---

### WatcherAI.cs

특징:

* 플레이어 시야 기반 행동
* 접근 패턴 변화

---

### LurkerAI.cs

특징:

* 소리 감지
* 플레이어 추적

---

### CleanerAI.cs

특징:

* 전리품 탐색
* 훔친 후 도주

---

## 전리품 시스템

### LootItem.cs

기본 전리품 클래스입니다.

보유 데이터:

* 가치(Value)
* 저주 수치(Curse Value)

---

### CursedDoll.cs

특수 효과:

* 저주 증가

---

### BrokenClock.cs

특수 효과:

* 난이도 증가

---

### Survivor.cs

담당 기능:

* 구조 이벤트
* 추가 보상 지급

---

## UI 및 오디오 시스템

### GameUIManager.cs

담당 기능:

* UI 생성
* 패널 관리
* 알림 출력

---

### HUDManager.cs

담당 기능:

* 체력 표시
* 산소 표시
* 스태미나 표시

---

### AmbientAudioManager.cs

담당 기능:

* 환경음 재생
* 경고음 출력
* 저주 단계별 효과음 적용

---

## 설계 원칙

본 프로젝트는 다음 원칙을 기반으로 설계되었습니다.

* 시스템 간 결합도 최소화
* 씬 독립 초기화 지원
* 확장 가능한 매니저 구조
* 데이터 중심 설계 지향
* 유지보수성을 고려한 역할 분리
