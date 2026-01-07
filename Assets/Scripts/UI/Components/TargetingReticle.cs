using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.UI
{
    /// <summary>
    /// UI component for weapon targeting.
    /// </summary>
    public class TargetingReticle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer reticleSprite;
        [SerializeField] private LineRenderer lineRenderer;

        private ITargetable currentTarget;
        private bool isTargeting = false;

        public bool IsTargeting => isTargeting;
        public ITargetable CurrentTarget => currentTarget;

        public void StartTargeting()
        {
            isTargeting = true;
            gameObject.SetActive(true);
        }

        public void StopTargeting()
        {
            isTargeting = false;
            currentTarget = null;
            gameObject.SetActive(false);
        }

        public void SetTarget(ITargetable target)
        {
            currentTarget = target;
            if (target != null)
            {
                transform.position = target.Position;
            }
        }

        private void Update()
        {
            if (isTargeting && currentTarget != null)
            {
                transform.position = currentTarget.Position;
            }
        }
    }
}
