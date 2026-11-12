using UnityEngine;

namespace PolarityFacility
{
    public sealed class MassTransferStation : MonoBehaviour
    {
        [SerializeField] private Transform nearPad, farPad;
        [SerializeField] private Renderer nearLamp, farLamp, conduitLamp;
        [SerializeField] private Material idle, live;
        [SerializeField] private GameObject ballastA, ballastB;
        [SerializeField, Min(.01f)] private float lightMass = 1f, ballastMass = 2.2f;
        [SerializeField, Min(.01f)] private float launchImpulse = 10.8f;
        [SerializeField, Min(0f)] private float contactSeconds = .35f;

        private float contactTime;

        internal Transform NearPad => nearPad;
        internal Transform FarPad => farPad;
        internal float LaunchImpulse => launchImpulse;
        internal float LightMass => lightMass;
        internal float HeavyMass => lightMass + ballastMass;
        internal float CombinedMass => HeavyMass + ballastMass;
        internal bool Transferred { get; private set; }

        internal void ResetStation(Rigidbody a, Rigidbody b)
        {
            contactTime = 0;
            Transferred = false;
            SetBallast(a, b);
            UpdateLights(false, false);
        }

        internal void Step(Rigidbody a, Rigidbody b, int nearBall, int farBall, float dt)
        {
            if (Transferred) return;

            bool bothPresent = nearBall >= 0 && farBall >= 0 && nearBall != farBall;
            contactTime = bothPresent ? contactTime + dt : 0;
            if (bothPresent && contactTime >= contactSeconds)
            {
                Transferred = true;
                SetBallast(a, b);
            }
            UpdateLights(nearBall >= 0, farBall >= 0);
        }

        private void SetBallast(Rigidbody a, Rigidbody b)
        {
            a.mass = Transferred ? CombinedMass : HeavyMass;
            b.mass = Transferred ? LightMass : HeavyMass;
            ballastA.SetActive(Transferred);
            ballastB.SetActive(!Transferred);
        }

        private void UpdateLights(bool nearOccupied, bool farOccupied)
        {
            nearLamp.sharedMaterial = nearOccupied || Transferred ? live : idle;
            farLamp.sharedMaterial = farOccupied || Transferred ? live : idle;
            conduitLamp.sharedMaterial = Transferred ? live : idle;
        }
    }
}
