using UnityEngine;

namespace DefaultNamespace
{
    public class WaterController : MonoBehaviour
    {
        [Header("Liquid Prefab (assign whichever one this beaker starts with, or leave empty)")]
        public GameObject LiquidPrefab;

        [Header("Weight Tracking")]
        public float givenVolume = 90;
        private float _hundredMil = 0.03f;
        public BluetoothInterfacer BluetoothInterface;

        private Transform _liquidInstance;

        void Start()
        {
            BluetoothInterface = BluetoothInterfacer.Instance;

            if (LiquidPrefab != null)
            {
                SpawnLiquid(LiquidPrefab);
            }
        }

        void Update()
        {
            if (_liquidInstance == null) return; // nothing spawned yet, nothing to scale

            givenVolume = BluetoothInterface.CurrentWeight;
            if (givenVolume > 0)
            {
                float actualScale = givenVolume * _hundredMil / 100;

                Vector3 currentScale = _liquidInstance.localScale;
                currentScale.y = actualScale;
                _liquidInstance.localScale = currentScale;

                Vector3 pos = _liquidInstance.localPosition;
                pos.y = actualScale * 1f; // 1f = default cylinder mesh half-height
                _liquidInstance.localPosition = pos;
            }
        }

        /// <summary>
        /// Spawns (or replaces) the liquid inside this beaker.
        /// Call this from ButtonsScript when the user picks water / boiling water / acid.
        /// </summary>
        public void SpawnLiquid(GameObject liquidPrefab)
        {
            if (liquidPrefab == null)
            {
                Debug.LogWarning("SpawnLiquid called with a null prefab.");
                return;
            }

            // Remove any existing liquid first
            if (_liquidInstance != null)
            {
                Destroy(_liquidInstance.gameObject);
                _liquidInstance = null;
            }

            GameObject instance = Instantiate(liquidPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            Vector3 startScale = instance.transform.localScale;
            startScale.y = 0f; // start empty, Update() will grow it
            instance.transform.localScale = startScale;

            _liquidInstance = instance.transform;
        }
    }
}