using System;



public class ElementMarkState
{

    private static readonly int ElementCount = Enum.GetValues(typeof(MagicElement)).Length;

    private readonly int[] stacks = new int[ElementCount];
    private readonly float[] remainingDurations = new float[ElementCount];

    public event Action<ElementMarkChange> Changed;

    public ElementMarkSnapshot Get(MagicElement element)
    {
        int index = (int)element;

        if (index < 0 || index >= ElementCount)
        {
            return new ElementMarkSnapshot(element, 0, 0f);

        }

        return new ElementMarkSnapshot(element, stacks[index], remainingDurations[index]);


    }


    public void Apply(MagicElement element, int amount, float duration)
    {
        int index = (int)element;

        if (index < 0 || index >= ElementCount || amount <= 0f)
        {
            return;
        }

        ElementMarkSnapshot previous = Get(element);

        stacks[index] = Math.Min(ElementMarkRules.MaximumStacks, stacks[index] + amount);

        remainingDurations[index] = duration;

        RaiseChanged(previous, Get(element));



    }


    public void Consume(MagicElement element, int amount)
    {
        int index = (int)element;

        if (index < 0 || index >= ElementCount || amount <= 0)
        {
            return;
        }

        ElementMarkSnapshot previous = Get(element);

        stacks[index] = Math.Max(0, stacks[index] - amount);

        if (stacks[index] == 0)
        {
            remainingDurations[index] = 0f;
        }

        RaiseChanged(previous, Get(element));

    }

    // 개별 Update를 만들지 않고
    // 적을 순회하는 쪽이 불러줌
    public void Tick(float deltaTime)
    {
        for (int index = 0; index < ElementCount; index++)
        {
            if (stacks[index] <= 0)
            {
                continue;
            }

            remainingDurations[index] -= deltaTime;

            if (remainingDurations[index] > 0f)
            {
                continue;
            }

            MagicElement element = (MagicElement)index;

            ElementMarkSnapshot previous = Get(element);

            stacks[index] = 0;
            remainingDurations[index] = 0f;

            RaiseChanged(previous, Get(element));
        }
    }



    // 풀에서 재사용될 때 호출
    public void Reset()
    {
        Array.Clear(stacks, 0, stacks.Length);
        Array.Clear(remainingDurations, 0, remainingDurations.Length);
    }

    private void RaiseChanged(ElementMarkSnapshot previous, ElementMarkSnapshot current)
    {
        // 지속시간만 갱신된 경우는 알리지 않음
        if (previous.Stacks == current.Stacks)
        {
            return;
        }

        Changed?.Invoke(new ElementMarkChange(previous, current));
    }
}
