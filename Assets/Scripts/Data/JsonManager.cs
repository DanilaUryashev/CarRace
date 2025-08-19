using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class JsonManager : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void SyncFiles();
#endif

    private static string SAVE_PATH => Path.Combine(Application.persistentDataPath, "race_data.json");

    private void Awake()
    {
        if (!File.Exists(SAVE_PATH))
        {
            CreateDefaultSaveFile();
            SaveToDisk(); // синхронизируем в WebGL
        }
    }

    private void CreateDefaultSaveFile()
    {
        CharacterData defaultData = new CharacterData()
        {
            ghost = new GhostSetting[0],
            raceSetting = new RaceSetting[]
            {
                new RaceSetting()
                {
                    lapsToWin = 3,
                    vsGhost = false
                }
            },
            ghostTrajectories = new GhostTrajectoryData[0]
        };

        string json = JsonUtility.ToJson(defaultData, true);
        File.WriteAllText(SAVE_PATH, json);
    }

    public void SaveRaceData(GhostSetting[] ghostSettings, RaceSetting[] raceSettings, GhostTrajectoryData[] ghostTrajectories)
    {
        CharacterData data = LoadRaceData();

        if (ghostSettings != null) data.ghost = ghostSettings;
        if (raceSettings != null) data.raceSetting = raceSettings;
        if (ghostTrajectories != null) data.ghostTrajectories = ghostTrajectories;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SAVE_PATH, json);

        SaveToDisk();
        Debug.Log("Данные сохранены в " + SAVE_PATH);
    }

    public CharacterData LoadRaceData()
    {
        if (!File.Exists(SAVE_PATH))
        {
            Debug.LogWarning("Файл сохранения не найден, создаём дефолтный");
            CreateDefaultSaveFile();
            SaveToDisk();
        }

        string json = File.ReadAllText(SAVE_PATH);
        CharacterData data = JsonUtility.FromJson<CharacterData>(json);

        if (data.ghost == null) data.ghost = new GhostSetting[0];
        if (data.raceSetting == null) data.raceSetting = new RaceSetting[0];
        if (data.ghostTrajectories == null) data.ghostTrajectories = new GhostTrajectoryData[0];

        return data;
    }

    private void SaveToDisk()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFiles(); // очень важно — сохраняет в IndexedDB
#endif
    }
}



//Структура JSON----------------------------------------

[System.Serializable]
public class CharacterData
{
    public GhostSetting[] ghost;
    public RaceSetting[] raceSetting;
    public GhostTrajectoryData[] ghostTrajectories;
}

[System.Serializable]
public class GhostSetting // настройки призраков
{
    public Maps maps;
    public Difficulty difficulty;
    public int idTrajectory;
    public float lapsToWon;
    public float timeCompliteRace;
}

[System.Serializable]
public class GhostTrajectoryData
{
    public int id;
    public List<GhostRecorder.TransformData> recordedData;
}

[System.Serializable]
public class RaceSetting  // настройки гонки (круги, погода).
{
    public int lapsToWin;
    public bool vsGhost;
}

public enum Difficulty { easy, normal, hard }

public enum Maps
{
    CartoonRaceTrack, // желательно по названию сцены
    Night
}
