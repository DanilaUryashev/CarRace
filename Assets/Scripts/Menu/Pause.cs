using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{

    [SerializeField] private GameObject interfaceUI; // �������� � ���������� � �����������
    [SerializeField] private GameObject finishRaceUI; // ���� ������
    [SerializeField] private GameObject SettingPauseMenu; // ���� �������� �����
    [SerializeField] private GameObject MainPauseElements; // �������� ����� �����
    [SerializeField] private GameObject menuPause; // ���� �����

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

    public void OpenClosePauseMenu()
    {
        if (finishRaceUI.activeSelf) return;// ����� ������� �������� ���� �� �� �� ����� ��������� �����
        else
        {
            if (menuPause.activeSelf)// ��� ��������� ����� ������� ���������
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
        JsonManager jsonManager = FindAnyObjectByType<JsonManager>();

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
        string sceneName = Enum.GetName(typeof(Maps), mapCurrent-1);
        Debug.Log(sceneName);
        // 2. ������������� �����
        SceneManager.LoadScene(sceneName);

        // 3. ��������������� ����� (���� �����)
        Time.timeScale = 1f;
    }
    #endregion

}
