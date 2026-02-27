using Singleton.Component;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : SingletonComponent<UIManager>
{
    public EventSystem UIEventSystem { get; private set; }

    [SerializeField] private Transform m_OpenedUITrs;
    [SerializeField] private Transform m_ClosedUITrs;
    [SerializeField] private Image m_Fade;
    private UIBase m_FrontUI;
    private Dictionary<System.Type, GameObject> m_OpenUIPool = new Dictionary<System.Type, GameObject>();
    private Dictionary<System.Type, GameObject> m_ClosedUIPool = new Dictionary<System.Type, GameObject>();
    private Coroutine _fadeRoutine;

    #region Singleton
    protected override void AwakeInstance()
    {

    }

    protected override bool InitInstance()
    {
        UIEventSystem = GetComponentInChildren<EventSystem>();
        m_Fade.transform.localScale = Vector3.zero;

        return true;
    }

    protected override void ReleaseInstance()
    {

    }
    #endregion

    private UIBase GetUI<T>(out bool isAlreadyOpen)
    {
        System.Type uiType = typeof(T);

        UIBase ui = null;
        isAlreadyOpen = false;

        if (m_OpenUIPool.ContainsKey(uiType))
        {
            ui = m_OpenUIPool[uiType].GetComponent<UIBase>();
            isAlreadyOpen = true;
        }
        else if (m_ClosedUIPool.ContainsKey(uiType))
        {
            ui = m_ClosedUIPool[uiType].GetComponent<UIBase>();
            m_ClosedUIPool.Remove(uiType);
        }
        else
        {
            var uiObj = Instantiate(Resources.Load<GameObject>($"UI/{uiType.Name}"));
            ui = uiObj.GetComponent<UIBase>();
        }

        return ui;
    }

    public void OpenUI<T>(UIBaseData uiData)
    {
        System.Type uiType = typeof(T);

        Debug.Log($"{GetType()}::OpenUI({uiType})");

        bool isAlreadyOpen = false;
        var ui = GetUI<T>(out isAlreadyOpen);

        if (!ui)
        {
            Debug.LogError($"{uiType} does not exist.");
            return;
        }

        BindCloseEvent(ui);

        if (isAlreadyOpen)
        {
            Debug.LogError($"{uiType} is already open.");
            ui.transform.SetAsLastSibling();
            m_FrontUI = ui;
            ui.SetInfo(uiData);
            ui.ShowUI();
            return;
        }

        ui.Init(m_OpenedUITrs);
        ui.gameObject.SetActive(true);
        ui.SetInfo(uiData);
        ui.ShowUI();

        m_FrontUI = ui;
        m_OpenUIPool[uiType] = ui.gameObject;
    }

    public void CloseUI(UIBase ui)
    {
        if (!ui) return;

        System.Type uiType = ui.GetType();

        if (!m_OpenUIPool.TryGetValue(uiType, out var go) || go != ui.gameObject)
            return;

        Debug.Log($"CloseUI UI:{uiType}");

        UnbindCloseEvent(ui);

        ui.gameObject.SetActive(false);
        m_OpenUIPool.Remove(uiType);
        m_ClosedUIPool[uiType] = ui.gameObject;
        ui.transform.SetParent(m_ClosedUITrs);

        m_FrontUI = null;
        if (m_OpenedUITrs.childCount > 0)
        {
            var lastChild = m_OpenedUITrs.GetChild(m_OpenedUITrs.childCount - 1);
            if (lastChild)
            {
                m_FrontUI = lastChild.gameObject.GetComponent<UIBase>();
            }
        }
    }

    public T GetActiveUI<T>()
    {
        var uiType = typeof(T);
        return m_OpenUIPool.ContainsKey(uiType) ? m_OpenUIPool[uiType].GetComponent<T>() : default(T);
    }

    public bool ExistsOpenUI()
    {
        return m_FrontUI != null;
    }

    public UIBase GetCurrentFrontUI()
    {
        return m_FrontUI;
    }

    public void CloseCurrFrontUI()
    {
        if (m_FrontUI == null) return;
        m_FrontUI.CloseUI();
    }

    public void CloseAllOpenUI()
    {
        var opened = new List<GameObject>(m_OpenUIPool.Values);
        foreach (var go in opened)
        {
            if (!go) continue;
            var ui = go.GetComponent<UIBase>();
            if (ui) ui.CloseUI(true);
        }
    }

    private void BindCloseEvent(UIBase ui)
    {
        // 중복 구독 방지
        ui.RequestedClose -= OnUIRequestedClose;
        ui.RequestedClose += OnUIRequestedClose;
    }

    private void UnbindCloseEvent(UIBase ui)
    {
        ui.RequestedClose -= OnUIRequestedClose;
    }

    private void OnUIRequestedClose(UIBase ui)
    {
        CloseUI(ui);
    }

    public void Fade(Color color, float startAlpha, float endAlpha, float duration, float startDelay, bool deactiveOnFinish, Action onFinish = null)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeCo(color, startAlpha, endAlpha, duration, startDelay, deactiveOnFinish, onFinish));
    }

    public void CancelFade()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
        m_Fade.transform.localScale = Vector3.zero;
    }

    private IEnumerator FadeCo(Color color, float startAlpha, float endAlpha, float duration, float startDelay, bool deactiveOnFinish, Action onFinish)
    {
        yield return new WaitForSeconds(startDelay);

        m_Fade.transform.localScale = Vector3.one;
        m_Fade.color = new Color(color.r, color.g, color.b, startAlpha);

        var startTime = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - startTime < duration)
        {
            m_Fade.color = new Color(color.r, color.g, color.b, Mathf.Lerp(startAlpha, endAlpha, (Time.realtimeSinceStartup - startTime) / duration));
            yield return null;
        }

        m_Fade.color = new Color(color.r, color.g, color.b, endAlpha);

        if (deactiveOnFinish)
        {
            m_Fade.transform.localScale = Vector3.zero;
        }

        onFinish?.Invoke();
    }
}
