using System;
using System.Collections;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Evolution
{
    /// <summary>
    /// Handles the visual transformation of the ship based on evolution path.
    /// This is where the "living ship" magic happens.
    /// </summary>
    public class ShipVisualEvolver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipController ship;
        [SerializeField] private SpriteRenderer shipRenderer;
        [SerializeField] private SpriteRenderer[] detailRenderers;

        [Header("Current State")]
        [SerializeField] private EvolutionPath currentPath = EvolutionPath.None;
        [SerializeField] private int currentStage = 0;

        [Header("Visuals")]
        [SerializeField] private ShipVisualSet predatorVisuals;
        [SerializeField] private ShipVisualSet phantomVisuals;
        [SerializeField] private ShipVisualSet heraldVisuals;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private ParticleSystem evolutionParticles;

        // Events
        public event Action<int> OnEvolutionComplete;

        private bool isTransitioning = false;

        public void Initialize(ShipController shipController)
        {
            ship = shipController;

            // Get or create sprite renderer
            shipRenderer = ship.GetComponentInChildren<SpriteRenderer>();
            if (shipRenderer == null)
            {
                var spriteObj = new GameObject("ShipSprite");
                spriteObj.transform.SetParent(ship.transform);
                shipRenderer = spriteObj.AddComponent<SpriteRenderer>();
            }

            // Initialize visual sets with procedural generation
            // (In full implementation, these would be loaded from assets)
            predatorVisuals = GeneratePredatorVisuals();
            phantomVisuals = GeneratePhantomVisuals();
            heraldVisuals = GenerateHeraldVisuals();

            Debug.Log("[ShipVisualEvolver] Initialized");
        }

        /// <summary>
        /// Transitions the ship to a new evolution stage.
        /// </summary>
        public void EvolveToStage(EvolutionPath path, int stage)
        {
            if (isTransitioning)
            {
                Debug.Log("[ShipVisualEvolver] Evolution already in progress");
                return;
            }

            if (path == currentPath && stage == currentStage)
            {
                return;
            }

            StartCoroutine(DoEvolutionTransition(path, stage));
        }

        private IEnumerator DoEvolutionTransition(EvolutionPath newPath, int newStage)
        {
            isTransitioning = true;

            Debug.Log($"[ShipVisualEvolver] Evolving to {newPath} stage {newStage}");

            // Play evolution particles
            if (evolutionParticles != null)
            {
                evolutionParticles.Play();
            }

            // Get target visuals
            var targetVisuals = GetVisualSet(newPath);
            if (targetVisuals == null)
            {
                isTransitioning = false;
                yield break;
            }

            var targetData = targetVisuals.GetStage(newStage);
            if (targetData == null)
            {
                isTransitioning = false;
                yield break;
            }

            // Start transition
            float elapsed = 0f;
            Color startColor = shipRenderer.color;
            Color targetColor = targetData.tintColor;

            // Flash white during transformation
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / transitionDuration);

                // Pulse glow effect
                float glowIntensity = Mathf.Sin(t * Mathf.PI) * 0.5f;
                shipRenderer.color = Color.Lerp(startColor, Color.white, glowIntensity);

                yield return null;
            }

            // Apply new visuals
            ApplyVisualStage(targetData);

            currentPath = newPath;
            currentStage = newStage;

            // Fade back to normal
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                shipRenderer.color = Color.Lerp(Color.white, targetColor, t);
                yield return null;
            }

            if (evolutionParticles != null)
            {
                evolutionParticles.Stop();
            }

            isTransitioning = false;
            OnEvolutionComplete?.Invoke(newStage);

            Debug.Log($"[ShipVisualEvolver] Evolution complete: {newPath} stage {newStage}");
        }

        private void ApplyVisualStage(VisualStageData data)
        {
            // Apply main sprite
            if (data.mainSprite != null)
            {
                shipRenderer.sprite = data.mainSprite;
            }

            // Apply tint
            shipRenderer.color = data.tintColor;

            // Apply scale (ships grow/change shape)
            ship.transform.localScale = data.scale;

            // Apply detail sprites (spines, arrays, etc.)
            for (int i = 0; i < detailRenderers.Length && i < data.detailSprites.Length; i++)
            {
                if (detailRenderers[i] != null)
                {
                    detailRenderers[i].sprite = data.detailSprites[i];
                    detailRenderers[i].enabled = data.detailSprites[i] != null;
                }
            }

            // Update emission/glow for the path
            UpdateEmission(data);
        }

        private void UpdateEmission(VisualStageData data)
        {
            // In a full implementation, this would update shader parameters
            // for glowing effects, particle trails, etc.

            // Predator: aggressive red glow intensifies
            // Phantom: subtle blue pulse, harder to see
            // Herald: warm golden glow, inviting
        }

        private ShipVisualSet GetVisualSet(EvolutionPath path)
        {
            return path switch
            {
                EvolutionPath.Predator => predatorVisuals,
                EvolutionPath.Phantom => phantomVisuals,
                EvolutionPath.Herald => heraldVisuals,
                _ => null
            };
        }

        // ==================== PROCEDURAL VISUAL GENERATION ====================
        // In a full implementation, these would load from ScriptableObjects

        private ShipVisualSet GeneratePredatorVisuals()
        {
            var set = new ShipVisualSet(EvolutionPath.Predator);

            // Stage 0: Base ship
            set.AddStage(new VisualStageData
            {
                stage = 0,
                tintColor = Color.white,
                scale = Vector3.one
            });

            // Stage 1: Subtle red tint, slight spines
            set.AddStage(new VisualStageData
            {
                stage = 1,
                tintColor = new Color(1f, 0.95f, 0.95f),
                scale = Vector3.one * 1.02f
            });

            // Stage 2: More pronounced, weapons visible
            set.AddStage(new VisualStageData
            {
                stage = 2,
                tintColor = new Color(1f, 0.85f, 0.85f),
                scale = Vector3.one * 1.05f
            });

            // Stage 3: Dramatic transformation, aggressive
            set.AddStage(new VisualStageData
            {
                stage = 3,
                tintColor = new Color(1f, 0.7f, 0.7f),
                scale = Vector3.one * 1.1f
            });

            // Stage 4: Apex Predator - terrifying
            set.AddStage(new VisualStageData
            {
                stage = 4,
                tintColor = new Color(0.9f, 0.3f, 0.3f),
                scale = Vector3.one * 1.15f
            });

            return set;
        }

        private ShipVisualSet GeneratePhantomVisuals()
        {
            var set = new ShipVisualSet(EvolutionPath.Phantom);

            // Stage 0: Base ship
            set.AddStage(new VisualStageData
            {
                stage = 0,
                tintColor = Color.white,
                scale = Vector3.one
            });

            // Stage 1: Slightly darker
            set.AddStage(new VisualStageData
            {
                stage = 1,
                tintColor = new Color(0.95f, 0.95f, 1f),
                scale = Vector3.one * 0.98f
            });

            // Stage 2: Angular, darker
            set.AddStage(new VisualStageData
            {
                stage = 2,
                tintColor = new Color(0.8f, 0.85f, 1f),
                scale = Vector3.one * 0.95f
            });

            // Stage 3: Nearly invisible against space
            set.AddStage(new VisualStageData
            {
                stage = 3,
                tintColor = new Color(0.5f, 0.55f, 0.7f),
                scale = Vector3.one * 0.92f
            });

            // Stage 4: Phantom Complete - a shadow
            set.AddStage(new VisualStageData
            {
                stage = 4,
                tintColor = new Color(0.2f, 0.25f, 0.4f),
                scale = Vector3.one * 0.9f
            });

            return set;
        }

        private ShipVisualSet GenerateHeraldVisuals()
        {
            var set = new ShipVisualSet(EvolutionPath.Herald);

            // Stage 0: Base ship
            set.AddStage(new VisualStageData
            {
                stage = 0,
                tintColor = Color.white,
                scale = Vector3.one
            });

            // Stage 1: Warm glow
            set.AddStage(new VisualStageData
            {
                stage = 1,
                tintColor = new Color(1f, 1f, 0.95f),
                scale = Vector3.one * 1.02f
            });

            // Stage 2: Communication arrays visible
            set.AddStage(new VisualStageData
            {
                stage = 2,
                tintColor = new Color(1f, 0.98f, 0.85f),
                scale = Vector3.one * 1.05f
            });

            // Stage 3: Elegant, welcoming
            set.AddStage(new VisualStageData
            {
                stage = 3,
                tintColor = new Color(1f, 0.95f, 0.7f),
                scale = Vector3.one * 1.08f
            });

            // Stage 4: Herald Ascendant - radiant
            set.AddStage(new VisualStageData
            {
                stage = 4,
                tintColor = new Color(1f, 0.9f, 0.5f),
                scale = Vector3.one * 1.12f
            });

            return set;
        }
    }

    [Serializable]
    public class ShipVisualSet
    {
        public EvolutionPath Path;
        public VisualStageData[] Stages = new VisualStageData[5];

        public ShipVisualSet(EvolutionPath path)
        {
            Path = path;
        }

        public void AddStage(VisualStageData data)
        {
            if (data.stage >= 0 && data.stage < Stages.Length)
            {
                Stages[data.stage] = data;
            }
        }

        public VisualStageData GetStage(int stage)
        {
            if (stage >= 0 && stage < Stages.Length)
            {
                return Stages[stage];
            }
            return null;
        }
    }

    [Serializable]
    public class VisualStageData
    {
        public int stage;
        public Sprite mainSprite;
        public Sprite[] detailSprites = new Sprite[0];
        public Color tintColor = Color.white;
        public Vector3 scale = Vector3.one;
        public float emissionIntensity = 0f;
    }
}
