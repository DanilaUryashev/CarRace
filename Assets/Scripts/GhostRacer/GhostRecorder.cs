using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class GhostRecorder : MonoBehaviour
{
    [Header("���������")]
    public float recordInterval = 0.1f;
    public GameObject ghostPrefab;
    public float ghostSpeedMultiplier = 1f; // ��������� �������� ����������
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

        // ������� �������� ����������
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
        // ��������� � ����������� ��������
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

        // ���������� ����� � ����������
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
            Debug.LogError("��� ������ ��� ��������!");
            return;
        }

        // ������� ��������� �������� ��� ������� �����/�����
        GhostSetting ghostSetting = Array.Find(data.ghost, g =>
            g.maps == GetCurrentMap() &&
            (int)g.lapsToWon == raceProgressTracker.lapsToComplete
        );

        if (ghostSetting == null)
        {
            Debug.LogError("���������� ������� �� ������!");
            return;
        }

        // ������� ��� ����������
        GhostTrajectoryData trajectory = Array.Find(data.ghostTrajectories, t => t.id == ghostSetting.idTrajectory);
        if (trajectory == null || trajectory.recordedData == null || trajectory.recordedData.Count < 2)
        {
            Debug.LogError("��� �������� ��� ����������!");
            return;
        }

        // ���������� ������-�������
        ghostPrefab.SetActive(true);
        ghostPrefab.transform.position = trajectory.recordedData[0].position;
        ghostPrefab.transform.rotation = trajectory.recordedData[0].rotation;

        // ������ �������� � ���������
        GhostAnimator ghostAnim = ghostPrefab.AddComponent<GhostAnimator>();
        float speedMult = ghostSetting.difficulty == Difficulty.easy ? 0.8f : 1f;
        ghostAnim.Initialize(trajectory.recordedData, speedMult);

        Debug.Log("������� ���������.");
    }


    public bool HasGhostForCurrentSettings() //�������� �� ������� �������� ��� ������ �������� � �����
    {
        // ��������� ����������� ������
        CharacterData data = FindFirstObjectByType<JsonManager>().LoadRaceData();

        if (data == null || data.ghost == null || data.ghost.Length == 0)
            return false; // ��� ����������� ���������

        // ������� ��������� �����
        int currentMap = (int)GetCurrentMap(); // ������� ��������� ����� (int)
        int currentLaps = raceProgressTracker.lapsToComplete; // ������� ���-�� ������ 

        // ���� �������� � ����������� �����������
        foreach (var ghost in data.ghost)
        {
            if ((int)ghost.maps == currentMap && ghost.lapsToWon == currentLaps)
            {
                return true; // ������ ���������� �������
            }
        }

        return false; // �� ������
    }

    public Maps GetCurrentMap()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // �������� ������������� ��� ����� � enum Maps
        if (Enum.TryParse(currentSceneName, out Maps map))
        {
            Debug.Log(map);
            return map;
        }

        // ���� ��� ����� �� ��������� � enum, ���������� �������� �� ���������
        Debug.LogError($"����� '{currentSceneName}' �� ������� � enum Maps!");
        return Maps.CartoonRaceTrack; // ��� ������ �������� �� ���������
    }

    public void SaveGhostIfBetter(float newTime)
    {
        if (newTime <= 0f)
        {
            Debug.LogError("������������ ����� �����!");
            return;
        }

        // ������� ������� �����
        Maps currentMap = GetCurrentMap();
        int lapsToCompleteCurrent = raceProgressTracker.lapsToComplete;

        GhostRecorder recorder = GetComponent<GhostRecorder>();
        if (recorder == null)
        {
            Debug.LogError("�� ������ ��������� GhostRecorder!");
            return;
        }

        // ��������� ����������
        JsonManager jm = FindFirstObjectByType<JsonManager>();
        CharacterData data = jm.LoadRaceData() ?? new CharacterData();
        if (data.ghost == null) data.ghost = new GhostSetting[0];
        if (data.ghostTrajectories == null) data.ghostTrajectories = new GhostTrajectoryData[0];

        // ����� ����������� ��������
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

        // ���������� ID ����������
        int nextTrajectoryId = GenerateNextTrajectoryId(data.ghostTrajectories);

        if (foundIndex >= 0)
        {
            // ������� ��� ����
            GhostSetting existing = data.ghost[foundIndex];

            if (newTime < existing.timeCompliteRace)
            {
                // ��������� �����
                existing.timeCompliteRace = newTime;

                // ����� �������� ������ idTrajectory (����� � ���������� �����������)
                int trajectoryId = existing.idTrajectory;

                // ������ ����� ����������
                GhostTrajectoryData newTrajectory = new GhostTrajectoryData()
                {
                    id = trajectoryId,
                    recordedData = recorder.GetRecordedData()
                };

                // �������� ������ ����������
                UpdateTrajectory(data, newTrajectory);

                // ��������� ������ ��������
                data.ghost[foundIndex] = existing;

                Debug.Log($"������� �������! ����� ������ �����: {newTime}");
            }
            else
            {
                Debug.Log($"����� ���� ({newTime}), ���������� ���������.");
            }
        }
        else
        {
            // ������ ������ ��������
            int newTrajectoryId = nextTrajectoryId;

            GhostSetting newGhost = new GhostSetting()
            {
                maps = currentMap,
                difficulty = (Difficulty)2, // ���� ������
                idTrajectory = newTrajectoryId,
                lapsToWon = lapsToCompleteCurrent,
                timeCompliteRace = newTime
            };

            GhostTrajectoryData newTrajectory = new GhostTrajectoryData()
            {
                id = newTrajectoryId,
                recordedData = recorder.GetRecordedData()
            };

            // ��������� � ������
            var ghostList = new List<GhostSetting>(data.ghost);
            ghostList.Add(newGhost);
            data.ghost = ghostList.ToArray();

            var trajList = new List<GhostTrajectoryData>(data.ghostTrajectories);
            trajList.Add(newTrajectory);
            data.ghostTrajectories = trajList.ToArray();

            Debug.Log($"����� ������� �������! �����: {newTime}");
        }

        // ��������� �������
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
                // �������� ������
                data.ghostTrajectories[i] = newTrajectory;
                return;
            }
        }

        // ���� �� �����, ���������
        var list = new List<GhostTrajectoryData>(data.ghostTrajectories);
        list.Add(newTrajectory);
        data.ghostTrajectories = list.ToArray();
    }
    public List<TransformData> GetRecordedData()
    {
        return new List<TransformData>(recordedData); // ����� ������, ����� ��� ������ ���� ������� �������
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

        // ������� ��������� �������� �����
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

        // ������������� ����� �������
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

        // ������������ ��������
        if (progress >= 1f)
        {
            startTime = Time.time;
        }
    }


    
}