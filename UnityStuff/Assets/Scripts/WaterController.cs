using UnityEngine;

namespace DefaultNamespace
{
    public class WaterController : MonoBehaviour
    {
        public float givenVolume = 90;
        public float hundredMil = 0.03f;

        void Update()
        {
            if (givenVolume>0) {
                
                float actualScale = givenVolume*hundredMil / 100;

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