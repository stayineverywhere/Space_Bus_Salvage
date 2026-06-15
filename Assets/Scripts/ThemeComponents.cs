using UnityEngine;

/// <summary>
/// Damages the player while they stand in a toxic pool.
/// </summary>
public class ToxicPoolDamage : MonoBehaviour
{
    public float damagePerSecond = 8f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        pm?.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}

/// <summary>
/// Blinking orange warning light on mining drills.
/// </summary>
public class MiningWarningBlink : MonoBehaviour
{
    private Light _light;
    private float _timer;

    private void Start()
    {
        _light = GetComponent<Light>();
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_light != null)
            _light.enabled = Mathf.Sin(_timer * Mathf.PI * 2f) > 0f;
    }
}

/// <summary>
/// Slowly floats a rock up and down (Gravity planet).
/// </summary>
public class GravityFloatAnimation : MonoBehaviour
{
    private float _originY;
    private float _phase;
    private float _amplitude;
    private float _speed;

    private void Start()
    {
        _originY = transform.position.y;
        _phase = Random.Range(0f, Mathf.PI * 2f);
        _amplitude = Random.Range(0.3f, 1.2f);
        _speed = Random.Range(0.4f, 1.0f);
    }

    private void Update()
    {
        Vector3 p = transform.position;
        p.y = _originY + Mathf.Sin(Time.time * _speed + _phase) * _amplitude;
        transform.position = p;
        transform.Rotate(Vector3.up * 8f * Time.deltaTime, Space.World);
    }
}

/// <summary>
/// Pulsing purple glow at the gravity anomaly center.
/// </summary>
public class GravityAnomalyPulse : MonoBehaviour
{
    private Light _light;
    private float _baseIntensity;

    private void Start()
    {
        _light = GetComponent<Light>();
        if (_light != null) _baseIntensity = _light.intensity;
    }

    private void Update()
    {
        if (_light == null) return;
        float pulse = _baseIntensity + Mathf.Sin(Time.time * 2.2f) * (_baseIntensity * 0.5f);
        _light.intensity = pulse;
    }
}
