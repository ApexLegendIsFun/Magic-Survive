# 마법사 뱀서류 — 개발 계획

## 1. 게임 한 줄

**마법사가 몬스터 떼를 잡고, 마법 스킬트리를 성장시키는 뱀서류 게임.**

## 2. 핵심 재미

- 이동은 직접
- 공격은 자동
- 몬스터 대량 처치
- 경험치 획득
- 레벨업
- 마법 스킬트리 성장
- 서로 다른 원소 조합
- 새로운 융합 마법 해금
- 보스 처치

## 3. 마법

기본 학파 4개.

- 화염
- 냉기
- 번개
- 암흑

융합 예시.

- 화염 + 번개 = 플라즈마
- 냉기 + 번개 = 폭풍
- 화염 + 암흑 = 지옥불
- 냉기 + 암흑 = 죽음

## 4. 게임 규모

- 플레이어 1명
- 맵 1개
- 일반 적 4종
- 엘리트 2종
- 보스 1종
- 기본 마법 약 8개
- 융합 마법 3~4개
- 플레이 시간 10~15분

## 5. 사용 에셋

### 캐릭터

- 2D Pixel Unit Maker - SPUM
- 2D Retro Heroes - SPUM Premium Addon Pack
- 2D Monster ORC - SPUM Premium Addon Pack
- 2D Monster Undead - SPUM Premium Addon Pack
- Fantasy Monsters Animated [Bosses]

### 마법

- 2D Pixel FX:Element
- 2D Pixel Effect : Attack&Skill
- 2D Pixel FX:StateEffect(Particle)

### UI / 연출

- Damage Numbers Pro
- All In 1 Sprite Shader
- Casual Fantasy GUI

### 사운드

- Casual Games SFX Pack
- Casual Game UI Sound

## 6. 인원

### 이선동 — 팀장 / PD / 기획 / 성장 및 게임진행 프로그래머

- 전체 기획
- 게임 방향 결정
- 일정 관리
- 업무 분배
- 최종 의사결정
- 경험치
- 레벨업
- 스킬트리
- 마법 성장
- 원소 융합
- 적 스폰
- 난이도 증가
- 보스 등장 조건
- 승리 / 패배
- 전체 게임 플로우

담당 에셋:

- 2D Pixel FX:Element
- 2D Pixel Effect : Attack&Skill
- 2D Pixel FX:StateEffect(Particle)

추가 역할:

- 어떤 VFX를 어떤 마법에 사용할지 결정
- 콘텐츠 우선순위 결정
- 스코프 관리
- 최종 빌드 판단

### 맹유신 — 전투 프로그래머

- 플레이어 이동
- 적 AI
- 체력
- 데미지
- 투사체
- 자동공격
- 적 사망
- 충돌 처리
- 오브젝트 풀링
- SPUM 캐릭터 적용

담당 에셋:

- 2D Pixel Unit Maker - SPUM
- 2D Retro Heroes
- 2D Monster ORC
- 2D Monster Undead
- Fantasy Monsters Animated [Bosses]

### 한승범 — UI / 연출 / 통합 프로그래머

- HUD
- HP / EXP UI
- 타이머
- 스킬트리 UI
- 레벨업 UI
- 게임오버 UI
- 결과 화면
- Damage Number
- VFX 연결
- SFX 연결
- 셰이더
- 화면 연출
- 최적화
- 빌드 관리
- 에셋 통합

담당 에셋:

- Damage Numbers Pro
- All In 1 Sprite Shader
- Casual Fantasy GUI
- Casual Games SFX Pack
- Casual Game UI Sound

추가 역할:

- 이선동이 선택한 마법 VFX 실제 게임 연결
- 프리팹 / 머티리얼 / UI 최종 통합

### 유태환 — QA

- 매일 최신 빌드 플레이
- 기능 테스트
- 버그 등록
- 버그 재현
- 회귀 테스트
- 밸런스 테스트
- 성능 테스트
- 대량 몬스터 테스트
- 최종 빌드 검수

QA는 에셋 내부 수정 안 함. 게임에서 실제로 정상 작동하는지만 확인.

## 7. 개발 구조

### 맹유신 — 전투 기반

```text
Player
Enemy
Weapon
Projectile
Damage
Pooling
```

### 이선동 — 게임 규칙

```text
EXP
Level
Skill Tree
Magic
Fusion
Spawn
Difficulty
Boss
Game Flow
```

### 한승범 — 표현 및 통합

```text
UI
VFX
SFX
Shader
Damage Number
Optimization
Build
```

### 유태환 — 검증

```text
Bug
Balance
Performance
Regression
Final Test
```

## 8. 30일 개발

### 1주차 — 게임 돌아가게 만들기

맹유신:

- 이동
- 적
- 공격
- 데미지
- 적 사망

이선동:

- 경험치
- 레벨업
- 기본 게임 진행
- 기본 스킬 구조

한승범:

- 기본 HUD
- HP
- EXP
- 타이머
- 에셋 프로젝트 통합

유태환:

- 매일 빌드 테스트 시작

**목표: 재미 없어도 한 판 플레이 가능.**

### 2주차 — 핵심 콘텐츠

맹유신:

- 적 종류 추가
- 무기 처리
- 전투 안정화

이선동:

- 화염
- 냉기
- 번개
- 암흑
- 스킬트리
- 웨이브
- 난이도

한승범:

- 스킬트리 UI
- 레벨업 UI
- VFX 기본 연결
- Damage Number

유태환:

- 기능 테스트
- 전투 / 성장 버그 집중 체크

**목표: 기본 게임 완성.**

### 3주차 — 차별점 + 완성품화

맹유신:

- 엘리트
- 보스 전투
- 풀링 / 성능 개선

이선동:

