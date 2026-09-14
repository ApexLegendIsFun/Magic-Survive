# 실제 Windows 플레이 검증 — 2026-09-13

현재 판정: Windows 빌드 성공, 실행 화면 확인, 타이틀 진입 흐름 실패. 완주 미검증. 작업 전체 완료 아님.

## 소스와 환경

- HEAD: `70e9d0ebaf5ad5e3be9dfff630212776130fc2a1` + 기존 미커밋 작업.
- Unity `6000.3.17f1`, Windows Intel 64-bit, Development Build 해제.
- 빌드 씬 순서: `TitleScene`, `SampleScene`.
- 로컬 증거: `Logs/FullRun-20260913/HEAD.txt`, `status-before.txt`, `working-before.patch`, `source-manifest.csv`.
- Git 제외 구매 에셋을 사용하는 로컬 빌드다. HEAD만으로 동일 빌드가 재현되지는 않는다.

## 확인한 실패

1. 실제 Unity Game View에 BGM/SFX 설정창이 초기 표시된다. 설정창이 켜져 있고 메인 화면보다 위에 배치되어 있다.
2. 첫 Windows 빌드 실패: `SPUM_SpriteEditManager.cs:6`, CS0234 (`UnityEditor.U2D.Sprites`). 실행 파일 생성 전 실패했다. 원본 로그: `Logs/FullRun-20260913/build-baseline-failed.log`.
3. 기존 Title smoke는 raycast 전체에서 시작 버튼을 찾고 `onClick.Invoke()`로 진입해 가림 결함을 놓친다. 이 PASS는 실제 사용자 클릭 성공의 증거가 아니다.

## 수정

- 로컬 SPUM `Sprite_Editor(Beta)/Script/SPUM_SpriteEditManager.cs`: UnityEditor using 구역을 `#if UNITY_EDITOR`로 감쌌다. 원래 클래스와 동작은 유지한다. 구매 원본은 Git 제외이므로 다른 PC에도 동일 조건부 컴파일 수정이 필요하다.
- `TitlePlayModeSmokeEditor`: 활성/상호작용 가능 상태와 첫 raycast의 실제 클릭 수신 대상을 검사한다. 이 검사는 보조 검사이며 실제 빌드 조작을 대체하지 않는다.

## 실제 실행 결과

- 재빌드 성공: 2026-09-13 23:25:51–23:26:39 KST, 48초. `Logs/FullRun-20260913/build-succeeded.log`.
- 실행 파일: `Builds/FullRun-20260913/Baseline/Magic-Survive.exe`. 같은 폴더의 Data/DLL/MonoBleedingEdge도 함께 필요하다.
- 1280×720 창 모드로 실제 실행했다. 초기 화면은 BGM/SFX와 Game Description이고 시작 버튼이 보이지 않는다. 설정 UI가 창 밖으로 잘린다. `Logs/FullRun-20260913/title-1280.png`, `Player-baseline.log`.
- 창 최대화 버튼 클릭으로 크기가 바뀌지 않았다. Alt+F4로 닫았다. 게임 내부 종료 버튼 성공으로 계산하지 않는다.
- 1920×1080으로 재실행한 프로세스와 초기 로그는 확인했다. 창 활성화와 캡처는 `foreground window did not report a process id` 오류로 복구 재시도에도 실패했다. 1080p 화면/입력은 미검증이며 실행 창은 남겨두었다.
- 이동·전투·레벨업·보스·승리·사망·시간 초과·재시작·타이틀 복귀·게임 내부 종료는 이번 실행에서 모두 미검증이다. 정상 속도 완주 없음.
- 수정한 smoke 도구는 에디터 컴파일을 거쳤지만 아직 실행하지 않았다. 과거 smoke PASS를 이번 결과에 포함하지 않는다.
- 빌드 파일 SHA256 목록은 `Logs/FullRun-20260913/build-manifest.csv`에 기록했다.

## 승범 전달 요청 — 아직 미발송

담당 task와 Slack 이름 검색에서 연락 대상을 식별하지 못했다. 사용자에게 연락 경로를 요청했다.

기존 TitleScene의 타이틀 제목, 게임 시작, 설정, 종료를 보이게 하고 Option_Panel은 초기 숨김으로 설정한다. 기존 설정 UI의 열기/닫기와 게임 종료를 연결한다. 기존 Game_Start 및 TitleSceneController의 SampleScene 연결은 유지한다. 새 UI로 대체하지 않는다. 실제 마우스로 시작/설정 열기/닫기/종료 확인 후 변경 파일 또는 SHA를 전달한다.

## 남은 합격 검사

- 실행 파일에서 타이틀 → 시작 원소 → 이동/자동 공격 → 경험치/레벨업 → 보스 처치 → 승리 결과.
- 별도 판에서 사망과 시간 초과 패배.
- 결과에서 재시작과 타이틀 복귀, 타이틀에서 프로그램 종료.
- 정상 속도 한 판 완주. 이벤트 호출, 무적, 시간 단축으로 대체하지 않는다.
- 최종 수정 빌드로 재검증하고 화면/Player.log/빌드 로그를 남긴다.
