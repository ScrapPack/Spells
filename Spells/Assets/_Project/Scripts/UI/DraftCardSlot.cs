using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Individual card slot in the draft UI.
/// Shows card name, positive effects, negative effects, tier indicator.
/// </summary>
public class DraftCardSlot : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardArtImage;
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI positiveText;
    [SerializeField] private TextMeshProUGUI negativeText;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private Image[] tierPips;

    [Header("Colors")]
    [SerializeField] private Color tier1Color = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color tier2Color = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color tier3Color = new Color(1f, 0.6f, 0f);

    private PowerCardData currentCard;

    public void Show(PowerCardData card)
    {
        currentCard = card;
        gameObject.SetActive(true);

        if (cardNameText != null)
            cardNameText.text = card.cardName;
        if (positiveText != null)
            positiveText.text = card.positiveDescription;
        if (negativeText != null)
            negativeText.text = card.negativeDescription;

        // Tier display
        if (tierText != null)
            tierText.text = $"Tier {card.tier}";

        Color tierColor = card.tier switch
        {
            2 => tier2Color,
            3 => tier3Color,
            _ => tier1Color
        };

        if (cardBackground != null)
            cardBackground.color = tierColor;

        // Tier pips
        if (tierPips != null)
        {
            for (int i = 0; i < tierPips.Length; i++)
            {
                if (tierPips[i] != null)
                {
                    tierPips[i].gameObject.SetActive(i < card.tier);
                    tierPips[i].color = tierColor;
                }
            }
        }

        // Card art
        if (cardArtImage != null)
        {
            if (card.cardArt != null)
            {
                cardArtImage.sprite = card.cardArt;
                cardArtImage.gameObject.SetActive(true);
            }
            else
            {
                cardArtImage.gameObject.SetActive(false);
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentCard = null;
    }
}
