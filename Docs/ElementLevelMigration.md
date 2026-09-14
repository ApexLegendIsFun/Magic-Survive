# 5원소 레벨 전환 — 2026-09-13

## 변경

- 28노드 카탈로그·노드 타입·미리보기·공용 강화 선택, 별도 범위/융합 마법 정의와 융합 반응 계약을 제거했다.
- `PlayerSkillTree`는 원소별 0~8레벨을 보관한다. 0은 미보유다. 기존 `MagicElement`와 남은 `MagicId`의 직렬화 숫자는 유지했다.
- `PlayerSkillSystem.GetSkillLevel(element)`, `TrySelectSkill(element)`, `CancelSelectedSkill()`, `ConfirmSelectedSkill()`을 사용한다. `SkillLevelChanged(element, level)`은 런타임 갱신 후 발행한다.
- `Choices`는 현재 제시된 원소 목록이다. 보유 원소의 다음 레벨과 이웃 미보유 원소를 후보로 최대 3개 무작위 추첨한다. 후보가 적으면 전부 표시한다. 선택/취소로 재추첨하지 않는다.
- 시작 원소는 무료 1레벨이다. 확정마다 정확히 1레벨 올린다. 전체 만렙 뒤에는 선택 창을 생략하고 기존 `PlayerProgression`의 최대 HP 10% 회복만 한 번 적용한다.
- `MagicContentCatalog.GetStats(element, level)`이 기본 피해·쿨다운·관통의 단일 원본이다. 프리팹 연결·투사체 이동 수치는 기존 `ProjectileMagicDefinition`을 사용한다.
- 승인된 승범 UI 세 파일의 의존성을 전환했다. 완성된 `PopupUi`가 있으면 기존 배치의 버튼을 사용하고, 없으면 `GrayboxGameFlowView`로 선택한다. 결과의 융합 목록도 제거했다.

## 이번 작업 밖

새 원소 고유 효과(3·5·7·8레벨, 번개 연쇄 등), 표식 payload 확장, 엘리트·보스 개편은 전투 담당 작업이다. 현재 표시하는 미리보기는 적용된 기본 수치만 안내한다. 기존 표식 저장·만료·풀 초기화 코드는 유지한다.

`FusionLine`·`FusionLineManager`는 승인 범위 밖의 승범 UI 표현 코드다. 원소 연결선 역할이 있어 이름만 보고 삭제하지 않았다. 성장 시스템의 융합 기능은 없다.

## 검증 방법

- `TenMinutePlanValidationEditor.Run`: 5개 시작 원소, 이웃 조건, 취소/중복 확정, 8레벨 제한, 초기 상태, 기본 수치, 표식 만료/초기화, 결과 스냅샷, 기존 난이도·시간 규칙, 기준 씬·프리팹 Missing Script 검사.
- `MvpPlayModeSmokeEditor.RunBatch`: 실제 시작/선택/확정 버튼, 자동 공격, EXP·회복·대기 선택, 사망·재시작, 5개 런타임과 전체 만렙 후 회복 검사.
- `TitlePlayModeSmokeEditor.RunBatch`: 타이틀 버튼에서 시작 원소 선택까지 전환 검사.

## 실행 결과

Unity 6000.3.17f1 배치 실행 기준:

- 컴파일 및 성장 규칙 검사 통과. 기준 씬·게임플레이 프리팹 Missing Script 0.
- 실제 UI 버튼·자동 공격·EXP·선택 확정·사망·재시작·전체 만렙 후 회복 검사 통과.
- 타이틀 검사 통과. 추가 승인 후 `SoundManager.ChangeBgm()`에 배열 범위·누락 오디오 검사만 추가했다. BGM이 없으면 재생을 건너뛴다. 타이틀 버튼에서 SampleScene 원소 선택까지 정상 전환을 확인했다.
- 로그: `Logs/ElementLevelValidation.log`, `Logs/ElementLevelPlayMode.log`, `Logs/ElementLevelTitle.log` (로컬, Git 제외).
