using System.Collections;
using UnityEngine;
public class PhysGun : MonoBehaviour
{
    [SerializeField] private float m_fireRate;
    [SerializeField] private Rigidbody m_frontRb;
    [SerializeField] private Rigidbody m_backRb;
    private bool m_canShoot = true;
    private float m_shotDelay;
    void Start()
    {
        m_shotDelay = 1f / (m_fireRate / 60f);
    }
    void Update()
    {
        if (Input.GetKey(KeyCode.Mouse0) && m_canShoot) Shoot();
    }
    public void Shoot()
    {
        m_frontRb.AddForce(10f * transform.up, ForceMode.Impulse);
        m_backRb.AddForce(-25f * transform.forward, ForceMode.Impulse);
        StartCoroutine(DelayShot());
    }
    private IEnumerator DelayShot()
    {
        m_canShoot = false;
        yield return new WaitForSeconds(m_shotDelay);
        m_canShoot = true;
    }
}
