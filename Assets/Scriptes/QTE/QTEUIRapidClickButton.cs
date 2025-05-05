using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class RapidClickButtonUI
{
    public Button mainButton;           // 메인 버튼
    public Image timerImage;            // 타이밍 시각화용 이미지 (시간 진행)
    public Image progressImage;         // 연타 진행도 이미지
}

public class QTEUIRapidClickButton : QTEUIBase
{
    [SerializeField] private RapidClickButtonUI ui;
    
    [Header("타이밍 설정")]
    [SerializeField] private float duration = 3f;          // QTE 지속 시간
    [SerializeField] private int requiredClicks = 10;      // 성공에 필요한 클릭 수
    [SerializeField] private KeyCode triggerKey = KeyCode.Space; // 스페이스 키
    
    [Header("시각화 설정")]
    [SerializeField] private float initialScale = 3f;      // 시작 시 이미지 크기
    
    private float currentTime;
    private int currentClicks;
    private bool isActive;

    private void Awake()
    {
        SetupUI();
    }

    public override void StartQTE(System.Action<bool> onCompleteCallback)
    {
        base.StartQTE(onCompleteCallback);
        currentTime = 0f;
        currentClicks = 0;
        isActive = true;
        ResetVisuals();
        gameObject.SetActive(true);
        
        Debug.Log($"연타 QTE 시작: duration={duration}, 목표={requiredClicks}회");
    }

    private void Update()
    {
        if (!isActive) return;
        
        currentTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentTime / duration);
        
        // 타이머 스케일 업데이트 (시간 진행에 따라 줄어듦)
        float timerScale = Mathf.Lerp(initialScale, 1f, progress);
        ui.timerImage.transform.localScale = Vector3.one * timerScale;
        
        // 키보드 입력 감지
        if (Input.GetKeyDown(triggerKey))
        {
            IncrementClicks();
        }
        
        // 시간 초과 시 결과 확인
        if (progress >= 1f)
        {
            CheckSuccess();
        }
    }
    
    private void SetupUI()
    {
        ui.mainButton.onClick.AddListener(IncrementClicks);
        ResetVisuals();
    }
    
    private void ResetVisuals()
    {
        ui.timerImage.transform.localScale = Vector3.one * initialScale;
        ui.progressImage.transform.localScale = Vector3.one * initialScale;
    }
    
    private void IncrementClicks()
    {
        if (!isActive) return;
        
        currentClicks++;
        
        // 연타 진행도 시각화 (클릭에 따라 줄어듦)
        float clickProgress = Mathf.Clamp01((float)currentClicks / requiredClicks);
        float progressScale = Mathf.Lerp(initialScale, 1f, clickProgress);
        ui.progressImage.transform.localScale = Vector3.one * progressScale;
        
        // 클릭 효과 (옵션)
        PlayClickFeedback();
        
        // 목표 달성 시 바로 성공
        if (currentClicks >= requiredClicks)
        {
            CheckSuccess();
        }
    }
        
    private void PlayClickFeedback()
    {
        // 애니메이션 없이 즉시 효과
        ui.mainButton.transform.localScale = Vector3.one * 0.95f;
        // 다음 프레임에 원래 크기로
        Invoke("ResetButtonScale", 0.1f);
    }

    private void ResetButtonScale()
    {
        ui.mainButton.transform.localScale = Vector3.one;
    }
    
    private void CheckSuccess()
    {
        bool isSuccess = currentClicks >= requiredClicks;
        CompleteQTE(isSuccess);
        isActive = false;
    }
    
    public override void CancelQTE()
    {
        isActive = false;
        gameObject.SetActive(false);
        base.CancelQTE();
    }
}