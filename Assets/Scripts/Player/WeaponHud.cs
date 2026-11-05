using UnityEngine;
using UnityEngine.UI;

public class WeaponHud : MonoBehaviour
{
    [SerializeField] private Font font;
    private Canvas canvas;
    private Text title, ammo, prompt, hit;
    private Image icon;
    private GameObject fallbackIcon;
    private double hitEnds;
    private void Awake()
    {
        var root = new GameObject("Weapon HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 90;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = 0.5f;
        title = Label(root.transform,"Weapon",new Vector2(1,0),new Vector2(-210,115),new Vector2(360,40),24);
        ammo = Label(root.transform,"Ammo",new Vector2(1,0),new Vector2(-210,65),new Vector2(360,50),28);
        prompt = Label(root.transform,"Pickup",new Vector2(.5f,.5f),new Vector2(0,-95),new Vector2(950,55),24);
        hit = Label(root.transform,"Hit",new Vector2(.5f,.5f),Vector2.zero,new Vector2(45,45),28);
        var image = new GameObject("Equipped Weapon Icon",typeof(RectTransform),typeof(Image));
        icon = image.GetComponent<Image>(); icon.raycastTarget = false; icon.preserveAspect = true;
        var rect = image.GetComponent<RectTransform>(); rect.SetParent(root.transform,false);
        rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.anchoredPosition=new Vector2(-210,175);rect.sizeDelta=new Vector2(100,55);
        fallbackIcon = new GameObject("Pistol Silhouette",typeof(RectTransform));
        fallbackIcon.transform.SetParent(rect,false);
        Mark(new Vector2(0,10),new Vector2(78,17)); Mark(new Vector2(-24,-8),new Vector2(16,29));
        Mark(new Vector2(-5,-5),new Vector2(28,5));
        ShowWeapon(null,null,false); ShowPrompt(""); hit.text="";
    }
    private void Mark(Vector2 position,Vector2 size)
    {
        var obj=new GameObject("Shape",typeof(RectTransform),typeof(Image));
        var rect=obj.GetComponent<RectTransform>();rect.SetParent(fallbackIcon.transform,false);rect.anchoredPosition=position;rect.sizeDelta=size;
        obj.GetComponent<Image>().raycastTarget=false;
    }
    private Text Label(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,int fontSize)
    {
        var obj=new GameObject(name,typeof(RectTransform),typeof(Text),typeof(Outline));
        var rect=obj.GetComponent<RectTransform>();rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;
        var text=obj.GetComponent<Text>();text.font=font;text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.raycastTarget=false;
        obj.GetComponent<Outline>().effectColor=Color.black;return text;
    }
    public void SetVisible(bool value) { if(canvas) canvas.enabled=value; }
    public void ShowWeapon(WeaponDefinition definition,WeaponInstance state,bool reloading)
    {
        title.text=definition ? definition.displayName+(reloading ? " (Reloading)" : "") : "Unarmed";
        ammo.text=state==null ? "" : $"{state.Magazine} / {state.MaxMagazine}   |   {state.Reserve}";
        icon.sprite=definition ? definition.hudIcon : null;icon.enabled=icon.sprite;
        fallbackIcon.SetActive(definition && !definition.hudIcon);
    }
    public void ShowPrompt(string value) { prompt.text=value; }
    public void ShowHit(bool defeated) { hit.text="X";hit.color=defeated ? Color.red : Color.white;hitEnds=Time.timeAsDouble+.12f; }
    private void LateUpdate() { if(hit && Time.timeAsDouble>=hitEnds) hit.text=""; }
    private void OnDisable() { SetVisible(false); }
    private void OnDestroy() { if(canvas) Destroy(canvas.gameObject); }
}
