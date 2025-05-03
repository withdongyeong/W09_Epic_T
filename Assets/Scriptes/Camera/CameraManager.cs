using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private Camera mainCamera;
    [SerializeField] private float defaultSize = 5f;

    private Coroutine focusCoroutine;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void FocusBetweenPoints(Vector3 p1, Vector3 p2, float duration, float targetSize)
    {
        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);

        focusCoroutine = StartCoroutine(FocusAndZoomCoroutine(p1, p2, duration, targetSize));
    }

    public void ZoomOut(float duration)
    {
        if (focusCoroutine != null)
            StopCoroutine(focusCoroutine);

        focusCoroutine = StartCoroutine(FocusAndZoomCoroutine(mainCamera.transform.position, mainCamera.transform.position, duration, defaultSize));
    }

    private IEnumerator FocusAndZoomCoroutine(Vector3 p1, Vector3 p2, float duration, float targetSize)
    {
        Vector3 center = (p1 + p2) * 0.5f;
        Vector3 startPos = mainCamera.transform.position;
        Vector3 targetPos = new Vector3(center.x, center.y, startPos.z);

        float startSize = mainCamera.orthographicSize;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            yield return null;
        }

        mainCamera.transform.position = targetPos;
        mainCamera.orthographicSize = targetSize;
    }

    public void Shake(float strength, float duration)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine(strength, duration));
    }

    private IEnumerator ShakeCoroutine(float strength, float duration)
    {
        Vector3 originalPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;
            mainCamera.transform.position = originalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }
}
