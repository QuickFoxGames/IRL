using System;
using System.Collections;
using UnityEngine;
namespace MGUtilities
{
    [System.Serializable]
    public class FloatTransformKeyValuePair
    {
        public float Key;
        public Transform Value;

        public FloatTransformKeyValuePair(float key, Transform value)
        {
            Key = key;
            Value = value;
        }
    }
    public class Coroutines
    {
        #region Vector3
        public static IEnumerator LerpVector3OverTime(Vector3 start, Vector3 end, float duration, Action<Vector3> onUpdate)
        {
            float timePassed = 0f;

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector3 currentValue = Vector3.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        public static IEnumerator LerpVector3ToZeroOverTime(bool state, float duration, Action<Vector3> onUpdate)
        {
            float timePassed = 0f;
            Vector3 start = state ? Vector3.zero : Vector3.one;
            Vector3 end = state ? Vector3.one : Vector3.zero;
            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector3 currentValue = Vector3.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        public static IEnumerator LerpVector3ToZeroOverTime(bool state, float duration, Vector3 currentScale, Action<Vector3> onUpdate)
        {
            float timePassed = 0f;
            Vector3 start = currentScale; // Start from the current scale
            Vector3 end = state ? Vector3.one : Vector3.zero; // Target scale

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector3 currentValue = Vector3.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end); // Ensure it reaches the exact target
        }

        public static IEnumerator PingPongVector3OverTime(Vector3 start, Vector3 end, float duration, Action<Vector3> onUpdate)
        {
            float timePassed = 0f;
            float d = 0.5f * duration;
            while (timePassed < d)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector3 currentValue = Vector3.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            timePassed = 0f;
            while (timePassed < d)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector3 currentValue = Vector3.Lerp(end, start, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(start);
        }
        public static IEnumerator PingPongVector3OverTime(Vector3 start, Vector3 end, float duration1, float duration2, Action<Vector3> onUpdate)
        {
            float timePassed = 0f;
            while (timePassed < duration1)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration1);
                Vector3 currentValue = Vector3.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            timePassed = 0f;
            while (timePassed < duration2)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration2);
                Vector3 currentValue = Vector3.Lerp(end, start, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(start);
        }
        #endregion
        #region Vector2
        public static IEnumerator LerpVector2OverTime(Vector2 start, Vector2 end, float duration, Action<Vector2> onUpdate)
        {
            float timePassed = 0f;

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector2 currentValue = Vector2.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        public static IEnumerator LerpVector2OverTime(bool state, float duration, Action<Vector2> onUpdate)
        {
            float timePassed = 0f;
            Vector2 start = state ? Vector2.zero : Vector2.one;
            Vector2 end = state ? Vector2.one : Vector2.zero;
            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector2 currentValue = Vector2.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        public static IEnumerator PingPongVector2OverTime(Vector2 start, Vector2 end, float duration, Action<Vector2> onUpdate)
        {
            float timePassed = 0f;
            float d = 0.5f * duration;
            while (timePassed < d)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector2 currentValue = Vector2.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            timePassed = 0f;
            while (timePassed < d)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Vector2 currentValue = Vector2.Lerp(end, start, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(start);
        }
        public static IEnumerator PingPongVector2OverTime(Vector2 start, Vector2 end, float duration1, float duration2, Action<Vector2> onUpdate)
        {
            float timePassed = 0f;
            while (timePassed < duration1)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration1);
                Vector2 currentValue = Vector2.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            timePassed = 0f;
            while (timePassed < duration2)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration2);
                Vector2 currentValue = Vector2.Lerp(end, start, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(start);
        }
        #endregion
        #region Float
        public static IEnumerator LerpFloatOverTime(float start, float end, float duration, Action<float> onUpdate)
        {
            float timePassed = 0f;

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                float currentValue = Mathf.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        #endregion
        #region Color
        public static IEnumerator LerpColorOverTime(Color start, Color end, float duration, Action<Color> onUpdate)
        {
            float timePassed = 0f;

            while (timePassed < duration)
            {
                timePassed += Time.deltaTime;
                float lerpFactor = Mathf.Clamp01(timePassed / duration);
                Color currentValue = Color.Lerp(start, end, lerpFactor);

                onUpdate?.Invoke(currentValue);

                yield return null;
            }
            onUpdate?.Invoke(end);
        }
        #endregion
        public static IEnumerator DelayBoolChange(bool startState, bool endState, float waitTime, Action<bool> onUpdate)
        {
            onUpdate?.Invoke(startState);
            yield return new WaitForSeconds(waitTime);
            onUpdate?.Invoke(endState);
        }
    }
}