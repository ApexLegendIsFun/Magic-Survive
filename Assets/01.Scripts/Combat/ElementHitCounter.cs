using System;


// 원소별 적중 횟수. 번개 대지의 N번째 적중시 효과용
//
// GameEvents.Clear 호출부가 아직 0개라 static이면 재시작 후에도 카운트가 살아남음
public class ElementHitCounter
{
    private static readonly int ElementCount = Enum.GetValues(typeof(MagicElement)).Length;

    private readonly int[] hitCounts = new int[ElementCount];

    /// <summary>
    /// 적중 1회를 기록. 이번 적중이 발동 차례면 true를 돌려주고 카운트를 0으로 되돌림
    /// </summary>
    /// 
    public bool RegisterHit(MagicElement element)
    {

        int index = (int)element;


        if (index < 0 || index >= ElementCount)
        {
            return false;
        }

        hitCounts[index]++;

        if (hitCounts[index] < ElementReactionValues.HitsPerTrigger)
        {
            return false;
        }

        hitCounts[index] = 0;

        return true;
    }
}