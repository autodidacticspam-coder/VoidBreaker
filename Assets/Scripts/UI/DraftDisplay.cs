using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Events;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Displays draft options (choose 1 of 3) after encounters.
    /// </summary>
    public class DraftDisplay : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("Option Cards")]
        [SerializeField] private DraftOptionCard[] optionCards;
        [SerializeField] private Transform cardContainer;

        [Header("Skip Button")]
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI skipButtonText;

        [Header("Animation")]
        [SerializeField] private float cardRevealDelay = 0.2f;

        private DraftOption[] currentOptions;
        private RewardDrafter drafter;
        private Action<DraftOption> onSelectionCallback;

        private void Awake()
        {
            drafter = FindObjectOfType<RewardDrafter>();

            if (skipButton)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }
        }

        public void DisplayOptions(DraftOption[] options, Action<DraftOption> callback = null)
        {
            currentOptions = options;
            onSelectionCallback = callback;

            if (titleText)
            {
                titleText.text = "CHOOSE YOUR REWARD";
            }

            if (instructionText)
            {
                instructionText.text = "Select one of the following rewards:";
            }

            // Display each option
            for (int i = 0; i < optionCards.Length; i++)
            {
                if (i < options.Length)
                {
                    optionCards[i].gameObject.SetActive(true);
                    optionCards[i].Initialize(options[i], i, OnOptionSelected);

                    // Animate reveal
                    StartCoroutine(RevealCard(optionCards[i], i * cardRevealDelay));
                }
                else
                {
                    optionCards[i].gameObject.SetActive(false);
                }
            }
        }

        private System.Collections.IEnumerator RevealCard(DraftOptionCard card, float delay)
        {
            card.SetRevealed(false);
            yield return new WaitForSecondsRealtime(delay);
            card.Reveal();
        }

        private void OnOptionSelected(int index)
        {
            if (index < 0 || index >= currentOptions.Length)
                return;

            var selected = currentOptions[index];

            // Apply the reward
            drafter?.SelectDraftOption(selected);

            onSelectionCallback?.Invoke(selected);

            // Close draft display
            CloseDraft();
        }

        private void OnSkipClicked()
        {
            // Player chooses to skip reward
            onSelectionCallback?.Invoke(null);
            CloseDraft();
        }

        private void CloseDraft()
        {
            var uiManager = FindObjectOfType<UIManager>();
            uiManager?.ShowSectorMap();
        }
    }

    /// <summary>
    /// Individual draft option card.
    /// </summary>
    public class DraftOptionCard : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private Button cardButton;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI typeText;

        [Header("Colors")]
        [SerializeField] private Color scrapColor = new Color(0.8f, 0.7f, 0.2f);
        [SerializeField] private Color weaponColor = Color.red;
        [SerializeField] private Color crewColor = Color.blue;
        [SerializeField] private Color augmentColor = Color.green;
        [SerializeField] private Color resourceColor = Color.cyan;
        [SerializeField] private Color mutationColor = Color.magenta;

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float revealDuration = 0.3f;

        private int cardIndex;
        private Action<int> onSelected;
        private bool isRevealed = false;

        public void Initialize(DraftOption option, int index, Action<int> callback)
        {
            cardIndex = index;
            onSelected = callback;

            // Set display
            if (nameText)
            {
                nameText.text = option.Name;
            }

            if (descriptionText)
            {
                descriptionText.text = option.Description;
            }

            if (typeText)
            {
                typeText.text = option.Type.ToString().ToUpper();
            }

            // Set color based on type
            Color typeColor = option.Type switch
            {
                DraftOptionType.Scrap => scrapColor,
                DraftOptionType.Weapon => weaponColor,
                DraftOptionType.Crew => crewColor,
                DraftOptionType.Augment => augmentColor,
                DraftOptionType.Resources => resourceColor,
                DraftOptionType.MutationPoints => mutationColor,
                _ => Color.white
            };

            if (cardBackground)
            {
                cardBackground.color = typeColor;
            }

            if (cardButton)
            {
                cardButton.onClick.RemoveAllListeners();
                cardButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetRevealed(bool revealed)
        {
            isRevealed = revealed;

            if (canvasGroup)
            {
                canvasGroup.alpha = revealed ? 1f : 0f;
            }

            if (cardButton)
            {
                cardButton.interactable = revealed;
            }
        }

        public void Reveal()
        {
            if (isRevealed) return;

            StartCoroutine(RevealAnimation());
        }

        private System.Collections.IEnumerator RevealAnimation()
        {
            float timer = 0f;

            while (timer < revealDuration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / revealDuration;

                if (canvasGroup)
                {
                    canvasGroup.alpha = t;
                }

                // Scale pop effect
                transform.localScale = Vector3.one * (0.8f + 0.2f * t);

                yield return null;
            }

            if (canvasGroup) canvasGroup.alpha = 1f;
            transform.localScale = Vector3.one;

            isRevealed = true;

            if (cardButton) cardButton.interactable = true;
        }

        private void OnClicked()
        {
            if (!isRevealed) return;

            onSelected?.Invoke(cardIndex);
        }
    }
}
