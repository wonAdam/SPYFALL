using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using LitJson;

[CreateAssetMenu(fileName = "NewCardAsset.asset", menuName = "Custom Data/Card")]
[System.Serializable]
public class CardAsset : ScriptableObject
{

    [SerializeField] public string place;

    [SerializeField] public string player1;
    [SerializeField] public string player2;
    [SerializeField] public string player3;
    [SerializeField] public string player4;
    [SerializeField] public string player5;
    [SerializeField] public string player6;
    [SerializeField] public string player7;
    [SerializeField] public string player8;
    [SerializeField] public string player9;

}
