using TMPro;
using UnityEngine;

// Shows the player's coin total and gives the number a little pop whenever it goes up.
public class CoinCounterUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public UIPunch punch;

    private int shown = -1;

    void OnEnable()
    {
        PlayerProfile.Changed += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        PlayerProfile.Changed -= Refresh;
    }

    void Refresh()
    {
        int coins = PlayerProfile.Coins;
        if (shown >= 0 && coins > shown && punch != null) punch.Punch();

        shown = coins;
        if (text != null) text.text = coins.ToString();
    }
}
