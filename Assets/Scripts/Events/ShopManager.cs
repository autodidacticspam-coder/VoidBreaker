using System;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;
using VoidBreaker.Ship;
using VoidBreaker.Data;

namespace VoidBreaker.Events
{
    /// <summary>
    /// Manages shop encounters - where players spend scrap on upgrades.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool shopOpen = false;
        [SerializeField] private ShopInventory currentShop;

        [Header("Settings")]
        [SerializeField] private int weaponSlots = 3;
        [SerializeField] private int augmentSlots = 2;
        [SerializeField] private int crewSlots = 2;
        [SerializeField] private int resourceSlots = 3;

        [Header("Pricing")]
        [SerializeField] private float priceVariance = 0.2f;
        [SerializeField] private float sectorPriceMultiplier = 0.1f;

        [Header("References")]
        [SerializeField] private ShipController playerShip;

        public bool ShopOpen => shopOpen;
        public ShopInventory CurrentShop => currentShop;

        // Events
        public event Action OnShopOpened;
        public event Action OnShopClosed;
        public event Action<ShopItem> OnItemPurchased;

        public void Initialize(ShipController player)
        {
            playerShip = player;
        }

        public void OpenShop()
        {
            if (shopOpen) return;

            shopOpen = true;
            currentShop = GenerateShopInventory();

            OnShopOpened?.Invoke();

            GameEvents.TriggerShopOpened(new ShopOpenedArgs
            {
                itemCount = currentShop.AllItems.Count
            });

            Debug.Log("[ShopManager] Shop opened");
        }

        public void CloseShop()
        {
            if (!shopOpen) return;

            shopOpen = false;

            OnShopClosed?.Invoke();

            Debug.Log("[ShopManager] Shop closed");
        }

        private ShopInventory GenerateShopInventory()
        {
            var inventory = new ShopInventory();
            int currentSector = GameManager.Instance?.CurrentRun?.State.currentSector ?? 1;

            // Generate weapons
            for (int i = 0; i < weaponSlots; i++)
            {
                var weapon = GenerateRandomWeapon(currentSector);
                if (weapon != null)
                {
                    inventory.Weapons.Add(weapon);
                    inventory.AllItems.Add(weapon);
                }
            }

            // Generate augments
            for (int i = 0; i < augmentSlots; i++)
            {
                var augment = GenerateRandomAugment(currentSector);
                if (augment != null)
                {
                    inventory.Augments.Add(augment);
                    inventory.AllItems.Add(augment);
                }
            }

            // Generate crew
            for (int i = 0; i < crewSlots; i++)
            {
                var crew = GenerateCrewForHire(currentSector);
                if (crew != null)
                {
                    inventory.Crew.Add(crew);
                    inventory.AllItems.Add(crew);
                }
            }

            // Generate resources
            inventory.Resources.Add(new ShopItem
            {
                ItemType = ShopItemType.Fuel,
                Name = "Fuel (3 units)",
                Description = "Fuel for FTL jumps.",
                Price = CalculatePrice(6, currentSector),
                Amount = 3
            });

            inventory.Resources.Add(new ShopItem
            {
                ItemType = ShopItemType.Missiles,
                Name = "Missiles (5 units)",
                Description = "Ammunition for missile weapons.",
                Price = CalculatePrice(8, currentSector),
                Amount = 5
            });

            inventory.Resources.Add(new ShopItem
            {
                ItemType = ShopItemType.DroneParts,
                Name = "Drone Parts (3 units)",
                Description = "Parts for drone deployment.",
                Price = CalculatePrice(10, currentSector),
                Amount = 3
            });

            // Add repair option
            inventory.Services.Add(new ShopItem
            {
                ItemType = ShopItemType.Repair,
                Name = "Hull Repairs",
                Description = "Repair 5 hull damage.",
                Price = CalculatePrice(3, currentSector),
                Amount = 5
            });

            inventory.AllItems.AddRange(inventory.Resources);
            inventory.AllItems.AddRange(inventory.Services);

            return inventory;
        }

        private ShopItem GenerateRandomWeapon(int sector)
        {
            // Create a random weapon appropriate for the sector
            WeaponType[] types = { WeaponType.Laser, WeaponType.Missile, WeaponType.Beam, WeaponType.Ion };
            var type = types[UnityEngine.Random.Range(0, types.Length)];

            int tier = Mathf.Min(sector, 3);
            int damage = 1 + tier;
            int power = 1 + tier;
            float chargeTime = 10f - tier;
            int basePrice = 40 + (tier * 20);

            return new ShopItem
            {
                ItemType = ShopItemType.Weapon,
                Name = $"{type} Mk.{tier + 1}",
                Description = $"{damage} damage, {power} power, {chargeTime}s charge",
                Price = CalculatePrice(basePrice, sector),
                WeaponType = type,
                Tier = tier
            };
        }

        private ShopItem GenerateRandomAugment(int sector)
        {
            string[] augmentNames = {
                "Shield Booster",
                "Weapon Pre-Igniter",
                "Long-Range Scanners",
                "Automated Reloader",
                "Hull Plating"
            };

            string[] descriptions = {
                "+1 max shield layer",
                "Weapons start 50% charged",
                "See adjacent beacon types",
                "+10% weapon charge speed",
                "+15 max hull"
            };

            int[] prices = { 50, 80, 30, 45, 40 };

            int index = UnityEngine.Random.Range(0, augmentNames.Length);

            return new ShopItem
            {
                ItemType = ShopItemType.Augment,
                Name = augmentNames[index],
                Description = descriptions[index],
                Price = CalculatePrice(prices[index], sector),
                AugmentId = augmentNames[index].ToLower().Replace(" ", "_")
            };
        }

