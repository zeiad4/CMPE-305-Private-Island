using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PrivateIsland
{
    public sealed class IslandInventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private IslandInventoryUI owner;
        private Image background;
        private Image iconImage;
        private Text countLabel;
        private Text nameLabel;
        private Text slotNumberLabel;

        public int SlotIndex { get; private set; }

        public void Bind(IslandInventoryUI inventoryUI, int slotIndex, Font font)
        {
            owner = inventoryUI;
            SlotIndex = slotIndex;

            background ??= GetComponent<Image>();
            if (background == null)
            {
                background = gameObject.AddComponent<Image>();
            }

            background.color = new Color(0.16f, 0.19f, 0.23f, 0.98f);

            iconImage ??= GetOrCreateImage("Icon");
            iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            iconImage.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            iconImage.rectTransform.sizeDelta = new Vector2(62f, 62f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            slotNumberLabel ??= GetOrCreateText("SlotNumber", font);
            slotNumberLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            slotNumberLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            slotNumberLabel.rectTransform.pivot = new Vector2(0f, 1f);
            slotNumberLabel.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            slotNumberLabel.rectTransform.sizeDelta = new Vector2(24f, 20f);
            slotNumberLabel.fontSize = 13;
            slotNumberLabel.fontStyle = FontStyle.Normal;
            slotNumberLabel.alignment = TextAnchor.UpperLeft;
            slotNumberLabel.color = new Color(0.77f, 0.83f, 0.88f);
            slotNumberLabel.text = (slotIndex + 1).ToString();

            countLabel ??= GetOrCreateText("CountLabel", font);
            countLabel.rectTransform.anchorMin = new Vector2(1f, 1f);
            countLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            countLabel.rectTransform.pivot = new Vector2(1f, 1f);
            countLabel.rectTransform.anchoredPosition = new Vector2(-8f, -8f);
            countLabel.rectTransform.sizeDelta = new Vector2(34f, 22f);
            countLabel.fontSize = 16;
            countLabel.fontStyle = FontStyle.Bold;
            countLabel.alignment = TextAnchor.UpperRight;
            countLabel.color = Color.white;

            nameLabel ??= GetOrCreateText("NameLabel", font);
            nameLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            nameLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            nameLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            nameLabel.rectTransform.anchoredPosition = new Vector2(0f, 8f);
            nameLabel.rectTransform.sizeDelta = new Vector2(96f, 28f);
            nameLabel.fontSize = 12;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.alignment = TextAnchor.LowerCenter;
            nameLabel.color = new Color(0.94f, 0.96f, 0.98f);
            nameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameLabel.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public void Refresh(IslandInventory.InventoryStack stack, bool hideItem)
        {
            if (stack.IsEmpty || hideItem)
            {
                background.color = new Color(0.16f, 0.19f, 0.23f, 0.98f);
                iconImage.enabled = false;
                countLabel.text = string.Empty;
                nameLabel.text = string.Empty;
                return;
            }

            IslandItemCatalog.ItemDefinition definition = IslandItemCatalog.GetDefinition(stack.ItemId);
            background.color = Color.Lerp(new Color(0.18f, 0.21f, 0.25f, 0.98f), definition.PrimaryColor, 0.28f);
            iconImage.enabled = true;
            iconImage.sprite = IslandItemCatalog.GetIcon(stack.ItemId);
            countLabel.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
            nameLabel.text = definition.DisplayName;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginDrag(SlotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        private Image GetOrCreateImage(string childName)
        {
            Transform existing = transform.Find(childName);
            GameObject child = existing != null
                ? existing.gameObject
                : new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(transform, false);

            Image image = child.GetComponent<Image>();
            if (image == null)
            {
                image = child.AddComponent<Image>();
            }

            return image;
        }

        private Text GetOrCreateText(string childName, Font font)
        {
            Transform existing = transform.Find(childName);
            GameObject child = existing != null
                ? existing.gameObject
                : new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            child.transform.SetParent(transform, false);

            Text text = child.GetComponent<Text>();
            if (text == null)
            {
                text = child.AddComponent<Text>();
            }

            text.font = font;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }
    }
}
