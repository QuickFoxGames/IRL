using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RagdollSetup : MonoBehaviour
{
    [Header("Left Hand")]
    [SerializeField] private Transform m_leftMiddleFinger;
    [SerializeField] private Transform[] m_lFingers;
    [Header("Right Hand")]
    [SerializeField] private Transform m_rightMiddleFinger;
    [SerializeField] private Transform[] m_rFingers;
    private List<Rigidbody> m_bodies = new();
    void Start()
    {
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            m_bodies.Add(rb);
        }
        SetIsKinematic(true);
    }
    private void Update()
    {
        if (!m_bodies[0].isKinematic)
        {
            UpdateFingers(m_lFingers, m_leftMiddleFinger);
            UpdateFingers(m_rFingers, m_rightMiddleFinger);
        }
    }
    private void UpdateFingers(Transform[] fingers, Transform finger)
    {
        foreach (Transform obj in fingers)
        {
            obj.rotation = finger.rotation;
        }
    }
    public void EnterRagdoll(Animator a)
    {
        a.enabled = false;
        SetIsKinematic(false);
        // disable player controll
        // set main rb kinematic to true
        // set player pos.x and .z to m_bodies.center of mass
        // set player pos.y to ground hit.y
    }
    public void ExitRagdoll(Animator a)
    {
        SetIsKinematic(true);
        // set main rb kinematic to false
        // lerp bones towards standup animation starting position
        // enable the animator
        // play stand up animation
        // enable player controll after standup animation plays
    }
    private void SetIsKinematic(bool state)
    {
        foreach(Rigidbody rb in m_bodies)
        {
            rb.isKinematic = state;
        }
    }
}