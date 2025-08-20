using System.Collections.Generic;
using UnityEngine;

public class Card
{
    private readonly CardData cardData;

    public Card(CardData _cardData)
    {
        cardData = _cardData;
    }

    public Sprite Sprite { get => cardData.Sprite; }
    public string Title { get => cardData.name; }
    public int Cost { get => cardData.Cost; }
    public List<CardEffect> Effects { get => cardData.Effects; }

    public void PerformEffect()
    {
        foreach (var effect in cardData.Effects)
        {
            effect.Perform();
        }
    }
}
