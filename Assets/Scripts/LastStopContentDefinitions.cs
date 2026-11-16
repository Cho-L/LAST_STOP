using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastStop
{
    /// <summary>
    /// Stable IDs are intentionally separate from display names and art paths. Future
    /// unlocks and saves should persist these IDs, never localized player-facing text.
    /// </summary>
    public static class LastStopContentIds
    {
        public const string RookieGuard = "character.rookie_guard";
        public const string YellowBus = "vehicle.yellow_bus";
        public const string Rusher = "enemy.rusher";

        // Reserved content IDs for future unlocks. They have no gameplay implementation yet.
        public const string DogRider = "character.dog_rider";
        public const string Motorbike = "vehicle.motorbike";
        public const string Soldier = "character.soldier";
        public const string ArmoredCarrier = "vehicle.apc";
    }

    public enum EnemyKind
    {
        Normal,
        Special,
        Boss
    }

    [Serializable]
    public sealed class CharacterDefinition
    {
        public string Id;
        public float MoveSpeed;
        public string ExteriorSprite;
        public string InteriorSprite;
    }

    [Serializable]
    public sealed class VehicleDefinition
    {
        public string Id;
        public float MaxHealth;
        public int WeaponSlots;
        public int SupportSlots;
        public string ExteriorSprite;
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        public string Id;
        public EnemyKind Kind;
        public float MaxHealth;
        public float ExteriorSpeed;
        public float InteriorSpeed;
        public string ExteriorSprite;
        public string InteriorSprite;
    }

    /// <summary>
    /// The initial 1/1/1 catalogue. It is code-backed for this runtime-only prototype;
    /// later it can be moved to ScriptableObjects without changing the IDs or contracts.
    /// </summary>
    public static class LastStopContentCatalog
    {
        public static readonly CharacterDefinition RookieGuard = new()
        {
            Id = LastStopContentIds.RookieGuard,
            MoveSpeed = 5f,
            ExteriorSprite = "Player_Exterior_TopDown",
            InteriorSprite = "Player_Interior_SideView"
        };

        public static readonly VehicleDefinition YellowBus = new()
        {
            Id = LastStopContentIds.YellowBus,
            MaxHealth = 100f,
            WeaponSlots = 5,
            SupportSlots = 3,
            ExteriorSprite = "Bus_Exterior_Yellow"
        };

        public static readonly EnemyDefinition Rusher = new()
        {
            Id = LastStopContentIds.Rusher,
            Kind = EnemyKind.Normal,
            MaxHealth = 3f,
            ExteriorSpeed = 1.15f,
            InteriorSpeed = 1.25f,
            ExteriorSprite = "Enemy_Exterior_TopDown",
            InteriorSprite = "Enemy_Interior_SideView"
        };
    }

    [Serializable]
    public sealed class LastStopRunState
    {
        public string CharacterId = LastStopContentIds.RookieGuard;
        public string VehicleId = LastStopContentIds.YellowBus;
        public int Level = 1;
        public float Experience;
        public float ExperienceToNextLevel = 10f;
        public float SharedHealth = LastStopContentCatalog.YellowBus.MaxHealth;
        public float SharedMaxHealth = LastStopContentCatalog.YellowBus.MaxHealth;
        public readonly List<string> WeaponIds = new();
        public readonly List<string> SupportIds = new();

        public bool IsGameOver => SharedHealth <= 0f;
    }
}
