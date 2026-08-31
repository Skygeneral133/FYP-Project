using UnityEngine;

namespace DefaultNamespace
{
    public class WaterController : MonoBehaviour
    {
        [Header("Beaker Prefabs")]
        [SerializeField] private GameObject normalBeakerPrefab;
        [SerializeField] private GameObject boilingBeakerPrefab;
        [SerializeField] private GameObject acidBeakerPrefab;

        [Header("Starting Beaker")]
        [SerializeField] private bool spawnNormalOnStart = true;

        private GameObject currentBeaker;

        private void Start()
        {
            if (spawnNormalOnStart)
            {
                SelectNormalWater();
            }
        }

        public void SelectBoilingWater()
        {
            SpawnLiquid(boilingBeakerPrefab);
        }

        public void SelectNormalWater()
        {
            SpawnLiquid(normalBeakerPrefab);
        }

        public void SelectAcid()
        {
            SpawnLiquid(acidBeakerPrefab);
        }

        public void SpawnLiquid(GameObject selectedPrefab)
        {
            if (selectedPrefab == null)
            {
                Debug.LogWarning(
                    "The selected prefab is not assigned."
                );

                return;
            }

            if (currentBeaker != null)
            {
                currentBeaker.SetActive(false);
                Destroy(currentBeaker);
            }

            currentBeaker = Instantiate(
                selectedPrefab,
                transform,
                false
            );

            currentBeaker.transform.localPosition =
                Vector3.zero;

            currentBeaker.transform.localRotation =
                Quaternion.identity;
        }
    }
}