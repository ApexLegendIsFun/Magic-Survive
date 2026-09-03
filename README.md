# Magic Survive

## 팀 작업 시작점

팀원은 작업 전에 아래 문서를 순서대로 읽는다.

1. [게임 기획 요약](Docs/GameDesignBrief.md) — 무엇을 만드는지, 이번 제출에서 무엇을 반드시 남기는지
2. [팀 일정과 작업 카드](Docs/TeamSchedule.md) — 오늘 누가 무엇을 끝내는지, 선행 조건과 컷 기준
3. [10분 플레이 상세 규칙](Docs/TenMinuteRunPlan.md) — 성장, 표식, 융합, 적, 보스, 공개 계약
4. [담당자별 최종 인수 조건](Docs/TeamRequests_10MinuteBuild.md) — 하루치 지시가 아닌 전체 완료 체크리스트
5. [개발 계획과 통합 이력](Docs/DevelopmentPlan.md) — 담당 경계, 30일 계획, 통합 SHA와 검증 기록

## 작업 시작 규칙

1. 자기 브랜치의 작업 중 변경을 먼저 커밋한다.
2. 최신 `origin/Seondong`을 자기 브랜치에 병합한다.
3. 위 문서를 읽고 아래 형식으로 수신 확인한다.
4. [팀 일정과 작업 카드](Docs/TeamSchedule.md)의 오늘 배정량만 시작한다.

```text
[수신 확인]
이름:
병합한 Seondong SHA:
오늘 작업 카드:
막힌 것:
예상 전달 시각:
```

담당자 브랜치에서 `SampleScene` 최종본을 만들지 않는다. 담당자는 자기 코드·프리팹·작업 씬을 커밋해 SHA를 전달하고, 선동이 기준 씬에 통합한다.

## 기준 충돌 시 우선순위

- 이번 제출에 **넣을 범위·우선순위·컷**: [게임 기획 요약](Docs/GameDesignBrief.md)과 [팀 일정](Docs/TeamSchedule.md)
- 넣기로 한 기능의 **정확한 규칙·수치·공개 계약**: 코드의 `MagicContentCatalog`, `DifficultyRules`, `RunTimelineRules`와 [10분 플레이 상세 규칙](Docs/TenMinuteRunPlan.md)
- 담당 경계와 장기 원안: [개발 계획](Docs/DevelopmentPlan.md)

상세 원안에 있어도 일정에서 P1·P2로 분류된 기능은 P0보다 먼저 만들지 않는다. 일정이 상세 규칙을 바꾸는 것은 아니며, 이번 제출에 포함할지 여부만 정한다.

작업자가 임의로 빈 규칙을 채우지 않는다. 기획 공백은 선동에게 질문하고 결정이 문서나 코드에 반영된 뒤 구현한다.
