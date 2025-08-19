using Ashsvp;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class RaceProgressTracker : MonoBehaviour
{
    public static RaceProgressTracker Instance;
    [SerializeField] private GameObject Car;
    [Header("Race Settings")]
    [SerializeField] FinishMenu finishMenu;
    public int lapsToComplete = 3;
    public List<GameObject> checkpointObjects = new List<GameObject>();

    [Header("Progress")]
    public int currentTargetCheckpoint = 0;
    public int currentLap = 1;
    public bool raceStarted = false;
    public bool allCheckpointsPassed = false; // Новый флаг

    private List<Checkpoint> checkpoints = new List<Checkpoint>();
    [Header("Checkpoints Marker Settings")]
    [SerializeField] private GameObject checkpointEffect;
    [SerializeField] private Material CheckpointMaterial;
    [SerializeField] private float thicknessCheckpointMiniMap = 0;
    private bool vsGhost;

    [SerializeField] GhostRecorder recorderGhost;
    private void Start()
    {
        if(recorderGhost == null) recorderGhost = FindFirstObjectByType<GhostRecorder>();
        InitializeCheckpoints();
        Invoke("InitializeOptions",1f);
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

       
    }

    private void InitializeCheckpoints()
    {
        checkpoints.Clear();
        allCheckpointsPassed = false;

        for (int i = 0; i < checkpointObjects.Count; i++)
        {
            if (checkpointObjects[i] == null) continue;

            var checkpoint = checkpointObjects[i].GetComponent<Checkpoint>() ??
                           checkpointObjects[i].AddComponent<Checkpoint>();
            checkpoint.checkpointEffect = checkpointEffect;
            checkpoint.CheckpointMaterial = CheckpointMaterial;
            checkpoint.thicknessCheckpointMiniMap = thicknessCheckpointMiniMap;
            checkpoint.Initialize(i, i == 0); // Явная инициализация
            
            if (!checkpointObjects[i].TryGetComponent<Collider>(out _))
            {
                var collider = checkpointObjects[i].AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = Vector3.one * 5f;
            }

            checkpoints.Add(checkpoint);
        }
    }
    private void InitializeOptions() // загружаем количество кругов
    {
        CharacterData data = FindAnyObjectByType<JsonManager>().LoadRaceData();

        // Если есть сохранённые настройки - применяем
        if (data != null && data.raceSetting.Length > 0)
        {
            lapsToComplete = data.raceSetting[0].lapsToWin;
            finishMenu.lapsToCompleteCurrent = lapsToComplete;
            vsGhost = data.raceSetting[0].vsGhost;
        }
        else
        {
            lapsToComplete = 1;
            vsGhost = false;
        }
        if (vsGhost)
        {
            recorderGhost.SpawnGhost();
        }

    }
    public void OnCheckpointPassed(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;
        Checkpoint nextCheckpoint = checkpoint;
        if (checkpoint.checkpointID == 0) // Старт/финиш
        {
            if (!raceStarted)
            {
                checkpoints[currentTargetCheckpoint + 1].CheckCheckpoint(true);
                StartRace();
            }
            else if (allCheckpointsPassed) // Проверяем флаг
            {
                CompleteLap();
            }
        }
        else if (checkpoint.checkpointID == currentTargetCheckpoint)
        {
            if((checkpoint.checkpointID+1) < checkpoints.Count)
            {
                checkpoints[currentTargetCheckpoint + 1].CheckCheckpoint(true);
                checkpoints[currentTargetCheckpoint].CheckCheckpoint(false);
            }
            else if (lapsToComplete > currentLap)
            {
                checkpoints[currentTargetCheckpoint].CheckCheckpoint(false);
                checkpoints[0].CheckCheckpoint(true);
            }
            PassCheckpoint();
        }
    }

    private void StartRace()
    {
        raceStarted = true;
        currentTargetCheckpoint = 1;
        allCheckpointsPassed = false;
        Debug.Log("Гонка началась!");
        finishMenu.ResetLapTime(); // Сброс перед стартом
        finishMenu.StartRaceTime();
        finishMenu.StartLapTime();
    }

    private void PassCheckpoint()
    {
        Debug.Log($"Чекпоинт {currentTargetCheckpoint} пройден!");
        currentTargetCheckpoint++;

        // Проверяем, все ли чекпоинты пройдены
        if (currentTargetCheckpoint >= checkpoints.Count)
        {
            allCheckpointsPassed = true;
            Debug.Log("Все чекпоинты пройдены! Направляйтесь к финишу");
        }
    }

    private void CompleteLap()
    {
        finishMenu.StopLapTime(); // Автоматически сохранит время круга

        currentLap++;
        currentTargetCheckpoint = 1;
        allCheckpointsPassed = false;
        if (currentLap > lapsToComplete)
        {
            FinishRace();
        }
        else
        {
            // Сбрасываем таймер круга
            finishMenu.ResetLapTime();
            finishMenu.StartLapTime();
        }
    }

    private void FinishRace()
    {
        Debug.Log($"Гонка завершена! Пройдено кругов: {lapsToComplete}");
       // Car.GetComponent<GhostRecorder>().SpawnGhost();
        finishMenu.StopRaceTime();
        finishMenu.StopLapTime();
        // Дополнительные действия при финише.
        finishMenu.ViewFinishMenu();
        Debug.Log(finishMenu.GetTotalTime());
        recorderGhost.SaveGhostIfBetter(finishMenu.GetTotalTime());
    }
}
public class Checkpoint : MonoBehaviour
{
    [SerializeField, HideInInspector] public int checkpointID;
    [SerializeField, HideInInspector] public bool isStartFinish;
    public GameObject checkpointEffect;
    private GameObject checkpointCubeMiniMap;
    public Material CheckpointMaterial;
    public bool activCheckpoint = false;
    private GameObject effectLeft;
    private GameObject effectRight;
    public float thicknessCheckpointMiniMap = 0;


