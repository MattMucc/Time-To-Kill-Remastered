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
    private GrappleController grapple;
    private Image leftGrapple, rightGrapple;
    private RectTransform targetBox;
    private readonly Image[] boxEdges=new Image[4];

    private void Awake()
    {
        input = GetComponent<InputReader>();
        view = GetComponentInChildren<Camera>();
        grapple = GetComponent<GrappleController>();
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
        leftGrapple=AddMark(Vector2.left*(size+4f),new Vector2(3f,12f));
        rightGrapple=AddMark(Vector2.right*(size+4f),new Vector2(3f,12f));
        var box=new GameObject("Enemy Grapple Target",typeof(RectTransform));
        targetBox=box.GetComponent<RectTransform>();targetBox.SetParent(root.transform,false);
        targetBox.anchorMin=targetBox.anchorMax=Vector2.zero;
        for(int i=0;i<4;i++) boxEdges[i]=AddMark(Vector2.zero,Vector2.one,targetBox);
        box.SetActive(false);
    }

    private Image AddMark(Vector2 position, Vector2 dimensions, Transform parent=null)
    {
        var mark = new GameObject("Mark", typeof(RectTransform), typeof(Image), typeof(Outline));
        var rect = mark.GetComponent<RectTransform>();
        rect.SetParent(parent ? parent : reticle, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        mark.GetComponent<Image>().raycastTarget = false;
        var outline = mark.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = Vector2.one;
        return mark.GetComponent<Image>();
    }

    private void LateUpdate()
    {
        canvas.enabled = input.GameplayActive && view && view.enabled;
        if (view) reticle.anchoredPosition = view.pixelRect.center;
        if(!canvas.enabled) {targetBox.gameObject.SetActive(false);return;}
        GrappleController.TargetInfo target=default;
        bool valid=grapple && grapple.TryGetPreview(out target);
        Color color=valid ? (grapple.IsAttached ? Color.cyan : Color.green) : new Color(.45f,.45f,.45f);
        leftGrapple.color=rightGrapple.color=color;
        bool show=valid && target.enemy && target.hasScreenRect;
        targetBox.gameObject.SetActive(show);
        if(!show) return;
        targetBox.anchoredPosition=target.screenRect.center;
        float width=target.screenRect.width+10f,height=target.screenRect.height+10f;
        targetBox.sizeDelta=new Vector2(width,height);
        for(int i=0;i<4;i++)
        {
            bool horizontal=i<2;
            float sign=i%2==0 ? -1f : 1f;
            boxEdges[i].rectTransform.anchoredPosition=horizontal ? new Vector2(0,sign*height*.5f) : new Vector2(sign*width*.5f,0);
            boxEdges[i].rectTransform.sizeDelta=horizontal ? new Vector2(width,2f) : new Vector2(2f,height);
            boxEdges[i].color=color;
        }
    }

    private void OnDisable() { if (canvas) canvas.enabled = false; }
    private void OnDestroy() { if (canvas) Destroy(canvas.gameObject); }
}
