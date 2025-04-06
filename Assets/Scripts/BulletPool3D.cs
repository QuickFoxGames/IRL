using System.Collections.Generic;
using TMPro;
using UnityEngine;
using MGUtilities;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
namespace MGUtilities
{
    public class BulletPool3D : Singleton_template<BulletPool3D>
    {
        [SerializeField] private int m_initialBulletCount;
        [SerializeField] private float m_flightTime;
        [SerializeField] private Rigidbody m_bulletPrefab;
        [SerializeField] private Transform m_bulletHolder;
        [SerializeField] private Material[] m_hitMaterials;

        private List<Bullet> m_bullets = new();
        private List<Bullet> m_activeBullets = new();
        private Ray m_bulletRay = new();
        public void HandleBullets(PoolManager poolManager, GameManager gameManager)
        {
            if (m_bullets.Count <= m_initialBulletCount)
            {
                AddBulletToPool();
            }
            foreach (Bullet bullet in new List<Bullet>(m_activeBullets))
            {
                Vector3 dir = bullet.m_rb.position - bullet.m_lastPosition;
                if (dir == Vector3.zero) continue;
                bullet.m_rb.transform.forward = dir.normalized;
                if (bullet.m_numCollisionChecks < 1)
                {
                    var cam = Camera.main.transform;
                    m_bulletRay.origin = cam.position;
                    m_bulletRay.direction = cam.forward;
                    HandleHits(poolManager, gameManager, bullet, 1f);
                    m_bulletRay.origin = bullet.m_lastPosition;
                    m_bulletRay.direction = dir.normalized;
                    HandleHits(poolManager, gameManager, bullet, dir.magnitude);
                }
                else
                {
                    bullet.m_trailRenderer.enabled = true;
                    m_bulletRay.origin = bullet.m_lastPosition;
                    m_bulletRay.direction = dir.normalized;
                    HandleHits(poolManager, gameManager, bullet, dir.magnitude);
                }
                bullet.m_numCollisionChecks++;
                bullet.m_elapsedTime += Time.deltaTime;
                if (bullet.m_elapsedTime >= m_flightTime) ReturnBullet(bullet);
                bullet.m_lastPosition = bullet.m_rb.position;
            }
        }
        private void HandleHits(PoolManager poolManager, GameManager gameManager, Bullet bullet, float distance)
        {
            RaycastHit[] hits = new RaycastHit[10];// = Physics.RaycastAll(bullet.m_lastPosition, dir.normalized, dir.magnitude);
            if (Physics.RaycastNonAlloc(m_bulletRay, hits, distance, bullet.m_targetLayer) > 0)
            {
                System.Array.Sort(hits, (a, b) => // sort for which was hit first
                {
                    float distanceA = Vector3.Distance(bullet.m_lastPosition, a.point);
                    float distanceB = Vector3.Distance(bullet.m_lastPosition, b.point);

                    return distanceA.CompareTo(distanceB); // Sort by distance in ascending order
                });
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (bullet.m_lastHits.Contains(hit.collider)) continue;
                    else bullet.m_lastHits.Add(hit.collider);
                    // collision hit logic goes here
                    if (hit.collider.gameObject.layer == 10)
                    {
                        bool died = gameManager.DealDamageToEnemy(bullet.m_damage, hit.collider);

                        var hitMarker = poolManager.SpawnFromPool("HitMarker", Vector3.zero, Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f)));

                        if (died) hitMarker.GetComponent<Image>().color = Color.red;
                        else hitMarker.GetComponent<Image>().color = Color.white;

                        hitMarker.transform.localPosition = Vector3.zero;
                        StartCoroutine(Coroutines.PingPongVector3OverTime(0.9f * Vector3.one, 2f * Vector3.one, 0.125f, value => hitMarker.transform.localScale = value));
                        poolManager.ReturnToPoolDelayed("HitMarker", hitMarker, 0.126f);

                        var pointsIndicator = poolManager.SpawnFromPool("PointsIndicator", Camera.main.WorldToScreenPoint(hit.point), Quaternion.identity);
                        int pointsToAdd = died ? gameManager.m_onKillPoints : gameManager.m_onHitPoints;
                        gameManager.m_points += pointsToAdd;
                        pointsIndicator.GetComponent<TextMeshProUGUI>().text = pointsToAdd.ToString();
                        pointsIndicator.GetComponent<Rigidbody2D>().AddForce(300f * Vector3.up, ForceMode2D.Impulse);
                        StartCoroutine(Coroutines.LerpVector3ToZeroOverTime(true, 0.5f, value => pointsIndicator.transform.localScale = value));
                        poolManager.ReturnToPoolDelayed("PointsIndicator", pointsIndicator, 0.55f);
                        hit.collider.gameObject.GetComponent<Rigidbody>().AddForceAtPosition(bullet.m_rb.linearVelocity.magnitude * -bullet.m_rb.mass * hit.normal, hit.point);
                    }
                    else if (hit.collider.gameObject.layer == 11)
                    {
                        var hitMarker = poolManager.SpawnFromPool("HitMarker", Vector3.zero, Quaternion.Euler(0f, 0f, Random.Range(-15f, 15f)));
                        hitMarker.transform.localPosition = Vector3.zero;
                        StartCoroutine(Coroutines.PingPongVector3OverTime(0.9f * Vector3.one, 2f * Vector3.one, 0.125f, value => hitMarker.transform.localScale = value));
                        poolManager.ReturnToPoolDelayed("HitMarker", hitMarker, 0.126f);
                    }
                    if (hit.normal != Vector3.zero)
                    {
                        var bulletHit = poolManager.SpawnFromPool("BulletHit", hit.point, Quaternion.LookRotation(-hit.normal));
                        if (hit.transform.CompareTag("Blood")) bulletHit.GetComponent<DecalProjector>().material = m_hitMaterials[1];
                        else if (hit.transform.CompareTag("Glass")) bulletHit.GetComponent<DecalProjector>().material = m_hitMaterials[2];
                        else bulletHit.GetComponent<DecalProjector>().material = m_hitMaterials[0];
                        bulletHit.transform.SetParent(hit.transform);
                        bulletHit.transform.localRotation *= Quaternion.Euler(0f, 0f, Random.Range(-360f, 360f));
                        poolManager.ReturnToPoolDelayed("BulletHit", bulletHit, 8f);
                    }
                    bullet.m_penetration--;
                    bullet.m_damage *= 0.5f;
                    bullet.m_rb.linearVelocity *= 0.8f;
                    if (bullet.m_penetration < 0)
                    {
                        ReturnBullet(bullet);
                        break;
                    }
                }
            }
        }
        private Bullet AddBulletToPool()
        {
            var b = new Bullet(Instantiate(m_bulletPrefab, m_bulletHolder));
            m_bullets.Add(b);
            b.m_rb.gameObject.SetActive(false);
            return b;
        }
        public Bullet GetBullet()
        {
            Bullet b;
            if (m_bullets.Count == 0) b = AddBulletToPool();
            else b = m_bullets[0];
            m_bullets.Remove(b);
            m_activeBullets.Add(b);
            b.m_rb.gameObject.SetActive(true);
            return b;
        }
        public Bullet GetBullet(Vector3 pos, Quaternion rot)
        {
            Bullet b;
            if (m_bullets.Count == 0) b = AddBulletToPool();
            else b = m_bullets[0];
            m_bullets.Remove(b);
            b.m_rb.transform.SetPositionAndRotation(pos, rot);
            b.m_lastPosition = pos;
            m_activeBullets.Add(b);
            b.m_rb.gameObject.SetActive(true);
            return b;
        }
        public void ReturnBullet(Bullet b)
        {
            b.ResetBullet();
            m_activeBullets.Remove(b);
            m_bullets.Add(b);
        }
    }
    public class Bullet
    {
        public int m_targetLayer;
        public int m_numCollisionChecks;
        public int m_penetration;
        public float m_elapsedTime;
        public float m_damage;
        public Vector3 m_lastPosition;
        public Rigidbody m_rb;
        public List<Collider> m_lastHits;
        public TrailRenderer m_trailRenderer;
        public Bullet(Rigidbody rb)
        {
            m_elapsedTime = 0f;
            m_damage = 0f;
            m_penetration = 0;
            m_lastPosition = Vector3.zero;
            m_rb = rb;
            m_numCollisionChecks = 0;
            m_lastHits = new();
            m_targetLayer = 100;
            m_trailRenderer = m_rb.GetComponent<TrailRenderer>();
            m_trailRenderer.enabled = false;
        }
        public void ResetBullet()
        {
            m_rb.gameObject.SetActive(false);
            m_trailRenderer.enabled = false;
            m_elapsedTime = 0f;
            m_damage = 0f;
            m_penetration = 0;
            m_lastPosition = Vector3.zero;
            m_numCollisionChecks = 0;
            m_lastHits.Clear();
            m_targetLayer = 100;
        }
    }
}