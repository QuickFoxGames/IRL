using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using MGUtilities;
using static UnityEngine.GraphicsBuffer;
public class Player : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private float m_planeSize = 1f;
    [SerializeField] private Color m_planeColor = Color.magenta;
    [Header("CrossHair")]
    [SerializeField] private CrossHair m_crossHair;
    [Header("Camera")]
    [SerializeField] private Cam m_cam;
    [Header("Gun")]
    [SerializeField] private float m_recoilSnap;
    [SerializeField] private LayerMask m_targetLayers;
    [SerializeField] private List<Gun> m_guns;
    [SerializeField] private TextMeshProUGUI CurrentAmmo;
    [SerializeField] private TextMeshProUGUI ReserveAmmo;
    // Inputs // vv
    private bool m_jumpInput;
    private bool m_sprintInput;
    private bool m_crouchInput;
    private bool m_shootInput;
    private bool m_aimInput;
    private bool m_reloadInput;
    private float m_mouseWheel;
    private float m_mouseX;
    private float m_mouseY;
    private float m_verticalInput;
    private float m_horizontalInput;
    // Inputs // ^^
    private int m_gunIndex = 0;

    private Transform m_transform;
    private Rigidbody m_rb;
    private HealthSystem m_healthSystem;
    //private Vehicle m_vehicle;
    private Coroutine CrosshairCoroutine;
    private void OnDrawGizmos()
    {
        if (m_groundNormal != Vector3.zero)
        {
            Vector3 center = m_transform.position;
            Vector3 tangent1 = Vector3.Cross(m_groundNormal, Vector3.up).normalized * m_planeSize;
            /////
            if (tangent1 == Vector3.zero) tangent1 = Vector3.Cross(m_groundNormal, Vector3.forward).normalized * m_planeSize;
            /////
            Vector3 tangent2 = Vector3.Cross(m_groundNormal, tangent1).normalized * m_planeSize;
            Vector3 corner1 = center + tangent1 + tangent2;
            Vector3 corner2 = center + tangent1 - tangent2;
            Vector3 corner3 = center - tangent1 + tangent2;
            Vector3 corner4 = center - tangent1 - tangent2;
            /////
            Gizmos.color = m_planeColor;
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner4);
            Gizmos.DrawLine(corner4, corner3);
            Gizmos.DrawLine(corner3, corner1);
        }
    }
    void Start()
    {
        // Setup
        m_rb = GetComponent<Rigidbody>();
        m_healthSystem = GetComponent<HealthSystem>();
        m_transform = transform;
        // Initializers
        m_healthSystem.Init();
        m_crossHair.Init();

        ToggleMouse(); // toggles the mouse lock and visibility

        foreach (Gun g in m_guns) // setup guns
        { 
            g.TargetLayer = m_targetLayers.value;
        }
    }
    #region Misc
    public void ToggleMouse()
    {
        Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Cursor.visible;
    }
    private string GetAmmoText()
    {
        char[] array = new char[m_guns[m_gunIndex].CurrentAmmo];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = 'l';
        }
        return array.ArrayToString();
    }
    private void HandleSwapWeapon()
    {
        m_guns[m_gunIndex].Disable();
        m_gunIndex += m_mouseWheel > 0f ? 1 : -1;
        if (m_gunIndex >= m_guns.Count) m_gunIndex = 0;
        if (m_gunIndex < 0) m_gunIndex = m_guns.Count - 1;
        m_guns[m_gunIndex].gameObject.SetActive(true);
    }
    #endregion
    void Update()
    {
        #region Handle Inputs
        m_mouseWheel = Input.GetAxisRaw("Mouse ScrollWheel");
        m_mouseX = Input.GetAxisRaw("Mouse X");
        m_mouseY = Input.GetAxisRaw("Mouse Y");
        m_verticalInput = Input.GetAxisRaw("Vertical");
        m_horizontalInput = Input.GetAxisRaw("Horizontal");
        m_jumpInput = Input.GetKey(KeyCode.Space);
        m_sprintInput = Input.GetKey(KeyCode.LeftShift);
        m_crouchInput = Input.GetKey(KeyCode.LeftControl);
        m_shootInput = Input.GetKey(KeyCode.Mouse0);
        m_aimInput = Input.GetKey(KeyCode.Mouse1);
        m_reloadInput = Input.GetKey(KeyCode.R);

        if (m_mouseWheel != 0) HandleSwapWeapon();
        if (m_sprintInput && m_isGrounded)
        {
            m_currentSpeed = m_runSpeed;
            m_currentAcceleration = m_runAcceleration;
        }
        else if (m_crouchInput && m_isGrounded)
        {
            m_currentSpeed = m_crouchSpeed;
            m_currentAcceleration = m_crouchAcceleration;
        }
        else if (m_isGrounded)
        {
            m_currentSpeed = m_walkSpeed;
            m_currentAcceleration = m_walkAcceleration;
        }
        else
        {
            m_currentSpeed = m_inAirSpeed;
            m_currentAcceleration = m_inAirAcceleration;
        }
        if (Input.GetKeyDown(KeyCode.Mouse1) && m_crossHair.m_object)
        {
            if (CrosshairCoroutine != null)
                StopCoroutine(CrosshairCoroutine);
            CrosshairCoroutine = StartCoroutine(Coroutines.LerpVector3ToZeroOverTime(false, 0.25f, m_crossHair.m_object.transform.localScale,
                value => m_crossHair.m_object.transform.localScale = value));
        }
        if (Input.GetKeyUp(KeyCode.Mouse1) && m_crossHair.m_object)
        {
            if (CrosshairCoroutine != null)
                StopCoroutine(CrosshairCoroutine);
            CrosshairCoroutine = StartCoroutine(Coroutines.LerpVector3ToZeroOverTime(true, 0.25f, m_crossHair.m_object.transform.localScale,
                value => m_crossHair.m_object.transform.localScale = value));
        }
        #endregion
        CheckGround();
        if (Cursor.visible) return; // if the cursor is visible nothing below runs

        if (CurrentAmmo) CurrentAmmo.text = GetAmmoText();
        if (ReserveAmmo) ReserveAmmo.text = "|" + m_guns[m_gunIndex].ReserveAmmo.ToString();

        Hover();
        m_flatVelocity = Vector3.ProjectOnPlane(m_rb.linearVelocity, m_groundNormal);

        Vector3 recoil = m_guns[m_gunIndex].Shoot(m_shootInput);
        m_cam.UpdateFov(m_flatVelocity.magnitude, m_walkSpeed, m_runSpeed);
        Vector2 camRots = m_cam.UpdateCamera(m_mouseX, m_mouseY, m_recoilSnap, recoil, m_transform);

        HandleAnimations(camRots.x);

        m_healthSystem.RegenHp();

        m_guns[m_gunIndex].UpdateAnimations(m_aimInput);
        if (m_reloadInput) m_guns[m_gunIndex].Reload();

        if (!m_aimInput) m_crossHair.UpdateCrossHair(recoil.x);
    }
    #region HandleAnimations
    [Header("Animations")]
    [SerializeField] private Transform m_lLegTarget;
    [SerializeField] private Transform m_lHandTarget;
    [SerializeField] private Transform m_rHandTarget;
    [SerializeField] private Transform m_bodyIkTarget;
    [SerializeField] private Transform[] m_lookTargets;
    private void HandleAnimations(float xrot)
    {
        float map = MGFunc.MapRangeTo01(xrot, m_cam.m_camLowerBounds, m_cam.m_camUpperBounds); // Assuming MapRange maps to [0, 1]

        // Calculate the segment count and normalized value
        int segmentCount = m_lookTargets.Length - 1;
        float normalizedValue = Mathf.Clamp01(map); // wrap value to 0-1

        // Determine the current segment based on the normalized value
        int currentSegment = Mathf.Min((int)(normalizedValue * segmentCount), segmentCount - 1);

        // Identify the start and end transforms for interpolation
        Transform startTrans = m_lookTargets[currentSegment];
        int next = Mathf.Min(currentSegment + 1, segmentCount); // Clamp next segment index
        Transform endTrans = m_lookTargets[next];

        // Calculate the interpolation factor (segT) within the current segment
        float segT = (normalizedValue * segmentCount) - currentSegment;

        // Slerp position and rotation between the start and end transforms
        m_bodyIkTarget.SetLocalPositionAndRotation(
            Vector3.Slerp(startTrans.localPosition, endTrans.localPosition, segT),
            Quaternion.Slerp(startTrans.localRotation, endTrans.localRotation, segT)
        );

        m_lHandTarget.SetPositionAndRotation(m_guns[m_gunIndex].m_lIkTarget.position, m_guns[m_gunIndex].m_lIkTarget.rotation);
        m_rHandTarget.SetPositionAndRotation(m_guns[m_gunIndex].m_rIkTarget.position, m_guns[m_gunIndex].m_rIkTarget.rotation);
    }
    #endregion
    private void FixedUpdate()
    {
        /*if (m_vehicle)
        {
            //m_vehicle.Drive(m_verticalInput);
            //m_vehicle.Steer(m_horizontalInput);
            //if (m_jumpInput) m_vehicle.Brake();
            return;
        }*/
        Movement();
        CheckStairs();
        if (m_jumpInput && m_isGrounded) Jump(m_jumpHeight);
    }
    /*private void EnterVehicle()
    {
        transform.position = m_vehicle.transform.position + m_vehicle.m_seatPos;
        m_rb.isKinematic = true;
        m_rb.AddComponent<FixedJoint>();
    }*/

    #region CheckGround
    [Header("Ground Handling")]
    [SerializeField] private float m_groundCheckDistance;
    [SerializeField] private float m_maxSlope;
    [SerializeField] private float m_maxFrictionMultiplier;
    [SerializeField] private float m_groundCheckRadius;
    [SerializeField] private float m_groundCheckNum;
    [Space]
    [SerializeField] private LayerMask m_groundMask;
    [SerializeField] private PhysicsMaterial m_physicsMaterial;
    private bool m_isGrounded;
    private float m_groundDist;
    private Vector3 m_groundNormal;
    public void CheckGround()
    {
        float angleStep = 360f / m_groundCheckNum;
        m_groundDist = 0f;
        List<Vector3> hits = new();
        /////
        for (int i = 0; i < 8; i++)
        {
            float angleInRadians = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleInRadians) * m_groundCheckRadius;
            float z = Mathf.Sin(angleInRadians) * m_groundCheckRadius;
            Vector3 origin = new(m_transform.position.x + x, m_transform.position.y + 1f + m_groundCheckDistance, m_transform.position.z + z);
            /////
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1f + 2f * m_groundCheckDistance, m_groundMask))
            {
                m_groundDist += hit.distance;
                hits.Add(hit.point);
                Debug.DrawLine(origin, hit.point, Color.green);
            }
            else Debug.DrawLine(origin, origin + Vector3.down * m_groundCheckDistance, Color.red);
        }
        m_groundDist /= 8f;
        /////
        if (hits.Count >= 3)
        {
            Vector3 cumulativeNormal = Vector3.zero;
            int normalCount = 0;
            /////
            for (int i = 0; i < hits.Count - 1; i++)
            {
                for (int j = i + 1; j < hits.Count; j++)
                {
                    for (int k = j + 1; k < hits.Count; k++)
                    {
                        Vector3 v1 = hits[j] - hits[i];
                        Vector3 v2 = hits[k] - hits[i];
                        Vector3 normal = Vector3.Cross(v1, v2).normalized;
                        /////
                        if (normal != Vector3.zero)
                        {
                            cumulativeNormal += normal;
                            normalCount++;
                        }
                    }
                }
            }
            /////
            if (normalCount > 0) cumulativeNormal /= normalCount;
            /////
            m_groundNormal = -cumulativeNormal;
        }
        m_isGrounded = hits.Count > 0;
    }
    #endregion
    #region Movement
    [Header("Movement")]
    [SerializeField] private float m_inAirSpeed;
    [SerializeField] private float m_crouchSpeed;
    [SerializeField] private float m_walkSpeed;
    [SerializeField] private float m_runSpeed;
    [Space]
    [SerializeField] private float m_inAirAcceleration;
    [SerializeField] private float m_crouchAcceleration;
    [SerializeField] private float m_walkAcceleration;
    [SerializeField] private float m_runAcceleration;
    [Space]
    [SerializeField] private float m_moveBackMultiplier;
    [SerializeField] private float m_strafeMultiplier;
    private float m_currentSpeed;
    private float m_currentAcceleration;
    private Vector3 m_flatVelocity;
    private void Movement()
    {
        Vector3 moveDirection = m_currentSpeed * Vector3.ProjectOnPlane(m_verticalInput * m_transform.forward + 
            m_horizontalInput * m_strafeMultiplier * m_transform.right, m_groundNormal).normalized;
        if (m_flatVelocity.magnitude > moveDirection.magnitude && !m_isGrounded) m_flatVelocity = moveDirection.magnitude * m_flatVelocity.normalized;
        Vector3 moveForce = (m_verticalInput < 0 ? m_moveBackMultiplier : 1f) * m_rb.mass * m_currentAcceleration * (moveDirection - m_flatVelocity);
        Vector3 frictionForce = Vector3.zero;
        if (m_isGrounded)
        {
            float theta = Vector3.Angle(m_groundNormal, Vector3.up);
            frictionForce = (MGFunc.CalculateFrictionMultiplier(theta, m_maxSlope, m_maxFrictionMultiplier)
                * m_physicsMaterial.dynamicFriction * m_rb.mass * Physics.gravity.magnitude * Mathf.Cos(theta)) * moveForce.normalized;
        }
        m_rb.AddForce(moveForce + frictionForce + (!m_jumpInput ? m_hoverForce * m_transform.up : Vector3.zero));
    }
    [Space]
    [SerializeField] private float m_jumpHeight;
    private void Jump(float height)
    {
        Vector3 jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * height) * Vector3.up;
        m_rb.linearVelocity = new(m_rb.linearVelocity.x, 0f, m_rb.linearVelocity.z);
        m_rb.AddForce(m_rb.mass * jumpVelocity, ForceMode.Impulse);
    }
    [Space]
    [SerializeField] private float m_maxStairHeight;
    private void CheckStairs()
    {
        if (Physics.Raycast(m_transform.position + 0.1f * m_transform.up, m_rb.linearVelocity.normalized, 0.75f)) // if stair in the way
        {
            if (!Physics.Raycast(m_transform.position + m_maxStairHeight * m_transform.up, m_rb.linearVelocity.normalized, 0.85f)) // if stair less than maxStairHeight
            {
                if (Physics.Raycast(m_transform.position + m_maxStairHeight * m_transform.up + 0.85f * m_rb.linearVelocity.normalized, Vector3.down, out RaycastHit hit, m_maxStairHeight)) // if stair less than maxStairHeight
                {
                    float jumpHeight = hit.point.y - m_transform.position.y;
                    if (jumpHeight > 0.1f) Jump(jumpHeight);
                }
            }
        }
    }
    [Space]
    [SerializeField] private float m_dampRate = 3000f;
    [SerializeField] private float m_springRate = 5000f;
    [SerializeField] private float m_baseRestLength, m_crouchRestLength, m_baseSpringTravel, m_crouchSpringTravel;
    private float m_hoverForce;
    private void Hover()
    {
        float maxLength = (m_crouchInput ? m_crouchRestLength + m_crouchSpringTravel : m_baseRestLength + m_baseSpringTravel);
        m_hoverForce = 0f;
        if (m_groundDist < maxLength && m_groundDist > 0f)
        {
            float currentSpringLength = m_groundDist;
            float springCompression = (m_crouchInput ? m_crouchRestLength : m_baseRestLength - currentSpringLength) / (m_crouchInput ? m_crouchSpringTravel : m_baseSpringTravel);

            float springSpeed = Vector3.Dot(m_rb.GetPointVelocity(m_transform.position), m_transform.up);
            float dampingForce = m_dampRate * springSpeed;

            float springForce = m_springRate * springCompression;

            m_hoverForce = springForce - dampingForce;
        }
    }
    #endregion
}
[System.Serializable]
public class Cam
{
    [SerializeField] private float m_baseFov;
    [SerializeField] private float m_maxFov;
    [SerializeField] private float m_mouseSensitivity;
    public float m_camUpperBounds;
    public float m_camLowerBounds;
    [SerializeField] private float m_cameraShake;
    [SerializeField] private Transform m_head;
    [SerializeField] private Transform m_camParent;
    [SerializeField] private Camera m_cam;
    private float m_xRot, m_yRot;
    private Vector3 m_RecoilRot;
    public Vector2 UpdateCamera(float mouseX, float mouseY, float recoilSnap, Vector3 recoil, Transform parent)
    {
        m_xRot -= mouseY * m_mouseSensitivity * Time.fixedDeltaTime;
        m_yRot += mouseX * m_mouseSensitivity * Time.fixedDeltaTime;

        if (m_xRot > m_camUpperBounds) m_xRot = m_camUpperBounds;
        if (m_xRot < m_camLowerBounds) m_xRot = m_camLowerBounds;

        m_RecoilRot.x += recoil.x;
        m_RecoilRot.y += recoil.y;
        m_RecoilRot.z += Random.Range(-1, 1) * recoil.z;

        m_cam.transform.localRotation = Quaternion.Euler(m_RecoilRot.x, m_RecoilRot.y, m_RecoilRot.z);
        parent.rotation = Quaternion.Euler(0f, m_yRot, 0f);

        m_RecoilRot = Vector3.Lerp(m_RecoilRot, Vector3.zero, recoilSnap * Time.deltaTime);

        m_camParent.SetPositionAndRotation(m_head.position, m_head.rotation);

        return new Vector2(m_xRot, m_yRot);
    }
    public void UpdateFov(float currentSpeed, float minSpeed, float maxSpeed)
    {
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        m_cam.fieldOfView = Mathf.Lerp(m_cam.fieldOfView, m_baseFov + ((currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * (m_maxFov - m_baseFov), 3f * Time.deltaTime);
    }
}
[System.Serializable]
public class CrossHair
{
    [SerializeField] private float m_crossHairSnap;
    [SerializeField] private float m_maxDisplacement;
    [SerializeField] private RectTransform m_center;
    [SerializeField] private List<RectTransform> m_pegs = new();
    private List<Vector3> m_originalPositions = new();
    public GameObject m_object;
    public void Init()
    {
        if (m_pegs.Count <= 0 || !m_object || !m_center) return;
        foreach (var p in m_pegs)
        {
            m_originalPositions.Add(p.position);
        }
    }
    public void UpdateCrossHair(float x)
    {
        if (m_pegs.Count <= 0 || !m_object || !m_center) return;
        for (int i = 0; i < m_pegs.Count; i++)
        {
            m_pegs[i].position -= x * (m_pegs[i].position - m_center.position).normalized;
            m_pegs[i].position = Vector3.Lerp(m_pegs[i].position, m_originalPositions[i], m_crossHairSnap * Time.deltaTime);
        }
    }
}