using System.Collections;
using UnityEngine;
using WeaponAttachments;
public class Gun : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int m_fireRate;
    [SerializeField] private int Penetration;
    [SerializeField] private float MuzzleVelocity;
    [SerializeField] private float Damage;
    [SerializeField] private float m_reloadTime;
    [SerializeField] private float m_hipAngle;
    [Header("Ammo")]
    [SerializeField] private int m_maxReserveAmmo;
    [Header("Recoil")]
    [SerializeField] private Vector3 m_camRecoil;
    [SerializeField] private Vector4 m_visualRecoil;
    [Header("Attachments")]
    [SerializeField] private Attachment[] m_attachments;
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
    public Transform m_lIkTarget;
    public Transform m_rIkTarget;
    [Header("Gun SFX")]
    [SerializeField] private AudioSource m_audioSource;
    [SerializeField] private AudioClip[] m_audioClips;

    private bool m_canShoot = true;
    private bool m_isReloading;
    private bool m_aimState;
    private int m_numShots = 0;
    private int m_currentReserveAmmo;
    private float m_shotDelay;
    private BulletPool m_bulletPool;
    private PoolManager m_pools;

    private Mag m_mag;
    private Sight m_sight;

    private int m_penetration;
    private float m_muzzleVelocity;
    private float m_damage;
    public int TargetLayer { private get; set; }
    private void Awake()
    {
        m_bulletPool = BulletPool.Instance();
        m_pools = PoolManager.Instance();
        m_currentReserveAmmo = m_maxReserveAmmo;
        m_shotDelay = 1f / (m_fireRate / 60f);
        Init();
    }
    private void Init()
    {
        int penetration = Penetration;
        float damage = Damage;
        float muzzleVelocity = MuzzleVelocity;
        foreach (Attachment a in m_attachments)
        {
            //a.gameObject.TryGetComponent<Mag>(out m_mag);
            //a.gameObject.TryGetComponent<Sight>(out m_sight);
            if (a.m_isMag) m_mag = a.GetComponent<Mag>();
            if (a.m_isSight) m_sight = a.GetComponent<Sight>();
            penetration *= a.m_penetrationMulti;
            damage *= a.m_damageMulti;
            muzzleVelocity *= a.m_muzzleVelocityMulti;
        }
        if (!m_mag) Debug.LogError(name + " has No Mag");
        if (!m_sight) Debug.LogError(name + " has No Sight");
        m_penetration = penetration;
        m_damage = damage;
        m_muzzleVelocity = muzzleVelocity;
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
        m_aimState = state;
        m_gunAnimation.RunAimState(m_aimState, m_sight);
        m_gunAnimation.AddLookSway(m_lookSwayX, m_lookSwayY);
        m_gunAnimation.AddIdleSway(m_idleFactor.x, m_idleFactor.y, m_idleSpeed);
        m_gunAnimation.RunReloadAnimation(m_isReloading, m_reloadTime);
    }
    public Vector3 Shoot(bool shootInput)
    {
        if (m_canShoot && !m_isReloading && m_mag && shootInput)
        {
            if (m_mag.m_currentAmmo > 0)
            {
                m_numShots++;
                UpdateBullet(m_bulletPool.GetBullet(m_bulletSpawn.position, m_bulletSpawn.rotation));
                m_mag.m_currentAmmo--;
                m_gunAnimation.AddRecoilImpulse(m_visualRecoil, m_numShots);
                m_audioSource.clip = m_audioClips[0];
                m_audioSource.Play();
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
    private void UpdateBullet(Bullet b)
    {
        b.m_targetLayer = TargetLayer;
        b.m_penetration = m_penetration;
        b.m_damage = m_damage;
        Vector3 direction = m_bulletSpawn.forward;
        if (!m_aimState)
        {
            direction = Quaternion.AngleAxis(Random.Range(-m_hipAngle, m_hipAngle), Vector3.up) * direction;
            direction = Quaternion.AngleAxis(Random.Range(-m_hipAngle, m_hipAngle), Vector3.right) * direction;
        }
        b.m_rb.linearVelocity = m_muzzleVelocity * direction;
    }
    public void Reload()
    {
        if (!m_isReloading && m_mag.m_currentAmmo < m_mag.m_capacity && m_currentReserveAmmo > 0)
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
        if (m_currentReserveAmmo < m_mag.m_capacity)
        {
            m_mag.m_currentAmmo = m_currentReserveAmmo;
            m_currentReserveAmmo = 0;
        }
        else
        {
            m_mag.m_currentAmmo = m_mag.m_capacity;
            m_currentReserveAmmo -= m_mag.m_capacity;
        }
        m_isReloading = false;
    }
    public int CurrentAmmo { get { return m_mag.m_currentAmmo; } }
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
    [SerializeField] private Animator m_animator;
    [SerializeField] private AnimationClip m_reloadClip;
    private bool m_state = false;
    private float m_t = 0f;
    private Vector3 m_lastForward;
    private Vector3 m_targetPosition = Vector3.zero;
    private Quaternion m_targetRotation = Quaternion.identity;
    public void RunAimState(bool state, Sight sight)
    {
        m_state = state;
        m_model.SetLocalPositionAndRotation(Vector3.Slerp(m_model.localPosition, (m_state ? m_aimPosition - m_model.InverseTransformPoint(sight.transform.position).y * Vector3.up : m_hipPosition) + m_targetPosition, m_aimSpeed * Time.deltaTime), m_targetRotation);
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
        m_targetRotation *= Quaternion.Euler(xAngle * swayFactorX * 0.1f, yAngle * swayFactorY, 0f);
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