using UnityEngine;

namespace PolarityFacility
{
    public static class MagnetRules
    {
        public const float Radius = .48f;
        public const float Range = 8.5f;
        public const float LaunchSpeed = 10.8f;
        public const float SpeedLimit = 13f;

        // Positive pulls toward the other pole. Negative pushes away.
        public static float PairForce(bool same, float distance)
        {
            float force = Mathf.Clamp(9f / (distance * distance + .55f), 0, 7f);
            return same ? -force : force;
        }

        public static Vector3 Planar(Vector3 value) => new Vector3(value.x, 0, value.z);
        public static float Distance(Vector3 a, Vector3 b) => Planar(a - b).magnitude;

        public static Vector3 LimitOutwardDrive(Vector3 input, Vector3 towardPartner, float distance)
        {
            float radial = Vector3.Dot(input, towardPartner);
            float strength = Mathf.InverseLerp(Range - 1.4f, Range, distance);
            return input - towardPartner * Mathf.Min(0, radial) * strength;
        }

        public static bool CanLatch(bool same, float distance, float heightDelta, float cooldown)
            => !same && distance <= .995f && heightDelta < .4f && cooldown <= 0;

        public static bool IsRelease(bool latched, bool afterSame) => latched && afterSame;
        public static bool TwoSockets(bool aLeft, bool aRight, bool bLeft, bool bRight)
            => (aLeft && bRight) || (bLeft && aRight);
    }
}