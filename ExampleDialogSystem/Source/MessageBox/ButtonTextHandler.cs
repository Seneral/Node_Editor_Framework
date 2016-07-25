using UnityEngine;
using UnityEngine.UI;

public class ButtonTextHandler : MonoBehaviour
{
    [SerializeField]
    private Text _buttonText;

    public void SetText(EButtonText eButtonText)
    {
        _buttonText.text = eButtonText.ToString();
    }

    public void ShowButton(bool show)
    {
        gameObject.SetActive(show);
    }
}

public enum EButtonText
{
    OKAY,
    NEXT,
}
