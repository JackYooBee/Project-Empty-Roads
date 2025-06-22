using UnityEngine;

public class PlayerLightFollow : MonoBehaviour
{
    [Header("Light Settings")]
    public Light playerLight;
    public float lightRange = 5f;
    public float lightIntensity = 2f;
    public Color lightColor = Color.white;

    [Header("Position Settings")]
    public Vector3 lightOffset = new Vector3(0, 1.5f, 0); // Slightly above player
    public bool smoothFollow = true;
    public float followSpeed = 10f;

    [Header("Flickering Effect")]
    public bool enableFlicker = false;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 2f;

    private float baseIntensity;

    private void Start()
    {
        // Create light if not assigned
        if (playerLight == null)
        {
            GameObject lightObj = new GameObject("Player Light");
            lightObj.transform.SetParent(transform);
            playerLight = lightObj.AddComponent<Light>();
        }

        // Configure the light
        playerLight.type = LightType.Point;
        playerLight.range = lightRange;
        playerLight.intensity = lightIntensity;
        playerLight.color = lightColor;
        playerLight.shadows = LightShadows.Soft; // For atmospheric effect

        baseIntensity = lightIntensity;

        // Position the light
        UpdateLightPosition();
    }

    private void Update()
    {
        UpdateLightPosition();

        if (enableFlicker)
        {
            ApplyFlickerEffect();
        }
    }

    private void UpdateLightPosition()
    {
        Vector3 targetPosition = transform.position + lightOffset;

        if (smoothFollow)
        {
            playerLight.transform.position = Vector3.Lerp(
                playerLight.transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            playerLight.transform.position = targetPosition;
        }
    }

    private void ApplyFlickerEffect()
    {
        float flicker = Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        playerLight.intensity = baseIntensity + flicker;
    }

    // Public methods to control the light
    public void SetLightIntensity(float intensity)
    {
        lightIntensity = intensity;
        baseIntensity = intensity;
        if (playerLight != null)
            playerLight.intensity = intensity;
    }

    public void SetLightRange(float range)
    {
        lightRange = range;
        if (playerLight != null)
            playerLight.range = range;
    }
}