# 2026-09-21 일일 통합 결과

## 판정

로컬 병합·전용 씬 배선 완료. 과제용 필수 실행 흐름은 통과했지만 **정상 통합본 확정과 push는 보류**했다.
현재 Mac에 구매 그래픽·사운드 원본이 없다. 스프라이트 없는 전투 화면을 정상으로 판정하지 않는다.
사용자 요청으로 장시간 성장·승리·시간 초과 검사를 중단하고 필수 검사만 남겼다.

## 실행 위치

- 프로젝트: `/Users/leeseondong/Magic-Survive`
- 전용 폴더: `Assets/Seondong/IntegrationGame`
- 시작: `Scenes/TitleScene.unity`
- 게임: `Scenes/SampleScene.unity`
- Unity 메뉴: `Tools > Seondong Integration > 2. Play From Title`
- 일반 Build Settings는 원본 씬을 유지한다. 전용 메뉴가 실행 동안만 전용 씬 목록을 사용한다.
- 설치된 Unity `6000.3.19f1`, Mac 빌드로 확인했다. 문서의 `6000.3.17f1`·Windows 검증으로 대체하지 않는다.

## 반영

- 시작 `Seondong@e9acf04`; `Yushin@6ba4c1e` → `Seungbum@5530b07` → 작업 중 도착한 `Yushin@c3f0c85` 병합. 팀 이력·저자 보존, 충돌 없음.
- 병합 커밋 `a72b357`, `8e0ae4e`, `3dc7042`; 통합 배선 커밋 `8a8772c`.
- 임시 돌진자·보스를 실제 `Enemy_Dasher`·`Enemy_Boss` 데이터로 교체. 실제 Summoner 유지. 이후 실행 메뉴가 다시 임시 보스로 덮어쓰던 조건 수정.
- 기존 팀 프리팹으로 번개·소환·돌진·보스 충격파·대지 장판 매니저 각각 한 개 연결. 전체 효과 프리팹에 있던 중복 소환 매니저와 테스트 트리거는 추가하지 않음.
- 팀 GameplayUI의 등장 알림 subtree 재사용, RunDirector와 소환술사 항목 연결. 선택·결과 Graybox 흐름 유지.
- 기존 presentation 사운드 인스턴스에 팀 슬롯 연결. 통합 코드가 배열을 두 칸으로 잘라 원소·소환 사운드를 지우던 문제 수정.
- 원본 그림·애니메이션·바닥 텍스처 누락을 잡도록 시각 검사 강화. Mac 빌드 메뉴와 실행/종료 증거를 남기는 Python 검사기 추가.
- 마지막 검사기 수정: 카드 수를 과거 최대 3개가 아닌 `PlayerSkillSystem.Choices`와 비교. 게임 성장 규칙은 변경하지 않았다.

## 필요한 검사 결과

| 검사 | 결과 | 근거 |
|---|---|---|
| 최신 팀 통합 소스 컴파일·Mac 빌드 | PASS | `Logs/DailyIntegration-20260921/final/build-current.log` |
| 사망·레벨업·재시작·타이틀·실제 종료 | PASS | `final/current-death-Fire/report.json`, `process.json` |
| 전용 씬 handoff 검사 | FAIL | `final/handoff-current.log`: `Summon sound slot missing.` |
| 시각 원본 검사 | FAIL | `final/graphics-validation.log`: `Missing source sprites: Fire_Tier1.` |
| 5원소 Lv8·엘리트·보스·승리·시간 초과 | 미검증 | 사용자 요청으로 장시간 검사 중단. 완료로 표시하지 않음. |

필수 실행 검사는 1280×720, 전체 프로세스 54.269초, 첫 전투 29.31초/18처치/레벨업 2회였다.
이동 → 실제 입력으로 선택·확정 → 자연 사망 → 재시작 초기화 → 두 번째 자연 사망 → 타이틀 → QUIT → exit code 0.
report·process 모두 PASS, 수집된 Error/Exception/Assert 0건이다. 가상 입력 검사이며 실제 물리 입력 검사는 아니다.
외형 단계 숫자 기록은 런타임 컴포넌트 상태일 뿐, 누락된 스프라이트의 시각 검증을 뜻하지 않는다.

빌드·실행 기준은 `8a8772c`. 빌드 당시 설정·파일 해시는 `final/source-manifest.json`, `final/build-manifest.json`에 보존했다.
이후 변경은 검사기의 카드 수 가정·실행 파일 해시 채취 시점과 문서다. 검사기 카드 수 변경 후 장시간 재실행은 하지 않았다.
최종 소스 컴파일 기록은 `final/compile-final.log`다.

병합 전 실패는 `before/Editor.log`, `before/death/report.json`에 보존했다. 인덱스 예외는 `UnityEditor.Search.SearchDatabase`에서 발생했고 ConfirmButton 대기도 한 차례 실패했다.
병합 직후 동일 검사는 사망·재시작·타이틀까지 진행했지만 에디터 검색 예외 때문에 FAIL이었다. Mac 플레이어 필수 검사는 이 예외 없이 통과했다.
처음 시작했던 victory/timeout 검사는 과거 최대 3카드 가정 때문에 약 20초에 조기 실패했다. 게임 승패 결함으로 판정하지 않았고 검사 조건만 고쳤다.
중단한 growth 5건과 이전 빌드의 중복 smoke는 각 `cancellation.json`에 사용자 요청에 따른 중단으로 표시했다.

## 남은 항목·보존

- 원본 `.unitypackage`를 **원래 GUID와 함께** 반입해야 한다. 현재 폴더·보조 worktree·확인한 Downloads/Documents에 패키지가 없다. `Assets/04.ThirdParty`는 공개 저장소에서 의도적으로 제외돼 있다.
- 패키지 스크립트 GUID를 제외해도 연결된 unresolved asset GUID 39개. 목록: `Logs/DailyIntegration-20260921/missing-asset-references.json`.
- 소환 사운드, 플레이어·마법 그래픽, 바닥 텍스처 및 팀 HUD의 `TMP_ExBold_BlackShadow` 원본 누락.
- 승범의 암흑 적 표시 완성 prefab, Frost Lv8 장판 표현 전달 필요. HUD 레벨 문자열도 U+FFFD로 깨져 표시되는 문제 확인.
- 담당자 구현을 대신 수정하지 않았다. 요청은 `Assets/Seondong/IntegrationGame/Requests.md`에 기록만 했으며 발송하지 않았다.
- 사용자 `ProjectVersion.txt`, `_Recovery` 원본 해시 보존. 보조 worktree·`main` 변경 없음.
- Unity가 자동 변경한 URP·PlayerSettings·UnityConnect 설정은 patch 보관 후 원상복원. 자동 생성 Resources는 로그 폴더로 보존.
- Unity YAML/meta의 자동 trailing whitespace는 별도 기록하고 불필요한 전체 재포맷을 하지 않았다.
- 원격 `Seondong`은 `e9acf04` 유지. 검증 미완료이므로 push하지 않았다.
