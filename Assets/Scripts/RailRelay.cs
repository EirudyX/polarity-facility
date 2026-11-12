using System;
using UnityEngine;

namespace PolarityFacility
{
    public sealed class RailRelay : MonoBehaviour
    {
        [Serializable]
        private struct Rail
        {
            [SerializeField] private Transform start, end;
            [SerializeField, Min(.1f)] private float range;
            [SerializeField, Min(0f)] private float strength;

            internal Vector3 ForceAt(Vector3 position, bool samePole)
            {
                Vector3 span = MagnetRules.Planar(end.position - start.position);
                float lengthSquared = span.sqrMagnitude;
                if (lengthSquared < .0001f) return Vector3.zero;
                Vector3 offset = MagnetRules.Planar(position - start.position);
                float along = Mathf.Clamp01(Vector3.Dot(offset, span) / lengthSquared);
                Vector3 inward = span * along - offset;
                float distance = inward.magnitude;
                if (distance < .01f || distance >= range) return Vector3.zero;

                float force = strength * (1f - distance / range);
                return inward * ((samePole ? -force : force) / distance);
            }
        }

        [SerializeField] private Rail guide, reversingRail;
        [SerializeField] private Transform controlPad;
        [SerializeField] private Renderer controlLamp, railLamp;
        [SerializeField] private Material idle, live, north, south;
        [SerializeField] private Rigidbody shutter;
        [SerializeField, Min(.1f)] private float shutterTravel = 2.3f, shutterSpeed = 5f;
        [SerializeField, Min(.1f)] private float exitRadius = 1.24f;

        private Vector3 closedPosition;

        internal Transform ControlPad => controlPad;
        internal float ExitRadius => exitRadius;
        internal bool Reversed { get; private set; }

        internal void Initialize() => closedPosition = shutter.position;

        internal void ResetRelay()
        {
            SetReversed(false);
            shutter.position = closedPosition;
        }

        internal void Step(bool held, bool receiverReached, float dt)
        {
            bool reverse = held || receiverReached;
            if (reverse != Reversed) SetReversed(reverse);
            Vector3 target = closedPosition + Vector3.up * (reverse ? shutterTravel : 0f);
            shutter.MovePosition(Vector3.MoveTowards(shutter.position, target, shutterSpeed * dt));
        }

        internal Vector3 ForceAt(Vector3 position, bool ballNorth)
        {
            return guide.ForceAt(position, ballNorth)
                + reversingRail.ForceAt(position, ballNorth != Reversed);
        }

        private void SetReversed(bool reverse)
        {
            Reversed = reverse;
            controlLamp.sharedMaterial = reverse ? live : idle;
            railLamp.sharedMaterial = reverse ? south : north;
        }
    }
}
