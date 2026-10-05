using System.Collections;
using UnityEngine;

public class OneShotEffect : MonoBehaviour
{
    public float clipLength = 1.25f;   // length of the animation in seconds at normal speed
    public float playbackSpeed = 1f;   // higher = faster

    IEnumerator Start()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.speed = playbackSpeed;

        // Realtime, so the effect still finishes and cleans itself up if the game is frozen
        yield return new WaitForSecondsRealtime(clipLength / playbackSpeed);
        Destroy(gameObject);
    }
}