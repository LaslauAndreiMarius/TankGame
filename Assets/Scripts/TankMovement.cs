using UnityEngine;
using UnityEngine.InputSystem;

public class TankMovement : MonoBehaviour
{
    [HideInInspector] public float speedMultiplier = 1f;
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;   // degrees per second

    private Rigidbody2D rb;
    private float forwardInput;
    private float turnInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Update runs every frame: good place to READ input
        var kb = Keyboard.current;
        if (kb == null) return;

        forwardInput = 0f;
        turnInput = 0f;

        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    forwardInput += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  forwardInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turnInput += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  turnInput -= 1f;
    }

    void FixedUpdate()
    {
        // FixedUpdate runs at a fixed rate synced with physics: good place to MOVE physics objects
        rb.linearVelocity = (Vector2)transform.up * forwardInput * moveSpeed * speedMultiplier;
        rb.angularVelocity = -turnInput * rotateSpeed;
    }
}