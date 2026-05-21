using System.Collections.Generic;
using UnityEngine;

namespace PrivateIsland
{
    [DisallowMultipleComponent]
    public sealed class IslandFlowerClusterPickup : IslandInteractable
    {
        private readonly List<FlowerGroup> flowerGroups = new List<FlowerGroup>();

        private bool initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        public override bool CanInteract(Transform interactor)
        {
            return interactor != null && GetRemainingFlowerCount() > 0;
        }

        public override void Interact(Transform interactor)
        {
            if (interactor == null)
            {
                return;
            }

            IslandActionToolVisual toolVisual = IslandActionToolVisual.GetOrCreate();
            toolVisual?.PlayOneShot(IslandActionToolVisual.ToolKind.Hand, FocusPoint, 0.18f);

            IslandInventory inventory = interactor.GetComponent<IslandInventory>() ?? interactor.GetComponentInParent<IslandInventory>();
            if (inventory == null || !inventory.TryAddItem(IslandItemCatalog.FlowerId, 1))
            {
                return;
            }

            EnsureInitialized();

            for (int i = flowerGroups.Count - 1; i >= 0; i--)
            {
                FlowerGroup group = flowerGroups[i];
                if (group == null || group.IsPicked)
                {
                    continue;
                }

                group.SetPicked(true);
                break;
            }

            RefreshPrompt();

            if (GetRemainingFlowerCount() <= 0)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            BuildFlowerGroups();

            SetInteractionRadius(2.5f);
            if (IslandInteractionUtility.TryGetCompositeBounds(transform, out Bounds bounds))
            {
                SetFocusHeight(Mathf.Max(0.25f, bounds.size.y * 0.5f));
            }
            else
            {
                SetFocusHeight(0.35f);
            }

            RefreshPrompt();
            initialized = true;
        }

        private void BuildFlowerGroups()
        {
            flowerGroups.Clear();

            List<Transform> stems = new List<Transform>();
            List<Transform> parts = new List<Transform>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                parts.Add(child);
                if (child.name.Contains("Stem"))
                {
                    stems.Add(child);
                }
            }

            if (stems.Count == 0)
            {
                return;
            }

            Dictionary<Transform, FlowerGroup> groupsByStem = new Dictionary<Transform, FlowerGroup>();
            foreach (Transform stem in stems)
            {
                FlowerGroup group = new FlowerGroup(stem);
                groupsByStem[stem] = group;
                flowerGroups.Add(group);
            }

            foreach (Transform part in parts)
            {
                Transform nearestStem = null;
                float bestDistance = float.MaxValue;

                foreach (Transform stem in stems)
                {
                    float distance = (part.localPosition - stem.localPosition).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearestStem = stem;
                    }
                }

                if (nearestStem != null)
                {
                    groupsByStem[nearestStem].AddPart(part.gameObject);
                }
            }
        }

        private int GetRemainingFlowerCount()
        {
            int remaining = 0;
            for (int i = 0; i < flowerGroups.Count; i++)
            {
                if (flowerGroups[i] != null && !flowerGroups[i].IsPicked)
                {
                    remaining++;
                }
            }

            return remaining;
        }

        private void RefreshPrompt()
        {
            int remaining = GetRemainingFlowerCount();
            SetInteractionPrompt(remaining > 1
                ? $"Press E or F to pick a flower ({remaining} left)"
                : "Press E or F to pick the last flower");
        }

        private sealed class FlowerGroup
        {
            private readonly List<GameObject> parts = new List<GameObject>();

            public FlowerGroup(Transform stem)
            {
                Stem = stem;
            }

            public Transform Stem { get; }
            public bool IsPicked { get; private set; }

            public void AddPart(GameObject part)
            {
                if (part != null && !parts.Contains(part))
                {
                    parts.Add(part);
                }
            }

            public void SetPicked(bool picked)
            {
                IsPicked = picked;

                foreach (GameObject part in parts)
                {
                    if (part != null)
                    {
                        part.SetActive(!picked);
                    }
                }
            }
        }
    }
}
