using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIWheel : MonoBehaviour
{
    [SerializeField] private Texture m_defaultTexture;
    [SerializeField] private Texture m_highlightTexture;
    [SerializeField] private float radius;
    [SerializeField] private float angleOffset = 0f;
    [SerializeField] private List<Slice> slices;
    private Vector2 center;
    private Slice currentHighlighted;

    [System.Serializable]
    public class Slice
    {
        public RawImage image;
        public UnityEvent onClick;   // UnityEvent for specifying actions via Inspector
        [HideInInspector] public float startAngle;
        [HideInInspector] public float endAngle;
    }

    void Start()
    {
        if (center == Vector2.zero)
            center = new Vector2(Screen.width / 2, Screen.height / 2);
        CalculateSliceAngles();
    }

    void Update()
    {
        CheckCircularSlices();

        // Detect click on the current highlighted slice
        if (currentHighlighted != null && Input.GetMouseButtonDown(0))
        {
            currentHighlighted.onClick.Invoke(); // Invoke the associated method when clicked
        }
    }

    private void CalculateSliceAngles()
    {
        int sliceCount = slices.Count;
        float anglePerSlice = 360f / sliceCount;

        for (int i = 0; i < sliceCount; i++)
        {
            slices[i].startAngle = (i * anglePerSlice + angleOffset) % 360;
            slices[i].endAngle = ((i + 1) * anglePerSlice + angleOffset) % 360;
        }
    }

    private void CheckCircularSlices()
    {
        Vector2 mousePos = Input.mousePosition;
        bool foundSlice = false;

        foreach (Slice slice in slices)
        {
            if (IsMouseInCircularSlice(mousePos, slice.startAngle, slice.endAngle))
            {
                foundSlice = true;

                if (currentHighlighted != slice)
                {
                    ResetCurrentHighlighted();

                    slice.image.texture = m_highlightTexture;
                    currentHighlighted = slice;
                }
                return;
            }
        }

        if (!foundSlice)
            ResetCurrentHighlighted();
    }

    private bool IsMouseInCircularSlice(Vector2 mousePos, float startAngle, float endAngle)
    {
        Vector2 dir = mousePos - center;

        if (dir.magnitude > radius)
            return false;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (angle < 0)
            angle += 360;

        return IsAngleWithinRange(angle, startAngle, endAngle);
    }

    private bool IsAngleWithinRange(float angle, float startAngle, float endAngle)
    {
        if (startAngle <= endAngle)
            return angle >= startAngle && angle <= endAngle;
        else
            return angle >= startAngle || angle <= endAngle;
    }

    private void ResetCurrentHighlighted()
    {
        if (currentHighlighted != null)
        {
            currentHighlighted.image.texture = m_defaultTexture;
            currentHighlighted = null;
        }
    }
}
