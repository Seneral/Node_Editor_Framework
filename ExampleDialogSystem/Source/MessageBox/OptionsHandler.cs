using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject _optionPrefab;

    private List<OptionsButtonHandler> _buttonHandlers = new List<OptionsButtonHandler>();
    private Action<int> _callback;

    public void CreateOptions(List<string> allOptions, Action<int> callBack)
    {
        _callback = callBack;
        for(int x = 0; x < allOptions.Count; x++)
        {
            OptionsButtonHandler buttonHandler = Instantiate(_optionPrefab).GetComponent<OptionsButtonHandler>();
            buttonHandler.transform.SetParent(transform,false);
            buttonHandler.SetText(allOptions[x]);
            buttonHandler.SetValueAndButtonCallBack(x, ButtonCallBack);
            _buttonHandlers.Add(buttonHandler);
        }
    }

    void ButtonCallBack(int value)
    {
        _callback(value);
    }

    public float CellHeight()
    {
        return GetComponent<GridLayoutGroup>().cellSize.y;
    }

    public void ClearList()
    {
        foreach(OptionsButtonHandler handler in _buttonHandlers)
        {
            Destroy(handler.gameObject);
        }
        _buttonHandlers.Clear();
    }
}
