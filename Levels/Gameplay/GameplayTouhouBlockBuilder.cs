using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Uif.Settables.Components;
using UnityEngine.UI;
using System.Linq;

public sealed class GameplayTouhouBlockBuilder : MonoBehaviour {
	public Material material;
	public Sprite[] sprites;

	[Button]
	public void BuildSprites() {
		var rect = GetComponent<RectTransform>();

		foreach (var sprite in sprites) {
			var nameSegs = sprite.name.Split('-').Select(x => x[0].ToString().ToUpper() + x.Substring(1)).ToList();
			nameSegs.RemoveRange(0, 3);
			var objName = string.Join("", nameSegs);

			var obj = new GameObject(objName, typeof(RectTransform));
			obj.transform.SetParent(rect, false);
			var color = obj.AddComponent<MultiGraphicColorSettable>();
			color.shouldPerserveAlpha = true;
			var imageObj = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer));
			imageObj.transform.SetParent(obj.transform, false);
			var image = imageObj.AddComponent<Image>();
			image.sprite = sprite;
			image.material = material;
			image.SetNativeSize();
			color.graphics.Add(image);
			color.color = Color.red;
			color.OnValidate();
		}
	}

	public Vector2 scale = Vector2.one;

	[Button]
	public void UpdateSprites() {
		for (int i = 0; i < transform.childCount; i++) {
			var child = transform.GetChild(i).gameObject;
			var imageObj = child.transform.GetChild(0).gameObject;
			var image = imageObj.GetComponent<Image>();
			image.SetNativeSize();
			var imageRect = imageObj.GetComponent<RectTransform>();
			imageRect.sizeDelta *= scale;
			Debug.Log(image.sprite.pivot / image.sprite.rect.size);
			imageRect.pivot = image.sprite.pivot / image.sprite.rect.size;
		}
	}
}
