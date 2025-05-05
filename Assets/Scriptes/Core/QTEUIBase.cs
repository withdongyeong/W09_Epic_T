using UnityEngine;

public abstract class QTEUIBase : MonoBehaviour
{
    protected System.Action<bool> onComplete;

    public virtual void StartQTE(System.Action<bool> onCompleteCallback)
    {
        onComplete = onCompleteCallback;
    }

    protected void CompleteQTE(bool isSuccess)
    {
        onComplete?.Invoke(isSuccess);
        Destroy(gameObject);
    }
    
    public virtual void CancelQTE()
    {
        // 자식 클래스에서 오버라이드 가능
        // 기본 동작: 완료 콜백 호출 없이 객체 파괴
        Destroy(gameObject);
    }
}