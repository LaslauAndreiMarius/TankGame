using UnityEngine;

public static class Sfx
{
    public static void Play(AudioClip clip, Vector2 position, float volume = 1f)
    {
        if (clip == null || Camera.main == null) return;

        // Quieter the further the sound is from the camera (the player's view)
        float distance = Vector2.Distance(position, Camera.main.transform.position);
        float falloff = Mathf.Clamp01(1f - distance / 30f);
        if (falloff <= 0f) return;

        // Play at the camera: playing at the tank's position would be very quiet in 2D,
        // because the camera sits 10 units behind the scene.
        AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume * falloff);
    }
}