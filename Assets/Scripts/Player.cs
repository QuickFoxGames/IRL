using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using MGUtilities;
public class Player : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private float m_planeSize = 1f;
    [SerializeField] private Color m_planeColor = Color.magenta;
    /////
    [Header("CrossHair")]
    [SerializeField] private CrossHair m_crossHair;
    /////
    [Header("Camera")]
    [SerializeField] private Cam m_cam;
    /////
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
    [Space]
    [SerializeField] private float m_strafeMultiplier;
    [SerializeField] private float m_jumpHeight;
    [SerializeField] private float m_maxStairHeight;
    private float m_currentSpeed;
    private float m_currentAcceleration;
    /////
    [Header("Ground Handling")]
    [SerializeField] private float m_groundCheckDistance;
    [SerializeField] private float m_maxSlope;
    [SerializeField] private float m_maxFrictionMultiplier;
    [SerializeField] private LayerMask m_groundMask;
    [SerializeField] private PhysicsMaterial m_physicsMaterial;
    private bool m_isGrounded;
    private Vector3 m_groundNormal;
    /////
    [Header("Gun")]
    [SerializeField] private float m_recoilSnap;
    [SerializeField] private LayerMask m_targetLayers;
    [SerializeField] private List<Gun> m_guns;
    [SerializeField] private TextMeshProUGUI CurrentAmmo;
    [SerializeField] private TextMeshProUGUI ReserveAmmo;
    /////
    [Header("Animations")]
    [SerializeField] private Animator m_animator;
    /////
    // Inputs //
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
    //
    private int m_gunIndex = 0;
    private float m_lastV;
    private float m_lastH;
    /////
    private Transform m_transform;
    private Rigidbody m_rb;
    /////
    private Coroutine crosshairCoroutine;
    /////
    private Vehicle m_vehicle;
    /////
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
        m_rb = GetComponent<Rigidbody>();
        m_transform = transform;
        ToggleMouse();
        m_cam.Initialize();
        m_crossHair.Initialize();
        //m_vehicle = FindFirstObjectByType<Vehicle>();
        if (m_vehicle) EnterVehicle();
        // after spawning guns into inventory
        foreach (Gun g in m_guns) 
        { 
            g.TargetLayer = m_targetLayers.value;
        }
    }
    public void ToggleMouse()
    {
        Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = !Cursor.visible;
    }
    void Update()
    {
        #region Inputs
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
        /////
        if (m_mouseWheel != 0)
        {
            m_guns[m_gunIndex].Disable();
            m_gunIndex += m_mouseWheel > 0f ? 1 : -1;
            if (m_gunIndex >= m_guns.Count) m_gunIndex = 0;
            if (m_gunIndex < 0) m_gunIndex = m_guns.Count - 1;
            m_guns[m_gunIndex].gameObject.SetActive(true);
        }
        if (m_sprintInput && m_isGrounded)
        {
            m_currentSpeed = m_runSpeed;
            m_currentAcceleration = m_runAcceleration;
        }/////
        else if (m_crouchInput && m_isGrounded)
        {
            m_currentSpeed = m_crouchSpeed;
            m_currentAcceleration = m_crouchAcceleration;
        }/////
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
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (crosshairCoroutine != null)
                StopCoroutine(crosshairCoroutine);
            crosshairCoroutine = StartCoroutine(Coroutines.LerpVector3ToZeroOverTime(false, 0.25f, m_crossHair.m_object.transform.localScale,
                value => m_crossHair.m_object.transform.localScale = value));
        }
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (crosshairCoroutine != null)
                StopCoroutine(crosshairCoroutine);
            crosshairCoroutine = StartCoroutine(Coroutines.LerpVector3ToZeroOverTime(true, 0.25f, m_crossHair.m_object.transform.localScale,
                value => m_crossHair.m_object.transform.localScale = value));
        }
        #endregion
        CheckGround();
        HandleAnimations();
        if (Cursor.visible) return; // if the cursor is visible nothing below runs
        CurrentAmmo.text = GetAmmoText();
        ReserveAmmo.text = "|" + m_guns[m_gunIndex].ReserveAmmo.ToString();
        Vector3 recoil = m_guns[m_gunIndex].Shoot(m_shootInput);
        m_cam.UpdateCamera(m_mouseX, m_mouseY, m_recoilSnap, recoil, m_transform);
        m_cam.UpdateFov(m_rb.linearVelocity.magnitude, m_walkSpeed, m_runSpeed);
        m_guns[m_gunIndex].UpdateAnimations(m_aimInput);
        if (!m_aimInput) m_crossHair.UpdateCrossHair(recoil.x);
        if (m_reloadInput) m_guns[m_gunIndex].Reload();
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
    private void HandleAnimations()
    {
        m_animator.SetBool("isJump", m_jumpInput);
        m_animator.SetBool("isCrouch", m_crouchInput);
        m_animator.SetBool("isSprint", m_sprintInput);
        m_animator.SetBool("isIdle", m_verticalInput == 0f && m_horizontalInput == 0f);
        if (m_horizontalInput != 0f) m_lastH = m_horizontalInput;
        m_animator.SetFloat("speedX", Vector3.Project(m_rb.linearVelocity, transform.right).magnitude * m_lastH);
        if (m_verticalInput != 0f) m_lastV = m_verticalInput;
        m_animator.SetFloat("speedZ", Vector3.Project(m_rb.linearVelocity, transform.forward).magnitude * m_lastV);
    }
    private void FixedUpdate()
    {
        if (m_vehicle)
        {
            //m_vehicle.Drive(m_verticalInput);
            //m_vehicle.Steer(m_horizontalInput);
            //if (m_jumpInput) m_vehicle.Brake();
            return;
        }
        Movement();
        CheckStairs();
        if (m_jumpInput && m_isGrounded) Jump(m_jumpHeight);
    }
    /////
    private void EnterVehicle()
    {
        transform.position = m_vehicle.transform.position + m_vehicle.m_seatPos;
        m_rb.isKinematic = true;
        m_rb.AddComponent<FixedJoint>();
    }
    /////
    public void CheckGround()
    {
        float angleStep = 360f / 8;
        List<Vector3> hits = new();
        /////
        for (int i = 0; i < 8; i++)
        {
            float angleInRadians = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleInRadians) * 0.5f;
            float z = Mathf.Sin(angleInRadians) * 0.5f;
            Vector3 origin = new(m_transform.position.x + x, m_transform.position.y + m_groundCheckDistance, m_transform.position.z + z);
            /////
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2f * m_groundCheckDistance, m_groundMask))
            {
                hits.Add(hit.point);
                Debug.DrawLine(origin, hit.point, Color.green);
            }
            else Debug.DrawLine(origin, origin + Vector3.down * m_groundCheckDistance, Color.red);
        }
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
    private void Movement()
    {
        Vector3 moveDirection = m_currentSpeed * Vector3.ProjectOnPlane(m_verticalInput * m_transform.forward + m_horizontalInput * m_strafeMultiplier * m_transform.right, m_groundNormal);
        Vector3 currentVelocity = Vector3.ProjectOnPlane(m_rb.linearVelocity, m_groundNormal);
        if (currentVelocity.magnitude > moveDirection.magnitude && !m_isGrounded) currentVelocity = moveDirection.magnitude * currentVelocity.normalized;
        Vector3 moveForce = (m_verticalInput < 0 ? m_moveBackMultiplier : 1f) * m_rb.mass * m_currentAcceleration * (moveDirection - currentVelocity);
        Vector3 frictionForce = Vector3.zero;
        if (m_isGrounded)
        {
            float theta = Vector3.Angle(m_groundNormal, Vector3.up);
            frictionForce = (CalculateFrictionMultiplier(theta) * m_physicsMaterial.dynamicFriction * m_rb.mass * Physics.gravity.magnitude * Mathf.Cos(theta)) * moveForce.normalized;
        }
        m_rb.AddForce(moveForce + frictionForce);
    }
    private float CalculateFrictionMultiplier(float theta)
    {
        theta = Mathf.Clamp(theta, 0f, m_maxSlope);
        return 1f + (theta / m_maxSlope) * (m_maxFrictionMultiplier - 1f);
    }
    private void Jump(float height)
    {
        Vector3 jumpVelocity = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * height) * Vector3.up;
        m_rb.linearVelocity = new(m_rb.linearVelocity.x, 0f, m_rb.linearVelocity.z);
        m_rb.AddForce(m_rb.mass * jumpVelocity, ForceMode.Impulse);
    }
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
    public void SwapWeapon()
    {

    }
}
[System.Serializable]
public class Cam
{
    [SerializeField] private float m_baseFov;
    [SerializeField] private float m_maxFov;
    [SerializeField] private float m_mouseSensitivity;
    [SerializeField] private float m_camUpperBounds;
    [SerializeField] private float m_camLowerBounds;
    [SerializeField] private float m_cameraShake;
    [SerializeField] private Transform m_head;
    [SerializeField] private Transform m_camParent;
    [SerializeField] private Camera m_cam;
    private float m_xRot, m_yRot;
    private float m_xRecoilRot, m_yRecoilRot, m_zRecoilRot;
    private Vector3 m_recoilAccumulator;
    private Transform m_camTransform;
    public void Initialize()
    {
        m_camTransform = m_cam.transform;
    }
    public void UpdateCamera(float mouseX, float mouseY, float recoilSnap, Vector3 recoil, Transform parent)
    {
        m_xRot -= mouseY * m_mouseSensitivity * Time.fixedDeltaTime;
        m_yRot += mouseX * m_mouseSensitivity * Time.fixedDeltaTime;

        if (m_xRot > m_camUpperBounds) m_xRot = m_camUpperBounds;
        if (m_xRot < m_camLowerBounds) m_xRot = m_camLowerBounds;

        m_camParent.position = m_head.position;
        m_camParent.localRotation = Quaternion.Slerp(Quaternion.identity, m_head.localRotation, m_cameraShake);

        m_xRecoilRot += recoil.x * (1f + m_recoilAccumulator.x);
        m_yRecoilRot += recoil.y * (1f + m_recoilAccumulator.y);
        m_zRecoilRot += Random.Range(-1, 1) * recoil.z * (1f + m_recoilAccumulator.z);

        m_recoilAccumulator.x += 1000f * Mathf.Abs(recoil.x) * Time.deltaTime;
        m_recoilAccumulator.y += 1000f * Mathf.Abs(recoil.y) * Time.deltaTime;
        m_recoilAccumulator.z += 1000f * Mathf.Abs(recoil.z) * Time.deltaTime;
        if (recoil == Vector3.zero) m_recoilAccumulator = Vector3.zero;

        m_camTransform.localRotation = Quaternion.Euler(m_xRot + m_xRecoilRot, m_yRecoilRot, m_zRecoilRot);
        parent.rotation = Quaternion.Euler(0f, m_yRot, 0f);

        m_xRecoilRot = Mathf.Lerp(m_xRecoilRot, 0f, recoilSnap / (3f * (1f + m_recoilAccumulator.x)) * Time.deltaTime);
        m_yRecoilRot = Mathf.Lerp(m_yRecoilRot, 0f, recoilSnap / (3f * (1f + m_recoilAccumulator.x)) * Time.deltaTime);
        m_zRecoilRot = Mathf.Lerp(m_zRecoilRot, 0f, recoilSnap / (3f * (1f + m_recoilAccumulator.x)) * Time.deltaTime);
    }
    public void UpdateFov(float currentSpeed, float minSpeed, float maxSpeed)
    {
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        m_cam.fieldOfView = m_baseFov + ((currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * (m_maxFov - m_baseFov);
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
    public void Initialize()
    {
        foreach (var p in m_pegs)
        {
            m_originalPositions.Add(p.position);
        }
    }
    public void UpdateCrossHair(float x)
    {
        for (int i = 0; i < m_pegs.Count; i++)
        {
            m_pegs[i].position -= x * (m_pegs[i].position - m_center.position).normalized;
            m_pegs[i].position = Vector3.Lerp(m_pegs[i].position, m_originalPositions[i], m_crossHairSnap * Time.deltaTime);
        }
    }
}