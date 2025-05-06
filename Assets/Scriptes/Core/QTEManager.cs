using UnityEngine;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }
    
    [SerializeField] private QTEUITimingButton timingButtonPrefab;
    [SerializeField] private QTEUIRapidClickButton rapidClickPrefab; // 연타 QTE 프리팹
    [SerializeField] private Transform defaultQTEParent; // 기본 부모 Transform (Canvas 등)
    
    private QTEUIBase currentQTE;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // 기본 부모가 설정되지 않았으면 자기 자신으로 설정
        if (defaultQTEParent == null)
        {
            defaultQTEParent = transform;
        }
    }

    /// <summary>
    /// 기본 위치에 QTE를 시작합니다.
    /// </summary>
    public void StartQTE(QTEType type, System.Action<bool> onComplete)
    {
        StartQTEAtPosition(type, defaultQTEParent, Vector3.zero, onComplete);
    }
    
    /// <summary>
    /// 지정된 부모와 위치에 QTE를 시작합니다.
    /// </summary>
    public void StartQTEAtPosition(QTEType type, Transform parent, Vector3 position, System.Action<bool> onComplete)
    {
        if (currentQTE != null)
        {
            Debug.LogWarning("QTE 이미 진행 중!");
            return;
        }

        // QTE 생성
        QTEUIBase qte = null;
        switch (type)
        {
            case QTEType.TimingButton:
                qte = Instantiate(timingButtonPrefab, parent);
                break;
            case QTEType.TapRapidly:
                qte = Instantiate(rapidClickPrefab, parent);
                break;
            default:
                Debug.LogError("알 수 없는 QTE 타입");
                return;
        }
        
        if (qte != null)
        {
            // QTE의 RectTransform 위치 설정
            RectTransform rectTransform = qte.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(position.x, position.y);
            }
            
            // 내부 Canvas가 있는 경우, Canvas의 위치도 설정
            Canvas nestedCanvas = qte.GetComponentInChildren<Canvas>();
            if (nestedCanvas != null)
            {
                RectTransform canvasRectTransform = nestedCanvas.GetComponent<RectTransform>();
                if (canvasRectTransform != null)
                {
                    // Canvas가 QTE 내부에 있는 경우, 0,0 위치에 배치
                    canvasRectTransform.anchoredPosition = Vector2.zero;
                }
                
                // Canvas의 렌더 모드 설정
                nestedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // 내부 UI 요소들의 위치 조정이 필요한 경우 추가 작업
                Debug.Log("QTE 내부에 Canvas가 발견되었습니다. 추가 위치 조정이 필요할 수 있습니다.");
            }
            
            currentQTE = qte;
            currentQTE.StartQTE(result =>
            {
                currentQTE = null;
                onComplete?.Invoke(result);
            });
        }
    }
    
    /// <summary>
    /// 월드 위치에 QTE를 시작합니다 (메인 카메라 기준).
    /// </summary>
    public void StartQTEAtWorldPosition(QTEType type, Vector3 worldPosition, System.Action<bool> onComplete)
    {
        if (Camera.main == null)
        {
            Debug.LogError("메인 카메라가 없습니다!");
            return;
        }
        
        // 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        // QTE 오브젝트 생성
        if (currentQTE != null)
        {
            Debug.LogWarning("QTE 이미 진행 중!");
            return;
        }
        
        QTEUIBase qte = null;
        switch (type)
        {
            case QTEType.TimingButton:
                qte = Instantiate(timingButtonPrefab, defaultQTEParent);
                break;
            case QTEType.TapRapidly:
                qte = Instantiate(rapidClickPrefab, defaultQTEParent);
                break;
            default:
                Debug.LogError("알 수 없는 QTE 타입");
                return;
        }
        
        if (qte != null)
        {
            // 루트 오브젝트 위치 설정
            RectTransform qteRectTransform = qte.GetComponent<RectTransform>();
            if (qteRectTransform != null)
            {
                // 스크린 좌표를 부모 Canvas 기준의 로컬 좌표로 변환
                Canvas parentCanvas = defaultQTEParent.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentCanvas.GetComponent<RectTransform>(),
                        screenPos,
                        parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                        out Vector2 localPoint);
                    
                    qteRectTransform.localPosition = localPoint;
                }
            }
            
            // 내부 Canvas가 있으면 조정
            Canvas nestedCanvas = qte.GetComponentInChildren<Canvas>();
            if (nestedCanvas != null)
            {
                // 내부 Canvas는 Screen Space Overlay로 설정하고, 위치를 원점으로
                nestedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                RectTransform canvasRectTransform = nestedCanvas.GetComponent<RectTransform>();
                if (canvasRectTransform != null)
                {
                    // Canvas 위치 조정 필요 시
                    // 현재 위치에서 스크린 좌표로 이동
                    canvasRectTransform.position = screenPos;
                }
            }
            
            currentQTE = qte;
            currentQTE.StartQTE(result =>
            {
                currentQTE = null;
                onComplete?.Invoke(result);
            });
        }
    }
    
    /// <summary>
    /// 진행 중인 QTE를 취소합니다.
    /// </summary>
    public void CancelCurrentQTE()
    {
        if (currentQTE != null)
        {
            currentQTE.CancelQTE();
            currentQTE = null;
        }
    }
}