        private ShopItem GenerateCrewForHire(int sector)
        {
            CrewRace[] races = {
                CrewRace.Human, CrewRace.Vex, CrewRace.Engi,
                CrewRace.Crystalline, CrewRace.Zoltan, CrewRace.Slug
            };

            var race = races[UnityEngine.Random.Range(0, races.Length)];
            int basePrice = GetCrewBasePrice(race);

            string name = GenerateCrewName(race);

            return new ShopItem
            {
                ItemType = ShopItemType.Crew,
                Name = name,
                Description = $"{race} crew member",
                Price = CalculatePrice(basePrice, sector),
                CrewRace = race
            };
        }

        private int GetCrewBasePrice(CrewRace race)
        {
            return race switch
            {
                CrewRace.Human => 45,
                CrewRace.Vex => 50,
                CrewRace.Engi => 50,
                CrewRace.Crystalline => 60,
                CrewRace.Zoltan => 65,
                CrewRace.Slug => 45,
                CrewRace.Lanius => 70,
                CrewRace.Synthetic => 80,
                _ => 50
            };
        }

        private string GenerateCrewName(CrewRace race)
        {
            string[] humanNames = { "Marcus", "Elena", "Viktor", "Kira", "Chen", "Yuki" };
            string[] vexNames = { "Vrex", "Zix", "Drex", "Nix" };
            string[] engiNames = { "Unit-7", "Circuit", "Cogsworth", "Spanner" };
            string[] crystallineNames = { "Quartz", "Shard", "Prism", "Facet" };
            string[] zoltanNames = { "Luminor", "Spark", "Radiant", "Flux" };
            string[] slugNames = { "Slime", "Goop", "Ooze", "Blob" };

            return race switch
            {
                CrewRace.Human => humanNames[UnityEngine.Random.Range(0, humanNames.Length)],
                CrewRace.Vex => vexNames[UnityEngine.Random.Range(0, vexNames.Length)],
                CrewRace.Engi => engiNames[UnityEngine.Random.Range(0, engiNames.Length)],
                CrewRace.Crystalline => crystallineNames[UnityEngine.Random.Range(0, crystallineNames.Length)],
                CrewRace.Zoltan => zoltanNames[UnityEngine.Random.Range(0, zoltanNames.Length)],
                CrewRace.Slug => slugNames[UnityEngine.Random.Range(0, slugNames.Length)],
                _ => "Crew"
            };
        }

        private int CalculatePrice(int basePrice, int sector)
        {
            // Apply variance
            float variance = UnityEngine.Random.Range(-priceVariance, priceVariance);
            float sectorBonus = sector * sectorPriceMultiplier;

            int finalPrice = Mathf.RoundToInt(basePrice * (1 + variance + sectorBonus));
            return Mathf.Max(1, finalPrice);
        }

        // ==================== PURCHASING ====================

        public bool TryPurchase(ShopItem item)
        {
            if (!shopOpen || item == null || item.Purchased)
            {
                Debug.Log("[ShopManager] Cannot purchase");
                return false;
            }

            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return false;

            if (run.State.scrap < item.Price)
            {
                Debug.Log("[ShopManager] Not enough scrap");
                return false;
            }

            // Spend scrap
            run.SpendScrap(item.Price);

            // Apply purchase
            ApplyPurchase(item);

            // Mark as purchased
            item.Purchased = true;

            OnItemPurchased?.Invoke(item);

            GameEvents.TriggerItemPurchased(new ItemPurchasedArgs
            {
                itemName = item.Name,
                price = item.Price,
                itemType = item.ItemType.ToString()
            });

            Debug.Log($"[ShopManager] Purchased {item.Name} for {item.Price} scrap");

            return true;
        }

        private void ApplyPurchase(ShopItem item)
        {
            var run = GameManager.Instance?.CurrentRun;
            if (run == null) return;

            switch (item.ItemType)
            {
                case ShopItemType.Weapon:
                    // Add weapon to ship (handled by UI/player)
                    break;

                case ShopItemType.Augment:
                    run.AddAugment(item.AugmentId);
                    break;

                case ShopItemType.Crew:
                    var crewManager = playerShip.GetComponent<CrewManager>();
                    crewManager?.SpawnNewCrew(item.CrewRace, item.Name);
                    break;

                case ShopItemType.Fuel:
                    run.AddFuel(item.Amount);
                    break;

                case ShopItemType.Missiles:
                    run.AddMissiles(item.Amount);
                    break;

                case ShopItemType.DroneParts:
                    run.AddDroneParts(item.Amount);
                    break;

                case ShopItemType.Repair:
                    playerShip.RepairHull(item.Amount);
                    break;
            }
        }

        public bool CanAfford(ShopItem item)
        {
            var run = GameManager.Instance?.CurrentRun;
            return run != null && run.State.scrap >= item.Price;
        }
    }

    [Serializable]
    public class ShopInventory
    {
        public List<ShopItem> Weapons = new List<ShopItem>();
        public List<ShopItem> Augments = new List<ShopItem>();
        public List<ShopItem> Crew = new List<ShopItem>();
        public List<ShopItem> Resources = new List<ShopItem>();
        public List<ShopItem> Services = new List<ShopItem>();
        public List<ShopItem> AllItems = new List<ShopItem>();
    }

    [Serializable]
    public class ShopItem
    {
        public ShopItemType ItemType;
        public string Name;
        public string Description;
        public int Price;
        public int Amount;
        public bool Purchased;

        // Type-specific data
        public WeaponType WeaponType;
        public int Tier;
        public string AugmentId;
        public CrewRace CrewRace;
    }

    public enum ShopItemType
    {
        Weapon,
        Augment,
        Crew,
        Fuel,
        Missiles,
        DroneParts,
        Repair
    }
}
