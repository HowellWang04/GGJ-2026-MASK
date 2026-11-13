using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

using TMPro;

public class UIManager: Singleton<UIManager>
{
    public GameObject NoteButton;//��ǩ��ť
    public GameObject NotePanel;//��ǩҳ��
    public GameObject TipPanel;//��ʾҳ��
    public Text TipText;//��ʾҳ���text���?

    [Header("ҳ������")]
    [Header("ҳ������")]
    public CanvasGroup[] pages; // ʹ��CanvasGroup������ʾ
    public float fadeDuration = 0.3f; // ���뵭��ʱ��

    [Header("��ť")]
    public Button nextButton;   // ��һҳ��ť
    public Button prevButton;   // ��һҳ��ť

    [Header("͸��������")]
    [Range(0, 1)] public float activeAlpha = 1.0f;     // ����ʱ��͸����
    [Range(0, 1)] public float inactiveAlpha = 0.5f;   // ������ʱ��͸����

    private int currentPage = 0; 
    private bool isTransitioning = false;
    private bool isOpen=true;

    [Header("��ʾ�ı�")]
    [SerializeField] private string[] hintTexts;

    [Header("��Ϸ˵������")]
    public GameObject DescriptionPanel;
    public GameObject StartPanel;
    public bool IsDescription=true;

    [Header("������Ϸ����")]
    public GameObject PassPanel;

    [Header("����ҳ������")]
    [SerializeField]public string[] memoryTexts;
    public GameObject memoryPanel;
    public Text memoryText;
    public Rigidbody2D player;

    [SerializeField] private bool autoHide = true;
    [SerializeField] private float autoHideSeconds = 2.0f;

    [Header("Level 2 UI Elements")]
    [SerializeField] private GameObject level2UIRoot;   // Level2UIRoot
    [SerializeField] private GameObject phoneUIRoot;    // PhoneUIRoot
    [SerializeField] private Text messageHeader;    // MessageHeader 上的 TMP
    [SerializeField] private Text messageText;      // MessageText 上的 TMP
    
    [Header("Level 2 Dialogue System")]
    [SerializeField] private PhoneDialogueRunner phoneDialogueRunner;
    [Header("手机图标与动画")]
    [SerializeField] private RectTransform phoneIcon;           // 左上角手机图标
    [SerializeField] private AudioClip phoneNotificationSound;  // 手机提示音效
    [SerializeField] private AudioSource uiAudioSource;         // UI音效源
    [SerializeField] private float shakeDuration = 0.5f;        // 抖动持续时间
    [SerializeField] private float shakeStrength = 15f;         // 抖动强度
    private bool isShaking = false;

    [Header("结果字幕显示")]
    [SerializeField] private GameObject resultPanel;         // 结果面板
    [SerializeField] private CanvasGroup resultCanvasGroup;  // 结果面板CanvasGroup（用于淡入淡出）
    [SerializeField] private Text resultText;               // 结果文本
    [SerializeField] private float resultFadeDuration = 1f; // 淡入淡出时长
    [SerializeField] private float resultDisplayTime = 3f;  // 结果显示时间
    public event System.Action OnResultFadeComplete;        // 结果字幕淡出完成事件
    [Serializable]
    public class ReachDialogueBinding
    {
        public ReachLocationType type;
        public DialogueScript dialogue;
    }

    [SerializeField] private List<ReachDialogueBinding> reachDialogueBindings = new();
    private Dictionary<ReachLocationType, DialogueScript> reachDialogueMap;

    private int openUICount = 0;  // 当前打开的UI数量
    private bool hasMadeChoice = false;  // 是否已做出选择（防止未选择就关闭）
    private Player playerController;  // 玩家控制器引用


    void Start()
    {
        DontDestroyOnLoad(gameObject);
        // 初始化显示第一页
        ShowPageImmediate(currentPage);
        // 绑定按钮事件
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
        // 更新按钮状态
        UpdateButtonStates();
        // Hide Level 2 UI at start
        level2UIRoot.SetActive(false);
        // Build reach dialogue map
        reachDialogueMap = new Dictionary<ReachLocationType, DialogueScript>();
        foreach (var b in reachDialogueBindings)
        {
            if (b == null) continue;
            if (b.dialogue == null) continue;
            reachDialogueMap[b.type] = b.dialogue;
        }

       
        // 获取玩家控制器引用
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<Player>();
        }

