using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionAsset inputActions;
    [Header("References")]
    public Ashsvp.ResetVehicle resetVehicle; // ссылка на скрипт ResetVehicle в сцене
    public Pause pauseMenu;


    private InputAction moveAction;
    private InputAction brakeAction;
    private InputAction nitrousAction;

    private InputAction resetAction;
    private InputAction pauseAction;
    private InputAction restartAction;

    // Публичные свойства для доступа к вводу
    public float SteerInput { get; private set; }
    public float AccelerationInput { get; private set; }
    public float BrakeInput { get; private set; }
    public bool IsNitrousActive { get; private set; }

    private void Awake()
    {
        Debug.Log("GameInput Awake");

        // Основные действия
        moveAction = inputActions.FindAction("Move");
        brakeAction = inputActions.FindAction("Brake");
        nitrousAction = inputActions.FindAction("Sprint");

        // Дополнительные действия
        resetAction = inputActions.FindAction("Reset");
        pauseAction = inputActions.FindAction("Pause");
        restartAction = inputActions.FindAction("Restart");

        if (resetAction == null || pauseAction == null || restartAction == null)
        {
            Debug.LogWarning("Не все действия (Reset/Quit/Restart) найдены в InputActionAsset!");
        }
    }


    private void OnEnable()
    {
        moveAction?.Enable();
        brakeAction?.Enable();
        nitrousAction?.Enable();

        if (resetAction != null)
        {
            resetAction.Enable();
            resetAction.performed += OnResetPerformed;
        }
        if (restartAction != null)
        {
            restartAction.Enable();
            restartAction.performed += OnRestartPerformed;
        }
        if (pauseAction != null)
        {
            pauseAction.Enable();
            pauseAction.performed += OnPausePerformed; // 👈 подписка
        }
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        brakeAction?.Disable();
        nitrousAction?.Disable();

        if (resetAction != null)
            resetAction.performed -= OnResetPerformed;
        if (restartAction != null)
            restartAction.performed -= OnRestartPerformed;

        resetAction?.Disable();
        pauseAction?.Disable();
        restartAction?.Disable();
    }

    private void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        SteerInput = moveInput.x;
        AccelerationInput = moveInput.y;
        BrakeInput = brakeAction.ReadValue<float>();

        if (!moveAction.enabled) EnableInputActions();

        IsNitrousActive = nitrousAction.ReadValue<float>() > 0.1f;
    }

    public void EnableInputActions()
    {
        if (moveAction != null && !moveAction.enabled) moveAction.Enable();
        if (brakeAction != null && !brakeAction.enabled) brakeAction.Enable();
        if (nitrousAction != null && !nitrousAction.enabled) nitrousAction.Enable();
    }

    // --- Дополнительные действия ---
    private void OnResetPerformed(InputAction.CallbackContext context)
    {
        resetVehicle?.ResetVehiclePosition();
    }

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        resetVehicle?.ResetScene();
    }
    private void OnPausePerformed(InputAction.CallbackContext context) =>
       pauseMenu?.OpenClosePauseMenu();
}
