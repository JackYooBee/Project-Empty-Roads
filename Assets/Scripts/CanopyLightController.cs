using UnityEngine;

public class CanopyLightController : MonoBehaviour
{
    [Header("Light Components")]
    public Light pointLight;
    public Renderer lightRenderer; // The object with the emissive material

    [Header("Light Settings")]
    public float lightIntensity = 2f;
    public float lightRange = 5f;
    public Color lightColor = Color.yellow;

    [Header("Emissive Material Settings")]
    public float emissiveIntensity = 2f;
    public string emissivePropertyName = "_EmissionColor"; 

    [Header("Flickering Settings")]
    public bool enableFlicker = false;
    public float flickerMinIntensity = 0.3f;
    public float flickerMaxIntensity = 1.2f;
    public float flickerMinInterval = 0.1f;
    public float flickerMaxInterval = 2f;

    private float baseIntensity;
    private float baseEmissiveIntensity;
    private Material lightMaterial;
    private Color baseEmissiveColor;
    private float nextFlickerTime;
    private bool isFlickering = false;
    private float flickerDuration = 0.1f;

    private void Start()
    {
        SetupLight();
        SetupMaterial();

        if (enableFlicker)
        {
            ScheduleNextFlicker();
        }
    }

    private void SetupLight()
    {
        // Create point light if not assigned
        if (pointLight == null)
        {
            GameObject lightObj = new GameObject("Point Light");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            pointLight = lightObj.AddComponent<Light>();
        }

        // Configure the point light
        pointLight.type = LightType.Point;
        pointLight.intensity = lightIntensity;
        pointLight.range = lightRange;
        pointLight.color = lightColor;
        pointLight.shadows = LightShadows.Soft;

        baseIntensity = lightIntensity;
    }

    private void SetupMaterial()
    {
        // Get the renderer if not assigned
        if (lightRenderer == null)
        {
            lightRenderer = GetComponent<Renderer>();
        }

        if (lightRenderer != null)
        {
            // Create instance of material to avoid affecting all objects with same material
            lightMaterial = lightRenderer.material;

            // Store base emissive values
            if (lightMaterial.HasProperty(emissivePropertyName))
            {
                baseEmissiveColor = lightMaterial.GetColor(emissivePropertyName);
                baseEmissiveIntensity = emissiveIntensity;

                // Apply initial emissive intensity
                UpdateEmissiveIntensity(emissiveIntensity);
            }
        }
    }

    private void Update()
    {
        if (enableFlicker && Time.time >= nextFlickerTime)
        {
            StartFlicker();
        }

        if (isFlickering)
        {
            UpdateFlicker();
        }
    }

    private void ScheduleNextFlicker()
    {
        nextFlickerTime = Time.time + Random.Range(flickerMinInterval, flickerMaxInterval);
    }

    private void StartFlicker()
    {
        isFlickering = true;
        flickerDuration = Random.Range(0.05f, 0.3f);
        Invoke(nameof(StopFlicker), flickerDuration);
    }

    private void StopFlicker()
    {
        isFlickering = false;

        // Reset to normal intensity
        SetLightIntensity(1f);

        // Schedule next flicker
        ScheduleNextFlicker();
    }

    private void UpdateFlicker()
    {
        // Random flicker intensity
        float flickerMultiplier = Random.Range(flickerMinIntensity, flickerMaxIntensity);
        SetLightIntensity(flickerMultiplier);
    }

    private void SetLightIntensity(float multiplier)
    {
        // Update point light
        if (pointLight != null)
        {
            pointLight.intensity = baseIntensity * multiplier;
        }

        // Update emissive material
        UpdateEmissiveIntensity(baseEmissiveIntensity * multiplier);
    }

    private void UpdateEmissiveIntensity(float intensity)
    {
        if (lightMaterial != null && lightMaterial.HasProperty(emissivePropertyName))
        {
            Color emissiveColor = baseEmissiveColor * intensity;
            lightMaterial.SetColor(emissivePropertyName, emissiveColor);
        }
    }

    // Public methods for external control
    public void SetFlickering(bool flicker)
    {
        enableFlicker = flicker;
        if (flicker)
        {
            ScheduleNextFlicker();
        }
        else
        {
            isFlickering = false;
            SetLightIntensity(1f);
        }
    }

    public void SetBaseBrightness(float intensity)
    {
        baseIntensity = intensity;
        baseEmissiveIntensity = intensity;
        if (!isFlickering)
        {
            SetLightIntensity(1f);
        }
    }
}