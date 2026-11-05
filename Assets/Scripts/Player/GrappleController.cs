using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody), typeof(InputReader))]
public class GrappleController : MonoBehaviour
{
    [SerializeField] private Transform aim;
    [SerializeField] private LayerMask grappleLayers = ~0;
    [SerializeField, Min(1f)] private float maxDistance = 35f;
    [SerializeField, Min(0.1f)] private float minimumLength = 2f;
    [SerializeField, Min(0f)] private float reelSpeed = 8f;
    [SerializeField, Min(0f)] private float slackSpeed = 10f;
    [SerializeField, Min(0.001f)] private float ropeWidth = 0.035f;
    [SerializeField] private Material ropeMaterial;
    [SerializeField, Min(0f)] private float swingAcceleration = 18f;
    [SerializeField, Min(0f)] private float swingAssistSpeed = 20f;
    [SerializeField, Range(1, 16)] private int maxWrapPoints = 8;
    [SerializeField, Min(0.02f)] private float cornerClearance = 0.12f;
    public bool IsAttached => joint != null;
    public float RopeLength { get; private set; }
    private Rigidbody body;
    private InputReader input;
    private ConfigurableJoint joint;
    private Collider target;
    private Vector3 localAnchor;
    private LineRenderer line;
    private readonly List<RopeCorner> corners = new List<RopeCorner>();
    private Vector3 currentAnchor;
    private float freeLength;
    public bool IsTaut => IsAttached && RopeWillTighten(body.worldCenterOfMass - currentAnchor,
        body.linearVelocity - AnchorVelocity(), freeLength, Time.fixedDeltaTime);

    private sealed class RopeCorner
    {
        public Collider collider;
        public Vector3 local;
        public Vector3 Position => collider.transform.TransformPoint(local);
    }

    private Vector3 AnchorVelocity() => joint && joint.connectedBody
        ? joint.connectedBody.GetPointVelocity(currentAnchor) : Vector3.zero;

    public void SyncBodyAnchor()
    {
        if (joint) joint.anchor = body.centerOfMass;
    }

    public void ApplySwingControl()
    {
        if (!IsAttached || !aim || !input.GameplayActive) return;
        Vector3 force = SwingAcceleration(body.linearVelocity - AnchorVelocity(),
            body.worldCenterOfMass - currentAnchor, aim.forward, aim.right, input.Move,
            swingAcceleration, swingAssistSpeed, Time.fixedDeltaTime);
        body.AddForce(force, ForceMode.Acceleration);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        input = GetComponent<InputReader>();
        if (!aim)
        {
            Camera view = GetComponentInChildren<Camera>();
            if (view) aim = view.transform;
        }
        var visual = new GameObject("Grapple Rope");
        visual.transform.SetParent(transform, false);
        line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = line.endWidth = ropeWidth;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = ropeMaterial;
        line.startColor = line.endColor = new Color(0.15f, 0.9f, 1f);
        line.enabled = false;
    }

    private void OnEnable()
    {
        if (!input) input = GetComponent<InputReader>();
        input.OnInputsCleared += Release;
    }

    private void OnDisable()
    {
        if (input) input.OnInputsCleared -= Release;
        Release();
    }

