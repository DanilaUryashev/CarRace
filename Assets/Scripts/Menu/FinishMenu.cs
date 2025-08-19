using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishMenu : MonoBehaviour
{
    [Header("���������")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.gray;
   
    [Header("������������ �������")]
    [SerializeField] private Transform parentOptionLaps; // ��������� ��� �������� ��� �����
    [SerializeField] private Transform parentOptionMaps;
    [SerializeField] private Transform parentOptionDifficulity;
    [Header("��������� �����")]
    public int lapsToCompleteCurrent;
    public int mapCurrent;
    public int difficultCurrent;
    [System.Serializable]
    public class Stopwatch
    {
        public TextMeshProUGUI display;
        public bool isRunning;
        public float currentTime;
        public List<float> lapTimes = new List<float>();
    }

    [Header("�����������")]
    [SerializeField] private Stopwatch totalStopwatch; // ����� ����� �����
    [SerializeField] private Stopwatch lapStopwatch;   // ������� ����
    private JsonManager jsonManager;

    [SerializeField] GameObject finishRaceUI;

    GhostRecorder recorderGhost;


    private void Start()
    {
        SelectLaps(1);
        SelectMap(1);
        SelectDifficulityGhost(2);
        recorderGhost = FindFirstObjectByType<GhostRecorder>();
        jsonManager = FindAnyObjectByType<JsonManager>();
    }

    void Update()
    {
        UpdateStopwatch(ref totalStopwatch);
        UpdateStopwatch(ref lapStopwatch);
    }
    #region
    public void ViewFinishMenu()
    {
        if (finishRaceUI != null) 
        {
            if (!finishRaceUI.activeSelf) 
            {
                finishRaceUI.SetActive(true);
            }
            else finishRaceUI.SetActive(false);

        }
        
    }
    #endregion

    #region MenuOptions

    public void SelectLaps(int laps) // ����� ��������� ������
    {
        HighlightSelectedOption(laps, parentOptionLaps);
        lapsToCompleteCurrent = laps;
       

    }
    public void SelectMap(int map) // ����� �����
    {
        HighlightSelectedOption(map, parentOptionMaps);
        mapCurrent = map;

    }
    public void SelectDifficulityGhost(int difficult) // ��������� ����/��������
    {
        HighlightSelectedOption(difficult, parentOptionDifficulity);
        difficultCurrent = difficult;
       

    }
    private void HighlightSelectedOption(int option, Transform setting) // ��������� ��������� �����
    {
        int childIndex = option - 1;

        for (int i = 0; i < setting.childCount; i++)
        {
            Transform child = setting.GetChild(i);
            TextMeshProUGUI text = child.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                // �������� ������ ��������� �������
                text.color = (i == childIndex) ? selectedColor : normalColor;
            }
        }

    }
    private void SaveRaceSettings()
    {

        RaceSetting raceSetting = new RaceSetting()
        {
            lapsToWin = lapsToCompleteCurrent // ���� �������� �� UI
        };

        // ��������� ����� JsonManager (����������� � ������)
        jsonManager.SaveRaceData(null, new RaceSetting[] { raceSetting }, null);

        Debug.Log($"��������� ������: {lapsToCompleteCurrent}");
    }

    public void ApplyAndRestart() // �������� ��������� ������
    {
        // 1. ��������� ���������
        SaveRaceSettings();
        string sceneName = Enum.GetName(typeof(Maps), mapCurrent - 1);
        Debug.Log(sceneName);
        // 2. ������������� �����
        SceneManager.LoadScene(sceneName);

        //// 3. ��������������� ����� (���� �����)
        //Time.timeScale = 1f;
    }

    public void RestartVSGhost()
    {
        if (recorderGhost.HasGhostForCurrentSettings())
        {
            Debug.Log("������� ����");
            RaceSetting raceSetting = new RaceSetting()
            {
                vsGhost = true,
                lapsToWin = lapsToCompleteCurrent

            };
            jsonManager.SaveRaceData(null, new RaceSetting[] { raceSetting }, null);
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
            //recorderGhost.SpawnGhost();
            //Time.timeScale = 1f;
        }
        else
        {

            Debug.Log("�������� ��� ������ ����� �����");
        }
    }
    #endregion

    #region Timer


    private void UpdateStopwatch(ref Stopwatch sw)
    {
        if (!sw.isRunning) return;

        sw.currentTime += Time.deltaTime;
        UpdateDisplay(sw);
    }

    private void UpdateDisplay(Stopwatch sw)
    {
        if (sw.display != null)
        {
            sw.display.text = FormatTime(sw.currentTime);
        }
    }

    // === ���������� ����� �������� ===
    public void StartRaceTime() => StartStopwatch(ref totalStopwatch);
    public void StopRaceTime() => StopStopwatch(ref totalStopwatch);

    // === ���������� �������� ����� ===
    public void StartLapTime() => StartStopwatch(ref lapStopwatch);

    public void StopLapTime()
    {
        if (lapStopwatch.isRunning)
        {
            lapStopwatch.lapTimes.Add(lapStopwatch.currentTime);
            Debug.Log($"���� {lapStopwatch.lapTimes.Count}: {FormatTime(lapStopwatch.currentTime)}");
        }
        StopStopwatch(ref lapStopwatch);
    }

    public void ResetLapTime()
    {
        lapStopwatch.currentTime = 0f;
        UpdateDisplay(lapStopwatch);
    }

    //����� ������ 
    private void StartStopwatch(ref Stopwatch sw)
    {
        sw.isRunning = true;
    }

    private void StopStopwatch(ref Stopwatch sw)
    {
        sw.isRunning = false;
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        int millisecs = Mathf.FloorToInt((seconds * 100) % 100);
        return $"{minutes:00}:{secs:00}.{millisecs:00}";
    }

    //�������
    public List<float> GetLapTimes() => lapStopwatch.lapTimes;
    public float GetCurrentLapTime() => lapStopwatch.currentTime;
    public float GetTotalTime() => totalStopwatch.currentTime;
    #endregion
}
