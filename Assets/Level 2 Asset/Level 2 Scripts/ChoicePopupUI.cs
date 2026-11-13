using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Security.Cryptography;

public class ChoicePopupUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Button buttonA;
    [SerializeField] private Button buttonB;
    [SerializeField] private Text textA;
    [SerializeField] private Text textB;

    [Header("字体设置")]
    [SerializeField] private Font defaultFont;

    public void Show(string a, string b, System.Action onA, System.Action onB)
    {
        root.SetActive(true);
        textA.text = Loc.T(a);
        textB.text = Loc.T(b);
        // 强制设置字体，确保打包后显示
        if (defaultFont != null)
        {
            textA.font = defaultFont;
            textB.font = defaultFont;
        }
        // 确保Text组件启用
        textA.enabled = true;
        textB.enabled = true;
        buttonA.onClick.RemoveAllListeners();
        buttonB.onClick.RemoveAllListeners();
        buttonA.onClick.AddListener(() => onA());
        buttonB.onClick.AddListener(() => onB());
        // 强制刷新Canvas
        Canvas.ForceUpdateCanvases();
    }

    public void Hide()
    {
        root.SetActive(false);
    }
}
