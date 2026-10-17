using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKAimController : MonoBehaviour
{
    [Header("IK References")]
    [SerializeField] private TwoBoneIKConstraint ikConstraint;
    [SerializeField] private Transform ikTarget;

    [Header("Aim Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask aimLayerMask;
    [SerializeField] private float defaultAimDistance = 50f;
    [SerializeField] private float smoothSpeed = 15f;

    [Header("State")]
    public bool isAiming;

    private void LateUpdate()
    {
        UpdateIKTarget();
    }

    private void UpdateIKTarget()
    {
        if (isAiming)
        {
            // 1. Activar el peso del IK de forma suave
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, 1f, Time.deltaTime * smoothSpeed);

            // 2. Calcular la posición a la que mira el centro de la pantalla
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPosition;

            if (Physics.Raycast(ray, out RaycastHit hit, defaultAimDistance, aimLayerMask))
            {
                targetPosition = hit.point;
            }
            else
            {
                targetPosition = ray.GetPoint(defaultAimDistance);
            }

            // 3. Mover el target del IK al punto proyectado
            ikTarget.position = Vector3.Lerp(ikTarget.position, targetPosition, Time.deltaTime * smoothSpeed);
            ikTarget.rotation = Quaternion.Lerp(ikTarget.rotation, Quaternion.LookRotation(ray.direction), Time.deltaTime * smoothSpeed);
        }
        else
        {
            // Desactivar el IK gradualmente cuando no se apunta
            ikConstraint.weight = Mathf.Lerp(ikConstraint.weight, 0f, Time.deltaTime * smoothSpeed);
        }
    }
}