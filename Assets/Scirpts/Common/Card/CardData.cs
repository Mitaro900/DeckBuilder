using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data/Card", menuName = "CardData", order = 0)]
public class CardData : ScriptableObject
{
    [SerializeReference]
    [SR]
    public List<CardEffect> effects;
}
