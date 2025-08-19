using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class GhostRecorder : MonoBehaviour
{
    [Header("Настройки")]
    public float recordInterval = 0.1f;
    public GameObject ghostPrefab;
    public float ghostSpeedMultiplier = 1f; // Множитель скорости привидения
    [SerializeField] FinishMenu finishMenu;
    [System.Serializable] 
    public class TransformData 
    {
        public Vector3 position;
        public Quaternion rotation;
        public float timestamp;

        public TransformData(Transform t, float time)
        {
            position = t.position;
            rotation = t.rotation;
            timestamp = time;
        }
    }

    private List<TransformData> recordedData = new List<TransformData>();
    private float distanceThreshold = 1f;
    private Vector3 lastRecordPosition;
    private bool isRecording;
    private float recordingStartTime;
    private bool ghostSpawned;

    RaceProgressTracker raceProgressTracker;

    private void Start()
    {
        raceProgressTracker = FindFirstObjectByType<RaceProgressTracker>();
        InitializePrefCar();
        StartRecording();
        //Invoke("SpawnGhost", 10f);
    }

    private void InitializePrefCar()
    {
        if (ghostPrefab != null) return;

        ghostPrefab = Instantiate(gameObject);
        ghostPrefab.SetActive(false);
        ghostPrefab.name = "GhostCar";

        // Удаляем ненужные компоненты
        var components = ghostPrefab.GetComponents<Component>();
        foreach (var component in components)
        {
            if (!(component is Transform))
            {
                DestroyImmediate(component);
            }
        }
        Transform bodyCollider = ghostPrefab.transform.Find("Body Collider");
        if (bodyCollider != null)
        {
            DestroyImmediate(bodyCollider.gameObject);
        }
        // Добавляем и настраиваем рендерер
        var renderer = ghostPrefab.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"))
            {
                color = new Color(1, 1, 1, 0.5f)
            };
        }
    }

    public void StartRecording()
    {
        recordedData.Clear();
        lastRecordPosition = transform.position;
        recordingStartTime = Time.time;
        recordedData.Add(new TransformData(transform, 0f));
        isRecording = true;
        ghostSpawned = false;
    }

    private void Update()
    {
        if (!isRecording) return;

        // Записываем кадры с интервалом
        if (Time.time - recordingStartTime >= recordInterval * recordedData.Count)
        {
            RecordFrame();
        }

    }

    private void RecordFrame()
    {
        recordedData.Add(new TransformData(transform, Time.time - recordingStartTime));
        lastRecordPosition = transform.position;
    }

    public void SpawnGhost()
    {
        CharacterData data = FindFirstObjectByType<JsonManager>().LoadRaceData();
        if (data == null || data.ghost == null || data.ghost.Length == 0)
        {
            Debug.LogError("Нет данных для призрака!");
            return;
        }

        // находим настройки призрака под текущую карту/круги
        GhostSetting ghostSetting = Array.Find(data.ghost, g =>
            g.maps == GetCurrentMap() &&
            (int)g.lapsToWon == raceProgressTracker.lapsToComplete
        );

        if (ghostSetting == null)
        {
            Debug.LogError("Подходящий призрак не найден!");
            return;
        }

        // находим его траекторию
        GhostTrajectoryData trajectory = Array.Find(data.ghostTrajectories, t => t.id == ghostSetting.idTrajectory);
        if (trajectory == null || trajectory.recordedData == null || trajectory.recordedData.Count < 2)
        {
            Debug.LogError("Для призрака нет траектории!");
            return;
        }

        // активируем объект-призрак
        ghostPrefab.SetActive(true);
        ghostPrefab.transform.position = trajectory.recordedData[0].position;
        ghostPrefab.transform.rotation = trajectory.recordedData[0].rotation;

        // вешаем аниматор и запускаем
        GhostAnimator ghostAnim = ghostPrefab.AddComponent<GhostAnimator>();
        float speedMult = ghostSetting.difficulty == Difficulty.easy ? 0.8f : 1f;
        ghostAnim.Initialize(trajectory.recordedData, speedMult);

        Debug.Log("Призрак заспавнен.");
    }


    public bool HasGhostForCurrentSettings() //ПРОВЕРКА НА НАЛИЧИЕ ПРИЗРАКА ДЛЯ ДАННЫХ НАСТРОЕК И КАРТЫ
    {
        // Загружаем сохраненные данные
        CharacterData data = FindFirstObjectByType<JsonManager>().LoadRaceData();

        if (data == null || data.ghost == null || data.ghost.Length == 0)
            return false; // Нет сохраненных призраков

        // Текущие настройки гонки
        int currentMap = (int)GetCurrentMap(); // Текущая выбранная карта (int)
        int currentLaps = raceProgressTracker.lapsToComplete; // Текущее кол-во кругов 

        // Ищем призрака с подходящими параметрами
        foreach (var ghost in data.ghost)
        {
            if ((int)ghost.maps == currentMap && ghost.lapsToWon == currentLaps)
            {
                return true; // Найден подходящий призрак
            }
        }

        return false; // Не найден
    }

    public Maps GetCurrentMap()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Пытаемся преобразовать имя сцены в enum Maps
        if (Enum.TryParse(currentSceneName, out Maps map))
        {
            Debug.Log(map);
            return map;
        }

        // Если имя сцены не совпадает с enum, возвращаем значение по умолчанию
        Debug.LogError($"Сцена '{currentSceneName}' не найдена в enum Maps!");
        return Maps.CartoonRaceTrack; // Или другое значение по умолчанию
    }

    public void SaveGhostIfBetter(float newTime)
    {
        if (newTime <= 0f)
        {
            Debug.LogError("Некорректное время гонки!");
            return;
        }

        // Текущие условия гонки
        Maps currentMap = GetCurrentMap();
        int lapsToCompleteCurrent = raceProgressTracker.lapsToComplete;

        GhostRecorder recorder = GetComponent<GhostRecorder>();
        if (recorder == null)
        {
            Debug.LogError("Не найден компонент GhostRecorder!");
            return;
        }

        // Загружаем сохранение
        JsonManager jm = FindFirstObjectByType<JsonManager>();
        CharacterData data = jm.LoadRaceData() ?? new CharacterData();
        if (data.ghost == null) data.ghost = new GhostSetting[0];
        if (data.ghostTrajectories == null) data.ghostTrajectories = new GhostTrajectoryData[0];

        // Поиск подходящего призрака
        int foundIndex = -1;
        for (int i = 0; i < data.ghost.Length; i++)
        {
            GhostSetting g = data.ghost[i];
            if (g != null &&
                g.maps == currentMap &&
                (int)g.lapsToWon == lapsToCompleteCurrent)
            {
                foundIndex = i;
                break;
            }
        }

        // Генерируем ID траектории
        int nextTrajectoryId = GenerateNextTrajectoryId(data.ghostTrajectories);

        if (foundIndex >= 0)
        {
            // Призрак уже есть
            GhostSetting existing = data.ghost[foundIndex];

            if (newTime < existing.timeCompliteRace)
            {
                // Обновляем время
                existing.timeCompliteRace = newTime;

                // Можно оставить старый idTrajectory (связь с обновлённой траекторией)
                int trajectoryId = existing.idTrajectory;

                // Создаём новую траекторию
                GhostTrajectoryData newTrajectory = new GhostTrajectoryData()
                {
                    id = trajectoryId,
                    recordedData = recorder.GetRecordedData()
                };

                // Заменяем старую траекторию
                UpdateTrajectory(data, newTrajectory);

                // Обновляем запись призрака
                data.ghost[foundIndex] = existing;

                Debug.Log($"Призрак обновлён! Новое лучшее время: {newTime}");
            }
            else
            {
                Debug.Log($"Время хуже ({newTime}), сохранение пропущено.");
            }
        }
        else
        {
            // Создаём нового призрака
            int newTrajectoryId = nextTrajectoryId;

            GhostSetting newGhost = new GhostSetting()
            {
                maps = currentMap,
                difficulty = (Difficulty)2, // твой дефолт
                idTrajectory = newTrajectoryId,
                lapsToWon = lapsToCompleteCurrent,
                timeCompliteRace = newTime
            };

            GhostTrajectoryData newTrajectory = new GhostTrajectoryData()
            {
                id = newTrajectoryId,
                recordedData = recorder.GetRecordedData()
            };

            // Добавляем в списки
            var ghostList = new List<GhostSetting>(data.ghost);
            ghostList.Add(newGhost);
            data.ghost = ghostList.ToArray();

            var trajList = new List<GhostTrajectoryData>(data.ghostTrajectories);
            trajList.Add(newTrajectory);
            data.ghostTrajectories = trajList.ToArray();

            Debug.Log($"Новый призрак сохранён! Время: {newTime}");
        }

        // Сохраняем обратно
        jm.SaveRaceData(data.ghost, data.raceSetting, data.ghostTrajectories);
    }

    private int GenerateNextTrajectoryId(GhostTrajectoryData[] trajectories)
    {
        int maxId = -1;
        if (trajectories != null)
        {
            foreach (var t in trajectories)
            {
                if (t != null && t.id > maxId)
                    maxId = t.id;
            }
        }
        return maxId + 1;
    }

    private void UpdateTrajectory(CharacterData data, GhostTrajectoryData newTrajectory)
    {
        for (int i = 0; i < data.ghostTrajectories.Length; i++)
        {
            if (data.ghostTrajectories[i].id == newTrajectory.id)
            {
                // заменяем старую
                data.ghostTrajectories[i] = newTrajectory;
                return;
            }
        }

        // если не нашли, добавляем
        var list = new List<GhostTrajectoryData>(data.ghostTrajectories);
        list.Add(newTrajectory);
        data.ghostTrajectories = list.ToArray();
    }
    public List<TransformData> GetRecordedData()
    {
        return new List<TransformData>(recordedData); // копия списка, чтобы его нельзя было сломать снаружи
    }
}

