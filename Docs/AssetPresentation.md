# 기존 기능 에셋 적용

적용 도구: Unity 메뉴 `Tools > Magic Survive > Apply Existing Feature Assets`.
구매 패키지를 먼저 로컬에 임포트한다. 도구는 필요한 파일이 없으면 누락 경로를 출력하고 적용을 중단한다.
전체 경로와 GUID는 `AssetPresentationDependencies.json`에 기록했다.

## 구성

- 플레이어: SPUM RetroHeroes 마법사. 이동·체력·충돌은 기존 컴포넌트 유지.
- Basic / Fast / Tank: 일반 좀비 / 파란 오크 / 엘리트 좀비 외형.
- 현재 보스: Demon01. 기존 BossSpawner를 SampleScene의 RunDirector·EnemyManager·Enemy_Boss 데이터에 연결.
- 화염 / 번개 / 냉기 / 대지 / 암흑: Fire_05 / Electric_07 / IceWater_01 / Skill_18 / Skill_19. 대지는 갈색, 암흑은 보라색.
- 적·투사체: 원본 상속 변형 프리팹. 기존 EnemyManager·ProjectileLauncher의 풀을 사용.
- AssetVisualLifecycle: 시각 자식만 방향 전환, 이동 애니메이션, 재활성화 시 애니메이션·스케일·VFX 초기화. 루트 이동·충돌 API 변경 없음.
- UI: 기존 배치에 Casual Fantasy GUI, NanumGothic. 선택 프레임과 원소 VFX 아이콘 적용.
- 데미지: 기존 DamageUi·UiObjectPool 재사용. DNPPixel SDF, 화면 좌표 연결. 과도한 팝업 생성을 줄이기 위해 프레임당 12개 표시.
- 소리: 기존 SoundManager의 클릭·레벨업 슬롯 사용. 씬 재시작 시 단일 인스턴스 유지, 획득 버튼의 같은 프레임 클릭음 중복 방지.

## 패키지와 보존

필수 로컬 패키지: SPUM(Undead, MS_Orc, RetroHeroes), FantasyMonsters, 2D_PFX, PixelAttackFx, CasualFantasyGUIPack, Casual Game UI Sound, DamageNumbersPro의 DNPPixel 폰트. TMP·UGUI는 프로젝트 패키지 사용.
구매 원본은 `Assets/Untracked Asset/`, 생성한 픽셀 폰트는 `Assets/04.ThirdParty/GeneratedPresentation/`에 있으며 둘 다 Git 제외 경로다. 다른 PC에서는 패키지 임포트 후 적용 도구를 실행해 로컬 폰트를 생성한다.
새 프리팹은 `Assets/02.Prefabs/Presentation/`. 원본 프리팹과 Combat/UI C#은 이번 에셋 적용에서 수정하지 않았다. TitleScene은 이미지·폰트·효과음 참조만 변경했다. 기존 기획서·목업·폰트 작업은 보존했다.
새 전투 효과·엘리트 기능·보스 패턴과 DamageNumbersPro 시스템 교체는 포함하지 않는다.

## 검증

Unity 6000.3.17f1에서 5원소 시작, 실제 UI 선택, 투사체·적 풀 재사용, 레벨업, 사운드 단일 인스턴스, 보스 등장·처치·승리 검사 통과.
새 프리팹의 Missing Script와 픽셀 폰트 재질·아틀라스 검사 통과.
화면 캡처: `Logs/AssetPresentation/`. 자동 검사는 음원의 실제 청취 품질까지 판정하지 않는다.

## 성능

동일 PC, RTX 3080 Ti, D3D12, 1920×1080 카메라 RenderTexture, Editor 배치 Play Mode. 일반 적 100마리, 자동 공격, 5초 준비 후 20초 측정. VSync·프레임 제한 해제.
적 이동 속도를 0.05로 고정하고 충분한 체력을 부여해 개체 수를 유지했다.
이 수치는 화면 표시와 UI 합성 전체를 포함하는 배포 빌드 FPS가 아닌 Editor 오프스크린 비교다.

| 상태 | 평균 프레임 | P95 | 평균 FPS |
|---|---:|---:|---:|
| 적용 전 | 0.719 ms | 0.948 ms | 1391.47 |
| 적용 후 | 9.774 ms | 15.875 ms | 102.31 |

100개 SPUM 애니메이션 외형 적용으로 평균 프레임 비용이 9.055 ms 증가했다. 원본 로그: `Logs/AssetBenchmark-before.txt`, `Logs/AssetBenchmark-after.txt`.

최종 회귀 검사: `AssetPresentationSmoke.log`의 전투·성장·게임오버·재시작 PASS, `AssetPresentationFinal.log`의 타이틀 진입·원소 최대 레벨·전체 성장 규칙·Missing Script PASS. 적용 프리팹·데이터·양쪽 씬의 GUID를 Assets 및 패키지 메타와 대조한 결과 누락 0개.
