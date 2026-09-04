

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

