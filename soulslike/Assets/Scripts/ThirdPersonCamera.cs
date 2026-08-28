using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Camera camera;

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followSmoothSpeed;

    [Header("Rotate")]
    [SerializeField] private InputActionReference cameraRotateAction;
    [SerializeField] private Transform cameraPivotTransform;

    /// <summary>
    /// If true, the maximum yaw rotate will increase if the magnitude
    /// of the rotate input increases (0.5 means half of max speed, 1.0 is full max speed). 
    /// NOTE: This only works for gamepad as joystick can have varied input unlike keyboard
    /// which just has either 0 or 1.0 state
    /// </summary>
    [Space(10)]
    [SerializeField] private bool lerpYawRotateMaxSpeedFromInputMagnitude;
    [SerializeField] private float yawRotateAcceleration;
    [SerializeField] private float yawRotateMaxSpeed;
    private float yawRotateSpeed = 0.0f;

    [Space(10)]
    [SerializeField] private float pitchRotateSpeed;
    [SerializeField] private float minPitchDegrees;
    [SerializeField] private float maxPitchDegrees;
    private float yaw;
    private float pitch;
    private Vector3 cameraVelocity;

    [Header("Object Collisions")]
    [SerializeField] private bool handleCameraWorldObjCollisions;
    [SerializeField] private float cameraCollisionRadius;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float collisionAdjustmentTime;
    private float defaultCameraPosZ;
    private float targetCameraPosZ;

    private void Start()
    {
        defaultCameraPosZ = camera.transform.localPosition.z;
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (followTarget != null) HandleFollowTarget(deltaTime);
        HandleRotation(deltaTime);
        if (handleCameraWorldObjCollisions) HandleCollisions();
    }

    private void HandleFollowTarget(float deltaTime)
    {
        transform.position = Vector3.SmoothDamp(transform.position, followTarget.transform.position, 
                                ref cameraVelocity, followSmoothSpeed * deltaTime);
    }
    private void HandleRotation(float deltaTime)
    {
        Vector2 lookInput = cameraRotateAction.action.ReadValue<Vector2>();
        if (Utils.VecApproxEquals(lookInput, Vector2.zero))
        {
            yawRotateSpeed = 0.0f;
            return;
        }

        float maxRotateSpeed = yawRotateMaxSpeed;
        if (lerpYawRotateMaxSpeedFromInputMagnitude) maxRotateSpeed *= lookInput.magnitude;

        yawRotateSpeed += yawRotateAcceleration * deltaTime;
        if (yawRotateSpeed >= maxRotateSpeed) yawRotateSpeed = maxRotateSpeed;

        yaw += lookInput.x * yawRotateSpeed * deltaTime;
        pitch -= lookInput.y * pitchRotateSpeed * deltaTime;
        pitch = Mathf.Clamp(pitch, minPitchDegrees, maxPitchDegrees);

        Quaternion targetRotation;
        targetRotation = Quaternion.Euler(new Vector3(0.0f, yaw, 0.0f));
        transform.rotation = targetRotation;

        targetRotation = Quaternion.Euler(new Vector3(pitch, 0.0f, 0.0f));
        cameraPivotTransform.localRotation = targetRotation;
    }

    private void HandleCollisions()
    {
        targetCameraPosZ = defaultCameraPosZ;
        Vector3 collisionCheckDir = camera.transform.position - cameraPivotTransform.position;
        collisionCheckDir.Normalize();

        RaycastHit hit;
        if (Physics.SphereCast(cameraPivotTransform.position, cameraCollisionRadius, collisionCheckDir, 
            out hit, Mathf.Abs(targetCameraPosZ), collisionLayers))
        {
            float distanceFromHitObj = Vector3.Distance(cameraPivotTransform.position, hit.point);
            targetCameraPosZ = -(distanceFromHitObj - cameraCollisionRadius);
        }

        if (Mathf.Abs(targetCameraPosZ) < cameraCollisionRadius)
        {
            targetCameraPosZ= -cameraCollisionRadius;
        }
        
        Vector3 newCameraLocalPos = camera.transform.localPosition;
        newCameraLocalPos.z = Mathf.Lerp(camera.transform.localPosition.z, targetCameraPosZ, collisionAdjustmentTime);
        camera.transform.localPosition = newCameraLocalPos;
    }
}
