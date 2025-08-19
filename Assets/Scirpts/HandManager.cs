using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    private List<GameObject> handCards = new();

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            DrawCard();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            foreach (GameObject card in handCards)
            {
                Destroy(card);
            }
            handCards.Clear();
        }
    }

    private void DrawCard()
    {
        if (handCards.Count >= maxHandSize) return;

        GameObject go = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);
        handCards.Add(go);
        UpdateCardPositions();
    }

    private void UpdateCardPositions()
    {
        if(handCards.Count == 0) return;
        float cardSpacing = 1f / maxHandSize;
        float firstCardPositon = 0.5f - (handCards.Count - 1) * cardSpacing / 2f;
        Spline spline = splineContainer.Spline;
        for (int i = 0; i < handCards.Count; i++)
        {
            float p = firstCardPositon + i * cardSpacing;
            Vector3 splinePosition = spline.EvaluatePosition(p);
            Vector3 forward = spline.EvaluateTangent(p);
            Vector3 up = spline.EvaluateUpVector(p);
            Quaternion rotation = Quaternion.LookRotation(up, Vector3.Cross(up, forward).normalized);
            handCards[i].transform.DOMove(splinePosition, 0.25f);
            handCards[i].transform.DOLocalRotateQuaternion(rotation, 0.25f);
        }
    }
}