public class GhostAnimator : MonoBehaviour
{
    private List<GhostRecorder.TransformData> animationData;
    private int currentIndex = 0;
    private float startTime;
    private float totalDuration;
    private float speedMultiplier = 1f;

    public void Initialize(List<GhostRecorder.TransformData> data, float speedMult)
    {
        animationData = data;
        startTime = Time.time;
        totalDuration = data[data.Count - 1].timestamp;
        speedMultiplier = speedMult;
    }

    private void Update()
    {
        if (animationData == null || animationData.Count == 0) return;

        float currentTime = (Time.time - startTime) * speedMultiplier;
        float progress = Mathf.Clamp01(currentTime / totalDuration);

        // Находим ближайшие ключевые кадры
        int prevIndex = 0;
        int nextIndex = 0;
        for (int i = 0; i < animationData.Count; i++)
        {
            if (animationData[i].timestamp / totalDuration <= progress)
            {
                prevIndex = i;
                nextIndex = Mathf.Min(i + 1, animationData.Count - 1);
            }
            else break;
        }

        // Интерполируем между кадрами
        float frameProgress = 0f;
        if (prevIndex != nextIndex)
        {
            float segmentStart = animationData[prevIndex].timestamp / totalDuration;
            float segmentEnd = animationData[nextIndex].timestamp / totalDuration;
            frameProgress = (progress - segmentStart) / (segmentEnd - segmentStart);
        }

        transform.position = Vector3.Lerp(
            animationData[prevIndex].position,
            animationData[nextIndex].position,
            frameProgress);

        transform.rotation = Quaternion.Lerp(
            animationData[prevIndex].rotation,
            animationData[nextIndex].rotation,
            frameProgress);

        // Зацикливание анимации
        if (progress >= 1f)
        {
            startTime = Time.time;
        }
    }


    
}