    // Called by movement before its physics decisions: no competing FixedUpdate order.
    public void StepInput()
    {
        if (!isActiveAndEnabled) return;
        bool pressed = input.ConsumeGrapplePressed();
        if (!input.IsGrappleHeld) { Release(); return; }
        if (pressed && !IsAttached) TryAttach();
        if (!IsAttached) return;
        if (!target || !target.enabled || !target.gameObject.activeInHierarchy)
        { Release(); return; }
        Vector3 anchor = target.transform.TransformPoint(localAnchor);
        if (!UpdateCorners(anchor)) { Release(); return; }
        float fixedLength = FixedLength(anchor);
        currentAnchor = corners.Count == 0 ? anchor : corners[corners.Count - 1].Position;
        Vector3 offset = currentAnchor - body.worldCenterOfMass;
        if (offset.magnitude + fixedLength > maxDistance + 2f) { Release(); return; }
        Rigidbody anchorBody = corners.Count == 0 ? target.attachedRigidbody : corners[corners.Count - 1].collider.attachedRigidbody;
        if (joint.connectedBody != anchorBody) joint.connectedBody = anchorBody;
        joint.connectedAnchor = anchorBody ? anchorBody.transform.InverseTransformPoint(currentAnchor) : currentAnchor;
        // Do not wind up a severely stretched constraint against a blocking obstacle.
        float reelInput = AllowedReelInput(input.GrappleReelInput, offset.magnitude + fixedLength, RopeLength);
        RopeLength = AdjustLength(RopeLength, reelInput, reelSpeed, slackSpeed,
            Mathf.Max(minimumLength, fixedLength + 0.5f), maxDistance, Time.fixedDeltaTime);
        freeLength = RopeLength - fixedLength;
        SoftJointLimit limit = joint.linearLimit;
        limit.limit = freeLength;
        joint.linearLimit = limit;
    }

