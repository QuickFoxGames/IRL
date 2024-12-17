/*using UnityEngine;
using UnityEngine.Animations.Rigging;

public class FPSTEST : MonoBehaviour
{
    [SerializeField] private ChainIKConstraint m_hips;
    [SerializeField] private ChainIKConstraint m_neck;
    [SerializeField] private Transform m_head;
    [SerializeField] private Transform m_cam;
    [SerializeField] private Transform m_lLegTarget;
    [SerializeField] private Transform m_lHandTarget;
    [SerializeField] private Transform m_rHandTarget;
    [SerializeField] private Transform m_lHandGunTarget;
    [SerializeField] private Transform m_rHandGunTarget;
    [SerializeField] private Transform[] m_lookTargets;
    [SerializeField] private Transform m_target;
    [Space]
    public LayerMask groundLayer;       // Layer mask for detecting the ground
    public float footOffset = 0.1f;     // Vertical offset to prevent clipping
    public float raycastDistance = 1.5f; // How far to cast the ray
    public float stepSmoothness = 5f;   // Smoothness for blending IK positions

    Player m_player;
    void Start()
    {
        m_player = FindFirstObjectByType<Player>();
    }
    void Update()
    {
        //float map = MapRangeTo01(m_player.camRotation.x, -70f, 80f); // Assuming MapRange maps to [0, 1]

        // Calculate the segment count and normalized value
        int segmentCount = m_lookTargets.Length - 1;
        float normalizedValue = Mathf.Clamp01(map); // Ensure it's in range [0, 1]

        // Determine the current segment based on the normalized value
        int currentSegment = Mathf.Min((int)(normalizedValue * segmentCount), segmentCount - 1);

        // Identify the start and end transforms for interpolation
        Transform startTrans = m_lookTargets[currentSegment];
        int next = Mathf.Min(currentSegment + 1, segmentCount); // Clamp next segment index
        Transform endTrans = m_lookTargets[next];

        // Calculate the interpolation factor (segT) within the current segment
        float segT = (normalizedValue * segmentCount) - currentSegment;

        // Slerp position and rotation between the start and end transforms
        m_target.SetLocalPositionAndRotation(
            Vector3.Slerp(startTrans.localPosition, endTrans.localPosition, segT),
            Quaternion.Slerp(startTrans.localRotation, endTrans.localRotation, segT)
        );

        m_neck.data.target = m_target;
        m_lHandTarget.SetPositionAndRotation(m_lHandGunTarget.position, m_lHandGunTarget.rotation);
        m_rHandTarget.SetPositionAndRotation(m_rHandGunTarget.position, m_rHandGunTarget.rotation);

        m_cam.SetPositionAndRotation(m_head.position, m_head.rotation);
    }
}*/