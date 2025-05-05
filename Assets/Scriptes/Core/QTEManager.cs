using UnityEngine;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }
    
    [SerializeField] private QTEUITimingButton timingButtonPrefab;
    [SerializeField] private QTEUIRapidClickButton rapidClickPrefab; // 연타 QTE 프리팹 추가

    private QTEUIBase currentQTE;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void StartQTE(QTEType type, System.Action<bool> onComplete)
    {
        if (currentQTE != null)
        {
            Debug.LogWarning("QTE 이미 진행 중!");
            return;
        }

        currentQTE = CreateQTE(type);

        if (currentQTE != null)
        {
            currentQTE.StartQTE(result =>
            {
                currentQTE = null; // 끝나면 해제
                onComplete?.Invoke(result); // 요청자에게 결과 전달
            });
        }
    }

    private QTEUIBase CreateQTE(QTEType type)
    {
        switch (type)
        {
            case QTEType.TimingButton:
                return Instantiate(timingButtonPrefab, transform);
            case QTEType.TapRapidly:
                return Instantiate(rapidClickPrefab, transform);
            default:
                Debug.LogError("알 수 없는 QTE 타입");
                return null;
        }
    }
    
    // 진행 중인 QTE 취소 (필요시)
    public void CancelCurrentQTE()
    {
        if (currentQTE != null)
        {
            currentQTE.CancelQTE();
            currentQTE = null;
        }
    }
}