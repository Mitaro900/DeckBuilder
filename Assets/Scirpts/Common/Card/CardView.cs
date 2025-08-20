using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardView : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI cost;

    private Card card;

    private Collider2D col;
    private Vector3 startDragPosition;

    private void Start()
    {
        col = GetComponent<Collider2D>();
    }

    public void Setup(Card card)
    {
        this.card = card;
        cardImage.sprite = card.Sprite;
        title.text = card.Title;
        cost.text = card.Cost.ToString();
    }

    private void OnMouseDown()
    {
        startDragPosition = transform.position;
        StartCoroutine(RotateCo(Quaternion.Euler(0, 0, 0)));
        transform.position = GetMousePositionInWorldSpace();
        if (HandManager.Instance.handCards.Contains(gameObject))
        {
            HandManager.Instance.handCards.Remove(gameObject); // Remove card from hand when dragging starts
            HandManager.Instance.UpdateCardPositions(); // Update card positions in hand when dragging starts
        }
    }

    private void OnMouseDrag()
    {
        transform.position = GetMousePositionInWorldSpace();
    }

    private void OnMouseUp()
    {
        col.enabled = false;
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
        col.enabled = true;
        if (hitCollider != null && hitCollider.TryGetComponent(out ICardDropArea cardDropArea))
        {
            cardDropArea.OnCardDrop(this);
        }
        else
        {
            transform.position = startDragPosition;
            StopAllCoroutines();
            HandManager.Instance.handCards.Add(gameObject); // Re-add card to hand if not dropped in a valid area
            HandManager.Instance.UpdateCardPositions(); // Update card positions in hand when dragging ends
        }
    }

    public Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f; // Assuming a 2D game, set z to 0
        return p;
    }

    private IEnumerator RotateCo(Quaternion targetRotation)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        Quaternion startRotation = transform.rotation;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }
        transform.rotation = targetRotation; // Ensure final rotation is set
    }
}
