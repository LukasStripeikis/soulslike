using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private InputActionReference cameraMoveAction;
    [SerializeField] private Transform target;

    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;

    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float followSmoothTime = 0.1f;

    private float yaw;
    private float pitch;

    private Vector3 positionVelocity;

    void LateUpdate()
    {
        Vector2 lookInput = cameraMoveAction.action.ReadValue<Vector2>();

        yaw += lookInput.x * rotateSpeed * Time.deltaTime;
        pitch -= lookInput.y * rotateSpeed * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, -60f, 60f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 offset =
            rotation * new Vector3(0f, 0f, -distance);

        Vector3 desiredPosition =
            target.position +
            Vector3.up * height +
            offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref positionVelocity,
            followSmoothTime
        );

        transform.LookAt(target.position + Vector3.up * height);
    }

    /*
    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 followTargetPosOffset;
    [SerializeField] private Vector3 moveThreshold;
    [SerializeField] private Vector3 followSpeed;
    [SerializeField] private Vector3 followAcceleration;
    private Vector3 cameraVelocity;
    private Vector3 cameraAcceleration;

    [Header("Orbit")]
    [SerializeField] private InputActionReference cameraMoveAction;
    [SerializeField] private bool onlyOrbitDuringStillCamera;
    [SerializeField] private Transform orbitTarget;
    [SerializeField] private Vector3 orbitTargetPosOffset;
    [SerializeField] private float orbitMinTargetDistance;

    [Space(20)]
    [SerializeField] private float yawRotateSpeed;
    [SerializeField] private Vector2 yawRange;
    [SerializeField] private float yawMinOrbitRadius;

    [Space(20)]
    [SerializeField] private float pitchRotateSpeed;
    [SerializeField] private Vector2 pitchRange;
    [SerializeField] private float pitchMinOrbitRadius;
    private float orbitYaw;
    private float orbitPitch;
    private const float INITIAL_YAW_ORBIT_RAD = 3 * Mathf.PI / 2; 
    private const float INITIAL_PITCH_ORBIT_RAD = Mathf.PI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        orbitYaw = INITIAL_YAW_ORBIT_RAD;
        orbitPitch = INITIAL_PITCH_ORBIT_RAD;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetPos = followTarget.position + followTargetPosOffset;
        float followOffsetDistance = followTargetPosOffset.magnitude;
        Vector3 targetDelta = targetPos - transform.position;
        Vector3 targetDir = targetDelta.normalized;

        Vector3 travelingDir = cameraVelocity.normalized;
        bool isCameraMoving = IsCameraMoving();
        Vector3 newCameraPos = transform.position;
        //Debug.Log($"Target pos: {targetPos} target delta: {targetDelta} current pos:{transform.position}");
        for (int i=0; i<3; i++)
        {
            bool isTargetDeltaPastThreshold = Mathf.Abs(targetDelta[i]) > moveThreshold[i];
            //If we are NOT moving and do not have enough movement past threshold
            //it means we are not ready for moving, so we check other axes
            if (!isCameraMoving && !isTargetDeltaPastThreshold) continue;

            //If we are NOT moving (and if we are here we are PAST the threshold)
            //OR we are moving in the wrong direction (which can happen if we target pos
            //switches to other dir AFTER we have alraedy begun moving to old target)
            if (!isCameraMoving || !Mathf.Approximately(travelingDir[i], targetDir[i]))
            {
                cameraVelocity[i] = followSpeed[i] * targetDir[i];//* targetDeltaDirSign;
                cameraAcceleration[i] = followAcceleration[i] * targetDir[i];//* targetDeltaDirSign;
            }

            //If we are here it means we either have begun setting velocity this
            //frame OR we have been moving from previous frames BUT we need to 
            //ensure that this next move still does not pass the target pos
            // - same side of target -> positive product
            // - crossed target      -> negative product
            // - exactly at target     -> zero
            newCameraPos[i] = transform.position[i] + (cameraVelocity[i] * Time.deltaTime);
            if (targetDelta[i] * (targetPos[i] - newCameraPos[i]) <= 0f)
            {
                newCameraPos[i] = targetPos[i];
                cameraVelocity[i] = 0;
                cameraAcceleration[i] = 0;
            }
        }
        transform.position = newCameraPos;
        cameraVelocity += cameraAcceleration * Time.deltaTime;
        isCameraMoving = IsCameraMoving();


        //NOTE: the rotate dir is in YAW, PITCH
        Vector2 orbitRotateDir = cameraMoveAction.action.ReadValue<Vector2>();
        if (Utils.VecApproxEquals(orbitRotateDir, Vector2.zero))
            return;
        if (onlyOrbitDuringStillCamera && isCameraMoving)
            return;

        Vector3 orbitTargetCenter = orbitTarget.position + orbitTargetPosOffset;
        Vector3 orbitTargetDelta = orbitTargetCenter - transform.position;
        float orbitTargetDistanceSquared = orbitTargetDelta.sqrMagnitude;
        if (orbitTargetDistanceSquared > orbitMinTargetDistance * orbitMinTargetDistance)
            return;

        Vector2 orbitSpeed = new Vector2(yawRotateSpeed, pitchRotateSpeed);
        Vector2 cameraRotateDistanceRad = orbitSpeed * orbitRotateDir * Time.deltaTime;
        
        const int YAW_INDEX = 1, PITCH_INDEX = 0;
        float orbitTargetYaw = orbitTargetDelta[YAW_INDEX];
        float orbitTargetPitch = orbitTargetDelta[PITCH_INDEX];
        //NOTE: first we find percentage of rotated distance around circumference
        //of the orbit with orbit target delta as radius in yaw (Y rotation), 
        //pitch(X rotation) order and then we multiply by 1 full rotation (2pi):
        // -> rotatedDist / (2 * PI * orbitRadius) * (2 * PI)
        Vector2 rotateDeltaAngleRad = Vector2.zero;
        if (Mathf.Abs(orbitTargetYaw) > 0)
            rotateDeltaAngleRad.x = cameraRotateDistanceRad.x / orbitTargetYaw;
        if (Mathf.Abs(orbitTargetPitch) > 0)
            rotateDeltaAngleRad.y = cameraRotateDistanceRad.y / orbitTargetPitch;
        
        //orbitYaw = Mathf.Clamp(orbitYaw + rotateDeltaAngleRad.x, yawRange.x, yawRange.y);
        //orbitPitch = Mathf.Clamp(orbitPitch + rotateDeltaAngleRad.y, pitchRange.x, pitchRange.y);
        orbitYaw += rotateDeltaAngleRad.x;
        orbitPitch += rotateDeltaAngleRad.y;
        float orbitRadiusYaw = Mathf.Abs(orbitTargetDelta[YAW_INDEX]);
        if (orbitRadiusYaw < yawMinOrbitRadius) orbitRadiusYaw = yawMinOrbitRadius;

        float orbitRadiusPitch = Mathf.Abs(orbitTargetDelta[YAW_INDEX]);
        if (orbitRadiusPitch < pitchMinOrbitRadius) orbitRadiusPitch = pitchMinOrbitRadius;

        transform.position = orbitTargetCenter + 
            (orbitRadiusYaw * new Vector3(Mathf.Cos(orbitYaw), 0, Mathf.Sin(orbitYaw)) + 
             orbitRadiusPitch * new Vector3(0, Mathf.Cos(orbitPitch), Mathf.Sin(orbitPitch)));
            
        //transform.rotation = Quaternion.LookRotation(orbitTargetDelta.normalized);
    }

    public bool IsCameraMoving()
    {
        return cameraVelocity.sqrMagnitude > 0.0f;
    }
    */
}
