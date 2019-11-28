using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainBlock : MonoBehaviour
{
    public int MainNum = 0;
    public TextMeshPro NumberUI;
    public List<Material> Mats;

    void Start()
    {
        NumberUI = GetComponentInChildren<TextMeshPro>();
    }

    public void SetMainNum(int num)
    {
        if (NumberUI == null)
            NumberUI = GetComponentInChildren<TextMeshPro>();

        MainNum = num;

        NumberUI.text = MainNum.ToString();
        SetColor();
    }

    public void SetColor()
    {
        switch (((MainNum - 1) % 25) / 5) // 칠할 색에 따라서 갈라진다.
        {
            default:
            case 0:
                GetComponent<Renderer>().material = Mats[0];
                break;
            case 1:
                GetComponent<Renderer>().material = Mats[1];
                break;
            case 2:
                GetComponent<Renderer>().material = Mats[2];
                break;
            case 3:
                GetComponent<Renderer>().material = Mats[3];
                break;
            case 4:
                GetComponent<Renderer>().material = Mats[4];
                break;
            case 5:
                GetComponent<Renderer>().material = Mats[5];
                break;
        }
    }

}
