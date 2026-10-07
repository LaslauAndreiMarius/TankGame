using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    private Weapon weapon;

    void Awake()
    {
        weapon = GetComponent<Weapon>();
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            weapon.TryShoot();
    }
}