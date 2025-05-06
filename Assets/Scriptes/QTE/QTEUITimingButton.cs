using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TimingButtonUI
{
    public Button mainButton;         // 메인 버튼
    public Image clockHand;           // 시계 바늘 이미지
    public Image successZone;         // 성공 구간 이미지
    public Image failZone;            // 실패 구간 이미지
}

public class QTEUITimingButton : QTEUIBase
{
    [SerializeField] private TimingButtonUI ui;
    
    [Header("키보드 설정")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Space; // 스페이스 키 기본값으로 설정
    
    [Header("타이밍 설정")]
    [SerializeField] private float duration = 0.5f;          // QTE 지속 시간 (초)
    [SerializeField] private float successStartTime = 0.4f;  // 성공 시작 시간 (초)
    [SerializeField] private float successEndTime = 0.5f;    // 성공 종료 시간 (초)
    
    private float startTime;          // QTE 시작 시간
    private float currentTime;        // 현재 진행 시간
    private bool isActive;

    private void Awake()
    {
        SetupUI();
    }

    public override void StartQTE(System.Action<bool> onCompleteCallback)
    {
        base.StartQTE(onCompleteCallback);
        startTime = Time.time;        // 시작 시간 기록
        currentTime = 0f;
        isActive = true;
        ResetVisuals();
        SetupSuccessZones();
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!isActive) return;
        
        // 절대 시간으로 진행 시간 계산
        currentTime = Time.time - startTime;
        float progress = Mathf.Clamp01(currentTime / duration);
        
        // 시계 바늘 회전 업데이트 (0도에서 360도까지)
        float rotationAngle = progress * 360f;
        ui.clockHand.rectTransform.rotation = Quaternion.Euler(0, 0, -rotationAngle);
        
        // 키보드 입력 감지
        if (Input.GetKeyDown(triggerKey))
        {
            CheckSuccess();
        }
        
        // 시간 초과 시 명시적 실패 처리
        // 버튼 사라지기 직전 여유분 시간 존재 (0.1)
        if (currentTime >= duration + 0.1f)
        {
            CompleteQTE(false); // 명시적으로 실패로 처리
            isActive = false;
        }
    }
    
    private void SetupUI()
    {
        ui.mainButton.onClick.AddListener(OnButtonClick);
        ResetVisuals();
    }
    
    private void ResetVisuals()
    {
        // 시계 바늘을 시작 위치(0도)로 설정
        ui.clockHand.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
    private void SetupSuccessZones()
    {
        // 성공 구간 비율 계산 (시각화용)
        float successStartRatio = successStartTime / duration;
        float successStartAngle = successStartRatio * 360f;
        
        // 성공 구간 시각화
        ui.successZone.gameObject.SetActive(true);
        ui.failZone.gameObject.SetActive(true);
        
        // 성공 구간 설정
        ui.successZone.fillMethod = Image.FillMethod.Radial360;
        ui.successZone.fillOrigin = (int)Image.Origin360.Top; // 12시 위치가 기준점
        ui.successZone.fillClockwise = false; // 반시계 방향으로 채움
        ui.successZone.fillAmount = (360f - successStartAngle) / 360f; // 시각적으로 표시
        
        // 실패 구간 설정
        ui.failZone.fillMethod = Image.FillMethod.Radial360;
        ui.failZone.fillOrigin = (int)Image.Origin360.Top; // 12시 위치가 기준점
        ui.failZone.fillClockwise = true; // 시계 방향으로 채움
        ui.failZone.fillAmount = successStartAngle / 360f; // 시각적으로 표시
    }
    
    private void OnButtonClick()
    {
        if (!isActive) return;
        CheckSuccess();
    }
    
    private void CheckSuccess()
    {
        // 절대 시간 기반 성공 여부 판단
        float elapsedTime = Time.time - startTime;
        // 사라지기 직전 여유분 시간 존재 (0.1)
        bool isSuccess = (elapsedTime >= successStartTime && elapsedTime <= successEndTime + 0.1);
        
        CompleteQTE(isSuccess);
        isActive = false;
    }
    
    public override void CancelQTE()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}