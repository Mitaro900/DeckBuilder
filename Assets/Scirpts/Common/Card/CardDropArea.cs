using UnityEngine;

public class CardDropArea : MonoBehaviour, ICardDropArea
{
    public void OnCardDrop(Card card)
    {
        card.transform.position = transform.position;
        Debug.Log("Card dropped here");
    }
}
