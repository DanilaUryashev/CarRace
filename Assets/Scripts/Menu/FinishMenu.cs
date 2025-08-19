using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishMenu : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color normalColor = Color.gray;
   
    [Header("Родительские объекты")]
    [SerializeField] private Transform parentOptionLaps; // указываем где хранятся все опции
    [SerializeField] private Transform parentOptionMaps;
    [SerializeField] private Transform parentOptionDifficulity;
    [Header("Выбранные опции")]
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

    [Header("Секундомеры")]
    [SerializeField] private Stopwatch totalStopwatch; // Общее время гонки
    [SerializeField] private Stopwatch lapStopwatch;   // Текущий круг
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

    public void SelectLaps(int laps) // выбор количеста кругов
    {
        HighlightSelectedOption(laps, parentOptionLaps);
        lapsToCompleteCurrent = laps;
       

    }
    public void SelectMap(int map) // выбор карты
    {
        HighlightSelectedOption(map, parentOptionMaps);
        mapCurrent = map;

    }
    public void SelectDifficulityGhost(int difficult) // сложность бота/призрака
    {
        HighlightSelectedOption(difficult, parentOptionDifficulity);
        difficultCurrent = difficult;
       

    }
    private void HighlightSelectedOption(int option, Transform setting) // подсветка выбранной опции
    {
        int childIndex = option - 1;

        for (int i = 0; i < setting.childCount; i++)
        {
            Transform child = setting.GetChild(i);
            TextMeshProUGUI text = child.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
            {
                // Выделяем только выбранный элемент
                text.color = (i == childIndex) ? selectedColor : normalColor;
            }
        }

    }
    private void SaveRaceSettings()
    {

        RaceSetting raceSetting = new RaceSetting()
        {
            lapsToWin = lapsToCompleteCurrent // Берём значение из UI
        };

        // Сохраняем через JsonManager (оборачиваем в массив)
        jsonManager.SaveRaceData(null, new RaceSetting[] { raceSetting }, null);

        Debug.Log($"Сохранено кругов: {lapsToCompleteCurrent}");
    }

    public void ApplyAndRestart() // проверка изменения кругов
    {
        // 1. Сохраняем настройки
        SaveRaceSettings();
        string sceneName = Enum.GetName(typeof(Maps), mapCurrent - 1);
        Debug.Log(sceneName);
        // 2. Перезагружаем сцену
        SceneManager.LoadScene(sceneName);

        //// 3. Восстанавливаем паузу (если нужно)
        //Time.timeScale = 1f;
    }

    public void RestartVSGhost()
    {
        if (recorderGhost.HasGhostForCurrentSettings())
        {
            Debug.Log("Призрак есть");
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

            Debug.Log("Призрака нет пройди трасу ебень");
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

    // === Управление общим временем ===
    public void StartRaceTime() => StartStopwatch(ref totalStopwatch);
    public void StopRaceTime() => StopStopwatch(ref totalStopwatch);

    // === Управление временем круга ===
    public void StartLapTime() => StartStopwatch(ref lapStopwatch);

    public void StopLapTime()
    {
        if (lapStopwatch.isRunning)
        {
            lapStopwatch.lapTimes.Add(lapStopwatch.currentTime);
            Debug.Log($"Круг {lapStopwatch.lapTimes.Count}: {FormatTime(lapStopwatch.currentTime)}");
        }
        StopStopwatch(ref lapStopwatch);
    }

    public void ResetLapTime()
    {
        lapStopwatch.currentTime = 0f;
        UpdateDisplay(lapStopwatch);
    }

    //Общие методы 
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

    //Геттеры
    public List<float> GetLapTimes() => lapStopwatch.lapTimes;
    public float GetCurrentLapTime() => lapStopwatch.currentTime;
    public float GetTotalTime() => totalStopwatch.currentTime;
    #endregion
}
