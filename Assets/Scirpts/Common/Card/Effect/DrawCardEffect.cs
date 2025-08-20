using UnityEngine;

public class DrawCardEffect : CardEffect
{
    public int amount;

    public override void Perform()
    {
        Debug.Log($"Draw {amount} Card");
    }
}
