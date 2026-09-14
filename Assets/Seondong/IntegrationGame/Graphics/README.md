# 마법 성장 외형과 돌바닥

기존 보유 에셋만 재조합했다. 마법사·적 프리팹과 전투 코드는 그대로 사용한다.

## 연결

| 원소 | 원본 시각 에셋 | 프로필 |
|---|---|---|
| 화염 | 2D_PFX / Fire_05 | Profiles/Fire.asset |
| 번개 | 2D_PFX / Electric_07 | Profiles/Lightning.asset |
| 냉기 | 2D_PFX / IceWater_01 | Profiles/Frost.asset |
| 대지 | PixelAttackFx / Skill_18 | Profiles/Earth.asset |
| 암흑 | PixelAttackFx / Skill_19 | Profiles/Dark.asset |

`Visuals/<원소>_Tier1..4.prefab` 총 20개. 단계는 레벨 1~2 / 3~4 / 5~7 / 8이다.
기본 형상 → 꼬리·잔광 → 양옆 보조 형상·중심 빛 → 회전 위성·후광 순서다.
작은 보조 형상은 원본 프레임을 쓰며 중심만 애니메이션한다. 냉기에는 기존 Light 스프라이트 잔광을 더했다.
연쇄·폭발·장판 같은 추가 피해 기능은 없다.

전용 `Magic` 데이터는 원본 ProjectileMagicDefinition을 복사하고 투사체 참조만 바꾼다.
전용 `Projectiles`는 기존 투사체를 복사해 기존 시각 자식만 교체한다. 피해·이동·충돌 크기를 변경하지 않는다.
씬 PlayerSkillSystem의 targetedMagicDefinitions에 이 데이터 다섯 개를 연결했다.

IntegrationMagicVisual은 투사체 활성화 시 해당 원소의 현재 레벨을 한 번 읽는다.
비행 도중 강화되어도 이미 발사된 외형은 유지한다. 풀 반환 시 자식을 비활성화하고 다음 발사에서 다시 선택한다.
각 단계 인스턴스는 투사체별로 필요할 때 한 번 생성한다. AssetVisualLifecycle의 애니메이터 재바인드·잔광 초기화를 재사용한다.
MagicVisualOrbit은 재사용 시 회전을 초기화하며 게임 시간으로만 움직인다.

## 배경

기존 AllIn1SpriteShader 데모의 RockTexture를 전용 StoneGround 재질로 사용한다.
WorldGround 셰이더가 월드 XY / 4로 텍스처를 반복한다. 원본 텍스처 임포트 설정은 수정하지 않는다.
IntegrationGround는 카메라 화면보다 8유닛 넓은 사각형을 유지한다. 카메라와 사각형이 이동해도 무늬 좌표는 월드에 고정된다.
어두운 저대비 바닥이며 장애물·충돌체는 없다.

## 재생성·검사

- 적용: Unity 메뉴 `Tools > Seondong Integration > 5. Apply Magic Growth And Ground`.
- 플레이/빌드: 상위 README의 전용 메뉴 사용.
- 자산·시각·풀 초기화·일시정지·합성 성능 검사: Unity 배치 실행에서 `-executeMethod Seondong.IntegrationGame.IntegrationGraphicsCheck.Run`. 검사가 끝나면 해당 배치 에디터가 종료된다.
- 적용 후 위 검사까지: `-executeMethod Seondong.IntegrationGame.IntegrationGraphicsCheck.InstallAndPreview`.
- 실제 성장: `Editor/Run-BuildCheck.ps1 -Scenario growth -Element Fire` (Lightning/Frost/Earth/Dark도 가능).
- 승리·사망·시간 초과: `-Scenario victory`, `death`, `timeout`.

성장 검사는 실제 경험치와 가상 키보드·마우스 입력을 사용한다. 선택한 원소를 우선 강화하고 8레벨 외형까지 본 뒤 적에게 접근해 자연 사망한다.
재시작 후 첫 투사체가 1레벨 외형인지 확인한다. 일반 게임 실행에서는 검사 입력이 꺼져 있다.
시각 전용 장면의 직접 프리팹 생성·일시정지 검사는 외형 진단이다. 실제 성장·게임 완주 증거와 구분한다.

로그와 화면은 프로젝트 루트 `Logs/GraphicsGrowth` 및 `Logs/SeondongIntegration/graphics-*`에 보관한다.
최종 결과는 상위 README와 `Logs/GraphicsGrowth/REPORT.md` 참조.
