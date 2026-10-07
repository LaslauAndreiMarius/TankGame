using UnityEngine;

public class TrackAnimationControl : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponentInParent<Rigidbody2D>();   // the tank's rigidbody is on the root
    }

    void Update()
    {
        bool moving = rb.linearVelocity.magnitude > 0.1f || Mathf.Abs(rb.angularVelocity) > 5f;

        // speed 1 = play normally, speed 0 = freeze on the current frame
        animator.speed = moving ? 1f : 0f;
    }
}