using UnityEngine;
using WeaponAttachments;
public class Laser : Attachment
{
    [SerializeField] private Transform m_bulletSpawn;
    [SerializeField] private Transform m_laser;
    private void Start()
    {
        m_laser.forward = ((25f * m_bulletSpawn.forward + m_bulletSpawn.position) - m_laser.position).normalized;
    }
    public void Update()
    {
        if (Physics.Raycast(m_bulletSpawn.position, m_bulletSpawn.forward, out RaycastHit hit))
        {
            m_laser.localScale = new(m_laser.localScale.x, m_laser.localScale.y, Vector3.Distance(m_laser.position, hit.point));
        }
        else m_laser.localScale = new(m_laser.localScale.x, m_laser.localScale.y, 200f);
    }
}