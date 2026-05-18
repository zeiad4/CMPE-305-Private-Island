using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrivateIsland
{
    public sealed class IslandInventoryUI : MonoBehaviour
    {
        private static IslandInventoryUI instance;

        private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        private Canvas inventoryCanvas;
        private GraphicRaycaster graphicRaycaster;
        private RectTransform overlayRect;
        private RectTransform panelRect;
        private Text footerText;
        private Image dragGhostBackground;
        private Image dragGhostIcon;
        private Text dragGhostText;
        private Text dragGhostCount;
        private RectTransform dragGhostRect;
        private IslandInventorySlotUI[] slotViews;
        private IslandInventory boundInventory;
        private int dragSourceIndex = -1;
        private IslandInventory.InventoryStack draggedStack;

        public static IslandInventoryUI GetOrCreate()
        {
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            instance = FindAnyObjectByType<IslandInventoryUI>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.EnsureUI();
                return instance;
            }

            GameObject root = new GameObject("Island Inventory UI");
            instance = root.AddComponent<IslandInventoryUI>();
            instance.EnsureUI();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            EnsureUI();
            Hide();
        }

        private void Update()
        {
            if (!Application.isPlaying || dragSourceIndex < 0)
            {
                return;
            }

            if (dragGhostRect != null)
            {
                dragGhostRect.position = Input.mousePosition + new Vector3(20f, -18f, 0f);
            }

            if (!Input.GetMouseButton(0))
            {
                CompleteDrag();
            }
        }

        public void Bind(IslandInventory inventory)
        {
            if (boundInventory == inventory)
            {
                Refresh();
                return;
            }

            if (boundInventory != null)
            {
                boundInventory.Changed -= Refresh;
            }

            boundInventory = inventory;

            if (boundInventory != null)
            {
                boundInventory.Changed += Refresh;
            }

            EnsureUI();
            Refresh();
        }

        public void Show()
        {
            EnsureUI();
            inventoryCanvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            CancelDrag();
            EnsureUI();
            inventoryCanvas.gameObject.SetActive(false);
        }

        public void BeginDrag(int slotIndex)
        {
            if (boundInventory == null)
            {
                return;
            }

            IslandInventory.InventoryStack stack = boundInventory.GetSlot(slotIndex);
            if (stack.IsEmpty)
            {
                return;
            }

            dragSourceIndex = slotIndex;
            draggedStack = stack;

            if (dragGhostBackground != null)
            {
                dragGhostBackground.gameObject.SetActive(true);
            }

            if (dragGhostIcon != null)
            {
                dragGhostIcon.sprite = IslandItemCatalog.GetIcon(draggedStack.ItemId);
                dragGhostIcon.enabled = true;
            }

            if (dragGhostText != null)
            {
                dragGhostText.text = IslandItemCatalog.GetDisplayName(draggedStack.ItemId);
            }

            if (dragGhostCount != null)
            {
                dragGhostCount.text = draggedStack.Count > 1 ? $"x{draggedStack.Count}" : string.Empty;
            }

            Refresh();
        }

        public void CancelDrag()
        {
            dragSourceIndex = -1;
            draggedStack = IslandInventory.InventoryStack.Empty;

            if (dragGhostBackground != null)
            {
                dragGhostBackground.gameObject.SetActive(false);
            }

            Refresh();
        }

        private void CompleteDrag()
        {
            if (boundInventory == null || dragSourceIndex < 0)
            {
                CancelDrag();
                return;
            }

            int hoveredSlot = GetHoveredSlotIndex();
            if (hoveredSlot >= 0)
            {
                boundInventory.TrySwapSlots(dragSourceIndex, hoveredSlot);
            }
            else if (!RectTransformUtility.RectangleContainsScreenPoint(panelRect, Input.mousePosition, inventoryCanvas.worldCamera))
            {
                boundInventory.TryDiscardSlot(dragSourceIndex);
            }

            CancelDrag();
        }

        private int GetHoveredSlotIndex()
        {
            if (EventSystem.current == null)
            {
                return -1;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                IslandInventorySlotUI slot = raycastResults[i].gameObject.GetComponentInParent<IslandInventorySlotUI>();
                if (slot != null)
                {
                    return slot.SlotIndex;
                }
            }

            return -1;
        }

        private void Refresh()
        {
            if (slotViews == null || boundInventory == null)
            {
                return;
            }

            for (int i = 0; i < slotViews.Length; i++)
            {
                bool hideItem = i == dragSourceIndex;
                slotViews[i].Refresh(boundInventory.GetSlot(i), hideItem);
            }

            if (footerText != null)
            {
                footerText.text = "Tab keeps the bag open while you walk. Drag outside the panel to drop the item back onto the island.";
            }
        }

        private void EnsureUI()
        {
            EnsureEventSystem();

            inventoryCanvas ??= GetComponent<Canvas>();
            if (inventoryCanvas == null)
            {
                inventoryCanvas = gameObject.AddComponent<Canvas>();
                inventoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                inventoryCanvas.sortingOrder = 140;
            }

            if (GetComponent<CanvasScaler>() == null)
            {
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            graphicRaycaster ??= GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            if (overlayRect == null)
            {
                GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                overlayObject.transform.SetParent(transform, false);
                overlayRect = overlayObject.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;

                Image overlayImage = overlayObject.GetComponent<Image>();
                overlayImage.color = new Color(0.03f, 0.05f, 0.08f, 0.56f);
            }

            if (panelRect == null)
            {
                GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                panelObject.transform.SetParent(overlayRect, false);
                panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.82f, 0.5f);
                panelRect.anchorMax = new Vector2(0.82f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(450f, 540f);

                Image panelImage = panelObject.GetComponent<Image>();
                panelImage.color = new Color(0.09f, 0.12f, 0.15f, 0.95f);
            }

            if (panelRect.Find("Title") == null)
            {
                CreateText("Title", panelRect, font, "Inventory", 34, FontStyle.Bold, TextAnchor.UpperCenter, Color.white,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(320f, 42f));
            }

            if (panelRect.Find("SubTitle") == null)
            {
                CreateText("SubTitle", panelRect, font, "3 x 3 Explorer Pack", 18, FontStyle.Normal, TextAnchor.UpperCenter,
                    new Color(0.75f, 0.82f, 0.87f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -66f), new Vector2(280f, 24f));
            }

            Transform existingGrid = panelRect.Find("Grid");
            RectTransform gridRect = existingGrid as RectTransform;
            if (gridRect == null)
            {
                GameObject gridObject = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
                gridObject.transform.SetParent(panelRect, false);
                gridRect = gridObject.GetComponent<RectTransform>();
                gridRect.anchorMin = new Vector2(0.5f, 0.5f);
                gridRect.anchorMax = new Vector2(0.5f, 0.5f);
                gridRect.pivot = new Vector2(0.5f, 0.5f);
                gridRect.anchoredPosition = new Vector2(0f, 8f);
                gridRect.sizeDelta = new Vector2(360f, 360f);

                GridLayoutGroup layout = gridObject.GetComponent<GridLayoutGroup>();
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 3;
                layout.cellSize = new Vector2(110f, 110f);
                layout.spacing = new Vector2(14f, 14f);
                layout.childAlignment = TextAnchor.MiddleCenter;
            }

            if (slotViews == null || slotViews.Length != 9)
            {
                slotViews = new IslandInventorySlotUI[9];

                for (int i = 0; i < slotViews.Length; i++)
                {
                    Transform existingSlot = gridRect.childCount > i ? gridRect.GetChild(i) : null;
                    GameObject slotObject;

                    if (existingSlot == null)
                    {
                        slotObject = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(IslandInventorySlotUI));
                        slotObject.transform.SetParent(gridRect, false);
                    }
                    else
                    {
                        slotObject = existingSlot.gameObject;
                        if (slotObject.GetComponent<Image>() == null)
                        {
                            slotObject.AddComponent<Image>();
                        }

                        if (slotObject.GetComponent<IslandInventorySlotUI>() == null)
                        {
                            slotObject.AddComponent<IslandInventorySlotUI>();
                        }
                    }

                    slotViews[i] = slotObject.GetComponent<IslandInventorySlotUI>();
                    slotViews[i].Bind(this, i, font);
                }
            }

            if (footerText == null)
            {
                footerText = CreateText("Footer", panelRect, font, string.Empty, 17, FontStyle.Normal, TextAnchor.LowerCenter,
                    new Color(0.79f, 0.86f, 0.9f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 32f), new Vector2(380f, 86f));
            }

            if (dragGhostBackground == null)
            {
                GameObject ghostObject = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                ghostObject.transform.SetParent(transform, false);

                dragGhostBackground = ghostObject.GetComponent<Image>();
                dragGhostBackground.color = new Color(0.09f, 0.12f, 0.15f, 0.95f);
                dragGhostBackground.raycastTarget = false;

                dragGhostRect = dragGhostBackground.rectTransform;
                dragGhostRect.anchorMin = new Vector2(0f, 0f);
                dragGhostRect.anchorMax = new Vector2(0f, 0f);
                dragGhostRect.pivot = new Vector2(0f, 1f);
                dragGhostRect.sizeDelta = new Vector2(210f, 72f);

                GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(dragGhostRect, false);
                dragGhostIcon = iconObject.GetComponent<Image>();
                dragGhostIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                dragGhostIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                dragGhostIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
                dragGhostIcon.rectTransform.anchoredPosition = new Vector2(12f, 0f);
                dragGhostIcon.rectTransform.sizeDelta = new Vector2(48f, 48f);
                dragGhostIcon.preserveAspect = true;
                dragGhostIcon.raycastTarget = false;

                dragGhostText = CreateText("Label", dragGhostRect, font, string.Empty, 19, FontStyle.Bold, TextAnchor.MiddleLeft,
                    Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(72f, 8f), new Vector2(-84f, 26f));
                dragGhostText.rectTransform.offsetMin = new Vector2(72f, 8f);
                dragGhostText.rectTransform.offsetMax = new Vector2(-52f, -8f);

                dragGhostCount = CreateText("Count", dragGhostRect, font, string.Empty, 16, FontStyle.Bold, TextAnchor.MiddleRight,
                    new Color(0.96f, 0.86f, 0.48f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(36f, 24f));
                dragGhostBackground.gameObject.SetActive(false);
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystemObject.transform.SetAsFirstSibling();
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            string content,
            int fontSize,
            FontStyle fontStyle,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.supportRichText = false;
            text.raycastTarget = false;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            return text;
        }
    }
}
