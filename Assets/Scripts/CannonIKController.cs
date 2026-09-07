using UnityEngine;

public class CannonIKController : MonoBehaviour
{
    [Header("Hand Targets")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;

    [Header("Aiming Settings")]
    public Transform cannonTransform;
    public Transform aimTarget; // El punto a donde debe apuntar al disparar/apuntar

    [Range(0f, 1f)]
    public float aimWeight = 0f; // 0 = Idle (Arriba), 1 = Apuntando (Frente)
    public float blendSpeed = 5f;

    [Header("Idle Offset")]
    [Tooltip("Rotación local cuando el cañón está en reposo (ej. X = -45 para apuntar arriba)")]
    public Vector3 idleLocalEuler = new Vector3(-45f, 0f, 0f);

    private float currentWeight;

    void Update()
    {
        // Transición suave entre Idle y Aim
        currentWeight = Mathf.Lerp(currentWeight, aimWeight, Time.deltaTime * blendSpeed);

        if (cannonTransform == null) return;

        // 1. Rotación en Idle (Orientación local respecto al cuerpo)
        Quaternion idleRotation = transform.rotation * Quaternion.Euler(idleLocalEuler);

        // 2. Rotación al Apuntar (Mirando hacia el aimTarget)
        Quaternion aimRotation = idleRotation;
        if (aimTarget != null)
        {
            Vector3 direction = (aimTarget.position - cannonTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                aimRotation = Quaternion.LookRotation(direction);
            }
        }

        // 3. Interpolación final según el peso del Aim
        cannonTransform.rotation = Quaternion.Slerp(idleRotation, aimRotation, currentWeight);
    }
}