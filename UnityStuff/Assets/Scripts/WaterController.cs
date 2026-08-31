using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DefaultNamespace
{
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class WaterController : MonoBehaviour
    {
        [Header("Beaker Prefabs")]
        [SerializeField] private GameObject normalBeakerPrefab;
        [SerializeField] private GameObject boilingBeakerPrefab;
        [SerializeField] private GameObject acidBeakerPrefab;

        [Header("Default Beaker On Track")]
        [SerializeField] private bool spawnNormalOnTrack = true;

        private ARTrackedImageManager trackedImageManager;

        // The transform of the marker we're currently spawning beakers on.
        // If you support multiple simultaneous markers, replace this with
        // a Dictionary<string, Transform> keyed by referenceImage.name,
        // and likewise make currentBeaker a Dictionary<string, GameObject>.
        private Transform beakerParent;
        private GameObject currentBeaker;
        private GameObject currentPrefab; // remembers last selection so re-tracking reuses it

        private void Awake()
        {
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        private void OnEnable()
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        private void OnDisable()
        {
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
        {
            foreach (var trackedImage in args.added)
            {
                beakerParent = trackedImage.transform;

                if (spawnNormalOnTrack)
                {
                    SelectNormalWater();
                }
            }

            foreach (var trackedImage in args.updated)
            {
                // Keep parent reference fresh in case tracking state toggled.
                if (trackedImage.trackingState == TrackingState.Tracking)
                {
                    beakerParent = trackedImage.transform;
                }
            }

            foreach (var trackedImage in args.removed)
            {
                if (beakerParent == trackedImage.transform)
                {
                    ClearCurrentBeaker();
                    beakerParent = null;
                }
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
                Debug.LogWarning("The selected prefab is not assigned.");
                return;
            }

            currentPrefab = selectedPrefab;

            if (beakerParent == null)
            {
                Debug.LogWarning(
                    "No tracked image is currently active; " +
                    "beaker will spawn once a marker is detected."
                );
                return;
            }

            ClearCurrentBeaker();

            currentBeaker = Instantiate(
                selectedPrefab,
                beakerParent,
                false
            );

            currentBeaker.transform.localPosition = Vector3.zero;
            currentBeaker.transform.localRotation = Quaternion.identity;
        }

        private void ClearCurrentBeaker()
        {
            if (currentBeaker == null) return;

            currentBeaker.SetActive(false);
            Destroy(currentBeaker);
            currentBeaker = null;
        }
    }
}