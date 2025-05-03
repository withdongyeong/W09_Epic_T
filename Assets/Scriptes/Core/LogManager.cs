using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI logText;
    private Queue<string> logQueue = new Queue<string>();
    private const int maxLines = 5;
    private float lastLogTime = -999f;
    private const float clearDelay = 2f; // 마지막 로그 이후 3초 후 초기화

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (logQueue.Count > 0 && Time.time - lastLogTime >= clearDelay)
        {
            ClearLogs();
        }
    }

    public void Log(string message)
    {
        logQueue.Enqueue(message);

        if (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        lastLogTime = Time.time;
        UpdateLogText();
    }

    private void UpdateLogText()
    {
        logText.text = string.Join("\n", logQueue.ToArray());
    }

    private void ClearLogs()
    {
        logQueue.Clear();
        UpdateLogText();
    }
}