        // 监听对话完成事件
        if (phoneDialogueRunner != null)
        {
            phoneDialogueRunner.OnDialogueComplete += OnDialogueComplete;
            phoneDialogueRunner.OnChoiceMade += OnChoiceMade;
        }

        if (GameTimerManager.Instance != null)
        {
            GameTimerManager.Instance.OnLevel2Complete += OnLevel2Complete;
        }

        // 初始化隐藏结果面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
            // 自动添加CanvasGroup组件
            if (resultCanvasGroup == null)
            {
                resultCanvasGroup = resultPanel.GetComponent<CanvasGroup>();
                if (resultCanvasGroup == null)
                {
                    resultCanvasGroup = resultPanel.AddComponent<CanvasGroup>();
                }
            }
            resultCanvasGroup.alpha = 0f;
        }
    }

    private void OnEnable()
    {
        EventHandler.UpdateTipsUI += UpdateTipTexts;

        EventHandler.UpdateMemoryUI += UpdateMemoryTexts;

        EventHandler.AfterSceneLoadEvent += OnAfterSceneLoad;

        StartCoroutine(SubscribeEventsNextFrame());
    }

    private void OnDisable()
    {
        EventHandler.UpdateTipsUI -= UpdateTipTexts;

        EventHandler.UpdateMemoryUI -= UpdateMemoryTexts;
        EventHandler.AfterSceneLoadEvent -= OnAfterSceneLoad;  
        if (GameTimerManager.Instance != null)
        {
            GameTimerManager.Instance.OnLevel2Complete -= OnLevel2Complete;
        }


    }
    private IEnumerator SubscribeEventsNextFrame()
    {
        yield return null;

        if (phoneDialogueRunner != null)
        {
            phoneDialogueRunner.OnDialogueComplete -= OnDialogueComplete;
            phoneDialogueRunner.OnChoiceMade -= OnChoiceMade;
            phoneDialogueRunner.OnDialogueComplete += OnDialogueComplete;
            phoneDialogueRunner.OnChoiceMade += OnChoiceMade;
            Debug.Log("[UIManager] 事件重新订阅完成");
        }
    }

    /// <summary>
    /// 场景加载完成后统一处理UI激活
    /// </summary>
    private void OnAfterSceneLoad()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        //// 进入Level2时激活手机UI
        //if (sceneName.Contains("Level2"))
        //{
        //    if (level2UIRoot != null)
        //    {
        //        level2UIRoot.SetActive(true);
        //        Debug.Log($"[UIManager] Level2场景加载完成，已激活Level2UIRoot");
        //    }
        //    else
        //    {
        //        Debug.LogError("[UIManager] level2UIRoot 引用丢失！请在Inspector中重新赋值");
        //    }
        //}
        //// 离开Level2时隐藏手机UI
        //else
        //{
        //    if (level2UIRoot != null)
        //    {
        //        level2UIRoot.SetActive(false);
        //    }
        //}

        // 离开Level2时隐藏手机UI
        if (!sceneName.Contains("Level2"))
        {
            if (level2UIRoot != null)
            {
                level2UIRoot.SetActive(false);
            }
        }
    }

    public void NextPage()
    {
        if (!isTransitioning && currentPage < pages.Length - 1)
        {
            StartCoroutine(TransitionToPage(currentPage + 1));
        }
    }

    // ��һҳ��������Ч����
    public void PrevPage()
    {
        if (!isTransitioning && currentPage > 0)
        {
            StartCoroutine(TransitionToPage(currentPage - 1));
        }
    }

    // ҳ������?��??
    private System.Collections.IEnumerator TransitionToPage(int newPage)
    {
        isTransitioning = true;

        // ������ǰҳ��
        yield return StartCoroutine(FadeCanvasGroup(pages[currentPage], 1, 0, fadeDuration));
        pages[currentPage].gameObject.SetActive(false);

        // ����ҳ��
        currentPage = newPage;

        // ������ҳ��
        pages[currentPage].gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(pages[currentPage], 0, 1, fadeDuration));

        // ���°�ť״̬
        UpdateButtonStates();
        isTransitioning = false;
    }

    // ������ʾҳ�棨�޹��ɣ�
    private void ShowPageImmediate(int pageIndex)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            bool isActive = (i == pageIndex);
            pages[i].gameObject.SetActive(isActive);
            pages[i].alpha = isActive ? 1 : 0;
            pages[i].interactable = isActive;
            pages[i].blocksRaycasts = isActive;
        }
    }

    // Э�̣����뵭��Ч��
    private System.Collections.IEnumerator FadeCanvasGroup(
        CanvasGroup group,
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            group.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        group.alpha = endAlpha;
    }

    // ���°�ť״̬
    private void UpdateButtonStates()
    {
        // ���ý���״̬
        bool canGoPrev = currentPage > 0;
        bool canGoNext = currentPage < pages.Length - 1;

        prevButton.interactable = canGoPrev;
        nextButton.interactable = canGoNext;

        // �ı䰴ť͸����
        SetButtonAlpha(prevButton, canGoPrev);
        SetButtonAlpha(nextButton, canGoNext);
    }

    // ���õ�����ť��͸����
    private void SetButtonAlpha(Button button, bool isActive)
    {
        // ��ȡ��ť��Image���?
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color newColor = buttonImage.color;
            newColor.a = isActive ? activeAlpha : inactiveAlpha;
            buttonImage.color = newColor;
        }

        // ��ѡ��ͬʱ�޸İ�ť�ϵ�����͸����
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            Color textColor = buttonText.color;
            textColor.a = isActive ? activeAlpha : inactiveAlpha;
            buttonText.color = textColor;
        }
    }
    public void ToggleNotes()
    {
        if (isOpen)
        {
            NotePanel.SetActive(true);
            isOpen=false;
        }
        else
        {
            NotePanel.SetActive(false);
            isOpen = true;
        }
    }

    //������ʾ�ı�
    public void UpdateTipTexts(int textIndex)
    {
        TipText.text= Loc.T(hintTexts[textIndex]);
        TipPanel.SetActive(true);

        // ʹ��Э��2������?
        StartCoroutine(HideAfterDelay(2f));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TipPanel.SetActive(false);
    }

    //˵�������ĵ��?
    public void ExplanationButton()
    {
        if (IsDescription)
        {
            DescriptionPanel.SetActive(true);
            IsDescription = false;
        }
        else
        {
            DescriptionPanel.SetActive(false);
            IsDescription = true;
        }
    }

    //�ÿ�ʼҳ��ر�?
    public void StartButton()
    {
        StartPanel.SetActive(false);
    }

    //�ر���Ϸ
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    //������Ϸ����Ĳ���?
    public void Passthelevel()
    {
        PassPanel.SetActive(true);
    }

    //���»����ı�
    public void UpdateMemoryTexts(int textIndex)
    {
        memoryText.text = Loc.T(memoryTexts[textIndex]);
        memoryPanel.SetActive(true);

        
        StartCoroutine(pass(6f));
    }

    private IEnumerator pass(float delay)
    {
        yield return new WaitForSeconds(delay);
        memoryPanel.SetActive(false);
    }

    //��ɫֹͣ�ƶ�
    public void playerDontMovent()
    {
        player.bodyType=RigidbodyType2D.Kinematic;
    }


    public void ShowPhoneDialogue(ReachLocationType type)
    {
        if (level2UIRoot != null)
        {
            level2UIRoot.SetActive(true);
        }

        // ========== 【打包修复】第二步：多种方式查找Level2UIRoot ==========
        if (level2UIRoot == null)
        {
            // 方式1：按名称查找
            level2UIRoot = GameObject.Find("Level2UIRoot");

            // 方式2：按标签查找（如果设置了标签）
            if (level2UIRoot == null)
            {
                GameObject[] allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (GameObject root in allRoots)
                {
                    if (root.name.Contains("Level2UI") || root.name.Contains("UI"))
                    {
                        level2UIRoot = root;
                        break;
                    }
                }
            }

            if (level2UIRoot == null)
            {
                Debug.LogError("【严重错误】level2UIRoot not found! " +
                              "请确保UIManager Inspector中已正确赋值！");
                return;
            }

            level2UIRoot.SetActive(true);
            Debug.Log($"[UIManager] 运行时找到level2UIRoot: {level2UIRoot.name}");
        }

        // 确保激活
        level2UIRoot.SetActive(true);

        // ========== 【打包修复】第三步：可靠查找PhoneUIRoot ==========
        if (phoneUIRoot == null)
        {
            // 方式1：从子对象中递归查找（包含未激活）
            phoneUIRoot = FindChildRecursive(level2UIRoot.transform, "PhoneUIRoot");

            // 方式2：全局查找
            if (phoneUIRoot == null)
            {
                phoneUIRoot = GameObject.Find("PhoneUIRoot");
            }

            if (phoneUIRoot == null)
            {
                Debug.LogError("【严重错误】phoneUIRoot not found! " +
                              "请确保UIManager Inspector中已正确赋值！");
                return;
            }

            Debug.Log($"[UIManager] 运行时找到phoneUIRoot: {phoneUIRoot.name}");
        }

        phoneUIRoot.SetActive(true);

        // ========== 其余原有代码保持不变 ==========
        if (phoneDialogueRunner == null)
        {
            phoneDialogueRunner = phoneUIRoot.GetComponentInChildren<PhoneDialogueRunner>(true);
        }

        if (phoneDialogueRunner == null)
        {
            Debug.LogError("PhoneDialogueRunner is not assigned on UIManager.");
            return;
        }
        if (reachDialogueMap == null)
        {
            Debug.LogError("Reach dialogue map not initialized.");
            return;
        }
        if (!reachDialogueMap.TryGetValue(type, out var script) || script == null)
        {
            Debug.LogError($"No DialogueScript bound for ReachLocationType: {type}. " +
                          $"Please configure reachDialogueBindings in UIManager Inspector!");
            return;
        }
        if (script.sections == null || script.sections.Count == 0)
        {
            Debug.LogError($"DialogueScript for {type} has no sections!");
            return;
        }

        // 确保PhoneUIRoot也激活
        if (phoneUIRoot != null)
        {
            phoneUIRoot.SetActive(true);
        }

        PlayPhoneShakeAnimation();
        PlayPhoneNotificationSound();
        openUICount++;
        hasMadeChoice = false;
        EventHandler.CallUIOpened(openUICount);

        
        if (playerController != null)
        {
            playerController.PauseMovement();
            Debug.Log("[UIManager] 手机UI弹出，暂停玩家移动");
        }
        else
        {
            // 兜底方案：直接操作Rigidbody
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerController = playerObj.GetComponent<Player>();
                player = playerObj.GetComponent<Rigidbody2D>();
                if (playerController != null)
                {
                    playerController.PauseMovement();
                }
                else if (player != null)
                {
                    player.bodyType = RigidbodyType2D.Kinematic;
                    player.velocity = Vector2.zero;
                }
            }
            Debug.Log("[UIManager] 手机UI弹出，使用兜底方式暂停");
        }

        phoneDialogueRunner.StartDialogue(script);
    }

    public void PhoneCloseBtn()
    {
       
        if (!hasMadeChoice)
        {
            Debug.Log("请先做出选择后再关闭！");
            return;  // 未做出选择，不允许关闭
        }
        // =========================================================

        phoneUIRoot.SetActive(false);
        Debug.Log("Close button clicked");

       
        openUICount--;
        EventHandler.CallUIClosed(openUICount);

        // 如果所有UI都关闭了
        if (openUICount <= 0)
        {
            openUICount = 0;
            EventHandler.CallAllUIClosed();

            // 确保恢复移动
            if (playerController != null)
            {
                playerController.ResumeMovement();
                Debug.Log("[UIManager] 手机UI关闭，恢复玩家移动");
            }
            else
            {
                // 兜底方案：重新获取并恢复
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerController = playerObj.GetComponent<Player>();
                    player = playerObj.GetComponent<Rigidbody2D>();
                    if (playerController != null)
                    {
                        playerController.ResumeMovement();
                    }
                    else if (player != null)
                    {
                        player.bodyType = RigidbodyType2D.Dynamic;
                    }
                }
                Debug.Log("[UIManager] 手机UI关闭，使用兜底方式恢复");
            }
        }
        
    }

    // ========== 【打包修复】递归查找子对象工具方法 ==========
    private GameObject FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }

            GameObject found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
    /// <summary>
    /// 对话全部完成时调用
    /// </summary>
    private void OnDialogueComplete()
    {
        Debug.Log("对话全部完成");
        // 对话完成后自动关闭UI
        PhoneCloseBtn();
    }

    /// <summary>
    /// 玩家做出选择时调用
    /// </summary>
    private void OnChoiceMade()
    {
        hasMadeChoice = true;
        Debug.Log("玩家已做出选择");
    }

    #region 手机图标抖动动画与音效

    /// <summary>
    /// 播放手机图标抖动动画
    /// </summary>
    private void PlayPhoneShakeAnimation()
    {
        if (phoneIcon == null || isShaking) return;

        StartCoroutine(ShakeCoroutine());
    }

    /// <summary>
    /// 抖动协程
    /// </summary>
    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        Vector2 originalPos = phoneIcon.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = UnityEngine.Random.Range(-1f, 1f) * shakeStrength;
            float y = UnityEngine.Random.Range(-1f, 1f) * shakeStrength;
            phoneIcon.anchoredPosition = originalPos + new Vector2(x, y);
            yield return null;
        }

        phoneIcon.anchoredPosition = originalPos;
        isShaking = false;
    }

    /// <summary>
    /// 播放手机提示音效
    /// </summary>
    private void PlayPhoneNotificationSound()
    {
        if (uiAudioSource == null || phoneNotificationSound == null) return;

        uiAudioSource.PlayOneShot(phoneNotificationSound);
    }

    #endregion


    #region 【Level2结果显示

    /// <summary>
    /// Level2完成时调用
    /// </summary>
    private void OnLevel2Complete(bool isOvertime)
    {
        ShowResultMessage(isOvertime);
    }

    /// <summary>
    /// 显示结果消息（带淡入效果）
    /// </summary>
    private void ShowResultMessage(bool isOvertime)
    {
        if (resultPanel == null || resultText == null)
        {
            Debug.LogWarning("Result UI not assigned!");
            return;
        }
        // 设置结果文本
        if (isOvertime)
        {
            resultText.text = Loc.T("你上班迟到了。。。");
        }
        else
        {
            resultText.text = Loc.T("你及时到达了公司");
        }
        // 显示结果面板并淡入
        resultPanel.SetActive(true);
        StartCoroutine(FadeResultIn());
    }

    /// <summary>
    /// 结果面板淡入协程
    /// </summary>
    private IEnumerator FadeResultIn()
    {
        if (resultCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < resultFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            resultCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / resultFadeDuration);
            yield return null;
        }
        resultCanvasGroup.alpha = 1f;

        // 显示指定时间后淡出
        yield return new WaitForSeconds(resultDisplayTime);
        StartCoroutine(FadeResultOut());
    }

    /// <summary>
    /// 结果面板淡出协程
    /// </summary>
    private IEnumerator FadeResultOut()
    {
        if (resultCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < resultFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            resultCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / resultFadeDuration);
            yield return null;
        }
        resultCanvasGroup.alpha = 0f;

        // 完全淡出后隐藏面板
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // 触发淡出完成事件，通知可以切换到第三关
        OnResultFadeComplete?.Invoke();
    }

    #endregion
}
