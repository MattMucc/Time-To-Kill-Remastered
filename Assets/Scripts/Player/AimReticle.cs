using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputReader))]
public class AimReticle : MonoBehaviour
{
    [SerializeField, Min(4f)] private float size = 18f;
    private Canvas canvas;
    private RectTransform reticle;
    private InputReader input;
    private Camera view;

    private void Awake()
    {
        input = GetComponent<InputReader>();
        view = GetComponentInChildren<Camera>();
        var root = new GameObject("Aim Reticle", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var center = new GameObject("Center", typeof(RectTransform));
        reticle = center.GetComponent<RectTransform>();
        reticle.SetParent(root.transform, false);
        reticle.anchorMin = reticle.anchorMax = Vector2.zero;
        AddMark(Vector2.zero, new Vector2(2f, 2f));
        float arm = size * 0.3f;
        float offset = size * 0.35f;
        AddMark(Vector2.left * offset, new Vector2(arm, 2f));
        AddMark(Vector2.right * offset, new Vector2(arm, 2f));
        AddMark(Vector2.up * offset, new Vector2(2f, arm));
        AddMark(Vector2.down * offset, new Vector2(2f, arm));
    }

    private void AddMark(Vector2 position, Vector2 dimensions)
    {
        var mark = new GameObject("Mark", typeof(RectTransform), typeof(Image), typeof(Outline));
        var rect = mark.GetComponent<RectTransform>();
        rect.SetParent(reticle, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        mark.GetComponent<Image>().raycastTarget = false;
        var outline = mark.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = Vector2.one;
    }

    private void LateUpdate()
    {
        canvas.enabled = input.GameplayActive && view && view.enabled;
        if (view) reticle.anchoredPosition = view.pixelRect.center;
    }

    private void OnDisable() { if (canvas) canvas.enabled = false; }
    private void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }
}
