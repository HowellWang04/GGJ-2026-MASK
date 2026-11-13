using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Swaps scene Text components between Chinese and English and adds a language button to the main menu.
// Created automatically at startup so no scene needs to reference it.
public class LocRuntime : MonoBehaviour
{
    class Original
    {
        public string zh;
        public bool bestFit;
        public int minSize, maxSize;
        public HorizontalWrapMode hOverflow;
        public VerticalWrapMode vOverflow;
    }

    readonly Dictionary<Text, Original> originals = new Dictionary<Text, Original>();
    Text toggleLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var go = new GameObject("Localization");
        DontDestroyOnLoad(go);
        go.AddComponent<LocRuntime>();
    }

    void Awake()
    {
        Loc.Init();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Loc.Changed += ApplyAll;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Loc.Changed -= ApplyAll;
    }

    void Start()
    {
        CreateToggleButton();
        ApplyAll();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyAll();

    void ApplyAll()
    {
        foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text == toggleLabel) continue;
            if (!originals.TryGetValue(text, out var orig))
            {
                if (!Loc.Has(text.text)) continue;
                orig = new Original
                {
                    zh = text.text,
                    bestFit = text.resizeTextForBestFit,
                    minSize = text.resizeTextMinSize,
                    maxSize = text.resizeTextMaxSize,
                    hOverflow = text.horizontalOverflow,
                    vOverflow = text.verticalOverflow,
                };
                originals[text] = orig;
            }

            // Leave texts that game code has since replaced with something else.
            string en = Loc.En(orig.zh);
            if (text.text != orig.zh && text.text != en) continue;

            text.text = Loc.English ? en : orig.zh;
            if (Loc.English) FitEnglish(text);
            else
            {
                text.resizeTextForBestFit = orig.bestFit;
                text.resizeTextMinSize = orig.minSize;
                text.resizeTextMaxSize = orig.maxSize;
                text.horizontalOverflow = orig.hOverflow;
                text.verticalOverflow = orig.vOverflow;
            }
        }
        if (toggleLabel != null) toggleLabel.text = Loc.English ? "中文" : "English";
    }

    // English runs longer than the Chinese the boxes were sized for, so let it shrink to fit.
    public static void FitEnglish(Text text)
    {
        if (!Loc.English || text.GetComponentInParent<LayoutGroup>() != null || text.GetComponent<ContentSizeFitter>() != null) return;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMaxSize = text.fontSize;
        text.resizeTextMinSize = Mathf.Max(10, text.fontSize / 2);
    }

    // Clones the main menu's quit button and pins it to the top left of the menu canvas.
    void CreateToggleButton()
    {
        Button quit = null;
        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null && label.text == "退出游戏" && button.transform.parent != null
                && HasSiblingLabel(button.transform.parent, "开始游戏"))
            {
                quit = button;
                break;
            }
        }
        if (quit == null) return;

        var clone = Instantiate(quit.gameObject, quit.transform.parent);
        clone.name = "LanguageButton";
        var rect = (RectTransform)clone.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(40f, -40f);
        rect.localRotation = Quaternion.identity;
        rect.localScale = quit.transform.localScale * 0.6f;

        var cloneButton = clone.GetComponent<Button>();
        cloneButton.onClick = new Button.ButtonClickedEvent();
        cloneButton.onClick.AddListener(Loc.Toggle);
        toggleLabel = clone.GetComponentInChildren<Text>(true);
    }

    static bool HasSiblingLabel(Transform parent, string label)
    {
        foreach (var text in parent.GetComponentsInChildren<Text>(true))
            if (text.text == label) return true;
        return false;
    }
}
