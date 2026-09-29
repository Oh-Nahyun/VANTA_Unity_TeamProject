using System;
using TMPro;
using UnityEngine;

public class AvoidKeyText : MonoBehaviour
{
   [SerializeField]private TextMeshProUGUI _textMeshPro;



   public void SetText(string text)
   {
       _textMeshPro.text = text;
   }
}
