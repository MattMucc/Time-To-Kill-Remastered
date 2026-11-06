using UnityEngine;

// Cosmetic only: world-space flight never changes hitscan damage or collision timing.
public sealed class WeaponShotEffects
{
    private readonly GameObject root;
    private readonly Transform muzzle;
    private readonly Transform flash;
    private readonly Transform view, burst;
    private readonly Light light;
    private readonly Mesh bulletMesh, flashMesh, burstMesh;
    private readonly Flight[] flights = new Flight[12];
    private int next;
    private double flashEnds;
    private const float FlashDuration = 0.06f;
    private const float BulletWidth = 0.09f;
    private const float BulletLength = 0.45f;

    private sealed class Flight
    {
        public Transform visual;
        public Vector3 start, end;
        public double started;
        public float duration;
        public bool active;
    }

    public WeaponShotEffects(Transform barrel, Transform cameraView, Material material)
    {
        muzzle = barrel;
        view = cameraView;
        root = new GameObject("Weapon Shot Effects");
        bulletMesh = new Mesh { name = "Bullet Visual" };
        bulletMesh.vertices = new[] { new Vector3(0,0,-.5f), new Vector3(0,0,.5f),
            new Vector3(.5f,0,0), new Vector3(0,.5f,0), new Vector3(-.5f,0,0), new Vector3(0,-.5f,0) };
        bulletMesh.triangles = new[] {0,3,2,0,4,3,0,5,4,0,2,5,1,2,3,1,3,4,1,4,5,1,5,2};
        bulletMesh.RecalculateBounds();
        flashMesh = CreateFlashMesh();
        flash = MakeVisual("Muzzle Flame", flashMesh, material, new Color(1f,.55f,.08f,.85f));
        Transform core = MakeVisual("Flash Core", bulletMesh, material, new Color(1f,.95f,.65f,1f));
        core.SetParent(flash, false); core.localPosition = new Vector3(0,0,.04f);
        core.localScale = new Vector3(.055f,.055f,.12f);
        burstMesh = CreateBurstMesh();
        burst = MakeVisual("Camera Facing Flash", burstMesh, material, new Color(1f,.65f,.12f,.9f));
        burst.SetParent(flash,false);
        burst.localPosition = new Vector3(0,0,.06f);
        burst.localScale = Vector3.one*.22f;
        Transform burstCore = MakeVisual("Burst Core",burstMesh,material,new Color(1f,1f,.8f,1f));
        burstCore.SetParent(burst,false);
        burstCore.localScale = Vector3.one*.4f;
        light = flash.gameObject.AddComponent<Light>();
        light.color = new Color(1f,.65f,.25f); light.range = 2f; light.intensity = 2f;
        light.shadows = LightShadows.None;
        flash.gameObject.SetActive(false);
        for (int i=0; i<flights.Length; i++)
        {
            Transform visual = MakeVisual("Neon Cyan Bullet", bulletMesh, material, new Color(.05f,1f,1f,1f));
            visual.localScale = new Vector3(BulletWidth,BulletWidth,BulletLength);
            visual.gameObject.SetActive(false);
            flights[i] = new Flight { visual = visual };
        }
    }

    private Transform MakeVisual(string name, Mesh mesh, Material material, Color color)
    {
        var obj = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        obj.transform.SetParent(root.transform, false);
        obj.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = obj.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock(); properties.SetColor("_Color",color);
        renderer.SetPropertyBlock(properties);
        return obj.transform;
    }

    public void Play(Vector3 start, Vector3 end, float speed, double now)
    {
        if (muzzle)
        {
            flashEnds = now + FlashDuration;
            flash.localScale = Vector3.one;
            light.intensity = 2f;
            flash.SetPositionAndRotation(muzzle.position + muzzle.forward * .005f, muzzle.rotation);
            if (view) burst.rotation = view.rotation;
            flash.gameObject.SetActive(true);
        }
        float distance = Vector3.Distance(start,end);
        if (distance < .01f) return;
        Flight flight = flights[next]; next = (next+1)%flights.Length;
        flight.start = start; flight.end = end; flight.started = now;
        flight.duration = VisualFlightDuration(distance,speed);
        flight.active = true;
        flight.visual.SetPositionAndRotation(start,Quaternion.LookRotation(end-start));
        flight.visual.localScale = new Vector3(BulletWidth,BulletWidth,Mathf.Min(BulletLength,distance*.25f));
        flight.visual.gameObject.SetActive(true);
    }

    public void Tick(double now)
    {
        if (now >= flashEnds || !muzzle) flash.gameObject.SetActive(false);
        else
        {
            // Follow both aim movement and player movement for the entire flash lifetime.
            flash.SetPositionAndRotation(muzzle.position + muzzle.forward*.005f,muzzle.rotation);
            if (view) burst.rotation = view.rotation;
            float fade = Mathf.Clamp01((float)((flashEnds-now)/FlashDuration));
            flash.localScale = Vector3.one * (.5f+.5f*fade);
            light.intensity = 2f*fade;
        }
        foreach (Flight flight in flights)
        {
            if (!flight.active) continue;
            float t = Mathf.Clamp01((float)((now-flight.started)/flight.duration));
            flight.visual.position = Vector3.Lerp(flight.start,flight.end,t);
            if (t >= 1f) { flight.active=false; flight.visual.gameObject.SetActive(false); }
        }
    }

    public void Clear()
    {
        flashEnds = 0d;
        // Scene teardown may destroy the independent effects root before the
        // player's OnDisable runs. Unity's destroyed-object check is required.
        if (flash) flash.gameObject.SetActive(false);
        foreach (Flight flight in flights)
        {
            if (flight == null) continue;
            flight.active=false;
            if (flight.visual) flight.visual.gameObject.SetActive(false);
        }
    }
    public void Dispose()
    {
        Clear();
        if (root) Object.Destroy(root);
        if (bulletMesh) Object.Destroy(bulletMesh);
        if (flashMesh) Object.Destroy(flashMesh);
        if (burstMesh) Object.Destroy(burstMesh);
    }
    public static float VisualFlightDuration(float distance, float speed)
        => Mathf.Clamp(distance / Mathf.Max(1f,speed),.025f,.04f);

    private static Mesh CreateBurstMesh()
    {
        var vertices = new Vector3[17]; var triangles = new int[48];
        for (int i=0; i<16; i++)
        {
            float angle=i*Mathf.PI/8f;
            float radius=i%2==0 ? 1f : .4f;
            vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
            triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%16+1;
        }
        var mesh=new Mesh {name="Camera Facing Muzzle Burst"};
        mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
    }

    private static Mesh CreateFlashMesh()
    {
        var vertices = new Vector3[24]; var triangles = new int[24];
        for (int i=0; i<8; i++)
        {
            float angle=i*Mathf.PI*.25f;
            Vector3 side=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            Vector3 across=new Vector3(-side.y,side.x,0);
            vertices[i*3]=side*.015f-across*.02f;
            vertices[i*3+1]=side*(i%2==0 ? .07f : .04f)+Vector3.forward*(i%2==0 ? .2f : .12f);
            vertices[i*3+2]=side*.015f+across*.02f;
            triangles[i*3]=i*3;triangles[i*3+1]=i*3+1;triangles[i*3+2]=i*3+2;
        }
        var mesh=new Mesh {name="Muzzle Flame"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();return mesh;
    }
}
