using System.Collections.Generic;
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    // A shared list of every box currently in the world. The spawner and the enemy AI both use it.
    public static List<PowerUpPickup> All = new List<PowerUpPickup>();

    public PowerUpType type;
    public float lifetime = 40f;   // disappears if nobody takes it

    void OnEnable()  { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        GetComponent<SpriteRenderer>().color = ColorFor(type);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Gentle pulse so the boxes catch the eye
        float s = 0.8f + Mathf.Sin(Time.time * 4f) * 0.08f;
        transform.localScale = new Vector3(s, s, 1f);
    }

    public static Color ColorFor(PowerUpType t)
    {
        switch (t)
        {
            case PowerUpType.DoubleDamage: return Color.yellow;
            case PowerUpType.DoubleArmor:  return new Color(0.3f, 0.6f, 1f);   // blue
            default:                       return new Color(0.8f, 0.4f, 1f);   // purple (speed)
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PowerUpReceiver receiver = other.GetComponentInParent<PowerUpReceiver>();
        if (receiver == null) return;   // not a tank (a wall, for example)

        receiver.Activate(type);
        Destroy(gameObject);
    }
}