    public void Initialize(int id, bool isStart)
    {
        checkpointID = id;
        isStartFinish = isStart;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           
            RaceProgressTracker.Instance?.OnCheckpointPassed(this);
        }
        else return;
    }
    public void CheckCheckpoint(bool activ)
    {
        if (activ) CheckPointMarkers();
        else DestroyEffect();
    }
    private void CheckPointMarkers()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
            Debug.Log("НЕтуКоллайдеар");

        // Левый и правый края по локальной оси X
        Vector3 center = boxCollider.center;
        Vector3 size = boxCollider.size;

        Vector3 leftLocal = center + new Vector3(-size.x / 2f, 0, 0);
        Vector3 rightLocal = center + new Vector3(size.x / 2f, 0, 0);

        // Преобразуем в мировые координаты
        Vector3 leftWorld = boxCollider.transform.TransformPoint(leftLocal);
        Vector3 rightWorld = boxCollider.transform.TransformPoint(rightLocal);

        Debug.Log("Left: " + leftWorld + " Right: " + rightWorld);
        Quaternion rotation = Quaternion.Euler(-90f, -90f, -90f);
        effectLeft = Instantiate(checkpointEffect, leftWorld, rotation);
        effectRight = Instantiate(checkpointEffect, rightWorld, rotation);
        MinimapView(boxCollider);
    }
    private void DestroyEffect()
    {
        if (effectLeft != null) Destroy(effectLeft);
        if(effectRight != null) Destroy(effectRight);
        if(checkpointCubeMiniMap!= null) Destroy(checkpointCubeMiniMap);
    }

    private void MinimapView(BoxCollider box)
    {

        
        if (box == null) return;

        // Создаем куб
        checkpointCubeMiniMap = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Делаем его дочерним, если нужно
        checkpointCubeMiniMap.transform.SetParent(transform);

        // Устанавливаем позицию и масштаб, чтобы повторял коллайдер
        checkpointCubeMiniMap.transform.localPosition = box.center;
        checkpointCubeMiniMap.transform.localRotation = Quaternion.identity;
        checkpointCubeMiniMap.transform.localScale = new Vector3(box.size.x, box.size.y, box.size.z +thicknessCheckpointMiniMap);
        // При желании отключаем коллайдер на кубе, чтобы не мешал
        Destroy(checkpointCubeMiniMap.GetComponent<BoxCollider>());
        checkpointCubeMiniMap.layer = 6;
        Renderer cubeRenderer = checkpointCubeMiniMap.GetComponent<Renderer>();
        cubeRenderer.material = CheckpointMaterial;
    }
    // находим края коллайдера чекпоинта


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = isStartFinish ? Color.green : Color.yellow;
        var collider = GetComponent<Collider>();
        if (collider != null)
        {
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
        UnityEditor.Handles.Label(transform.position,
            isStartFinish ? "Старт/Финиш" : $"Чекпоинт {checkpointID}");
    }
#endif
}