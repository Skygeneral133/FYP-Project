using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DefaultNamespace
{
    public class WaterController : MonoBehaviour
    {
        public float givenVolume = 90;
        private float _hundredMil = 0.03f;
        public BluetoothInterfacer BluetoothInterface;
        
        
        void Start()
        {
            BluetoothInterface = BluetoothInterfacer.Instance;
        }
        
        void Update()
        {
            givenVolume = BluetoothInterface.CurrentWeight;
            if (givenVolume > 0)
            {
                float actualScale = givenVolume * _hundredMil / 100;

                // Scale on Y as before
                Vector3 currentScale = transform.localScale;
                currentScale.y = actualScale;
                transform.localScale = currentScale;

                // Default Unity cylinder mesh half-height is 1 (in unscaled local units),
                // so after scaling, the cylinder's actual half-height is actualScale * 1.
                // Positioning the pivot at that offset above the anchor keeps the base pinned at y=0.
                Vector3 pos = transform.localPosition;
                pos.y = actualScale * 1f; // 1f = default cylinder mesh half-height
                transform.localPosition = pos;
            }
        }
    }
}