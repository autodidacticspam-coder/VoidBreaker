using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidBreaker.Core;
using VoidBreaker.Data;
using VoidBreaker.Evolution;

namespace VoidBreaker.UI
{
    /// <summary>
    /// Displays the evolution/mutation tree.
    /// </summary>
    public class EvolutionTreeDisplay : MonoBehaviour
    {
        [Header("Path Tabs")]
        [SerializeField] private Button predatorTab;
        [SerializeField] private Button phantomTab;
        [SerializeField] private Button heraldTab;
        [SerializeField] private Image predatorTabBg;
        [SerializeField] private Image phantomTabBg;
        [SerializeField] private Image heraldTabBg;

        [Header("Tree Container")]
        [SerializeField] private Transform treeContainer;
        [SerializeField] private MutationNode nodePrefab;
        [SerializeField] private Image connectionPrefab;

        [Header("Info Panel")]
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private TextMeshProUGUI mutationNameText;
        [SerializeField] private TextMeshProUGUI mutationDescText;
        [SerializeField] private TextMeshProUGUI mutationCostText;
        [SerializeField] private TextMeshProUGUI mutationEffectsText;
        [SerializeField] private Button unlockButton;
        [SerializeField] private TextMeshProUGUI unlockButtonText;

        [Header("Resources Display")]
        [SerializeField] private TextMeshProUGUI mutationPointsText;
        [SerializeField] private TextMeshProUGUI pathProgressText;

        [Header("Colors")]
        [SerializeField] private Color predatorColor = Color.red;
        [SerializeField] private Color phantomColor = Color.blue;
        [SerializeField] private Color heraldColor = Color.green;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;

        private EvolutionManager evolutionManager;
        private EvolutionPath currentPath = EvolutionPath.Predator;
        private Mutation selectedMutation;
        private List<MutationNode> activeNodes = new List<MutationNode>();
        private List<Image> activeConnections = new List<Image>();

        private void Awake()
        {
            // Setup tab buttons
            if (predatorTab) predatorTab.onClick.AddListener(() => ShowPath(EvolutionPath.Predator));
            if (phantomTab) phantomTab.onClick.AddListener(() => ShowPath(EvolutionPath.Phantom));
            if (heraldTab) heraldTab.onClick.AddListener(() => ShowPath(EvolutionPath.Herald));

            if (unlockButton) unlockButton.onClick.AddListener(OnUnlockClicked);

            if (infoPanel) infoPanel.SetActive(false);
        }

        public void Initialize(EvolutionManager manager)
        {
            evolutionManager = manager;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            UpdateResourceDisplay();
            ShowPath(currentPath);
        }

        private void ShowPath(EvolutionPath path)
        {
            currentPath = path;

            // Update tab visuals
            UpdateTabVisuals();

            // Clear existing nodes
            ClearTree();

            // Get mutations for this path
            if (evolutionManager == null) return;

            var tree = evolutionManager.GetMutationTree();
            if (tree == null) return;

            var mutations = tree.GetMutationsForPath(path);

            // Create node for each mutation
            foreach (var mutation in mutations)
            {
                CreateMutationNode(mutation);
            }

            // Draw connections
            DrawConnections(mutations);

            // Clear selection
            selectedMutation = null;
            if (infoPanel) infoPanel.SetActive(false);
        }

        private void UpdateTabVisuals()
        {
            Color activeAlpha = new Color(1, 1, 1, 1);
            Color inactiveAlpha = new Color(1, 1, 1, 0.5f);

            if (predatorTabBg)
            {
                predatorTabBg.color = currentPath == EvolutionPath.Predator ?
                    predatorColor * activeAlpha : predatorColor * inactiveAlpha;
            }

            if (phantomTabBg)
            {
                phantomTabBg.color = currentPath == EvolutionPath.Phantom ?
                    phantomColor * activeAlpha : phantomColor * inactiveAlpha;
            }

            if (heraldTabBg)
            {
                heraldTabBg.color = currentPath == EvolutionPath.Herald ?
                    heraldColor * activeAlpha : heraldColor * inactiveAlpha;
            }
        }

        private void ClearTree()
        {
            foreach (var node in activeNodes)
            {
                if (node != null) Destroy(node.gameObject);
            }
            activeNodes.Clear();

            foreach (var conn in activeConnections)
            {
                if (conn != null) Destroy(conn.gameObject);
            }
            activeConnections.Clear();
        }

        private void CreateMutationNode(Mutation mutation)
        {
            if (nodePrefab == null || treeContainer == null) return;

            var node = Instantiate(nodePrefab, treeContainer);
            node.Initialize(mutation, this);

            // Position based on tier
            float xPos = 0; // Could calculate based on position in tier
            float yPos = -mutation.Tier * 120f; // 120 pixels per tier
            node.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, yPos);

            // Set visual state
            bool unlocked = evolutionManager.IsMutationUnlocked(mutation.MutationId);
            bool available = evolutionManager.CanUnlockMutation(mutation.MutationId);

            Color pathColor = currentPath switch
            {
                EvolutionPath.Predator => predatorColor,
                EvolutionPath.Phantom => phantomColor,
                EvolutionPath.Herald => heraldColor,
                _ => Color.white
            };

