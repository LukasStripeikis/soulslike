using UnityEngine;
using UnityEngine.InputSystem;

public enum CameraRotateMode : uint
{
    /// <summary>
    /// Will use a lookat target position to determine the camera's rotation such that
    /// the camera has the lookat target position in view
    /// </summary>
    LookatTarget    = 0,
    /// <summary>
    /// Will allow the rotation to be determined by an input value from the player
    /// </summary>
    FreeOrbit       = 1
}
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Camera camera;

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followSmoothSpeed;

    [Header("Camera Rotation Mode")]
    [SerializeField] private CameraRotateMode rotateMode;

    [Header("Lookat Target")]
    [SerializeField] private Transform lookatTarget;
    /// <summary>
    /// A value in range (0.0, 1.0] that determines the point the camera should
    /// look for a lookat target where 0.5 would be the midpoint between camera and look at target
    /// positions and 1.0 would be directly at the lookat target
    /// </summary>
    [SerializeField] private float lookatBias;
    [SerializeField] private float lookatSmoothSpeed;

    [Header("Orbiting")]
    [SerializeField] private InputActionReference cameraOrbitAction;
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

        if (rotateMode == CameraRotateMode.FreeOrbit) HandleOrbit(deltaTime);
        else if (rotateMode == CameraRotateMode.LookatTarget) HandleLookatTarget(deltaTime);

        if (handleCameraWorldObjCollisions) HandleCollisions();
    }

    private void HandleFollowTarget(float deltaTime)
    {
        transform.position = Vector3.SmoothDamp(transform.position, followTarget.transform.position, 
                                ref cameraVelocity, followSmoothSpeed * deltaTime);
    }
    private void HandleOrbit(float deltaTime)
    {
        Vector2 lookInput = cameraOrbitAction.action.ReadValue<Vector2>();
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

       SetCameraRotation(yaw, pitch);
    }
    private void HandleLookatTarget(float deltaTime)
    {
        Vector3 targetLookatPos = Vector3.Lerp(transform.position, lookatTarget.position, lookatBias);
        Vector3 lookatDir = targetLookatPos - transform.position;
        lookatDir.Normalize();

        //NOTE: since quaternion Quaternion.LookRotation() -> eulerAngles for yaw, pitch 
        //may be unexpected especially at 90 degrees with multiple valid degree representations
        //we prevent this by directly getting the angle from direction
        float targetYaw = Mathf.Atan2(lookatDir.x, lookatDir.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Asin(lookatDir.y) * Mathf.Rad2Deg;

        yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, lookatSmoothSpeed * deltaTime);
        pitch = Mathf.MoveTowardsAngle(pitch, targetPitch, lookatSmoothSpeed * deltaTime);
        SetCameraRotation(yaw, pitch);
    }
    private void SetCameraRotation(float yaw, float pitch)
    {
        Quaternion targetRotation;
        targetRotation = Quaternion.Euler(new Vector3(0.0f, yaw, 0.0f));
        transform.rotation = targetRotation;

        targetRotation = Quaternion.Euler(new Vector3(pitch, 0.0f, 0.0f));
        cameraPivotTransform.localRotation = targetRotation;
    }
    private void ResetCameraRotation()
    {
        yaw = 0.0f;
        pitch = 0.0f;

        Quaternion noRotation = Quaternion.Euler(Vector3.zero);
        transform.rotation = noRotation;
        cameraPivotTransform.localRotation = noRotation;
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

    public void EnableOrbitMode()
    {
        rotateMode = CameraRotateMode.FreeOrbit;
    }
    public void EnableLookatMode(Transform lookatTarget, bool resetRotation)
    {
        rotateMode = CameraRotateMode.LookatTarget;
        this.lookatTarget = lookatTarget;
        if (resetRotation) ResetCameraRotation();
    }
    public Camera GetCamera() { return camera; }
}
