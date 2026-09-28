# 최종 재빌드 — 2026-09-25

현재 판정: **사용자가 지정한 최소 검사 범위 통과. v6 제출본 생성 완료.** 필수 참조·짧은 실제 플레이·정상 종료 및 압축 해제본 재실행을 확인했다. 이전 ZIP 대신 `Builds/FinalSubmission-Windows-20260925-v6.zip`을 사용한다.

## 기준

사용자 확정 기준은 작업자가 구현한 **기능 전체의 통합**이다. 시간·음원·스프라이트 등 리소스 조정은 허용한다. 작업 씬의 모든 임시 객체와 설정을 동일하게 복사하는 요구가 아니다.

GitHub 원격 최신 커밋을 다시 조회했다. Seondong `5ab47f5`, Yushin `a946cb0`, Seungbum `cd9d060`으로 이전 감사 이후 변경이 없다. 모두 현재 HEAD에 포함돼 있다. 원격 원본 코드·씬 비교는 `GitHubSceneAudit_20260925.md`, 최신 실제 연결 검사는 `Logs/FinalSubmission/Rebuild-20260925/functional-bindings.csv`에 기록한다.

## 기능 대조

| 담당자 | 구현 내용 | 원본 위치 | 최종 연결 위치 | 확인 방법 |
|---|---|---|---|---|
| 유신 | 5원소 Lv1~8, 표식·반응·전염·장판·낙뢰·낙석·처형, 특화 15종 | Combat, Progression/Skills, Yushin 작업 씬 | 같은 전투 코드 + 통합 마법 데이터/성장 외형 | 원격 코드 비교, 5원소 정의와 실제 발사 경로·이벤트 연결 검사, 새 실행 검사 |
| 유신 | 돌진자·소환술사·보스·2페이즈·면역 | Enemy_Dasher / Enemy_Summoner / Enemy_Boss 데이터와 프리팹 | IntegrationEliteSpawner, BossSpawner | 실제 데이터 경로, 행동 컴포넌트, 소환 데이터 검사; 보스 소환·충격파 컴포넌트는 2페이즈에서 활성화되는 원본 동작 유지 |
| 승범 | 타이틀·설정·원소 선택·특화·해금·MAX·결과·HUD | TitleScene, GameplayUI, 승범 작업 씬 | 빌드 첫 씬, FinalTeamUI | 컨트롤러/버튼/HUD 참조 검사, 새 실행 파일 UI 입력 검사 |
| 승범 | 원소 반응·연쇄·장판·돌진·소환·보스 연출·사운드 | Effect_All_Manager 및 UI/Effect&Sound | FinalTeamEffects, 실제 적 6종 | 이벤트별 필수 프리팹, 상태 오버레이, 5원소 표식, BGM/SFX 슬롯 검사 |
| 선동 | 성장 큐·런 진행·3/6/8/10분 흐름·배경·마법 외형 20종 | Progression, 전용 통합 게임 | 최종 게임 씬 | 데이터·시간선·성장 API와 실행 검사 |
| 태환 작업 씬 | 플레이어·입력·카메라·풀·성장·UI 구성 | SampleScene_Taehwan | 동일 공통 코드·데이터 | 작업 씬 스크립트 집합 대조. 해당 씬은 생성 이후 별도 기능 수정 이력이 없음 |

`TestEnemySpawner`와 DebugTrigger는 테스트 도구라 제외한다. 실제 생성·공격·연출은 기존 게임 이벤트에서 실행된다. Graybox UI는 전달된 최신 팀원 UI를 사용한다. 빈 BossHpbar·DamageText·SoundObjectPool 및 구형 LightningChainEffectPlayer는 각각 실제 BossHudUi·DamageUi/UiObjectPool·SoundManager·ChainReactionEffectPlayer로 대응된다.

일반 적 네 번째 슬롯 Ranged는 원본 작업 씬에서도 비어 있고 별도 Enemy_Ranged 데이터가 전달되지 않았다. 구현된 일반 적 Basic/Fast/Tank를 사용하며 원거리 공격 코드는 소환술사·보스에 연결된다. 이 예약 슬롯을 새로운 기능으로 만들지 않는다.

## 이번 변경

