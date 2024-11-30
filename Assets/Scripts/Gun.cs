using NUnit.Framework;
using System.Collections;
using System.Net.Http.Headers;
using UnityEngine;
public class Gun : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int m_fireRate;
    [SerializeField] private int m_penetration;
    [SerializeField] private float m_muzzleVelocity;
    [SerializeField] private float m_damage;
    [SerializeField] private float m_reloadTime;
    [Header("Ammo")]
    [SerializeField] private int m_maxAmmo;
    [SerializeField] private int m_maxReserveAmmo;
    [Header("Recoil")]
    [SerializeField] private Vector3 m_camRecoil;
    [SerializeField] private Vector4 m_visualRecoil;
    [Header("Dependancies")]
    [SerializeField] private Transform m_bulletSpawn;
    [SerializeField] private Transform m_muzzleFlashSpawn;
    [Header("Gun Animations")]
    [SerializeField] private float m_lookSwayX;
    [SerializeField] private float m_lookSwayY;
    [SerializeField] private float m_idleSpeed;
    [SerializeField] private Vector2 m_idleFactor;
    [SerializeField] private Vector3 m_muzzleFlashScale;
    [SerializeField] private bool m_useSparks;
    [SerializeField] private GunAnimation m_gunAnimation;

    private bool m_canShoot = true;
    private bool m_isReloading;
    private int m_numShots = 0;
    private int m_currentAmmo;
    private int m_currentReserveAmmo;
    private float m_shotDelay;
    private BulletPool m_bulletPool;
    private PoolManager m_pools;

    public int TargetLayer { private get; set; }
    private void Start()
    {
        m_bulletPool = BulletPool.Instance();
        m_pools = PoolManager.Instance();
        m_currentAmmo = m_maxAmmo;
        m_currentReserveAmmo = m_maxReserveAmmo;
        m_shotDelay = 1f / (m_fireRate / 60f);
        m_gunAnimation.Init();
    }
    public void Disable()
    {
        if (m_muzzleFlashSpawn.childCount > 0)
        {
            foreach (Transform t in m_muzzleFlashSpawn.GetComponentsInChildren<Transform>())
            {
                t.gameObject.SetActive(false);
            }
        }
        gameObject.SetActive(false);
    }
    public void UpdateAnimations(bool state)
    {
        m_gunAnimation.RunAimState(state);
        m_gunAnimation.AddLookSway(m_lookSwayX, m_lookSwayY);
        m_gunAnimation.AddIdleSway(m_idleFactor.x, m_idleFactor.y, m_idleSpeed);
        m_gunAnimation.RunReloadAnimation(m_isReloading, m_reloadTime);
    }
    public Vector3 Shoot(bool shootInput)
    {
        if (m_canShoot && !m_isReloading && shootInput)
        {
            if (m_currentAmmo > 0)
            {
                m_numShots++;
                var b = m_bulletPool.GetBullet(m_bulletSpawn.position, m_bulletSpawn.rotation);
                b.m_targetLayer = TargetLayer;
                b.m_penetration = m_penetration;
                b.m_damage = m_damage;
                b.m_rb.linearVelocity = m_muzzleVelocity * m_bulletSpawn.forward;
                m_currentAmmo--;
                m_gunAnimation.AddRecoilImpulse(m_visualRecoil, m_numShots);
                StartCoroutine(DelayShot());
                var flash = m_pools.SpawnFromPool("MuzzleFlash", m_muzzleFlashSpawn);
                flash.transform.GetChild(0).localScale = m_muzzleFlashScale;
                flash.transform.GetChild(1).localScale = m_muzzleFlashScale;
                if (m_useSparks) flash.transform.GetChild(2).gameObject.SetActive(true);
                else flash.transform.GetChild(2).gameObject.SetActive(false);
                m_pools.ReturnToPoolDelayed("MuzzleFlash", flash, 2.1f);
                float multi = 1f;
                if (m_numShots >= 4 && m_numShots < 8) multi = 2f;
                else if (m_numShots >= 8 && m_numShots < 16) multi = 3f;
                else if (m_numShots >= 16) multi = 4f;
                return new (m_camRecoil.x, m_camRecoil.y * multi, m_camRecoil.z);
            }
            else StartCoroutine(ReloadAmmo());
        }
        else m_numShots = 0;
        return Vector3.zero;
    }
    public void Reload()
    {
        if (!m_isReloading && m_currentAmmo < m_maxAmmo && m_currentReserveAmmo > 0)
            StartCoroutine(ReloadAmmo());
    }
    private IEnumerator DelayShot()
    {
        m_canShoot = false;
        yield return new WaitForSeconds(m_shotDelay);
        m_canShoot = true;
    }
    private IEnumerator ReloadAmmo()
    {
        m_isReloading = true;
        m_numShots = 0;
        yield return new WaitForSeconds(m_reloadTime);
        if (m_currentReserveAmmo < m_maxAmmo)
        {
            m_currentAmmo = m_currentReserveAmmo;
            m_currentReserveAmmo = 0;
        }
        else
        {
            m_currentAmmo = m_maxAmmo;
            m_currentReserveAmmo -= m_maxAmmo;
        }
        m_isReloading = false;
    }
    public int CurrentAmmo { get { return m_currentAmmo; } }
    public int ReserveAmmo { get { return m_currentReserveAmmo; } }
}
[System.Serializable]
public class GunAnimation
{
    [SerializeField] private float m_recoilSpeed;
    [SerializeField] private float m_aimSpeed;
    [SerializeField] private Vector3 m_aimPosition;
    [SerializeField] private Vector3 m_hipPosition;
    [SerializeField] private Transform m_model;
    [SerializeField] private Transform m_sight;
    [SerializeField] private Animator m_animator;
    [SerializeField] private AnimationClip m_reloadClip;
    private bool m_state = false;
    private float m_t = 0f;
    private float m_sightHeight = 0f;
    private Vector3 m_lastForward;
    private Vector3 m_targetPosition = Vector3.zero;
    private Quaternion m_targetRotation = Quaternion.identity;
    public void Init()
    {
        m_sightHeight = -m_model.InverseTransformPoint(m_sight.position).y;
    }
    public void RunAimState(bool state)
    {
        m_state = state;
        m_model.SetLocalPositionAndRotation(Vector3.Slerp(m_model.localPosition, (m_state ? m_aimPosition + -m_model.InverseTransformPoint(m_sight.position).y * Vector3.up : m_hipPosition) + m_targetPosition, m_aimSpeed * Time.deltaTime), m_targetRotation);
        m_targetPosition = Vector3.Lerp(m_targetPosition, Vector3.zero, 3f * m_recoilSpeed * Time.deltaTime);
        m_targetRotation = Quaternion.Slerp(m_targetRotation, Quaternion.identity, m_recoilSpeed * Time.deltaTime);
    }
    public void AddRecoilImpulse(Vector4 visualRecoil, int numShots)
    {
        visualRecoil *= 0.1f;
        m_targetPosition -= (m_state ? 0.75f : 1f) * visualRecoil.w * Vector3.forward;
        m_targetRotation *= Quaternion.Euler(visualRecoil.x, visualRecoil.y, visualRecoil.z);
    }
    public void AddLookSway(float swayFactorX, float swayFactorY)
    {
        float yAngle = Vector3.SignedAngle(m_lastForward, m_model.parent.parent.forward, Vector3.up);//AngleAroundYAxis(m_lastForward, m_model.parent.parent.forward);
        float xAngle = Vector3.SignedAngle(m_lastForward, m_model.parent.parent.forward, Vector3.right);//AngleAroundYAxis(m_lastForward, m_model.parent.parent.forward);
        //if (Mathf.Abs(angle) > 10f) angle = (angle / Mathf.Abs(angle)) * 10f;
        if (Mathf.Abs(yAngle) > 10f) yAngle = (yAngle / Mathf.Abs(yAngle)) * 10f;
        if (Mathf.Abs(xAngle) > 10f) xAngle = (xAngle / Mathf.Abs(xAngle)) * 10f;
        //m_targetRotation *= Quaternion.Euler(0f, angle * swayFactor, 0f);
        m_targetRotation *= Quaternion.Euler(xAngle * swayFactorX, yAngle * swayFactorY, 0f);
        m_lastForward = m_model.parent.parent.forward;
    }
    /*float AngleAroundYAxis(Vector3 from, Vector3 to)
    {
        // Project both vectors onto the XZ plane
        Vector3 fromXZ = new(from.x, 0, from.z);
        Vector3 toXZ = new(to.x, 0, to.z);

        // Calculate the signed angle between them
        return Vector3.SignedAngle(fromXZ, toXZ, Vector3.up);
    }*/
    public void AddIdleSway(float swayFactorX, float swayFactorY, float swaySpeed)
    {
        Transform target = m_model.parent.parent.GetChild(0);
        target.localPosition = new(-Mathf.Sin(swayFactorX * Mathf.PI * m_t), Mathf.Cos(swayFactorY * Mathf.PI * m_t), 1000f);
        m_targetRotation *= Quaternion.LookRotation((target.localPosition - m_model.localPosition).normalized);
        m_t += Time.deltaTime * swaySpeed;
        if (m_t > 1f) m_t = 0f;
    }
    public void RunReloadAnimation(bool state, float time)
    {
        if (!m_animator) return;
        m_animator.speed = m_reloadClip.length / time;
        m_animator.SetBool("Reload", state);
    }
}