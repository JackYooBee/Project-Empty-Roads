using UnityEngine;

public class WallLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Color lightColor = Color.white;
    [SerializeField] private float intensity = 2f;
    [SerializeField] private float range = 3f;
    [SerializeField] private float spotAngle = 60f;

    private Light lightComponent;

    void Start()
    {
        SetupLight();
    }

    void SetupLight()
    {
        // Add Light component if it doesn't exist
        lightComponent = GetComponent<Light>();
        if (lightComponent == null)
        {
            lightComponent = gameObject.AddComponent<Light>();
        }

        // Configure light for wall illumination
        lightComponent.type = LightType.Spot;
        lightComponent.color = lightColor;
        lightComponent.intensity = intensity;
        lightComponent.range = range;
        lightComponent.spotAngle = spotAngle;

        // Settings to focus light on nearby surfaces
        lightComponent.innerSpotAngle = 30f; // Creates softer falloff
        lightComponent.shadows = LightShadows.Soft; // Optional: adds soft shadows

        // Reduce light falloff for better wall illumination
        lightComponent.bounceIntensity = 1.5f; // Enhances indirect lighting
    }

    // Optional: Methods to control the light at runtime
    public void ToggleLight()
    {
        if (lightComponent != null)
        {
            lightComponent.enabled = !lightComponent.enabled;
        }
    }

    public void SetIntensity(float newIntensity)
    {
        if (lightComponent != null)
        {
            lightComponent.intensity = newIntensity;
        }
    }

    public void SetColor(Color newColor)
    {
        if (lightComponent != null)
        {
            lightComponent.color = newColor;
        }
    }
}