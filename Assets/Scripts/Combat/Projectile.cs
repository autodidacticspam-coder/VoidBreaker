using System;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;

namespace VoidBreaker.Combat
{
    /// <summary>
    /// Represents a projectile traveling between ships.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private Weapon sourceWeapon;
        [SerializeField] private Room targetRoom;
        [SerializeField] private bool isPlayerProjectile;

        [Header("Movement")]
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float speed = 8f;
        [SerializeField] private float progress = 0f;

        [Header("State")]
        [SerializeField] private bool isDestroyed = false;
        [SerializeField] private bool hasHit = false;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trail;

        public bool IsDestroyed => isDestroyed;
        public bool IsPlayerProjectile => isPlayerProjectile;
        public Weapon SourceWeapon => sourceWeapon;

        // Events
        public event Action<Projectile, Room> OnHit;
        public event Action<Projectile> OnMiss;

        public void Initialize(Weapon weapon, Room target, bool fromPlayer, float projectileSpeed)
        {
            sourceWeapon = weapon;
            targetRoom = target;
            isPlayerProjectile = fromPlayer;
            speed = projectileSpeed;

            // Set positions
            startPosition = weapon.transform.position;
            targetPosition = target.Position;
            transform.position = startPosition;

            // Create visuals
            CreateVisuals();

            // Look at target
            Vector3 direction = (targetPosition - startPosition).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void CreateVisuals()
        {
            // Create sprite renderer
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = GetProjectileColor();

            // Create a simple projectile sprite (in full implementation, load from definition)
            spriteRenderer.sprite = CreateProjectileSprite();

            // Add trail
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.2f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = spriteRenderer.color;
            trail.endColor = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0f);
        }

        private Color GetProjectileColor()
        {
            if (sourceWeapon == null) return Color.white;

            return sourceWeapon.WeaponType switch
            {
                WeaponType.Laser => Color.red,
                WeaponType.Missile => new Color(1f, 0.5f, 0f), // Orange
                WeaponType.Beam => Color.yellow,
                WeaponType.Ion => Color.cyan,
                WeaponType.Bomb => Color.magenta,
                WeaponType.Flak => Color.gray,
                _ => Color.white
            };
        }

        private Sprite CreateProjectileSprite()
        {
            // Create a simple 4x4 white texture
            var texture = new Texture2D(4, 4);
            var colors = new Color[16];
            for (int i = 0; i < 16; i++) colors[i] = Color.white;
            texture.SetPixels(colors);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        public void Tick(float deltaTime)
        {
            if (isDestroyed || hasHit) return;

            // Move towards target
            float distance = Vector3.Distance(startPosition, targetPosition);
            float travelTime = distance / speed;

            progress += deltaTime / travelTime;
            transform.position = Vector3.Lerp(startPosition, targetPosition, progress);

            // Check if arrived
            if (progress >= 1f)
            {
                ResolveHit();
            }
        }

        private void ResolveHit()
        {
            hasHit = true;

            // Get target ship
            var targetShip = targetRoom.GetComponentInParent<ShipController>();
            if (targetShip == null)
            {
                OnMiss?.Invoke(this);
                return;
            }

            // Roll for evasion (missiles bypass this)
            bool bypassesEvasion = sourceWeapon.WeaponType == WeaponType.Missile ||
                                   sourceWeapon.WeaponType == WeaponType.Bomb;

            if (!bypassesEvasion && targetShip.TryEvade())
            {
                OnMiss?.Invoke(this);
                return;
            }

            // Check shields (missiles bypass)
            bool bypassesShields = sourceWeapon.WeaponType == WeaponType.Missile ||
                                   sourceWeapon.WeaponType == WeaponType.Bomb;

            if (!bypassesShields)
            {
                var shields = targetShip.GetSystem<ShieldSystem>();
                if (shields != null && shields.CurrentLayers > 0)
                {
                    shields.AbsorbHit();
                    OnMiss?.Invoke(this); // Blocked by shields
                    return;
                }
            }

            // Hit!
            OnHit?.Invoke(this, targetRoom);
        }

        public DamageInfo CreateDamageInfo()
        {
            if (sourceWeapon == null)
                return new DamageInfo(1);

            return new DamageInfo
            {
                amount = sourceWeapon.Damage,
                type = sourceWeapon.WeaponType switch
                {
                    WeaponType.Laser => DamageType.Laser,
                    WeaponType.Missile => DamageType.Missile,
                    WeaponType.Beam => DamageType.Beam,
                    WeaponType.Ion => DamageType.Ion,
                    WeaponType.Bomb => DamageType.Missile,
                    WeaponType.Flak => DamageType.Laser,
                    _ => DamageType.Normal
                },
                ignoresShields = sourceWeapon.WeaponType == WeaponType.Missile ||
                                sourceWeapon.WeaponType == WeaponType.Bomb,
                causesBreach = UnityEngine.Random.value < 0.1f, // 10% breach chance
                causesFire = UnityEngine.Random.value < 0.1f,   // 10% fire chance
                ionDamage = sourceWeapon.WeaponType == WeaponType.Ion ? sourceWeapon.Damage : 0
            };
        }

        public void Destroy()
        {
            isDestroyed = true;

            // Spawn impact effect
            SpawnImpactEffect();

            Destroy(gameObject);
        }

        private void SpawnImpactEffect()
        {
            // In full implementation, instantiate particle effect
            // For now, just log
            Debug.Log($"[Projectile] Impact at {transform.position}");
        }
    }
}
