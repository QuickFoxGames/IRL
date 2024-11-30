using JetBrains.Rider.Unity.Editor;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
public class Vehicle : MonoBehaviour
{
    [Header("Interacions")]
    [SerializeField] public Vector3 m_seatPos;
    [SerializeField] public LayerMask m_groundLayers;
    [Header("Engine")]
    [SerializeField] private float m_enginePower;
    [SerializeField] private float m_engineTorque;
    [SerializeField] private float m_engineBrake;
    [Header("Suspension")]
    [SerializeField] private float m_steerSpeed;
    [SerializeField] private float m_wheelBaseLength;
    [SerializeField] private float m_wheelBaseWidth;
    [SerializeField] private List<Suspension> m_suspensions;

    private bool m_moveCar;
    private bool m_isGrounded;
    [SerializeField] private float m_currentSpeed;
    [SerializeField] private float m_currentAcceleration;
    private Vector3 m_groundNormal;
    private Rigidbody m_rb;
    [Serializable]
    public class Suspension
    {
        public float m_springStiffness; // max force spring can exert
        public float m_damperStiffness; // max force damper can exert
        public float m_restLength;      // default length of spring
        public float m_springTravel;    // distnace spring can strech/compress
        public float m_wheelRadius;
        public float m_force;
        public Transform m_transform;
        public Vector3 m_groundNormal;
        public bool Run(Rigidbody rb, LayerMask ground)
        {
            float maxLength = m_restLength + m_springTravel;
            if (Physics.Raycast(m_transform.position, -m_transform.up, out RaycastHit hit, maxLength + m_wheelRadius, ground))
            {
                float currentSpringLength = hit.distance - m_wheelRadius;
                float springCompression = (m_restLength - currentSpringLength) / m_springTravel;

                float springSpeed = Vector3.Dot(rb.GetPointVelocity(m_transform.position), m_transform.up);
                float dampingForce = m_damperStiffness * springSpeed;

                float springForce = m_springStiffness * springCompression;

                m_force = springForce - dampingForce;
                m_transform.GetChild(0).localPosition = new Vector3(0f, -currentSpringLength, 0f);
                m_groundNormal = hit.point;
                return true;
            }
            return false;
        }
    }
    private void Start()
    {
        m_rb = GetComponent<Rigidbody>();
        InitializeSuspension();
    }
    private void InitializeSuspension()
    {
        for (int i = 0; i < m_suspensions.Count; i++)
        {
            m_suspensions[i].m_transform.localPosition = new Vector3(m_wheelBaseWidth * 0.5f * (i % 2 == 0 ? -1f : 1f), 0f, m_wheelBaseLength * 0.5f * (i < 2 ? 1f : -1f));
        }
    }
    private void Update()
    {
        m_moveCar = Input.GetKey(KeyCode.Mouse1);
        int c = 0;
        foreach (Suspension s in m_suspensions)
        {
            c += s.Run(m_rb, m_groundLayers) ? 1 : 0;
        }
        if (c > 0) m_isGrounded = true;
        else m_isGrounded = false;
    }
    private void FixedUpdate()
    {
        //InitializeSuspension();
        foreach (Suspension s in m_suspensions)
        {
            m_rb.AddForceAtPosition(s.m_force * s.m_transform.up, s.m_transform.position);
        }
        Movement();
    }
    private void Movement()
    {
        Vector3 moveDirection = m_currentSpeed * Vector3.ProjectOnPlane((m_moveCar ? 1f : 0f) * transform.forward, m_groundNormal);
        Vector3 currentVelocity = Vector3.ProjectOnPlane(m_rb.linearVelocity, m_groundNormal);
        if (currentVelocity.magnitude > moveDirection.magnitude && !m_isGrounded) currentVelocity = moveDirection.magnitude * currentVelocity.normalized;
        Vector3 moveForce = m_rb.mass * m_currentAcceleration * (moveDirection - currentVelocity);
        Vector3 frictionForce = Vector3.zero;
        if (m_isGrounded)
        {
            float theta = Vector3.Angle(m_groundNormal, Vector3.up);
            frictionForce = (m_engineBrake * m_rb.mass * Physics.gravity.magnitude * Mathf.Cos(theta)) * moveForce.normalized;
        }
        m_rb.AddForce(moveForce + frictionForce);
    }
    private void GetGroundNormal()
    {
        Vector3 cumulativeNormal = Vector3.zero;
        int normalCount = 0;
        /////
        for (int i = 0; i < m_suspensions.Count - 1; i++)
        {
            for (int j = i + 1; j < m_suspensions.Count; j++)
            {
                for (int k = j + 1; k < m_suspensions.Count; k++)
                {
                    Vector3 v1 = m_suspensions[j].m_groundNormal - m_suspensions[i].m_groundNormal;
                    Vector3 v2 = m_suspensions[k].m_groundNormal - m_suspensions[i].m_groundNormal;
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
}