    private void TryAttach()
    {
        if (!aim) return;
        RaycastHit? nearest = null;
        foreach (RaycastHit hit in Physics.RaycastAll(aim.position, aim.forward, maxDistance,
            grappleLayers, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider)) continue;
            if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
        }
        if (!nearest.HasValue) return;
        RaycastHit selected = nearest.Value;
        float distance = Vector3.Distance(body.worldCenterOfMass, selected.point);
        if (distance < minimumLength || distance > maxDistance
            || Blocked(body.worldCenterOfMass, selected.point - body.worldCenterOfMass, selected.collider)) return;
        target = selected.collider;
        localAnchor = target.transform.InverseTransformPoint(selected.point);
        RopeLength = distance;
        currentAnchor = selected.point;
        freeLength = distance;
        corners.Clear();
        joint = gameObject.AddComponent<ConfigurableJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = transform.InverseTransformPoint(body.worldCenterOfMass);
        joint.connectedBody = selected.rigidbody;
        joint.connectedAnchor = selected.rigidbody
            ? selected.rigidbody.transform.InverseTransformPoint(selected.point) : selected.point;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Limited;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
        joint.linearLimit = new SoftJointLimit { limit = RopeLength, contactDistance = 0.01f };
        joint.projectionMode = JointProjectionMode.None;
        joint.enableCollision = true;
        // The solver enforces a maximum distance, not a fixed distance: slack stays free.
    }

    private bool IsSelf(Collider collider) => collider.attachedRigidbody == body || collider.transform.IsChildOf(transform);

    private bool Blocked(Vector3 origin, Vector3 offset, Collider anchorCollider)
        => FindBlock(origin, origin + offset, anchorCollider, out _);

    private bool FindBlock(Vector3 origin, Vector3 end, Collider endpoint, out RaycastHit nearest)
    {
        Vector3 offset = end - origin;
        nearest = default;
        float distance = float.PositiveInfinity;
        if (offset.sqrMagnitude < 0.0001f) return false;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, offset.normalized,
            Mathf.Max(0f, offset.magnitude - 0.05f), ~0, QueryTriggerInteraction.Ignore))
            if (!IsSelf(hit.collider) && (hit.collider != endpoint
                || hit.distance < offset.magnitude - 0.15f) && hit.distance < distance)
            { nearest = hit; distance = hit.distance; }
        return distance < float.PositiveInfinity;
    }

    private float FixedLength(Vector3 anchor)
    {
        float length = 0f;
        foreach (RopeCorner corner in corners)
        {
            Vector3 next = corner.Position;
            length += Vector3.Distance(anchor, next);
            anchor = next;
        }
        return length;
    }

    private bool UpdateCorners(Vector3 anchor)
    {
        foreach (RopeCorner corner in corners)
            if (!corner.collider || !corner.collider.enabled || !corner.collider.gameObject.activeInHierarchy) return false;
        Vector3 player = body.worldCenterOfMass;
        // Remove the nearest bend whenever the next segment becomes directly visible.
        while (corners.Count > 0)
        {
            Vector3 next = corners.Count == 1 ? anchor : corners[corners.Count - 2].Position;
            Collider end = corners.Count == 1 ? target : corners[corners.Count - 2].collider;
            if (FindBlock(player, next, end, out _)) break;
            corners.RemoveAt(corners.Count - 1);
        }
        // Moving geometry must not make an older segment pass through solid objects.
        Vector3 previous = anchor;
        Collider previousCollider = target;
        foreach (RopeCorner corner in corners)
        {
            if (FindBlock(corner.Position, previous, previousCollider, out _)) return false;
            previous = corner.Position;
            previousCollider = corner.collider;
        }
        Collider endpoint = corners.Count == 0 ? target : corners[corners.Count - 1].collider;
        if (!FindBlock(player, previous, endpoint, out RaycastHit obstruction)) return FixedLength(anchor) + 0.5f < RopeLength;
        // Find a collision-free route around the obstructing collider's box corners.
        // BoxColliders use their oriented local box; other shapes use conservative bounds.
        Vector3[] nodes = new Vector3[18];
        nodes[0] = player;
        int goal = nodes.Length - 1;
        nodes[goal] = previous;
        BoxCollider box = obstruction.collider as BoxCollider;
        Bounds bounds = obstruction.collider.bounds;
        Vector3 padding = Vector3.one * cornerClearance;
        if (box)
        {
            Vector3 scale = box.transform.lossyScale;
            padding = new Vector3(cornerClearance / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                cornerClearance / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
                cornerClearance / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        }
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
            if (box)
            {
                nodes[i + 1] = box.transform.TransformPoint(box.center + Vector3.Scale(box.size * 0.5f + padding, sign));
            }
            else nodes[i + 1] = bounds.center + Vector3.Scale(bounds.extents + Vector3.one * cornerClearance, sign);
        }
        // Mid-height edge candidates let the rope round a tall pillar/building
        // without routing all the way to its roof or base.
        for (int ring = 0; ring < 2; ring++)
        {
            Vector3 sample = ring == 0 ? obstruction.point : player;
            for (int i = 0; i < 4; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, 0f, (i & 2) == 0 ? -1 : 1);
                Vector3 center = box ? box.center : bounds.center;
                Vector3 extents = box ? box.size * 0.5f : bounds.extents;
                float y = box ? box.transform.InverseTransformPoint(sample).y : sample.y;
                Vector3 candidate = center + Vector3.Scale(extents + padding, sign);
                candidate.y = Mathf.Clamp(y, center.y - extents.y, center.y + extents.y);
                nodes[9 + ring * 4 + i] = box ? box.transform.TransformPoint(candidate) : candidate;
            }
        }
        int[] path = FindCornerRoute(nodes,
            (a, b) => !FindBlock(nodes[a], nodes[b], b == goal ? endpoint : null, out _), out float routeLength);
        // Never create extra rope or accept a route that would violently shorten the constraint.
        float fixedLength = FixedLength(anchor);
        if (path.Length == 0 || fixedLength + routeLength > RopeLength + 0.25f) return false;
        var route = new List<RopeCorner>();
        for (int i = path.Length - 2; i > 0; i--)
            route.Add(new RopeCorner { collider = obstruction.collider,
                local = obstruction.collider.transform.InverseTransformPoint(nodes[path[i]]) });
        if (corners.Count + route.Count > maxWrapPoints) return false;
        corners.AddRange(route);
        return FixedLength(anchor) + 0.5f < RopeLength;
    }

    public static int[] FindCornerRoute(Vector3[] nodes, System.Func<int, int, bool> isClear, out float length)
    {
        int count = nodes.Length;
        float[] distances = new float[count];
        int[] parents = new int[count];
        bool[] visited = new bool[count];
        for (int i = 0; i < count; i++) { distances[i] = float.PositiveInfinity; parents[i] = -1; }
        distances[0] = 0f;
        for (int pass = 0; pass < count; pass++)
        {
            int closest = -1;
            for (int i = 0; i < count; i++)
                if (!visited[i] && (closest < 0 || distances[i] < distances[closest])) closest = i;
            if (closest < 0 || float.IsPositiveInfinity(distances[closest])) break;
            if (closest == count - 1) break;
            visited[closest] = true;
            for (int i = 1; i < count; i++)
            {
                if (visited[i] || i == closest) continue;
                float candidate = distances[closest] + Vector3.Distance(nodes[closest], nodes[i]);
                if (candidate >= distances[i] || !isClear(closest, i)) continue;
                distances[i] = candidate;
                parents[i] = closest;
            }
        }
        length = distances[count - 1];
        if (parents[count - 1] < 0) return System.Array.Empty<int>();
        var path = new List<int>();
        for (int i = count - 1; i >= 0; i = parents[i]) path.Add(i);
        path.Reverse();
        return path.ToArray();
    }

    public void Release()
    {
        if (joint)
        {
            // Destroy is deferred; free constraints immediately for this physics step.
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;
            Destroy(joint);
            joint = null;
        }
        target = null;
        corners.Clear();
        if (line) line.enabled = false;
        // Never overwrite velocity on release.
    }

    private void LateUpdate()
    {
        if (!input.GameplayActive) Release();
        if (!IsAttached || !target || !aim) { if (line) line.enabled = false; return; }
        line.enabled = true;
        line.positionCount = corners.Count + 2;
        line.SetPosition(0, aim.position + aim.right * 0.2f - aim.up * 0.15f);
        for (int i = 0; i < corners.Count; i++)
        {
            if (!corners[corners.Count - 1 - i].collider) { Release(); return; }
            line.SetPosition(i + 1, corners[corners.Count - 1 - i].Position);
        }
        line.SetPosition(corners.Count + 1, target.transform.TransformPoint(localAnchor));
    }

    private void OnDestroy()
    {
        if (line) Destroy(line.gameObject);
    }

    public static float AllowedReelInput(float input, float distance, float length)
        => input > 0f && distance > length + 0.25f ? 0f : input;

    public static bool RopeWillTighten(Vector3 offset, Vector3 relativeVelocity, float length, float dt)
        => offset.magnitude + Mathf.Max(0f, Vector3.Dot(relativeVelocity, offset.normalized)) * dt >= length - 0.12f;

    public static Vector3 SwingAcceleration(Vector3 velocity, Vector3 ropeOffset, Vector3 forward,
        Vector3 right, Vector2 move, float acceleration, float assistSpeed, float dt)
    {
        move = Vector2.ClampMagnitude(move, 1f);
        forward = new Vector3(forward.x, 0f, forward.z).normalized;
        right = new Vector3(right.x, 0f, right.z).normalized;
        Vector3 wish = forward * move.y + right * move.x;
        Vector3 tangent = Vector3.ProjectOnPlane(wish, ropeOffset.normalized);
        if (tangent.sqrMagnitude < 0.0001f || dt <= 0f) return Vector3.zero;
        Vector3 direction = tangent.normalized;
        float available = Mathf.Max(0f, assistSpeed - Vector3.Dot(velocity, direction)) / dt;
        return direction * Mathf.Min(acceleration * tangent.magnitude, available);
    }

    public static float AdjustLength(float length, float input, float reelRate, float slackRate,
        float minimum, float maximum, float dt)
    {
        float rate = input >= 0f ? reelRate : slackRate;
        return Mathf.Clamp(length - Mathf.Clamp(input, -1f, 1f) * rate * dt, minimum, maximum);
    }
}
