using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{

    [SerializeField] private GameObject interfaceUI; // интерфес с миникартой и спидометром
    [SerializeField] private GameObject finishRaceUI; // меню финиша
    [SerializeField] private GameObject SettingPauseMenu; // меню настроек гонки
    [SerializeField] private GameObject MainPauseElements; // основные пунты паузы
    [SerializeField] private GameObject menuPause; // меню паузы

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

    public void OpenClosePauseMenu()
    {
        if (finishRaceUI.activeSelf) return;// еслим открыто финишное меню то мы не можем поставить паузу
        else
        {
            if (menuPause.activeSelf)// при отккрытии паузы убираем интерфейс
            {
                menuPause.SetActive(false);
                interfaceUI.SetActive(true);
                Time.timeScale = 1f;
            }
            else
            {
                interfaceUI.SetActive(false);
                menuPause.SetActive(true);
                Time.timeScale = 0f;
            }
        }
       
    }

    public void OpenCloseSettingRaceMenu()
    {
        if (SettingPauseMenu.activeSelf)
        {
            SettingPauseMenu.SetActive(false);
            MainPauseElements.SetActive(true);
        }
        else
        {
            SettingPauseMenu.SetActive(true);
            MainPauseElements.SetActive(false);
        }
    }
    public void ExitMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
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
        JsonManager jsonManager = FindAnyObjectByType<JsonManager>();

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
        string sceneName = Enum.GetName(typeof(Maps), mapCurrent-1);
        Debug.Log(sceneName);
        // 2. Перезагружаем сцену
        SceneManager.LoadScene(sceneName);

        // 3. Восстанавливаем паузу (если нужно)
        Time.timeScale = 1f;
    }
    #endregion

}
