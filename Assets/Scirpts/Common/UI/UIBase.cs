using System;
using UnityEngine;

//UI 데이터를 담는 클래스
public class UIBaseData
{
    public Action OnShow;  //UI가 표시될 때 실행할 콜백
    public Action OnClose; //UI가 닫힐 때 실행할 콜백
}

public class UIBase : MonoBehaviour
{
    public Animation m_UIOpenAnim; //UI 열림 애니메이션
    
    public event Action<UIBase> RequestedClose; //UI 닫기 요청 이벤트

    private Action m_OnShow;    //UI 표시 시 실행할 액션
    private Action m_OnClose;   //UI 닫기 시 실행할 액션

    // UI 초기화 메서드
    public virtual void Init(Transform anchor)
    {
        m_OnShow = null;     // 표시 액션 초기화
        m_OnClose = null;    // 닫기 액션 초기화

        transform.SetParent(anchor, false);    // 부모 Transform 설정

        var rectTransform = GetComponent<RectTransform>();    // RectTransform 컴포넌트 가져오기
        if (!rectTransform)    // RectTransform이 없는 경우
        {
            return;    // 메서드 종료
        }

        rectTransform.localPosition = Vector3.zero; // 로컬 위치 설정
        rectTransform.localScale = Vector3.one;     // 로컬 스케일 설정
        rectTransform.offsetMin = Vector2.zero;     // 최소 오프셋 설정
        rectTransform.offsetMax = Vector2.zero;     // 최대 오프셋 설정
    }

    // UI 정보 설정 메서드
    public virtual void SetInfo(UIBaseData uiData)
    {
        m_OnShow = uiData?.OnShow;      // 표시 콜백 설정
        m_OnClose = uiData?.OnClose;    // 닫기 콜백 설정
    }


    // UI 표시 메서드
    public virtual void ShowUI()
    {
        if (m_UIOpenAnim)    // 열림 애니메이션이 있는 경우
        {
            m_UIOpenAnim.Play();    // 애니메이션 재생
        }

        m_OnShow?.Invoke();    // 표시 콜백 실행
        m_OnShow = null;       // 표시 콜백 초기화
    }

    // UI 닫기 메서드
    public virtual void CloseUI(bool isCloseAll = false)
    {
        if (!isCloseAll)    // 전체 닫기가 아닌 경우
        {
            m_OnClose?.Invoke();    // 닫기 콜백 실행
        }
        m_OnClose = null;    // 닫기 콜백 초기화

        RequestedClose?.Invoke(this);
    }

    // 닫기 버튼 클릭 시 실행되는 메서드
    public virtual void OnClickCloseButton()
    {
        //AudioManager.Instance.PlaySFX(SFX.ui_close);    // 버튼 클릭 효과음 재생
        CloseUI();    // UI 닫기 실행
    }
}
