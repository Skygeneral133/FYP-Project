using UnityEngine;

namespace DefaultNamespace
{
    public class WaterController : MonoBehaviour
    {
        [Header("Starting Liquid")]
        [SerializeField] private GameObject liquidPrefab;

        private GameObject liquidInstance;

        private void Start()
        {
            if (liquidPrefab != null)
            {
                SpawnLiquid(liquidPrefab);
            }
        }

        public void SpawnLiquid(GameObject newLiquidPrefab)
        {
            if (newLiquidPrefab == null)
            {
                Debug.LogWarning(
                    "SpawnLiquid received a null prefab."
                );

                return;
            }

            if (liquidInstance != null)
            {
                Destroy(liquidInstance);
            }

            liquidInstance = Instantiate(
                newLiquidPrefab,
                transform
            );

            liquidInstance.transform.localPosition =
                Vector3.zero;

            liquidInstance.transform.localRotation =
                Quaternion.identity;
        }
    }
}