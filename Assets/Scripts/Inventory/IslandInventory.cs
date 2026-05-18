using System;
using UnityEngine;

namespace PrivateIsland
{
    [DisallowMultipleComponent]
    public sealed class IslandInventory : MonoBehaviour
    {
        private static readonly InventoryStack[] DemoLoadout = Array.Empty<InventoryStack>();

        private const int DefaultSlotCount = 9;

        [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

        private InventoryStack[] slots;
        private IslandFirstPersonCamera firstPersonCamera;
        private EnvironmentMenuUI environmentMenuUI;
        private IslandInventoryUI inventoryUI;
        private bool initialized;
        private bool isOpen;

        public event Action Changed;

        public int SlotCount => DefaultSlotCount;
        public bool IsOpen => isOpen;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            inventoryUI = IslandInventoryUI.GetOrCreate();
            inventoryUI.Bind(this);
            inventoryUI.Hide();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            environmentMenuUI ??= FindAnyObjectByType<EnvironmentMenuUI>();

            if (Input.GetKeyDown(toggleKey))
            {
                if (!isOpen && environmentMenuUI != null && environmentMenuUI.IsMenuOpen)
                {
                    return;
                }

                SetOpen(!isOpen);
            }
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || !isOpen)
            {
                return;
            }

            SetOpen(false);
        }

        public InventoryStack GetSlot(int index)
        {
            EnsureInitialized();
            return IsValidIndex(index) ? slots[index] : InventoryStack.Empty;
        }

        public bool TryAddItem(string itemId, int count = 1)
        {
            EnsureInitialized();

            InventoryStack stack = InventoryStack.Create(itemId, count);
            if (stack.IsEmpty)
            {
                return false;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty && string.Equals(slots[i].ItemId, stack.ItemId, StringComparison.OrdinalIgnoreCase))
                {
                    slots[i] = slots[i].WithCount(slots[i].Count + stack.Count);
                    NotifyChanged();
                    return true;
                }
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty)
                {
                    slots[i] = stack;
                    NotifyChanged();
                    return true;
                }
            }

            return false;
        }

        public bool TryAddItem(InventoryStack stack)
        {
            return TryAddItem(stack.ItemId, stack.Count);
        }

        public bool TrySwapSlots(int fromIndex, int toIndex)
        {
            EnsureInitialized();

            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            {
                return false;
            }

            if (fromIndex == toIndex)
            {
                return true;
            }

            (slots[fromIndex], slots[toIndex]) = (slots[toIndex], slots[fromIndex]);
            NotifyChanged();
            return true;
        }

        public bool TryDiscardSlot(int index)
        {
            EnsureInitialized();

            if (!IsValidIndex(index) || slots[index].IsEmpty)
            {
                return false;
            }

            InventoryStack droppedStack = slots[index];
            if (!IslandWorldItem.TrySpawnDrop(transform, droppedStack))
            {
                return false;
            }

            slots[index] = InventoryStack.Empty;
            NotifyChanged();
            return true;
        }

        public void SetOpen(bool shouldOpen)
        {
            if (isOpen == shouldOpen)
            {
                return;
            }

            EnsureInitialized();
            inventoryUI ??= IslandInventoryUI.GetOrCreate();
            inventoryUI.Bind(this);

            isOpen = shouldOpen;

            if (isOpen)
            {
                inventoryUI.Show();
            }
            else
            {
                inventoryUI.Hide();
            }

            ApplyPresentationState();
        }

        private void EnsureInitialized()
        {
            if (initialized && slots != null && slots.Length == DefaultSlotCount)
            {
                return;
            }

            slots = new InventoryStack[DefaultSlotCount];
            for (int i = 0; i < DemoLoadout.Length && i < slots.Length; i++)
            {
                slots[i] = DemoLoadout[i];
            }

            initialized = true;
        }

        private void ApplyPresentationState()
        {
            firstPersonCamera ??= FindAnyObjectByType<IslandFirstPersonCamera>();
            if (firstPersonCamera != null)
            {
                firstPersonCamera.SetInputSuspended(isOpen);
            }

            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < SlotCount;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        [Serializable]
        public struct InventoryStack
        {
            [SerializeField] private string itemId;
            [SerializeField] private int count;

            public static InventoryStack Empty => default;

            public string ItemId => itemId;
            public int Count => count;
            public bool IsEmpty => string.IsNullOrWhiteSpace(itemId) || count <= 0;

            public InventoryStack WithCount(int quantity)
            {
                return Create(itemId, quantity);
            }

            public static InventoryStack Create(string itemIdValue, int quantity = 1)
            {
                if (string.IsNullOrWhiteSpace(itemIdValue) ||
                    quantity <= 0 ||
                    !IslandItemCatalog.TryGetDefinition(itemIdValue, out _))
                {
                    return Empty;
                }

                return new InventoryStack
                {
                    itemId = itemIdValue.Trim(),
                    count = Mathf.Max(1, quantity)
                };
            }
        }
    }
}
