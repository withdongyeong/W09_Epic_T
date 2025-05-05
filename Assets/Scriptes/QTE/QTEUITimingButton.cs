using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TimingButtonUI
{
    public Button mainButton;         // 메인 버튼
    public Image timerImage;          // 타이밍 시각화용 이미지
    public Color failColor = new Color(1f, 0.25f, 0.21f); // #FF4136 (빨강)
    public Color warningColor = new Color(1f, 0.86f, 0f); // #FFDC00 (노랑)
    public Color successColor = new Color(0.18f, 0.8f, 0.25f); // #2ECC40 (초록)
}

public class QTEUITimingButton : QTEUIBase
{
    [SerializeField] private TimingButtonUI ui;
    [Header("키보드 설정")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Space; // 스페이스 키 기본값으로 설정
    
    [Header("타이밍 설정")]
    private float initialScale = 3f;      // 시작 시 테두리 크기
    private float duration = 0.5f;          // QTE 지속 시간
    private float successTiming = 0.9f;   // 성공 타이밍 (0.9 = 90%, 거의 끝날 때)
    private float successWindow = 0.1f;   // 성공 허용 범위
    
    private float currentTime;
    private bool isActive;

    private void Awake()
    {
        SetupUI();
    }

    public override void StartQTE(System.Action<bool> onCompleteCallback)
    {
        base.StartQTE(onCompleteCallback);
        currentTime = 0f;
        isActive = true;
        ResetVisuals();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isActive) return;
        
        currentTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentTime / duration);
        
        // 타이머 이미지 크기 업데이트 (3배 크기에서 1배 크기로)
        float scale = Mathf.Lerp(initialScale, 1f, progress);
        ui.timerImage.transform.localScale = Vector3.one * scale;
        
        // 진행 상황에 따라 색상 변경
        UpdateColor(progress);
        
        // 키보드 입력 감지 추가
        if (Input.GetKeyDown(triggerKey))
        {
            CheckSuccess(progress);
        }
        
        // 시간 초과 시 실패
        if (progress >= 1f)
        {
            CheckSuccess(progress);
        }
    }
    
    private void UpdateColor(float progress)
    {
        // 성공 타이밍으로 단방향 진행 (빨강->노랑->초록)
        if (progress < successTiming - successWindow)
        {
            // 아직 타이밍 아님
            ui.timerImage.color = ui.failColor;
        }
        else if (progress < successTiming - (successWindow * 0.5f))
        {
            // 접근 중 (노랑)
            ui.timerImage.color = ui.warningColor;
        }
        else
        {
            // 성공 타이밍 (초록)
            ui.timerImage.color = ui.successColor;
        }
    }
    
    private void SetupUI()
    {
        ui.mainButton.onClick.AddListener(OnButtonClick);
        ResetVisuals();
    }
    
    private void ResetVisuals()
    {
        ui.timerImage.transform.localScale = Vector3.one * initialScale;
        ui.timerImage.color = ui.failColor;
    }
    
    private void OnButtonClick()
    {
        if (!isActive) return;
        CheckSuccess(currentTime / duration);
    }
    
    private void CheckSuccess(float progress) {
        float successStart = 0.75f; // 75% 지점
        bool isSuccess = (progress >= successStart && progress < 0.999f);
        
        CompleteQTE(isSuccess);
        isActive = false;
    }
    
    public override void CancelQTE()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}