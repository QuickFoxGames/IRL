using System.Collections.Generic;
using UnityEngine;
public class Plane : MonoBehaviour
{
    [SerializeField] private float engineForce;
    [SerializeField] private Rigidbody m_body;
    [SerializeField] private List<Wing> m_wings;
    private GameManager m_gameManager;
    void Start()
    {
        m_gameManager = GameManager.Instance();
    }
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W)) m_body.AddForce(engineForce * m_body.transform.forward);
        UpdateWings();
    }
    private void UpdateWings()
    {
        foreach (var wing in m_wings)
        {
            wing.ApplyLiftAndDragForcesToWing(m_gameManager.m_airDensityCurve.Evaluate(wing.m_transform.position.y * 0.001f));
        }
    }
}
[System.Serializable]
public class Wing
{
    [SerializeField] private float m_surfaceArea;
    [SerializeField] public Transform m_transform;
    [SerializeField] private Rigidbody m_rb;
    private const float MaxLiftForce = 2000f;
    private const float MaxDragForce = 500f;
    private const float MaxAngleOfAttack = 20f;
    public void ApplyLiftAndDragForcesToWing(float airDensity)
    {
        float cl = CalculateCL();
        float semiForce = 0.5f * airDensity * m_rb.linearVelocity.sqrMagnitude * m_surfaceArea;
        float liftForce = Mathf.Clamp(semiForce * cl, -MaxLiftForce, MaxLiftForce);
        float dragForce = Mathf.Clamp(semiForce * CalculateCD(cl), 0, MaxDragForce);
        m_rb.AddForce(liftForce * m_transform.up.normalized - dragForce * Vector3.Project(m_rb.linearVelocity, m_transform.forward).normalized);
    }
    private float CalculateCL()
    {
        Vector3 flatAirDirection = Vector3.ProjectOnPlane(-m_rb.linearVelocity, m_transform.right);
        float angle = Vector3.SignedAngle(flatAirDirection, m_transform.forward, m_transform.right);
        angle = Mathf.Clamp(angle, -MaxAngleOfAttack, MaxAngleOfAttack);

        return Mathf.PI * 2f * Mathf.Deg2Rad * angle;
    }
    private float CalculateCD(float cl)
    {
        return 0.02f + 0.05f * (cl * cl);
    }
}