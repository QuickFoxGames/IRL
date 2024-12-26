using System.Collections.Generic;
using UnityEngine;
namespace MGAnimations
{
    public class FancyAnimations : MonoBehaviour
    {
        public AnimationClip animationClip; // Assign the AnimationClip in the inspector
        public Transform rootBone;          // The root bone of the character rig

        private float currentFrameTime;     // Time of the current frame
        private float frameDuration;        // Duration of a single frame
        private float animationLength;      // Total length of the animation
        private Dictionary<string, List<Transform>> boneGroups; // Isolated bone groups

        private void Start()
        {
            if (animationClip == null || rootBone == null)
            {
                Debug.LogError("AnimationClip or rootBone not set.");
                return;
            }

            // Initialize animation playback variables
            currentFrameTime = 0f;
            frameDuration = 1f / animationClip.frameRate;
            animationLength = animationClip.length;

            // Categorize bones into groups
            boneGroups = CategorizeBones(rootBone);
        }

        private void Update()
        {
            if (animationClip == null || boneGroups == null)
                return;

            // Play the animation frame-by-frame
            currentFrameTime += Time.deltaTime;

            if (currentFrameTime > animationLength)
            {
                currentFrameTime = 0f; // Loop the animation
            }

            PlayAnimationFrame(currentFrameTime);
        }

        private void PlayAnimationFrame(float time)
        {
            // Sample animation at the current time
            foreach (var group in boneGroups)
            {
                foreach (var bone in group.Value)
                {
                    // Apply sampled animation to each bone
                    animationClip.SampleAnimation(bone.gameObject, time);
                }
            }
        }

        private Dictionary<string, List<Transform>> CategorizeBones(Transform root)
        {
            var groups = new Dictionary<string, List<Transform>>
        {
            { "hips", new List<Transform>() },
            { "leftLeg", new List<Transform>() },
            { "rightLeg", new List<Transform>() },
            { "leftArm", new List<Transform>() },
            { "rightArm", new List<Transform>() },
            { "spine", new List<Transform>() },
            { "head", new List<Transform>() }
        };

            // Traverse the hierarchy and assign bones to groups
            foreach (Transform bone in root.GetComponentsInChildren<Transform>())
            {
                if (bone.name.ToLower().Contains("hip")) groups["hips"].Add(bone);
                else if (bone.name.ToLower().Contains("left") && 
                    bone.name.ToLower().Contains("leg")) groups["leftLeg"].Add(bone);
                else if (bone.name.ToLower().Contains("right") &&
                    bone.name.ToLower().Contains("leg")) groups["rightLeg"].Add(bone);
                else if (bone.name.ToLower().Contains("left") &&
                    bone.name.ToLower().Contains("arm")) groups["leftArm"].Add(bone);
                else if (bone.name.ToLower().Contains("right") &&
                    bone.name.ToLower().Contains("arm")) groups["rightArm"].Add(bone);
                else if (bone.name.ToLower().Contains("spine")) groups["spine"].Add(bone);
                else if (bone.name.ToLower().Contains("head") || bone.name.ToLower().Contains("neck")) groups["head"].Add(bone);
            }

            return groups;
        }
    }
    public struct FancyAnimationClip
    {
        public FancyBone[] m_hipBones;
        public FancyBone[] m_leftLegBones;
        public FancyBone[] m_rightLegBones;
        public FancyBone[] m_leftArmBones;
        public FancyBone[] m_rightArmBones;
        public FancyBone[] m_spineBones;
        public FancyBone[] m_headBones;
    }
#nullable enable
    public struct FancyBone
    {
        public Transform m_transform;
        public Quaternion[] m_rotations;
        public Vector3[]? m_positions;
        public int currentFrame;
    }
    public static class FancyUtilities
    {
        public static FancyAnimationClip ConvertToFancyAnimationClip(AnimationClip unityClip, Transform rootBone, float sampleRate = 30f)
        {
            // Validate input
            if (unityClip == null || rootBone == null)
            {
                Debug.LogError("Invalid AnimationClip or root bone");
                return default;
            }

            var fancyClip = new FancyAnimationClip
            {
                m_hipBones = ExtractBones(unityClip, rootBone, sampleRate, includePosition: true),
                m_leftLegBones = ExtractBones(unityClip, rootBone.Find("LeftLeg"), sampleRate),
                m_rightLegBones = ExtractBones(unityClip, rootBone.Find("RightLeg"), sampleRate),
                m_leftArmBones = ExtractBones(unityClip, rootBone.Find("LeftArm"), sampleRate),
                m_rightArmBones = ExtractBones(unityClip, rootBone.Find("RightArm"), sampleRate),
                m_spineBones = ExtractBones(unityClip, rootBone.Find("Spine"), sampleRate),
                m_headBones = ExtractBones(unityClip, rootBone.Find("Head"), sampleRate)
            };

            return fancyClip;
        }
        private static FancyBone[] ExtractBones(AnimationClip unityClip, Transform bone, float sampleRate, bool includePosition = false)
        {
            if (bone == null)
                return new FancyBone[0];

            List<FancyBone> bones = new();
            var boneTransforms = bone.GetComponentsInChildren<Transform>();

            foreach (var t in boneTransforms)
            {
                var fancyBone = new FancyBone
                {
                    m_transform = t,
                    m_rotations = SampleRotations(unityClip, t, sampleRate),
                    m_positions = includePosition ? SamplePositions(unityClip, t, sampleRate) : null,
                    currentFrame = 0
                };
                bones.Add(fancyBone);
            }

            return bones.ToArray();
        }
        private static Quaternion[] SampleRotations(AnimationClip clip, Transform bone, float sampleRate)
        {
            List<Quaternion> rotations = new();
            float clipLength = clip.length;
            for (float t = 0; t <= clipLength; t += 1f / sampleRate)
            {
                clip.SampleAnimation(bone.gameObject, t);
                rotations.Add(bone.localRotation);
            }
            return rotations.ToArray();
        }
        private static Vector3[] SamplePositions(AnimationClip clip, Transform bone, float sampleRate)
        {
            List<Vector3> positions = new();
            float clipLength = clip.length;
            for (float t = 0; t <= clipLength; t += 1f / sampleRate)
            {
                clip.SampleAnimation(bone.gameObject, t);
                positions.Add(bone.localPosition);
            }
            return positions.ToArray();
        }
    }
}