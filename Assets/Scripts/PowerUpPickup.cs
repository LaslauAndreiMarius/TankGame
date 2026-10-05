using System.Collections.Generic;
using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    // A shared list of every box currently in the world. The spawner and the enemy AI both use it.
    public static List<PowerUpPickup> All = new List<PowerUpPickup>();

    public PowerUpType type;
    public float lifetime = 40f;   // disappears if nobody takes it
    public AudioClip pickupSound;
    
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
            case PowerUpType.DoubleDamage: return Color.darkRed;
            case PowerUpType.DoubleArmor:  return Color.darkBlue;   // blue
            default:                       return Color.yellow;   // purple (speed)
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PowerUpReceiver receiver = other.GetComponentInParent<PowerUpReceiver>();
        if (receiver == null) return;   // not a tank (a wall, for example)

        receiver.Activate(type);
        Sfx.Play(pickupSound, transform.position);
        Destroy(gameObject);
    }
}