using UnityEngine;
namespace WeaponAttachments
{
    public class Attachment : MonoBehaviour
    {
        public bool m_isMag = false;
        public bool m_isSight = false;
        public int m_penetrationMulti;
        public int m_fireRateMulti;
        public float m_damageMulti;
        public float m_muzzleVelocityMulti;
        public float m_aimSpeedMulti;
        public Vector3 m_recoilMulti;
        public float m_mass;
    }
}