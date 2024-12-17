using UnityEngine;
public class HealthSystem : MonoBehaviour
{
    [SerializeField] private float m_maxHp;
    [SerializeField] private float m_hpRegenRate;
    public float CurrentHp { get; private set; }
    public void Init()
    {
        CurrentHp = m_maxHp;
    }
    public void RegenHp()
    {
        if (CurrentHp <= 0f) return;
        CurrentHp += m_hpRegenRate * Time.deltaTime;
    }
    public void TakeDamage(float d)
    {
        if (CurrentHp <= 0f) return;
        CurrentHp -= d;
        if (CurrentHp <= 0f) Die();
    }
    private void Die()
    {
        Debug.Log(name + " died");
    }
}