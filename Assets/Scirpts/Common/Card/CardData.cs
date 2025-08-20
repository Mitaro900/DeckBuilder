using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data/Card", menuName = "CardData", order = 0)]
public class CardData : ScriptableObject
{
    [field: SerializeField] public Sprite Sprite { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }

    [field: SerializeReference]
    [field: SR]
    public List<CardEffect> Effects { get; private set; }
}