- 설치된 Unity의 일치하는 PDB로 종료 충돌 `UnityPlayer.dll + 0xb1b0a1`을 조회하면 `RuntimeStatic<PlatformAccessibilityManager,0>::StaticDestroy + 0x51`이다. 덤프는 null 포인터의 `+0xe0` 읽기 실패다. ComputeBuffer 경고를 충돌 원인으로 취급하지 않는다. DX11, Run In Background, 미사용 모듈 제거/stripping 진단은 해결하지 못해 모두 원복했다. 각 실패 기록은 보존했다.
- `WindowsAccessibilityExitWorkaround`는 Unity 6000.3.17f1 Windows 실행에서 자체 창의 WM_GETOBJECT 요청만 거절한다. 게임에 스크린 리더용 AccessibilityHierarchy가 구현돼 있지 않으며 게임 입력·전투·렌더링·Application.Quit는 변경하지 않는다. 종료 이벤트에서 창 콜백을 해제한다. 다른 엔진 버전과 Editor에는 적용하지 않는다. 패키지·원격 작업자의 게임 코드를 삭제하거나 프로세스를 강제 종료하는 방식은 사용하지 않는다.
- 통합 씬의 타이머·레벨·처치 글자만 밝게 조정한다.
- PlayerSkillSystem이 실행 시 자동 탐색하던 GameFlowController·LevelUpController를 씬에서 명시적으로 연결한다. 성장/전투 구현은 그대로 사용한다.
- 빌드 전 실제 UI·전투·효과·사운드 필수 참조 검사 추가. 보스의 의도적인 2페이즈 비활성 상태, 미구현 예약 슬롯은 구분한다.
- 자동 캡처의 엘리트·보스 화면은 등장 패널의 0.35초 진입 애니메이션 후 0.5초 시점에 찍는다. 화면 밖에서 들어오는 순간을 글자 잘림으로 잘못 판정하던 문제를 줄인다.

## 검증 기록

새 기록: `Logs/FinalSubmission/Rebuild-20260925`. 실제 빌드의 자동 입력 기록은 `Logs/SeondongIntegration/rebuild-*`로 따로 보존한다. 과거 실행 통과 기록으로 이번 빌드 합격을 대신하지 않는다. Unity 가상 입력과 사람이 직접 플레이한 결과를 구분한다.

DX11 첫 hidden 진단 실행은 화면이 검게 저장돼 시각 합격 증거에서 제외했다. 이어진 visible 진단은 캐릭터·배경·HUD를 캡처하고 직접 검토했으며 종료 코드 0이었다. 진단 실행의 ComputeBuffer 정리 경고는 남아 있다. 경고와 종료 충돌은 별도로 기록한다.

사용자의 최신 지시에 따라 장시간·반복 검사는 중단했다. v2 victory/timeout 실행은 `interrupted.json`에 중단을 기록했으며 통과에 포함하지 않는다. 이번 성장 API 검사는 124개 assertion을 통과했다. 별도의 Editor 전투 fixture는 실제 피해를 관측하지 못해 FAIL이며 기록을 보존했다. 필수 연결 검사 PASS와 실제 전투 전체 검증은 구분한다. 이후 검사는 종료 결함 확인과 최종 패키지의 짧은 실행만 수행한다.

최종 v6: Unity 6000.3.17f1 Windows x64 일반 빌드 성공. `rebuild-v6-quit`(720p)은 종료 코드 0. `rebuild-v6-smoke`(1080p)는 45.49초 동안 실제 UI 입력으로 타이틀·설정·화염 선택·이동·자동 공격·실제 처치 13회·레벨업 1회·사망·재시작 초기화·타이틀 복귀·종료를 확인했고 프로세스 종료 코드 0이다. 게임 예외/오류와 자동 입력 검사의 실패는 0개다. 캐릭터와 HUD는 새 실행 파일의 `03-combat.png`를 열어 시각적으로도 확인했다. ComputeBuffer 정리 **경고** 1종은 남아 있으며 native crash와 구분한다.

검사 입력은 Unity Input System 가상 장치다. 이번 v6에서 사람이 전체 플레이하거나 3/6/8/10분·모든 원소 전투·적 100마리 성능을 새로 검증한 것으로 표시하지 않는다. 사용자가 축소한 최소 검사 범위에 따른 판정이다. 원본 씬 스크린샷은 `Logs/SceneComparison-20260925`, 최신 기능 연결은 `functional-bindings.csv` 24행 PASS다.

## 제출물

- ZIP: `Builds/FinalSubmission-Windows-20260925-v6.zip` (59,507,408 bytes).
- SHA256: `0ac1f1a509206d26e4e85ec20ac1d581c2a5edc2a405f37a1a86339a594da847`.
- ZIP을 별도 `FinalSubmission-Windows-20260925-v6-Extracted` 폴더에 해제해 모든 파일의 SHA256 일치를 확인했다. 그 실행 파일을 720p에서 42.17초 실행해 게임 진입·실제 전투·성장·사망·재시작·타이틀·정상 종료(0)를 다시 확인했다. 기록: `Logs/SeondongIntegration/rebuild-v6-extracted`.
- 실행 파일, Data, MonoBleedingEdge, DLL, D3D12 런타임, 조작 안내, BuildInfo, 파일별 SHA256 포함. DoNotShip 디버그 폴더 제외.
- 소스 HEAD `228e6ddff6fa1fbc92320e044ad64d4e63ea7f5c`와 미커밋 변경 포함. 원본 에셋은 로컬 `Assets/Untracked Asset` 의존이며 필요한 런타임 콘텐츠는 빌드에 포함했다. 소스 상태·diff·신규 코드 사본과 최종 결과는 `Logs/FinalSubmission/Rebuild-20260925`에 보존했다.
