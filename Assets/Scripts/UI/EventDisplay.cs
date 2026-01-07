using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Events;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Displays text events and handles player choices.
    /// </summary>
    public class EventDisplay : MonoBehaviour
    {
        [Header("Event Content")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image eventImage;

        [Header("Choices")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private EventChoiceButton choiceButtonPrefab;
        [SerializeField] private List<EventChoiceButton> activeChoices = new List<EventChoiceButton>();

        [Header("Result")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button continueButton;

        [Header("Animation")]
        [SerializeField] private float typingSpeed = 0.02f;
        [SerializeField] private bool useTypingEffect = true;

        private EventManager eventManager;
        private GameEvent currentEvent;
        private bool showingResult = false;

        private void Awake()
        {
            eventManager = FindObjectOfType<EventManager>();

            if (resultPanel) resultPanel.SetActive(false);
            if (continueButton) continueButton.onClick.AddListener(OnContinueClicked);
        }

        public void DisplayEvent(GameEvent evt)
        {
            currentEvent = evt;
            showingResult = false;

            // Set title
            if (titleText)
            {
                titleText.text = evt.Title;
            }

            // Set description
            if (descriptionText)
            {
                if (useTypingEffect)
                {
                    StartCoroutine(TypeText(descriptionText, evt.Description));
                }
                else
                {
                    descriptionText.text = evt.Description;
                }
            }

            // Clear old choices
            ClearChoices();

            // Create choice buttons
            for (int i = 0; i < evt.Choices.Count; i++)
            {
                CreateChoiceButton(evt.Choices[i], i);
            }

            // Hide result panel
            if (resultPanel) resultPanel.SetActive(false);
        }

        private void ClearChoices()
        {
            foreach (var choice in activeChoices)
            {
                if (choice != null)
                    Destroy(choice.gameObject);
            }
            activeChoices.Clear();
        }

        private void CreateChoiceButton(EventChoice choice, int index)
        {
            if (choiceButtonPrefab == null || choiceContainer == null)
                return;

            var button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.Initialize(choice, index, OnChoiceSelected);

            // Check if choice is available
            var playerShip = GameManager.Instance?.PlayerShip;
            bool available = playerShip != null && choice.MeetsRequirements(playerShip);
            button.SetAvailable(available);

            activeChoices.Add(button);
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            if (showingResult) return;

            // Disable all choice buttons
            foreach (var choice in activeChoices)
            {
                choice.SetInteractable(false);
            }

            // Tell event manager to process choice
            eventManager?.MakeChoice(choiceIndex);
        }

        public void ShowResult(string text)
        {
            showingResult = true;

            if (resultPanel)
            {
                resultPanel.SetActive(true);
            }

            if (resultText)
            {
                if (useTypingEffect)
                {
                    StartCoroutine(TypeText(resultText, text));
                }
                else
                {
                    resultText.text = text;
                }
            }

            // Hide choice buttons
            foreach (var choice in activeChoices)
            {
                choice.gameObject.SetActive(false);
            }
        }

        private void OnContinueClicked()
        {
            // Close event display and return to sector map
            var uiManager = FindObjectOfType<UIManager>();
            uiManager?.ShowSectorMap();
        }

        private System.Collections.IEnumerator TypeText(TextMeshProUGUI textComponent, string fullText)
        {
            textComponent.text = "";

            foreach (char c in fullText)
            {
                textComponent.text += c;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
        }
    }

    /// <summary>
    /// Individual choice button component.
    /// </summary>
    public class EventChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI choiceText;
        [SerializeField] private TextMeshProUGUI requirementText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color availableColor = Color.white;
        [SerializeField] private Color unavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private int choiceIndex;
        private System.Action<int> onSelected;

        public void Initialize(EventChoice choice, int index, System.Action<int> callback)
        {
            choiceIndex = index;
            onSelected = callback;

            if (choiceText)
            {
                choiceText.text = choice.Text;
            }

            // Show requirements
            if (requirementText)
            {
                string reqText = "";
                if (choice.RequiredScrap > 0)
                    reqText += $"[{choice.RequiredScrap} Scrap] ";
                if (choice.RequiredFuel > 0)
                    reqText += $"[{choice.RequiredFuel} Fuel] ";
                if (choice.RequiredSystem.HasValue)
                    reqText += $"[Requires {choice.RequiredSystem.Value}] ";

                requirementText.text = reqText;
                requirementText.gameObject.SetActive(!string.IsNullOrEmpty(reqText));
            }

            if (button)
            {
                button.onClick.AddListener(OnClicked);
            }
        }

        public void SetAvailable(bool available)
        {
            if (button) button.interactable = available;
            if (backgroundImage) backgroundImage.color = available ? availableColor : unavailableColor;
        }

        public void SetInteractable(bool interactable)
        {
            if (button) button.interactable = interactable;
        }

        private void OnClicked()
        {
            onSelected?.Invoke(choiceIndex);
        }
    }
}
