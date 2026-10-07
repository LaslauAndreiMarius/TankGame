using UnityEngine;

// Makes a UI element "pop" for a moment: call Punch() and it jumps bigger, then settles back.
public class UIPunch : MonoBehaviour
{
    public float punchScale = 1.35f;
    public float recoverSpeed = 10f;

    private float current = 1f;

    public void Punch()
    {
        current = punchScale;
    }

    void Update()
    {
        current = Mathf.Lerp(current, 1f, 1f - Mathf.Exp(-recoverSpeed * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(current, current, 1f);
    }
}
