#pragma warning disable 0649
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] GameManager GM;

    public int min = 0;
    public int sec = 0;
    public bool notify = false;

    private void Start() {
        Initialize_Timer(min);
    }

    public void Initialize_Timer(int param_min){
        min = param_min;
        sec = param_min * 60;

        int ten_min = (sec / 600) % 6;
        int one_min = (sec / 60) % 10;
        int ten_sec = (sec / 10) % 6;
        int one_sec = sec % 10;

        GetComponent<Text>().text = ten_min.ToString() + one_min.ToString() + " : " + ten_sec.ToString() + one_sec.ToString();
    }

    public void Update_Timer(int sec){
        int ten_min = (sec / 600) % 6;
        int one_min = (sec / 60) % 10;
        int ten_sec = (sec / 10) % 6;
        int one_sec = sec % 10;

        GetComponent<Text>().text = ten_min.ToString() + one_min.ToString() + " : " + ten_sec.ToString() + one_sec.ToString();


    }

}