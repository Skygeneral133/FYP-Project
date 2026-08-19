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
            if (givenVolume>0) {
                
                float actualScale = givenVolume*_hundredMil / 100;

                Vector3 currentScale = gameObject.transform.localScale;
                currentScale.y = actualScale;
                gameObject.transform.localScale = currentScale;

                float halfHeight = 1f;
                Vector3 pos = transform.localPosition;
                pos.y = actualScale * halfHeight;
                transform.localPosition = pos;
            }
        }
    }
}