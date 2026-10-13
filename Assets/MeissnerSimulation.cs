using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MeissnerSimulation : MonoBehaviour
{
    [Header("UI Controls")]
    public Slider tempSlider;
    public Slider magneticSlider;
    public TextMeshProUGUI statusText;

    [Header("Simulation Objects")]
    public Transform superconductor;
    public Transform magnet;
    public Material scMaterial; // Drag SuperconductorMat here

    [Header("Parameters")]
    public float criticalTemp = 93.0f; // YBCO Tc in Kelvin
    public int lineCount = 14;
    public int pointsPerLine = 35;
    public float lineRadius = 1.6f;

    private LineRenderer[] lineRenderers;
    private Material scRuntimeMat;
    private float floatTimer = 0f;

    void Start()
    {
        // Setup Renderer and Material instance for the superconductor
        if (superconductor != null)
        {
            Renderer scRenderer = superconductor.GetComponent<Renderer>();
            if (scMaterial != null)
            {
                scRenderer.material = new Material(scMaterial);
            }
            scRuntimeMat = scRenderer.material;
        }

        // Initialize Dynamic Field Lines
        lineRenderers = new LineRenderer[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject("FieldLine_" + i);
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.06f;
            lr.endWidth = 0.06f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderers[i] = lr;
        }
    }

    void Update()
    {
        float currentTemp = tempSlider != null ? tempSlider.value : 100f;
        float fieldStrength = magneticSlider != null ? magneticSlider.value : 5f;
        bool isSuperconducting = currentTemp < criticalTemp;

        // 1. Dynamic Superconductor Thermal Color Shift
        if (scRuntimeMat != null)
        {
            Color normalColor = new Color(0.2f, 0.2f, 0.25f); // Dark metallic grey
            Color superColor = new Color(0.1f, 0.8f, 1.0f);   // Glowing cyan
            float t = Mathf.InverseLerp(criticalTemp + 30f, criticalTemp - 30f, currentTemp);
            scRuntimeMat.color = Color.Lerp(normalColor, superColor, t);
        }

        // 2. Levitation with Hover Motion
        if (magnet != null && superconductor != null)
        {
            Vector3 basePos = superconductor.position;
            float targetY = isSuperconducting ? 2.6f : 1.25f;

            if (isSuperconducting)
            {
                floatTimer += Time.deltaTime * 2.5f;
                targetY += Mathf.Sin(floatTimer) * 0.08f;
            }

            Vector3 targetPos = new Vector3(basePos.x, basePos.y + targetY, basePos.z);
            magnet.position = Vector3.Lerp(magnet.position, targetPos, Time.deltaTime * 4f);
        }

        // 3. UI Status Text Updates
        if (statusText != null)
        {
            statusText.text = $"Temp: {currentTemp:F1} K | Tc: {criticalTemp} K\n" +
                              $"Field (B): {fieldStrength:F1} T\n" +
                              $"State: {(isSuperconducting ? "<color=#00FFCC>SUPERCONDUCTING (Meissner Effect)</color>" : "<color=#FF4444>NORMAL STATE</color>")}";
        }

        // 4. Update Field Lines Visuals
        UpdateFieldLines(isSuperconducting, fieldStrength);
    }

    void UpdateFieldLines(bool isSuperconducting, float B)
    {
        if (lineRenderers == null || superconductor == null) return;

        float angleStep = 360f / lineCount;
        Vector3 scPos = superconductor.position;
        float scRadius = superconductor.localScale.x * 0.85f;

        // Dynamic Line Thickness based on Field Strength B, but fixed Cyan color
        float thickness = Mathf.Lerp(0.02f, 0.10f, B / 10f);
        Color constantLineColor = Color.cyan;

        for (int i = 0; i < lineCount; i++)
        {
            LineRenderer lr = lineRenderers[i];
            if (lr == null) continue;

        if (B < 0.1f)
            {
                lr.enabled = false;
                continue;
            }

            lr.enabled = true;
            lr.startWidth = thickness;
            lr.endWidth = thickness;
            lr.startColor = constantLineColor;
            lr.endColor = constantLineColor;
            lr.positionCount = pointsPerLine;

            float angle = i * angleStep * Mathf.Deg2Rad;
            float currentRadius = lineRadius * (1.0f + (B * 0.04f));
            float xOffset = Mathf.Cos(angle) * currentRadius;
            float zOffset = Mathf.Sin(angle) * currentRadius;

            for (int j = 0; j < pointsPerLine; j++)
            {
                float t = (float)j / (pointsPerLine - 1);
                float y = Mathf.Lerp(-4f, 4f, t);

                Vector3 point = new Vector3(xOffset, y, zOffset);

                if (isSuperconducting)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(point.x, point.z), Vector2.zero);
                    float yDist = Mathf.Abs(point.y - scPos.y);

                    if (yDist < 1.8f && distFromCenter < scRadius * 2.2f)
                    {
                        float bendStrength = 1.0f + (B * 0.12f);
                        float bendFactor = Mathf.Clamp01(1.0f - (yDist / 1.8f));
                        Vector2 dir = new Vector2(point.x, point.z).normalized;
                        Vector2 bentPos = dir * (scRadius + (0.45f * bendStrength));

                        point.x = Mathf.Lerp(point.x, bentPos.x, bendFactor);
                        point.z = Mathf.Lerp(point.z, bentPos.y, bendFactor);
                    }
                }

                lr.SetPosition(j, point);
            }
        }
    }
}