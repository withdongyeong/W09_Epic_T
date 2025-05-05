using UnityEngine;

public class QTETest : MonoBehaviour
{
    // 인스펙터에서 키 할당 가능
    [SerializeField] private KeyCode testKey1 = KeyCode.UpArrow;
    [SerializeField] private KeyCode testKey2 = KeyCode.DownArrow;

    private void Update()
    {
        // 테스트 키를 누르면 QTE 시작
        if (Input.GetKeyDown(testKey1))
        {
            StartTimingButtonQTE();
        }
        else if (Input.GetKeyDown(testKey2))
        {
            StartRapidQTE();
        }
        
    }

    private void StartTimingButtonQTE()
    {
        // QTE 시작
        Debug.Log("QTE Timing button 시작됨");
        QTEManager.Instance.StartQTE(QTEType.TimingButton, OnQTEComplete);
    }
    
    private void StartRapidQTE()
    {
        // QTE 시작
        Debug.Log("QTE rappid tapping 시작됨");
        QTEManager.Instance.StartQTE(QTEType.TapRapidly, OnQTEComplete);
    }

    private void OnQTEComplete(bool success)
    {
        // 결과 로그
        Debug.Log("QTE 결과: " + (success ? "성공" : "실패"));
    }
}