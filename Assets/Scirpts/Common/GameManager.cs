using Singleton.Component;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonComponent<GameManager>
{
    [SerializeField] private List<CardData> cardDatas;
    [SerializeField] private CardView cardView;

    private List<Card> deck;

    #region Singleton
    protected override void AwakeInstance()
    {
        
    }

    protected override bool InitInstance()
    {
        return true;
    }

    protected override void ReleaseInstance()
    {
        
    }
    #endregion

    private void Start()
    {
        SettingDeck();
    }

    public void DrawCard()
    {
        Card drawnCard = deck[Random.Range(0, deck.Count)];
        deck.Remove(drawnCard);
        CardView view = Instantiate(cardView);
        view.Setup(drawnCard);
        HandManager.Instance.handCards.Add(view.gameObject);
    }

    public void SettingDeck()
    {
        deck = new();
        for (int i = 0; i < 10; i++)
        {
            CardData data = cardDatas[Random.Range(0, cardDatas.Count)];
            Card card = new(data);
            deck.Add(card);
        }
    }
}
