// Unified level-up prototype: choose an element, then confirm one upgrade.
let chosenElement=null;
let upgrades=Array.from({length:5},()=>[0,0,0]);
const options=[
 [['집중 화염','화염탄 피해 +10%'],['넓은 점화','점화 폭발 반경 +10%'],['오래 타는 불','불장판 지속시간 +10%']],
 [['고압 전격','번개탄 피해 +10%'],['멀리 뻗는 전류','연쇄 탐색 거리 +10%'],['강한 방전','방전 피해 +10%']],
 [['날카로운 얼음','얼음창 피해 +10%'],['깊은 빙결','빙결 지속시간 +10%'],['퍼지는 서리','서리 장판 반경 +10%']],
 [['무거운 바위','바위창 피해 +10%'],['넓은 충격','충격파 반경 +10%'],['거센 밀침','밀치기 거리 +10%']],
 [['응축된 암흑','그림자 구체 피해 +10%'],['퍼지는 저주','표식 전염 탐색 거리 +10%'],['깊은 저주','3중첩 피해 증가 효과 +2%p']]
];
const positions=[[50,13],[86,39],[73,81],[27,81],[14,39]];
const baseOverlay=overlay;
overlay=function(){
 if(state.page!==3||paused)return baseOverlay();
 if(chosenElement!==null&&!eligible(chosenElement))chosenElement=null;
 const i=chosenElement;
 const any=elements.some((_,j)=>eligible(j));
 return `<div class="veil"><div class="unified-window"><div class="window-title"><small>LEVEL UP · CHOOSE YOUR PATH</small><h2>성장할 원소를 선택하세요</h2><span class="pending-count">남은 선택 ${state.queue}회</span></div><div class="unified-body"><div class="unified-left"><div class="step-label">01 · 원소 선택</div><div class="spell-tree unified-tree"><svg viewBox="0 0 100 100" preserveAspectRatio="none"><circle cx="50" cy="50" r="37"/><polygon points="50,13 86,39 73,81 27,81 14,39"/><path d="M50 13 L73 81 14 39 86 39 27 81 Z"/></svg><div class="tree-center">✧<span>원소를 선택하세요</span></div>${elements.map((e,j)=>`<button class="spell-node ${eligible(j)?'available':'locked'} ${i===j?'selected':''}" style="left:${positions[j][0]}%;top:${positions[j][1]}%;--element:${colors[j]}" data-act="affinity" data-val="${j}" ${eligible(j)?'':'disabled'}>${crest(j)}<strong>${e}</strong><span>${state.levels[j]===8?'MAX':state.levels[j]?'Lv.'+state.levels[j]:eligible(j)?'새 원소':'인접 원소 필요'}</span></button>`).join('')}</div><p class="selection-help">원소 선택은 무료입니다.<br>카드를 확정해야 성장합니다.</p></div><div class="unified-right"><div class="step-label">02 · 강화 카드 선택</div>${i===null?`<div class="choose-empty"><span>✦</span><h3>${any?'어떤 힘을 키울까요?':state.levels.some(Boolean)?'모든 원소 각성 완료':'시작 원소를 먼저 선택하세요'}</h3><p>${any?'왼쪽 원소를 누르면 해당 원소의 카드가 나옵니다.':state.levels.some(Boolean)?'최대 HP 10% 회복 · 목업에서는 안내만 표시':'시작 화면에서 무료 원소를 획득하세요.'}</p>${!any?gameButton('돌아가기','page',state.levels.some(Boolean)?1:0):''}</div>`:`<h3 class="affinity-title" style="color:${colors[i]}">${elements[i]} <span>Lv.${state.levels[i]} → Lv.${state.levels[i]+1}</span></h3><div class="fixed-reward"><b>레벨 상승 시 기본 획득</b><span>${effects[i][state.levels[i]]}</span></div><div class="affinity-cards">${(state.levels[i]?options[i]:[['원소 해금',effects[i][0]]]).map((o,k)=>`<button class="affinity-card" style="--element:${colors[i]}" data-act="affinity-card" data-val="${k}" ${state.queue?'':'disabled'}>${crest(i)}<h3>${o[0]}</h3><p>${o[1]}</p>${state.levels[i]?`<small>선택 ${upgrades[i][k]}회 · 추가 특화</small>`:'<small>첫 획득 · 특화 선택 없음</small>'}<span class="choose-label">${state.levels[i]?'이 강화로 성장':'원소 획득'}</span></button>`).join('')}</div><p class="future-help">${state.levels[i]?'아직 해금하지 않은 효과의 강화는 해금 후 적용됩니다.':'새 원소 획득에도 레벨업 선택 1회를 사용합니다.'}</p>`}</div></div></div></div>`;
};
const basePerform=perform;
perform=function(act,val){
 if(act==='affinity'){const i=Number(val);if(eligible(i))chosenElement=i;render();return;}
 if(act==='affinity-card'){
  const i=chosenElement,k=Number(val);
  if(i===null||!eligible(i)||state.queue<1||!Number.isInteger(k)||k<0||k>(state.levels[i]?2:0))return;
  const before=state.levels[i];if(before)upgrades[i][k]++;
  state.levels[i]++;state.queue--;chosenElement=null;
  state.note=`${elements[i]} Lv.${state.levels[i]} · ${before?options[i][k][0]:'원소 해금'} 적용 (목업).`;
  if(!state.queue)state.page=1;render();return;
 }
 if(act==='pick')return; // Retired mixed-element card action cannot bypass the tree.
 if(act==='start'||act==='reset'){chosenElement=null;upgrades=Array.from({length:5},()=>[0,0,0]);}
 if(act==='page'&&String(val)==='2')val='3';
 if(act==='level'){chosenElement=null;}
 basePerform(act,val);
};
const unifiedReset=reset;reset=function(){chosenElement=null;upgrades=Array.from({length:5},()=>[0,0,0]);unifiedReset();};
pages[3]='성장';
pageIssues[3]=[0,1,2,3,11];
const priorRender=render;render=function(){
 if(state.page===2){state.page=3;chosenElement=eligible(state.selected)?state.selected:null;}
 priorRender();
 $('nav').innerHTML=pages.map((p,i)=>i===2?'':action(p,'page',i,state.page===i?'active':'')).join('');
 $('screen').innerHTML=$('screen').innerHTML.replace('오각형 트리 <kbd>T</kbd>','성장 <kbd>T</kbd>');
 if(state.page===3){
  $('screen').innerHTML=$('screen').innerHTML.replace('LEVEL UP · CHOOSE YOUR PATH','ELEMENTAL GROWTH').replace('<h2>성장할 원소를 선택하세요</h2>',`<h2>${state.queue?'원소를 고르고 강화하세요':'원소 성장'}</h2><button class="close-growth" data-act="page" data-val="1">닫기</button>`);
  if(!state.queue)$('screen').innerHTML=$('screen').innerHTML.replace('남은 선택 0회','다음 레벨업에 강화 가능');
  $('notice').textContent=state.note||(state.queue?'카드 확정 시에만 성장 1회를 사용한다.':'열람 중 · 화면을 열어도 성장 횟수는 늘지 않는다.');
  $('scene-controls').innerHTML=action('검토용 레벨업 +1','level');
 }
};
issues[1]=['원소별 특화 카드','선택 원소의 고정 3종을 보여주는 시안이다. 추첨·희귀도는 미정.','반복 선택·누적 수치·카드 구성을 플레이 검증 후 확정한다.'];
issues[2]=['통합 성장 흐름','오각형에서 보유·인접 원소를 선택한 뒤 카드를 확정한다.','선택 변경은 무료, 카드 확정 때만 레벨과 대기 횟수를 갱신한다.'];
document.addEventListener('click',e=>{if(e.target.closest('[data-case]')){chosenElement=null;upgrades=Array.from({length:5},()=>[0,0,0]);render();}});
state.page=3;state.queue=1;chosenElement=0;render();
