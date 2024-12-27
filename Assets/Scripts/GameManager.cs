using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using MGUtilities;
public class GameManager : Singleton_template<GameManager>
{
    public bool m_spawnEnemies;
    public int m_onHitPoints;
    public int m_onKillPoints;
    [SerializeField] private GameObject m_weaponWheel;
    [SerializeField] public AnimationCurve m_airDensityCurve;
    [SerializeField] private EnemyManager m_enemyManager;

    [SerializeField] private TextMeshProUGUI m_PointsText;
    public int m_points;

    private Player m_player;
    private BulletPool3D m_bulletPool;
    private PoolManager m_poolManager;
    private void Start()
    {
        m_player = FindFirstObjectByType<Player>();
        m_bulletPool = BulletPool3D.Instance();
        m_poolManager = PoolManager.Instance();
        if (m_spawnEnemies) StartCoroutine(m_enemyManager.SpawnEnemy(5, m_poolManager));
    }
    void Update()
    {
        if (m_enemyManager.m_enemyList.Count <= 0) StartCoroutine(m_enemyManager.SpawnEnemy(5, m_poolManager));
        if (m_PointsText) m_PointsText.text = "$" + m_points;
        m_bulletPool.HandleBullets(m_poolManager, Instance());
        m_enemyManager.HandleEnemies(m_player.transform.position);
        HandleWeaponWheel();
    }
    private void HandleWeaponWheel()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            m_weaponWheel.SetActive(true);
            m_player.ToggleMouse();
        }
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            m_weaponWheel.SetActive(false);
            m_player.ToggleMouse();
        }
    }
    public bool DealDamageToEnemy(float damage, Collider c)
    {
        return m_enemyManager.TakeDamage(m_poolManager, damage, c);
    }
}
[System.Serializable]
public class EnemyManager
{
    [Header("Enemy Setup")]
    [SerializeField] private float m_timeBetweenSpawns;
    [SerializeField] private Vector2 m_maxHPRange;
    [SerializeField] private Vector2 m_damageRange;
    [SerializeField] private Vector2 m_speedRange;
    [SerializeField] private List<Transform> m_spawnPoints;
    [Header("Grounding")]
    [SerializeField] private float m_groundCheckDistance;
    [SerializeField] private float m_maxSlope;
    [SerializeField] private float m_maxFrictionMultiplier;
    [SerializeField] private PhysicsMaterial m_physicsMaterial;
    [SerializeField] private LayerMask m_groundMask;
    [Header("Effects")]

