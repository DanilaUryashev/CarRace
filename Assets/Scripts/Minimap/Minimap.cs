using UnityEngine;
using UnityEngine.UI;

public class AdvancedMinimapController : MonoBehaviour
{
    public enum MinimapMode { FollowPlayer, StaticFullMap }

    [Header("Режим работы")]
    public MinimapMode mode = MinimapMode.FollowPlayer;
    public Transform player;

    [Header("Параметры камеры")]
    public float followHeight = 50f;
    public Vector3 followOffset = new Vector3(0, 50, 0);

    [Header("Для статичного режима")]
    public Transform worldCorner1;
    public Transform worldCorner2;

    [SerializeField] private Camera minimapCamera;
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform playerIcon;

    private Vector3 staticCenter;
    private float staticSize;

    void Start()
    {
        if (minimapImage == null)
            minimapImage = GetComponent<RawImage>();

        // Создаем RenderTexture
        minimapCamera.targetTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32)
        {
            name = "MinimapRenderTexture",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear
        };
        minimapCamera.targetTexture.Create();
        minimapImage.texture = minimapCamera.targetTexture;

        if (mode == MinimapMode.StaticFullMap)
            SetupStaticMap();
    }

    void LateUpdate()
    {
        UpdateCameraPosition();
        UpdatePlayerIconPosition();
    }

    void UpdateCameraPosition()
    {
        switch (mode)
        {
            case MinimapMode.FollowPlayer:
                FollowPlayer();
                break;
            case MinimapMode.StaticFullMap:
                StaticMap();
                break;
        }
    }

    void UpdatePlayerIconPosition()
    {
        if (player == null || playerIcon == null) return;

        Vector3 viewportPos = minimapCamera.WorldToViewportPoint(player.position);
        playerIcon.anchorMin = playerIcon.anchorMax = viewportPos;
        playerIcon.anchoredPosition = Vector2.zero;

        if (mode == MinimapMode.FollowPlayer)
            playerIcon.localEulerAngles = Vector3.zero;
        else
            playerIcon.localEulerAngles = new Vector3(0, 0, -player.eulerAngles.y);
    }

    void FollowPlayer()
    {
        if (player == null) return;
        Vector3 newPos = player.position + followOffset;
        minimapCamera.transform.position = newPos;
        minimapCamera.transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f);
    }

    void SetupStaticMap()
    {
        if (worldCorner1 == null || worldCorner2 == null) return;
        staticCenter = (worldCorner1.position + worldCorner2.position) / 2f;
        Vector3 size = new Vector3(
            Mathf.Abs(worldCorner1.position.x - worldCorner2.position.x),
            0,
            Mathf.Abs(worldCorner1.position.z - worldCorner2.position.z)
        );
        staticSize = Mathf.Max(size.x / minimapCamera.aspect, size.z) / 2f;
        minimapCamera.orthographicSize = staticSize;
    }

    void StaticMap()
    {
        minimapCamera.transform.position = new Vector3(staticCenter.x, followHeight, staticCenter.z);
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void SwitchMode(MinimapMode newMode)
    {
        mode = newMode;
        if (mode == MinimapMode.StaticFullMap)
            SetupStaticMap();
    }
}
