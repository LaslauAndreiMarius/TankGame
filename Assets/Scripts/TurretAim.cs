using UnityEngine;
using UnityEngine.InputSystem;

public class TurretAim : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // Mouse position is in pixels; convert to a point in the game world
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorld = cam.ScreenToWorldPoint(screenPos);

        // Direction from turret to mouse, converted to an angle
        Vector2 direction = mouseWorld - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}