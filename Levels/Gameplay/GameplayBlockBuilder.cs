using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace TouhouMix.Levels.Gameplay {
	public class GameplayBlockBuilder : MonoBehaviour {
		public enum BlockSizingType {
			FIXED,
			STRECHED_PRESERVE_WIDTH,
			STRECHED_PRESERVE_RATIO,
			SLICED_PRESERVE_HEIGHT,
			TILED_PRESERVE_WIDTH,
		}

		const string INSTANT_BLOCK_NAME = "InstantBlock";
		const string SHORT_BLOCK_NAME = "ShortBlock";
		const string LONG_BLOCK_NAME = "LongBlock";
		const string CHILD_IMAGE_NAME = "Image";
		const string CHILD_IMAGE_BOTTOM_NAME = "Bottom";
		const string CHILD_IMAGE_TOP_NAME = "Top";
		const string CHILD_IMAGE_FILL_NAME = "Fill";

		[Header("Long Block")]
		[AssetSelector(Filter = "instant t:Sprite")]
		public Sprite instantBlockSprite;
		public BlockSizingType instantBlockSizingType;

		[Header("Short Block")]
		[AssetSelector(Filter = "short t:Sprite")]
		public Sprite shortBlockSprite;
		public BlockSizingType shortBlockSizingType;

		[Header("Long Block")]
		[AssetSelector(Filter = "long t:Sprite")]
		public Sprite longBlockBottomSprite;
		public BlockSizingType longBlockBottomSizingType;
		[AssetSelector(Filter = "long t:Sprite")]
		public Sprite longBlockTopSprite;
		public BlockSizingType longBlockTopSizingType;
		[AssetSelector(Filter = "long t:Sprite")]
		public Sprite longBlockFillSprite;
		public BlockSizingType longBlockFillSizingType;

		[Header("Common")]
		public Material material;
		public float judgeLineHeight = 80;
		public float blockWidth = 100;
		public float longBlockHeight = 200;

		[Button]
		public void BuildBlocks() {
			DestroyChild(INSTANT_BLOCK_NAME);
			var instantBlockTrans = CreateBlock(INSTANT_BLOCK_NAME, new Vector2(200, judgeLineHeight), new Vector2(blockWidth, 0));
			CreateChild(instantBlockTrans, CHILD_IMAGE_NAME, instantBlockSprite, instantBlockSizingType);

			DestroyChild(SHORT_BLOCK_NAME);
			var shortBlockTrans = CreateBlock(SHORT_BLOCK_NAME, new Vector2(400, judgeLineHeight), new Vector2(blockWidth, 0));
			CreateChild(shortBlockTrans, CHILD_IMAGE_NAME, shortBlockSprite, shortBlockSizingType);

			DestroyChild(LONG_BLOCK_NAME);
			var longBlockTrans = CreateBlock(LONG_BLOCK_NAME, new Vector2(600, judgeLineHeight), new Vector2(blockWidth, longBlockHeight));
			CreateChild(longBlockTrans, CHILD_IMAGE_FILL_NAME, longBlockFillSprite, longBlockFillSizingType);
			CreateChild(longBlockTrans, CHILD_IMAGE_BOTTOM_NAME, longBlockBottomSprite, longBlockBottomSizingType, 0);
			if (longBlockTopSprite) {
				CreateChild(longBlockTrans, CHILD_IMAGE_TOP_NAME, longBlockTopSprite, longBlockTopSizingType, 1);
			}
		}

		RectTransform CreateBlock(string blockName, Vector2 position, Vector2 size) {
			var instance = new GameObject(blockName, typeof(RectTransform));
			var trans = instance.GetComponent<RectTransform>();
			trans.SetParent(transform, false);
			trans.anchorMin = Vector2.zero;
			trans.anchorMax = Vector2.zero;
			trans.pivot = new Vector2(.5f, 0);
			trans.anchoredPosition = position;
			trans.sizeDelta = size;

			return trans;
		}

		void CreateChild(RectTransform parent, string childName, Sprite sprite, BlockSizingType sizingType, float y = .5f) {
			var instance = new GameObject(childName);

			var rect = instance.AddComponent<RectTransform>();
			rect.SetParent(parent, false);

			instance.AddComponent<CanvasRenderer>();
			var image = instance.AddComponent<Image>();
			image.sprite = sprite;
			image.material = material;
			image.raycastTarget = false;

			float aspect = sprite.textureRect.width / sprite.textureRect.height;

			if (sizingType == BlockSizingType.FIXED) {
				image.type = Image.Type.Simple;
				rect.anchorMin = new Vector2(.5f, y);
				rect.anchorMax = new Vector2(.5f, y);
				image.SetNativeSize();
			} else if (sizingType == BlockSizingType.STRECHED_PRESERVE_RATIO) {
				image.type = Image.Type.Simple;
				rect.anchorMin = new Vector2(.5f, y);
				rect.anchorMax = new Vector2(.5f, y);
				rect.sizeDelta = new Vector2(parent.sizeDelta.x, parent.sizeDelta.x / aspect);
			} else if (sizingType == BlockSizingType.SLICED_PRESERVE_HEIGHT) {
				image.type = Image.Type.Simple;
				image.SetNativeSize();
				image.type = Image.Type.Sliced;
				rect.anchorMin = new Vector2(0, y);
				rect.anchorMax = new Vector2(1, y);
				rect.anchoredPosition = Vector2.zero;
				rect.offsetMin = new Vector2(0, rect.offsetMin.y);
				rect.offsetMax = new Vector2(0, rect.offsetMax.y);
			} else if (sizingType == BlockSizingType.STRECHED_PRESERVE_WIDTH) {
				image.type = Image.Type.Simple;
				image.SetNativeSize();
				image.preserveAspect = false;
				rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
				rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
				rect.offsetMin = new Vector2(rect.offsetMin.x, 0);
				rect.offsetMax = new Vector2(rect.offsetMax.x, 0);
			} else if (sizingType == BlockSizingType.TILED_PRESERVE_WIDTH) {
				image.type = Image.Type.Simple;
				image.SetNativeSize();
				image.type = Image.Type.Tiled;
				image.preserveAspect = false;
				rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
				rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
				rect.offsetMin = new Vector2(rect.offsetMin.x, 0);
				rect.offsetMax = new Vector2(rect.offsetMax.x, 0);
			}
		}

		void DestroyChild(string name) {
			var childTrans = transform.Find(name);
			if (childTrans != null) {
#if UNITY_EDITOR
				DestroyImmediate(childTrans.gameObject);
#endif
			}
		}
	}
}
