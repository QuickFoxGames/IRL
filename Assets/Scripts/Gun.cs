using MGUtilities;
using System.Collections;
using UnityEngine;
using WeaponAttachments;
public class Gun : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private bool m_semiAuto;
    [SerializeField] private bool m_burst;
    [SerializeField] private int m_burstCount;
    [SerializeField] private float m_fullAutoBurstDelayMulti;
    [SerializeField] private bool m_multiShot;
    [SerializeField] private int m_multiShotCount;
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
    private int m_currentReserveAmmo;
    private int m_currentNumShots = 0;
    private float m_shotDelay;

    private BulletPool3D m_bulletPool;
    private PoolManager m_pools;
    private GameManager m_gameManager;

    private Mag m_mag;
    private Sight m_sight;

    private int m_penetration;
    private float m_muzzleVelocity;
    private float m_damage;
    public int TargetLayer { private get; set; }
    private void Awake()
    {
        m_bulletPool = BulletPool3D.Instance();
        m_pools = PoolManager.Instance();
        m_gameManager = GameManager.Instance();
        m_currentReserveAmmo = m_maxReserveAmmo;
        m_shotDelay = 1f / (m_fireRate / 60f);
    }
    public void Init()
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
    public void Enable()
    {
        m_gunAnimation.EnableAnimator();
        gameObject.SetActive(true);
    }
    public void UpdateAnimations(bool state)
    {
        m_aimState = state;
        m_gunAnimation.RunAimState(m_aimState, m_sight);
        m_gunAnimation.AddLookSway(m_lookSwayY);
        m_gunAnimation.AddIdleSway(m_idleFactor.x, m_idleFactor.y, m_idleSpeed);
        m_gunAnimation.RunReloadAnimation(m_isReloading, m_reloadTime);
    }
    public Vector3 Shoot(bool shootInput, bool inputReleased)
    {
        Vector3 vec = Vector3.zero;

        if (!m_isReloading && m_mag)
        {
            if (m_mag.m_currentAmmo > 0)
            {
                if (inputReleased)
                    m_canShoot = true;
                if (m_canShoot)
                {
                    if (m_burst && (m_currentNumShots > 0 || shootInput))
                        BurstMode(ref vec);
                    else if (m_multiShot && shootInput)
                        MultiShotMode(ref vec);
                    else if (shootInput)
                    {
                        vec = FireBullet(m_shotDelay);
                        SpawnVFX();
                        m_mag.m_currentAmmo--;
                    }
                }
            }
            else m_gameManager.StartCoroutine(ReloadAmmo());
        }
        else return Vector3.zero;

        return vec;
    }
    private void BurstMode(ref Vector3 vec)
    {
        m_currentNumShots++;
        if (m_currentNumShots <= m_burstCount)
        {
            float t = m_currentNumShots == m_burstCount ? m_shotDelay * m_fullAutoBurstDelayMulti : m_shotDelay;
            vec = FireBullet(t);
            SpawnVFX();
            m_mag.m_currentAmmo--;
        }
        else m_currentNumShots = 0;
    }
    private void MultiShotMode(ref Vector3 vec)
    {
        for (int i = 0; i < m_multiShotCount; i++)
        {
            vec += FireBullet(m_shotDelay);
        }
        SpawnVFX();
        vec = vec.normalized * (vec.magnitude / m_multiShotCount);
        m_mag.m_currentAmmo--;
    }
    private Vector3 FireBullet(float t)
    {
        UpdateBullet(m_bulletPool.GetBullet(m_bulletSpawn.position, m_bulletSpawn.rotation));
        m_gunAnimation.AddRecoilImpulse(m_visualRecoil);
        if ((m_semiAuto && m_burst && m_currentNumShots < m_burstCount) || !m_semiAuto) m_gameManager.StartCoroutine(DelayShot(t));
        else
        {
            m_canShoot = false;
            m_currentNumShots = 0;
        }
        // play audio //
        m_audioSource.clip = m_audioClips[0];
        m_audioSource.Play();
        return new(m_camRecoil.x, m_camRecoil.y, m_camRecoil.z);
    }
    private void SpawnVFX()
    {
        var flash = m_pools.SpawnFromPool("MuzzleFlash", m_muzzleFlashSpawn);
        flash.transform.GetChild(0).localScale = m_muzzleFlashScale;
        flash.transform.GetChild(1).localScale = m_muzzleFlashScale;
        if (m_useSparks) flash.transform.GetChild(2).gameObject.SetActive(true);
        else flash.transform.GetChild(2).gameObject.SetActive(false);
        m_pools.ReturnToPoolDelayed("MuzzleFlash", flash, 2.1f);
    }
    private void UpdateBullet(Bullet b)
    {
        b.m_targetLayer = TargetLayer;
        b.m_penetration = m_penetration;
        b.m_damage = m_damage;
        Vector3 direction = m_bulletSpawn.forward;
        if (!m_aimState || m_multiShot)
        {
            float angle = m_hipAngle * (m_aimState ? 0.5f : 1f); // if aiming reduce the angle by 50%
            direction = Quaternion.AngleAxis(Random.Range(-angle, angle), m_bulletSpawn.up) * direction; // random rotation around the local y axis
            direction = Quaternion.AngleAxis(Random.Range(-angle, angle), m_bulletSpawn.right) * direction; // random rotation around the local x axiz
        }
        b.m_rb.linearVelocity = m_muzzleVelocity * direction.normalized;
    }
    public void Reload()
    {
        if (!m_isReloading && m_mag.m_currentAmmo < m_mag.m_capacity && m_currentReserveAmmo > 0)
            m_gameManager.StartCoroutine(ReloadAmmo());
    }
    private IEnumerator DelayShot(float t)
    {
        if (!m_burst) m_currentNumShots = 0;
        m_canShoot = false;
        yield return new WaitForSeconds(t);
        m_canShoot = true;
    }
    private IEnumerator ReloadAmmo()
    {
        m_isReloading = true;
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
        m_canShoot = true;
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
    [SerializeField] public Animator m_animator;
    [SerializeField] private AnimationClip m_reloadClip;
    private bool m_state = false;
    private float m_t = 0f;
    private Vector3 m_lastForward;
    private Vector3 m_targetPosition = Vector3.zero;
    private Quaternion m_targetRotation = Quaternion.identity;

    private Transform[] m_bones;
    private Vector3[] m_defBonePositions;
    private Quaternion[] m_defBoneRotations;
    public void Init()
    {
        if (m_animator == null) return;
        m_bones = m_animator.GetComponentsInChildren<Transform>();
        m_defBonePositions = new Vector3[m_bones.Length];
        m_defBoneRotations = new Quaternion[m_bones.Length];
        for (int i = 0; i < m_bones.Length; i++)
        {
            m_defBonePositions[i] = m_bones[i].localPosition;
            m_defBoneRotations[i] = m_bones[i].localRotation;
        }
    }
    public void EnableAnimator()
    {
        if (m_animator == null) return;
        for (int i = 0; i < m_bones.Length; i++)
        {
            m_bones[i].SetLocalPositionAndRotation(m_defBonePositions[i], m_defBoneRotations[i]);
        }
    }
    public void RunAimState(bool state, Sight sight)
    {
        m_state = state;
        m_model.SetLocalPositionAndRotation(Vector3.Slerp(m_model.localPosition, (m_state ? m_aimPosition - m_model.InverseTransformPoint(sight.transform.position).y * Vector3.up : m_hipPosition) + m_targetPosition, m_aimSpeed * Time.deltaTime), m_targetRotation);
        m_targetPosition = Vector3.Lerp(m_targetPosition, Vector3.zero, 3f * m_recoilSpeed * Time.deltaTime);
        m_targetRotation = Quaternion.Slerp(m_targetRotation, Quaternion.identity, m_recoilSpeed * Time.deltaTime);
    }
    public void AddRecoilImpulse(Vector4 visualRecoil)
    {
        visualRecoil *= 0.1f;
        m_targetPosition -= (m_state ? 0.75f : 1f) * visualRecoil.w * Vector3.forward;
        m_targetRotation *= Quaternion.Euler(visualRecoil.x, visualRecoil.y, visualRecoil.z);
    }
    public void AddLookSway(float swayFactorY)
    {
        float yAngle = Vector3.SignedAngle(m_lastForward, m_model.parent.parent.forward, Vector3.up);
        m_targetRotation *= Quaternion.Euler(0f, yAngle * swayFactorY, 0f);
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