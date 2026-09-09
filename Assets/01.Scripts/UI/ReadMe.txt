

Damage_Number를 호출하려면 

Enemy가 받은 데미지,(Player가 가한 데미지) 위치정보(Transform.position)으로 호출하시면 됩니다.*테스트 할때는 Enemy의 TakeDamage에서 amount와 transform.position을 활용했습니다. * 

(UiManager의 EnemyDamageTextUi() 함수)

현재 캔버스 구상도

Canvas_HUD_Static(가장 변동이 적은 )
└── 아이템/스킬아이콘(상시 확인가능한)


Canvas_HUD_Dynamic (가장 변동이 많은)
└── EXP Bar /Lv Text
├── 타이머
└── 킬수   


Canvas_World_Space (Damage Number&Sound(피격음,공격사운드 등) => 오브젝트 풀링 사용예정)
└── Damage Number (뱀서류 특성상 Damage 텍스트가 있을 경우 매우 많으므로 따로 관리)
    Hp Bar (플레이어, 몬스터) 플레이어의 경우, 고정 캔버스 Hpbar 또는 WorldSpace hp bar 둘중 하나 (미정)
    


Canvas_Popup    => 레벨업 시 스킬 고르는 Ui 창, 게임오버 => 씬 전환 전 Ui(없을수도 있음) 
├── LevelUp
├── GameOver
└── Option

VFX, SFX

VFX를 담당할경우 풀링할 예정이며, 

SFX는 풀링 X

마찬가지로 VfxManager를 만들어 중계기처럼 쓸 예정


현재 레벨업Ui는 만들어었으며 아직은 레벨업과 연동하지 않았습니다.
레벨업Ui 또한 PopupUi의 스크립트를 활용하였으며, PopUpUi 클래스는 첫 실행시 원소선택 Ui로써 기능을 하며, 첫 원소선택 이후부터는 레벨업 Ui로 변합니다.
또한, 레벨업 Ui에서 원소를 획득하지 않고 취소하면 하단 HUD의 스킬포인트가 증가하게 만들어 해당 Hud를 클릭하면 레벨업 Ui가 다시 뜨는 방향으로 진행중입니다.

