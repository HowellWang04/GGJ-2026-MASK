using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Branch
{
    None,
    A,
    B
}

public class PhoneDialogueRunner : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;

    [Header("自动播放设置")]
    [SerializeField] private bool autoPlayAllLines = true;  // 自动播放所有行
    [SerializeField] private float lineDelay = 0.5f;       // 每行间隔时间

    [Header("Bubble Prefabs")]
    [SerializeField] private GameObject questionBubblePrefab;
    [SerializeField] private GameObject answerBubblePrefab;
    [Header("字体设置")]
    [SerializeField] private Font defaultFont; 

    [Header("Choice Popup")]
    [SerializeField] private ChoicePopupUI choicePopup;

    private DialogueScript currentScript;
    private int sectionIndex;
    private int lineIndex;
    private Branch currentBranch = Branch.None;
    private bool isPlayingLines = false;  // 是否正在播放行
    /// <summary>
    /// 所有对话完成事件
    /// </summary>
    public event System.Action OnDialogueComplete;

    /// <summary>
    /// 玩家做出选择事件
    /// </summary>
    public event System.Action OnChoiceMade;

    [Header("强制字体引用")]
    [SerializeField] private Font fallbackFont;  // 在Inspector拖入Arial字体
    [SerializeField] private TMPro.TMP_FontAsset fallbackTMPFont; // 拖入默认TMP字体

    // 用于强制Unity在打包时包含字体资源
    [SerializeField] private List<Object> forceIncludeAssets = new List<Object>();

    // ===================== 对外入口 =====================
    private void Awake()
    {
        // ========== 【打包修复】强制字体引用 ==========
        // 确保字体资源被打包
        if (defaultFont == null)
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (fallbackFont == null)
            fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 打印日志用于调试
        Debug.Log($"[PhoneDialogueRunner] 字体初始化完成: " +
                  $"defaultFont={defaultFont?.name}, " +
                  $"fallbackFont={fallbackFont?.name}, " +
                  $"fallbackTMPFont={fallbackTMPFont?.name}");
    }
    public void StartDialogue(DialogueScript script)
    {
        if (script == null)
        {
            Debug.LogError("PhoneDialogueRunner.StartDialogue: script is null!");
            return;
        }
        if (script.sections == null || script.sections.Count == 0)
        {
            Debug.LogError("PhoneDialogueRunner.StartDialogue: script has no sections!");
            return;
        }

        currentScript = script;
        sectionIndex = 0;
        lineIndex = 0;
        currentBranch = Branch.None;

        // 确保对话开始前清空聊天记录
        ClearChat();

        ShowSectionPrompt();
    }

    // ===================== Section 流程 =====================

    private void ShowSectionPrompt()
    {
        if (sectionIndex >= currentScript.sections.Count)
        {
            
            OnDialogueComplete?.Invoke();
            return;
        }
        var section = currentScript.sections[sectionIndex];
        // 1. 显示 prompt（问/答气泡）
        AddDialogueLine(section.prompt);
        // 2. 弹出选择 Popup
        choicePopup.Show(
            section.choiceA.text,
            section.choiceB.text,
            OnChooseA,
            OnChooseB
        );
    }

    private void OnChooseA()
    {
        var section = currentScript.sections[sectionIndex];
        // 把选中的 choice 写入聊天记录（Answer 气泡）
        AddAnswerBubble(section.choiceA.text);
        currentBranch = Branch.A;
        lineIndex = 0;
        choicePopup.Hide();

        
        OnChoiceMade?.Invoke();
        // =========================================================

        ShowNextLine();
    }
    private void OnChooseB()
    {
        var section = currentScript.sections[sectionIndex];
        AddAnswerBubble(section.choiceB.text);
        currentBranch = Branch.B;
        lineIndex = 0;
        choicePopup.Hide();

      
        OnChoiceMade?.Invoke();
        // =========================================================

        ShowNextLine();
    }


    // ===================== 分支内容播放 =====================

    private void ShowNextLine()
    {
        if (isPlayingLines) return;  // 防止重复调用

        var section = currentScript.sections[sectionIndex];
        List<DialogueLine> lines =
            currentBranch == Branch.A ? section.branchALines : section.branchBLines;

        if (lineIndex >= lines.Count)
        {
            // 当前 section 所有行播放完毕 → 进入下一个 section
            sectionIndex++;
            currentBranch = Branch.None;
            lineIndex = 0;
            isPlayingLines = false;
            ShowSectionPrompt();
            return;
        }

        if (autoPlayAllLines)
        {
            // 自动播放所有剩余行
            StartCoroutine(PlayAllLinesCoroutine(lines));
        }
        else
        {
            // 手动模式：只显示当前行（需要点击继续）
            AddDialogueLine(lines[lineIndex]);
            lineIndex++;
        }
    }

    private IEnumerator PlayAllLinesCoroutine(List<DialogueLine> lines)
    {
        isPlayingLines = true;

        while (lineIndex < lines.Count)
        {
            AddDialogueLine(lines[lineIndex]);
            lineIndex++;
            yield return new WaitForSeconds(lineDelay);
        }

        isPlayingLines = false;

        // 所有行播放完毕，自动进入下一个 section
        sectionIndex++;
        currentBranch = Branch.None;
        lineIndex = 0;
        ShowSectionPrompt();
    }

    // ===================== UI 工具函数 =====================

    private void AddDialogueLine(DialogueLine line)
    {
        GameObject prefab =
            line.type == LineType.Question
                ? questionBubblePrefab
                : answerBubblePrefab;
        GameObject bubble = Instantiate(prefab, content);

       
        bool textFound = false;

        // 1. 先尝试找原生Text组件
        Text dialogueText = bubble.GetComponentInChildren<Text>(true);
        if (dialogueText != null)
        {
            dialogueText.text = Loc.T(line.text);
            if (defaultFont != null)
            {
                dialogueText.font = defaultFont;
            }
            dialogueText.enabled = true;
            dialogueText.gameObject.SetActive(true);
            dialogueText.color = Color.black; // 强制设置颜色
            textFound = true;
        }

        // 2. 再尝试找TextMeshPro组件
        TMPro.TMP_Text tmpText = bubble.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = Loc.T(line.text);
            tmpText.enabled = true;
            tmpText.gameObject.SetActive(true);
            tmpText.color = Color.black; // 强制设置颜色
            textFound = true;
        }

        if (!textFound)
        {
            Debug.LogError($"气泡预制体 {prefab.name} 上没有找到任何Text组件！" +
                          $"请检查预制体是否有Text或TMP_Text组件");
        }

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    private void AddAnswerBubble(string text)
    {
        GameObject bubble = Instantiate(answerBubblePrefab, content);

       
        bool textFound = false;

        Text answerText = bubble.GetComponentInChildren<Text>(true);
        if (answerText != null)
        {
            answerText.text = Loc.T(text);
            if (defaultFont != null)
            {
                answerText.font = defaultFont;
            }
            answerText.enabled = true;
            answerText.gameObject.SetActive(true);
            answerText.color = Color.black;
            textFound = true;
        }

        TMPro.TMP_Text tmpText = bubble.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = Loc.T(text);
            tmpText.enabled = true;
            tmpText.gameObject.SetActive(true);
            tmpText.color = Color.black;
            textFound = true;
        }

        if (!textFound)
        {
            Debug.LogError("答案气泡预制体上没有找到任何Text组件！请检查预制体");
        }

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    private void ClearChat()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }
    }

    private void ScrollToBottom()
    {
        StartCoroutine(ScrollNextFrame());
    }

    private IEnumerator ScrollNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}
