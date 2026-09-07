using UnityEngine;
using Unity.Cinemachine;
using StarterAssets;
using UnityEngine.InputSystem;

public class ThirdPersonShooterConreoller : MonoBehaviour
{
    [SerializeField] private CinemachineCamera aimVirtualCamera;
    [SerializeField] private GameObject puntoMira;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform debugTransform;
    [SerializeField] private float aimRotationSmoothTime = 0.05f;
    [SerializeField] private Transform pfBulletProjectile;
    [SerializeField] private Transform spawnBulletPosition;


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

    private void Update()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        if (Physics.Raycast(ray, out RaycastHit rayCastHit, 999f, aimColliderLayerMask))
        {
            debugTransform.position = rayCastHit.point;
        }

        if (starterAssetsInputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            puntoMira.SetActive(true);
            thirdPersonController.SetSensitivity(aimSensitivity);

            // Rotar el personaje hacia la orientación de la cámara mientras apunta
            float targetRotation = mainCameraTransform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, aimRotationSmoothTime);
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            puntoMira.SetActive(false);
            thirdPersonController.SetSensitivity(normalSensitivity);
        }

        if (starterAssetsInputs.shoot)
        {
            Vector3 mouseWorldPosition = Vector3.zero;

            // Proyecta un rayo desde el centro de la pantalla
            Ray rays = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (Physics.Raycast(rays, out RaycastHit raycastHit, 999f))
            {
                mouseWorldPosition = raycastHit.point; // Si toca una pared u objeto
            }
            else
            {
                mouseWorldPosition = rays.GetPoint(999f); // Si apunta al aire o al cielo
            }

            // Calculas la dirección hacia donde se obtuvo el punto de impacto
            Vector3 aimDir = (mouseWorldPosition - spawnBulletPosition.position).normalized;

            Instantiate(pfBulletProjectile, spawnBulletPosition.position, Quaternion.LookRotation(aimDir, Vector3.up));

            starterAssetsInputs.shoot = false;
        }
    }
}