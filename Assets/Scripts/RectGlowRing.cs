using UnityEngine;

public class RectGlowRing : MonoBehaviour
{
    [Header("Corners (UI RectTransforms)")]
    public RectTransform topLeft;
    public RectTransform topRight;
    public RectTransform bottomRight;
    public RectTransform bottomLeft;

    [Header("Movement")]
    public float speed = 200f;

    [Header("Pulse")]
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public float pulseSpeed = 2f;

    private RectTransform rt;
    private Vector2[] points;

    private float distance;
    private float pulseT;

    private float[] lengths;
    private float totalLength;

    private int side;

    void Start()
    {
        rt = GetComponent<RectTransform>();

        // 🔥 مهم: تبدیل به LOCAL SPACE والد (Canvas/Button)
        RectTransform parent = topLeft.parent as RectTransform;

        points = new Vector2[4];
        points[0] = parent.InverseTransformPoint(topLeft.position);
        points[1] = parent.InverseTransformPoint(topRight.position);
        points[2] = parent.InverseTransformPoint(bottomRight.position);
        points[3] = parent.InverseTransformPoint(bottomLeft.position);

        lengths = new float[4];

        lengths[0] = Vector2.Distance(points[0], points[1]);
        lengths[1] = Vector2.Distance(points[1], points[2]);
        lengths[2] = Vector2.Distance(points[2], points[3]);
        lengths[3] = Vector2.Distance(points[3], points[0]);

        totalLength = lengths[0] + lengths[1] + lengths[2] + lengths[3];

        rt.localPosition = points[0];
    }

    void Update()
    {
        Move();
        Pulse();
    }

    void Move()
    {
        distance += speed * Time.deltaTime;

        if (distance > totalLength)
            distance -= totalLength;

        float d = distance;

        if (d < lengths[0])
        {
            rt.localPosition = Vector2.Lerp(points[0], points[1], d / lengths[0]);
            side = 0;
        }
        else if (d < lengths[0] + lengths[1])
        {
            d -= lengths[0];
            rt.localPosition = Vector2.Lerp(points[1], points[2], d / lengths[1]);
            side = 1;
        }
        else if (d < lengths[0] + lengths[1] + lengths[2])
        {
            d -= lengths[0] + lengths[1];
            rt.localPosition = Vector2.Lerp(points[2], points[3], d / lengths[2]);
            side = 2;
        }
        else
        {
            d -= lengths[0] + lengths[1] + lengths[2];
            rt.localPosition = Vector2.Lerp(points[3], points[0], d / lengths[3]);
            side = 3;
        }

        UpdateRotation();
    }

    void UpdateRotation()
    {
        rt.localEulerAngles = new Vector3(0, 0, -90f * side);
    }

    void Pulse()
    {
        pulseT += Time.deltaTime * pulseSpeed;

        float s = Mathf.Lerp(minScale, maxScale,
            (Mathf.Sin(pulseT) + 1f) * 0.5f);

        rt.localScale = new Vector3(s, s, 1f);
    }
}