    public readonly Dictionary<Collider, Enemy> m_enemyList = new();
    public struct Enemy
    {
        public float m_currentHp;
        public float m_damage;
        public float m_speed;
        public Vector3 m_groundNormal;
        public Rigidbody m_rb;
        public Transform m_followTarget;
        public NavMeshAgent m_agent;
        public Slider m_hpSlider;
    }
    public bool TakeDamage(PoolManager poolManager, float damage, Collider c)
    {
        if (m_enemyList.TryGetValue(c, out Enemy e))
        {
            e.m_currentHp -= damage;
            if (e.m_currentHp <= 0)
            {
                poolManager.ReturnToPool("RBEnemy", e.m_rb.gameObject);
                poolManager.ReturnToPool("AgentEnemy", e.m_agent.gameObject);
                m_enemyList.Remove(c);
                return true;
            }
            else m_enemyList[c] = e;
        }
        else Debug.LogWarning("Collider does NOT exist in m_enemyList");
        return false;
    }
    public IEnumerator SpawnEnemy(int numToSpawn, PoolManager poolManager)
    {
        float t = 0f;
        int i = Random.Range(0, m_spawnPoints.Count - 1);
        while (m_enemyList.Count < numToSpawn)
        {
            if (t > m_timeBetweenSpawns)
            {
                Vector3 pos = m_spawnPoints[i].position;
                Enemy e = new()
                {
                    m_currentHp = Random.Range(m_maxHPRange.x, m_maxHPRange.y),
                    m_damage = Random.Range(m_damageRange.x, m_damageRange.y),
                    m_speed = Random.Range(m_speedRange.x, m_speedRange.y),
                    m_groundNormal = Vector3.up,
                    m_rb = poolManager.SpawnFromPool("RBEnemy", pos, Quaternion.identity).GetComponent<Rigidbody>(),
                    m_agent = poolManager.SpawnFromPool("AgentEnemy", pos, Quaternion.identity).GetComponent<NavMeshAgent>()
                };
                e.m_rb.AddForce(5f * Vector3.forward, ForceMode.Impulse);
                e.m_followTarget = e.m_agent.transform;
                e.m_hpSlider = e.m_rb.GetComponentInChildren<Slider>();
                e.m_hpSlider.maxValue = e.m_currentHp;
                m_enemyList.Add(e.m_rb.GetComponent<Collider>(), e);
                t = 0f;
                int temp = Random.Range(0, m_spawnPoints.Count - 1);
                if (temp != i) t += m_timeBetweenSpawns;
                i = temp;
            }
            t += Time.deltaTime;
            yield return null;
        }

    }
    public void HandleEnemies(Vector3 playerPos)
    {
        foreach (Enemy enemy in new List<Enemy>(m_enemyList.Values))
        {
            if (Vector3.Distance(enemy.m_rb.position, enemy.m_agent.transform.position) > 3f)
                enemy.m_agent.Warp(enemy.m_rb.position + enemy.m_rb.linearVelocity.normalized * 0.4f);
            CheckGround(enemy.m_rb.transform, enemy);
            enemy.m_agent.SetDestination(playerPos);
            enemy.m_hpSlider.value = enemy.m_currentHp;
            Vector3 dir = (enemy.m_followTarget.position - enemy.m_rb.position).normalized;
            Vector3 flatDir = Vector3.ProjectOnPlane(dir, enemy.m_groundNormal);
            if (flatDir != Vector3.zero) enemy.m_hpSlider.transform.GetChild(0).forward = flatDir;
            float speed = enemy.m_rb.linearVelocity.magnitude;
            enemy.m_agent.speed = speed;
            enemy.m_rb.rotation = enemy.m_agent.transform.rotation;
            Movement(enemy.m_speed, flatDir, enemy.m_groundNormal, enemy.m_rb);
        }
    }
    private void Movement(float speed, Vector3 flatDir, Vector3 groundNormal, Rigidbody rb)
    {
        Vector3 moveDirection = speed * flatDir;
        Vector3 currentVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, groundNormal);
        Vector3 moveForce = rb.mass * 3f * (moveDirection - currentVelocity);
        float theta = Vector3.Angle(groundNormal, Vector3.up);
        Vector3 frictionForce = (CalculateFrictionMultiplier(theta) * m_physicsMaterial.dynamicFriction * rb.mass * Physics.gravity.magnitude * Mathf.Cos(theta)) * moveForce.normalized;
        rb.AddForce(moveForce + frictionForce);
    }
    private float CalculateFrictionMultiplier(float theta)
    {
        theta = Mathf.Clamp(theta, 0f, m_maxSlope);
        return 1f + (theta / m_maxSlope) * (m_maxFrictionMultiplier - 1f);
    }
    private void CheckGround(Transform transform, Enemy e)
    {
        float angleStep = 360f / 8;
        List<Vector3> hits = new();
        /////
        for (int i = 0; i < 8; i++)
        {
            float angleInRadians = Mathf.Deg2Rad * angleStep * i;
            float x = Mathf.Cos(angleInRadians) * 0.5f;
            float z = Mathf.Sin(angleInRadians) * 0.5f;
            Vector3 origin = new(transform.position.x + x, transform.position.y + m_groundCheckDistance, transform.position.z + z);
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
            e.m_groundNormal = -cumulativeNormal;
        }
    }
}