            if (unlocked)
            {
                node.SetState(MutationNodeState.Unlocked, pathColor);
            }
            else if (available)
            {
                node.SetState(MutationNodeState.Available, pathColor);
            }
            else
            {
                node.SetState(MutationNodeState.Locked, lockedColor);
            }

            activeNodes.Add(node);
        }

        private void DrawConnections(List<Mutation> mutations)
        {
            // Draw lines between connected mutations
            // This would require tracking node positions and prereqs
        }

        private void UpdateResourceDisplay()
        {
            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return;

            if (mutationPointsText)
            {
                mutationPointsText.text = $"Mutation Points: {run.State.mutationPoints}";
            }

            if (pathProgressText && evolutionManager != null)
            {
                var (path, level) = evolutionManager.GetDominantPath();
                pathProgressText.text = $"Dominant Path: {path} (Level {level})";
            }
        }

        // ==================== NODE INTERACTION ====================

        public void OnNodeClicked(Mutation mutation)
        {
            selectedMutation = mutation;
            ShowMutationInfo(mutation);
        }

        public void OnNodeHovered(Mutation mutation)
        {
            // Could show brief tooltip
        }

        private void ShowMutationInfo(Mutation mutation)
        {
            if (infoPanel) infoPanel.SetActive(true);

            if (mutationNameText) mutationNameText.text = mutation.MutationName;
            if (mutationDescText) mutationDescText.text = mutation.Description;
            if (mutationCostText) mutationCostText.text = $"Cost: {mutation.Cost} Mutation Points";

            // Build effects text
            if (mutationEffectsText && mutation.Effects != null)
            {
                string effects = "";
                foreach (var effect in mutation.Effects)
                {
                    effects += $"• {FormatEffect(effect)}\n";
                }
                mutationEffectsText.text = effects;
            }

            // Update unlock button
            bool unlocked = evolutionManager.IsMutationUnlocked(mutation.MutationId);
            bool canUnlock = evolutionManager.CanUnlockMutation(mutation.MutationId);

            if (unlockButton)
            {
                unlockButton.gameObject.SetActive(!unlocked);
                unlockButton.interactable = canUnlock;
            }

            if (unlockButtonText)
            {
                if (unlocked)
                    unlockButtonText.text = "UNLOCKED";
                else if (canUnlock)
                    unlockButtonText.text = "UNLOCK";
                else
                    unlockButtonText.text = "LOCKED";
            }
        }

        private string FormatEffect(MutationEffect effect)
        {
            string value = effect.IsPercent ? $"{effect.Value}%" : effect.Value.ToString();
            string sign = effect.Value > 0 ? "+" : "";

            return effect.Type switch
            {
                MutationEffectType.BonusDamage => $"{sign}{value} Weapon Damage",
                MutationEffectType.BonusEvasion => $"{sign}{value} Evasion",
                MutationEffectType.BonusShields => $"{sign}{value} Shield Layers",
                MutationEffectType.BonusHull => $"{sign}{value} Max Hull",
                MutationEffectType.BonusCrew => $"{sign}{value} Crew Combat",
                MutationEffectType.CrewRepair => $"{sign}{value} Repair Speed",
                MutationEffectType.CloakDuration => $"{sign}{value} Cloak Duration",
                MutationEffectType.ScrapBonus => $"{sign}{value} Scrap from Enemies",
                MutationEffectType.UnlockEnding => "Unlocks Final Boss",
                _ => effect.ToString()
            };
        }

        private void OnUnlockClicked()
        {
            if (selectedMutation == null) return;

            bool success = evolutionManager.TryUnlockMutation(selectedMutation.MutationId);

            if (success)
            {
                RefreshDisplay();
                ShowMutationInfo(selectedMutation); // Refresh info panel
            }
        }
    }

    /// <summary>
    /// Individual mutation node in the tree.
    /// </summary>
    public class MutationNode : MonoBehaviour
    {
        [SerializeField] private Button nodeButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image glowEffect;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI tierText;

        private Mutation mutation;
        private EvolutionTreeDisplay display;

        public void Initialize(Mutation m, EvolutionTreeDisplay d)
        {
            mutation = m;
            display = d;

            if (nameText) nameText.text = m.MutationName;
            if (tierText) tierText.text = $"Tier {m.Tier}";

            if (nodeButton)
            {
                nodeButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetState(MutationNodeState state, Color pathColor)
        {
            switch (state)
            {
                case MutationNodeState.Locked:
                    if (backgroundImage) backgroundImage.color = Color.gray;
                    if (glowEffect) glowEffect.gameObject.SetActive(false);
                    break;

                case MutationNodeState.Available:
                    if (backgroundImage) backgroundImage.color = pathColor * 0.7f;
                    if (glowEffect)
                    {
                        glowEffect.gameObject.SetActive(true);
                        glowEffect.color = pathColor;
                    }
                    break;

                case MutationNodeState.Unlocked:
                    if (backgroundImage) backgroundImage.color = pathColor;
                    if (glowEffect) glowEffect.gameObject.SetActive(false);
                    break;
            }
        }

        private void OnClicked()
        {
            display?.OnNodeClicked(mutation);
        }
    }

    public enum MutationNodeState
    {
        Locked,
        Available,
        Unlocked
    }
}
