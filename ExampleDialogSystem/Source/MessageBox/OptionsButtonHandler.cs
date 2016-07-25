using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OptionsButtonHandler : MonoBehaviour
{
    [SerializeField]
    private Text _optionText;

    [SerializeField]
    private Button _button;

    public void SetText(string option)
    {
        _optionText.text = option;
    }

    public void SetValueAndButtonCallBack(int value, Action<int> buttonCallBack)
    {
        _button.onClick.AddListener(delegate { buttonCallBack(value); });
    }
}
