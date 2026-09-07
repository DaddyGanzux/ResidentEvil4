using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

public class ThirdPersonShooterConreoller : MonoBehaviour
{
    [Header("Cámara y Apuntado")]
    [SerializeField] private CinemachineCamera aimVirtualCamera;
    [SerializeField] private GameObject puntoMira;
    [SerializeField] private float normalSensitivity = 1f;
    [SerializeField] private float aimSensitivity = 0.5f;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private float aimRotationSmoothTime = 0.05f;

    [Header("Disparo")]
    [SerializeField] private Transform spawnBulletPosition;
    [SerializeField] private GameObject pfBulletProjectile;

    [Header("IK Targets")]
    [SerializeField] private Transform targetL; // Asigna TargetL en el Inspector
    [SerializeField] private Transform targetR; // Asigna TargetR en el Inspector
    [SerializeField] private float ikTransitionSpeed = 10f;

    // Guardado de estado inicial (Idle)
    private Vector3 targetLInitialPos;
    private Quaternion targetLInitialRot;
    private Vector3 targetRInitialPos;
    private Quaternion targetRInitialRot;

    // Valores exactos para Apuntado (Aim) desde tus imágenes
    private readonly Vector3 targetLAimPos = new Vector3(-1.45f, 3.15f, 0.96f);
    private readonly Quaternion targetLAimRot = Quaternion.Euler(-6.1f, -92.7f, 25.8f);

    private readonly Vector3 targetRAimPos = new Vector3(-0.82f, 5.63f, -0.74f);
    private readonly Quaternion targetRAimRot = Quaternion.Euler(2.192f, 99.965f, -24.601f);

    private ThirdPersonController thirdPersonController;
    private StarterAssetsInputs starterAssetsInputs;
    private Transform mainCameraTransform;
    private float rotationVelocity;

    private void Awake()
    {
        thirdPersonController = GetComponent<ThirdPersonController>();
        starterAssetsInputs = GetComponent<StarterAssetsInputs>();
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        // Guardar las transformaciones locales iniciales del Idle
        if (targetL != null)
        {
            targetLInitialPos = targetL.localPosition;
            targetLInitialRot = targetL.localRotation;
        }

        if (targetR != null)
        {
            targetRInitialPos = targetR.localPosition;
            targetRInitialRot = targetR.localRotation;
        }
    }

    private void Update()
    {
        // 1. Raycast desde la cámara
        Vector3 mouseWorldPosition = Vector3.zero;
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

        if (Physics.Raycast(ray, out RaycastHit rayCastHit, 999f, aimColliderLayerMask))
        {
            if (debugTransform != null) debugTransform.position = rayCastHit.point;
            mouseWorldPosition = rayCastHit.point;
        }
        else
        {
            mouseWorldPosition = ray.GetPoint(999f);
        }

        // 2. Lógica de Apuntado
        if (starterAssetsInputs.aim)
        {
            // 1. Bloquea la rotación por movimiento de StarterAssets
            thirdPersonController.RotateOnMove = false;

            // 2. Fuerza al personaje a alinearse siempre al frente de la cámara
            float targetRotation = mainCameraTransform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0.0f, targetRotation, 0.0f);

            aimVirtualCamera.gameObject.SetActive(true);
            puntoMira.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);

            MoveTargetLocal(targetL, targetLAimPos, targetLAimRot);
            MoveTargetLocal(targetR, targetRAimPos, targetRAimRot);
        }
        else
        {
            // Restablece el movimiento estándar de StarterAssets
            thirdPersonController.RotateOnMove = true;

            aimVirtualCamera.gameObject.SetActive(false);
            puntoMira.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);

            MoveTargetLocal(targetL, targetLInitialPos, targetLInitialRot);
            MoveTargetLocal(targetR, targetRInitialPos, targetRInitialRot);
        }

        // 3. Disparo
        if (starterAssetsInputs.shoot)
        {
            Vector3 aimDir = (mouseWorldPosition - spawnBulletPosition.position).normalized;
            Instantiate(pfBulletProjectile, spawnBulletPosition.position, Quaternion.LookRotation(aimDir, Vector3.up));
            starterAssetsInputs.shoot = false;
        }
    }

    private void MoveTargetLocal(Transform target, Vector3 targetPos, Quaternion targetRot)
    {
        if (target == null) return;

        target.localPosition = Vector3.Lerp(target.localPosition, targetPos, Time.deltaTime * ikTransitionSpeed);
        target.localRotation = Quaternion.Lerp(target.localRotation, targetRot, Time.deltaTime * ikTransitionSpeed);
    }
}