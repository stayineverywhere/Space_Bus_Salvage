# Space Bus Salvage

[![Unity Version](https://img.shields.io/badge/Unity-6000.4.10f1-black?logo=unity)](https://unity.com/)
![URP](https://img.shields.io/badge/Render%20Pipeline-URP-orange)
![Input System](https://img.shields.io/badge/Input%20System-New-red)

> 위험한 외계 행성을 탐사하고 저주받은 전리품을 인양하여 살아 돌아오는 1인칭 공포 생존 인양 게임

---

## 게임 소개

**Space Bus Salvage**는 우주 버스를 거점으로 삼아 미지의 외계 행성을 탐사하고, 귀중한 전리품을 수거하여 생존하는 **1인칭 공포 생존 인양 게임**입니다.

플레이어는 기업과 계약을 맺고 다양한 행성에 착륙합니다. 행성 곳곳에 흩어진 전리품을 회수해 버스로 운반하고, 기지로 귀환하여 판매해야 합니다.

하지만 값비싼 전리품일수록 강력한 저주를 품고 있으며, 저주는 몬스터를 더욱 공격적으로 만들고 행성 전체를 위험하게 변화시킵니다.

---

## 핵심 게임 루프

```text
차고 (Garage Hub)
    ↓
계약 수주 및 준비
    ↓
행성 선택 및 출발
    ↓
탐사 및 전리품 수집
    ↓
저주 수치 증가
    ↓
버스 워프를 통한 탈출
    ↓
전리품 판매 및 업그레이드
    ↓
다음 계약 진행
```

---

## 주요 특징

### 계약 기반 진행 시스템

* 목표 크레딧과 기간이 정해진 계약 수주
* 전리품 판매를 통한 크레딧 획득
* 계약 실패 시 패널티 발생

### 외계 행성 탐사

* 다양한 환경의 행성 탐사
* 눈보라, 안개 등 기후 요소 존재
* 폐허 내부에서 전리품 수색

### 저주 시스템

* 전리품마다 고유 저주 수치 보유
* 저주 누적 시 난이도 상승
* 몬스터 출현 빈도 증가
* 아노말리 현상 발생

### 우주 버스 시스템

* 직접 운전 가능한 우주 버스
* 버스 내부와 외부 이동
* 전력 관리
* 워프를 통한 귀환

### 몬스터 AI

| 몬스터     | 특징               |
| ------- | ---------------- |
| Watcher | 플레이어 시야를 활용하는 위협 |
| Lurker  | 소리를 추적하는 위협      |
| Cleaner | 전리품을 훔쳐 도주하는 위협  |

---

## 조작법

| 키             | 동작        |
| ------------- | --------- |
| W / A / S / D | 이동        |
| 마우스 이동        | 시점 회전     |
| Left Shift    | 달리기       |
| Space         | 점프        |
| E             | 상호작용      |
| F             | 손전등       |
| Q             | 버스 게이트 이동 |
| T             | 운전 시작     |
| W / S         | 가속 / 감속   |
| A / D         | 조향        |
| Tab           | 운전 종료     |
| R             | 기지 워프     |

---

## 사용 기술

### Engine

* Unity 6000.4.10f1

### Rendering

* Universal Render Pipeline (URP)

### Input

* Unity New Input System

### Navigation

* AI Navigation (NavMesh)

### UI

* TextMesh Pro

### Language

* C#

---

## 실행 방법

### 요구 사항

* Unity 6000.4.10f1
* Universal Render Pipeline
* Input System
* AI Navigation
* TextMesh Pro

### 프로젝트 실행

```bash
git clone https://github.com/your-username/SpaceBusSalvage.git
```

Unity Hub로 프로젝트를 연 후,

```text
Assets/Scenes/GarageHub.unity
```

씬을 실행하면 게임을 플레이할 수 있습니다.

---

## 문서

* [CODE.md](CODE.md) : 프로젝트 코드 구조 및 시스템 설계 문서
