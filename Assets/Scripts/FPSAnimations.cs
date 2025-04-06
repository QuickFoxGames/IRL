using UnityEngine;
public class FPSAnimations : MonoBehaviour
{
    [SerializeField] private Animator m_leftHand;
    [SerializeField] private Animator m_rightHand;
    void Start()
    {
        
    }
    public void Reload(bool state)
    {
        m_leftHand.SetBool("Reload", state);
        m_rightHand.SetBool("Reload", state);
    }
    public void OnPullTrigger()
    {
        m_leftHand.SetBool("PT", true);
        m_rightHand.SetBool("PT", true);
    }
    public void OnReleaseTrigger()
    {
        m_leftHand.SetBool("RT", true);
        m_rightHand.SetBool("RT", true);
    }
}