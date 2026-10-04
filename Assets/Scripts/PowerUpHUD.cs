using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpHUD : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public PowerUpType type;
        public Image bar;               // the Image object (also the thing we show/hide)
        public TextMeshProUGUI label;
    }

    public Entry[] entries;

    private PowerUpReceiver receiver;

    void Update()
    {
        // Find the player's receiver (and find it again after a respawn)
        if (receiver == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) receiver = p.GetComponent<PowerUpReceiver>();
        }

        foreach (Entry e in entries)
        {
            float remaining = receiver != null ? receiver.GetRemaining(e.type) : 0f;

            e.bar.gameObject.SetActive(remaining > 0f);   // only visible while active
            if (remaining <= 0f) continue;

            e.label.text = Mathf.CeilToInt(remaining) + "s";
            e.bar.fillAmount = remaining / receiver.duration;
        }
    }
}