- 융합 마법
- 보스 조건
- 게임 밸런스
- 콘텐츠 조정

한승범:

- 실제 VFX 적용
- UI 최종 적용
- SFX
- 셰이더
- 화면 연출

유태환:

- 밸런스
- 프레임
- 장시간 플레이
- 보스 테스트

**목표: 보여줄 수 있는 게임.**

### 4주차 — 기능 추가 금지

전원:

- 신규 시스템 추가 금지

맹유신:

- 전투 버그 수정
- 성능 수정

이선동:

- 밸런스
- 스킬 수치
- 난이도
- 최종 기획 판단

한승범:

- UI 버그
- VFX
- SFX
- 최적화
- 최종 빌드

유태환:

- 회귀 테스트
- 엣지케이스
- 최종 빌드 검수

**목표: 안 터지는 게임.**

제외 범위:

- 맵 여러 개
- 캐릭터 여러 명
- 스토리 대량 추가
- 마법 수십 개
- 복잡한 장비 시스템
- 상점 / 메타 성장
- 마지막 주 신규 기능

## 9. 팀 운영 원칙

- 각 시스템 Owner 고정
- 외부 에셋 원본 직접 수정 금지
- 기능 추가보다 완성 우선
- 이선동이 최종 스코프 결정
- 충돌 생기면 담당자 기준으로 결정
- 마지막 7일은 무조건 안정화

## 최종 목표

**30일 안에 10~15분짜리 마법사 뱀서류 한 판을 처음부터 끝까지 완성한다.**

**차별점은 마법 스킬트리 + 원소 융합.**

**이선동은 팀장 / PD / 기획 / 성장 및 게임진행 프로그래밍을 겸하며 전체 프로젝트의 최종 방향과 스코프를 결정한다.**

## 작업 결과

### 2026-09-01 — 최소 통합 Play Mode 빌드

- 기준 브랜치: `Seondong`
- 승범 UI 통합 원본: `origin/Seungbum` `af2bafc`
- 승범 병합 커밋: `4b11f9081c587c3648f7d7887af902685bb2af7c`
- 유신 전투 통합 원본: `origin/Yushin` `802a667`
- 유신 병합 커밋: `8d1b70074054980a0be280c95e17d10bfb7c4033`
- 병합 기준 HEAD: `8d1b70074054980a0be280c95e17d10bfb7c4033`
- 최소 통합 런타임 커밋: `485c167efdbc4adc3310a29b4395845bc7101fff`
- Unity 검증 도구 커밋: `1c72df77029d52a38195b19025239930ccda50fe`
- 구현 변경 상태: 커밋 완료
- 승범 HUD Basic 원본: `origin/Seungbum` `e8b8cf1`
- 승범 HUD Basic 병합 커밋: `8e7888fb0c89077a4b4b6b6f476e616d91631e42`
- HUD Basic 통합 상태: 새 Image 기반 HP / EXP 계약에 맞춰 병합 완료
  - `GameplayHudBinder`를 `UpdateHp` / `UpdateLevelUp` API에 연결
  - 생성된 `GameplayUI`의 HP / EXP Image를 `Filled / Horizontal / Left`로 보정
  - 승범 원본 `UI_SampleScene`의 스타일과 레이아웃은 유지
  - 원본 씬의 HP / EXP Image 기능 설정도 `Filled`, 초기값 `1 / 0`으로 정리
- 성장 / 스킬 / 게임 흐름: 구현 완료
  - 적 사망 보상 직접 EXP 반영
  - `현재 레벨 × 5` 요구 EXP와 초과 EXP 보존
  - 다중 레벨업 대기열과 순차 선택
  - 위력 `+2`, 쿨다운 `×0.9`, 관통 `+1`
  - `Playing → LevelUp → Playing`, 사망 시 `GameOver`, 재시작
- 통합 씬 / UI: 구현 완료
  - 기준 씬 `Assets/00.Scenes/SampleScene.unity`
  - `GameplayUI` 프리팹과 `GameSystems` 루트 배치
  - Build Settings는 기준 씬 1개만 포함
  - HP / EXP / 레벨 / 킬 수 HUD 연결
  - 별도 회색 레벨업 / 게임오버 Canvas 연결
- Unity `6000.3.19f1` 검증 결과:
  - 컴파일 오류 0
  - 기준 씬 필수 참조 누락 0, Missing Script 0
  - Play Mode smoke PASS
  - 이동, 카메라, 스폰, 자동공격, 투사체 풀링, 적 사망 정상
  - 적 사망 이벤트 횟수와 EXP 보상 합계가 HUD / 진행도에 각각 1회 반영됨
  - HP 바, 다중 레벨업, 일시정지, 세 강화, 사망 우선순위, 재시작 정상
  - HUD Basic 적용 후 정적 검증과 Play Mode smoke PASS
  - 120초 combat soak PASS: 47킬, 활성 적 33, 풀 생성 투사체 1
  - 게임 런타임 Error / Exception 0
- 수정한 통합 결함:
  - 적이 플레이어와 완전히 겹치면 방향 벡터가 0이 되어 자동공격이 멈추던 문제 수정
  - 게임오버 뒤 같은 프레임 레벨업 요청이 다시 대기열에 쌓이던 문제 수정
- 제한사항:
  - 정확한 Unity `6000.3.17f1`은 미설치이며 디스크 여유 13GB라 안전하게 설치하지 못함
  - 프로젝트 버전 파일은 `6000.3.17f1`을 유지함
  - Unity `6000.3.19f1` batch Play Mode 진입 시 `UnityEditor.Search` 내부 인덱싱 예외가 발생하지만 게임 런타임 예외는 아님
  - EXP 오브, 15분 클리어, 보스, 타이틀 흐름, 완성 UI, 실행 파일 제외
