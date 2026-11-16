using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastStop
{
    internal static class LastStopBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreatePrototype()
        {
            if (Object.FindFirstObjectByType<LastStopPrototype>() != null)
            {
                return;
            }

            new GameObject("LAST STOP MVP").AddComponent<LastStopPrototype>();
        }
    }

    /// <summary>
    /// A self-contained, asset-free first-playable slice. It intentionally creates its
    /// placeholder world at runtime, so replacing art later does not require reauthoring
    /// the MVP scene.
    /// </summary>
    public sealed class LastStopPrototype : MonoBehaviour
    {
        private static Font uiFont;
        private const int BreachWarningThreshold = 50;
        private const float InteriorWorldOffsetX = 100f;
        private const float DemoDurationSeconds = 480f;
        private const float ExteriorAttackInterval = 0.55f;
        private const float InteriorAttackInterval = 0.18f;
        private const float InteriorComboWindow = 0.55f;
        private const float CombatDuckSeconds = 0.30f;
        private const float CombatDuckVolume = 0.25f;
        private const float FeedbackVolume = 0.22f;

        private readonly List<Enemy> exteriorEnemies = new();
        private readonly List<Enemy> interiorEnemies = new();
        private readonly Dictionary<string, Sprite> artSprites = new();
        private readonly Dictionary<int, AudioClip> feedbackClips = new();
        private readonly Dictionary<int, float> feedbackLastPlayed = new();
        private readonly LastStopRunState runState = new();

        private Camera exteriorCamera;
        private Camera interiorCamera;
        private Transform exteriorRoot;
        private Transform interiorRoot;
        private Transform exteriorPlayer;
        private Transform exteriorBus;
        private SpriteRenderer exteriorBusRenderer;
        private Transform interiorPlayer;
        private Transform interiorCore;
        private Transform coreVisual;
        private SpriteRenderer coreVisualRenderer;
        private Vector3 coreVisualBaseScale;
        private bool showingInterior;
        private bool gameOver;
        private bool breachWarningActive;
        private float coreFlashTimer;
        private float hitSourceLabelUntil;
        private float healthPulseUntil;
        private float combatDuckUntil;
        private string hitSourceMessage;
        private float hitFeedbackWindowUntil;
        private float hitFeedbackDamage;
        private int hitFeedbackCount;
        private bool hitFeedbackPlayerHit;
        private bool hitFeedbackCoreHit;
        private bool interiorAttackHintShown;
        private bool playtestMovementLogged;
        private bool playtestAttackLogged;
        private float exteriorDamage = 1f;
        private float exteriorAttackSpeedMultiplier = 1f;
        private bool exteriorPiercingUnlocked;
        private bool bubbleLauncherUnlocked;
        private float bubbleAttackTimer;
        private bool flameSkewerUnlocked;
        private int flameSkewerLevel;
        private float flameAttackTimer;
        private float busFlameTimer;
        private bool levelUpOpen;
        private AudioSource feedbackAudio;
        private int breaches;
        private int peakBreaches;
        private int kills;
        private float runElapsedSeconds;
        private float exteriorAttackTimer;
        private float interiorAttackTimer;
        private float interiorComboExpiresAt;
        private float exteriorSpawnTimer;
        private float nextMiddleWaveAt = 195f;
        private float busImpactFlashTimer;
        private float hintToastUntil;
        private string pendingHintMessage;
        private float pendingHintDuration;
        private float interiorFacing = -1f;
        private int interiorComboStep;
        private bool interiorHintShown;

        private UnityEngine.UI.Text healthLabel;
        private UnityEngine.UI.Image sharedHealthFill;
        private UnityEngine.UI.Text hitSourceLabel;
        private UnityEngine.UI.Text modeLabel;
        private UnityEngine.UI.Text experienceLabel;
        private UnityEngine.UI.Text runLabel;
        private UnityEngine.UI.Text breachLabel;
        private UnityEngine.UI.Text objectiveLabel;
        private UnityEngine.UI.Text loadoutLabel;
        private UnityEngine.UI.Text hintToastLabel;
        private UnityEngine.UI.Text gameOverLabel;
        private UnityEngine.UI.Image statusPanel;
        private UnityEngine.UI.Image loadoutPanel;
        private UnityEngine.UI.Image actionPanel;
        private UnityEngine.UI.Image hintToastPanel;
        private UnityEngine.UI.Image resultDim;
        private UnityEngine.UI.Image resultPanel;
        private UnityEngine.UI.Image levelUpDim;
        private UnityEngine.UI.Image levelUpPanel;
        private UnityEngine.UI.Text levelUpText;
        private float breachPulseUntil;
        private int interiorVisualSequence;
        private readonly UnityEngine.UI.Text[] levelUpChoiceLabels = new UnityEngine.UI.Text[3];
        private readonly UnityEngine.UI.Text[] levelUpChoiceDetails = new UnityEngine.UI.Text[3];

        private enum InteriorAttackTarget { None, Player, Core }

        private sealed class Enemy
        {
            public Transform Transform;
            public float Health;
            public float DamageTimer;
            public InteriorAttackTarget CurrentAttackTarget;
            public float SlowTimer;
            public float DoorwayStaggerTimer;
            public bool IsInside;
            public bool IsDefeated;
        }

        private void Start()
        {
            feedbackAudio = gameObject.AddComponent<AudioSource>();
            feedbackAudio.playOnAwake = false;
            feedbackAudio.volume = FeedbackVolume;
            CreateCameras();
            LoadArtSprites();
            CreateWorld();
            CreateHud();
            SetView(false);
            ShowHint("08:00 버티면 다음 정류장에 도착해요!\n버스 무기가 밖의 적을 자동으로 막습니다.", 3f);
            RecordPlaytestEvent("run_start");
        }

        private void Update()
        {
            if (feedbackAudio != null)
                feedbackAudio.volume = Time.unscaledTime < combatDuckUntil
                    ? FeedbackVolume * CombatDuckVolume : FeedbackVolume;

            if (gameOver)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    Restart();
                }

                return;
            }

            if (levelUpOpen)
            {
                HandleLevelUpInput();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                RecordPlaytestEvent("tab_input");
                SetView(!showingInterior);
            }

            SimulateExterior();
            SimulateBubbleLauncher();
            SimulateBusFlame();
            UpdateCorePresentation();
            SimulateInteriorThreats();
            if (gameOver)
            {
                RefreshHud();
                return;
            }

            if (showingInterior)
            {
                MoveInteriorPlayerAndAttack();
            }

            if (gameOver)
            {
                RefreshHud();
                return;
            }

            runElapsedSeconds += Time.deltaTime;
            if (runElapsedSeconds >= DemoDurationSeconds)
            {
                EndArrival();
                return;
            }

            RefreshHud();
        }

        private void CreateCameras()
        {
            exteriorCamera = Camera.main;
            if (exteriorCamera == null)
            {
                var cameraObject = new GameObject("Exterior Camera");
                exteriorCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            ConfigureCamera(exteriorCamera, new Vector3(0f, 0f, -10f));

            var interiorCameraObject = new GameObject("Interior Camera");
            interiorCamera = interiorCameraObject.AddComponent<Camera>();
            ConfigureCamera(interiorCamera, new Vector3(0f, 0f, -10f));
            interiorCamera.backgroundColor = new Color(0.035f, 0.045f, 0.075f);
        }

        private static void ConfigureCamera(Camera cameraToConfigure, Vector3 position)
        {
            cameraToConfigure.orthographic = true;
            cameraToConfigure.orthographicSize = 5.5f;
            cameraToConfigure.transform.position = position;
            cameraToConfigure.clearFlags = CameraClearFlags.SolidColor;
            cameraToConfigure.backgroundColor = new Color(0.045f, 0.07f, 0.09f);
        }

        private void CreateWorld()
        {
            exteriorRoot = new GameObject("Exterior World").transform;
            interiorRoot = new GameObject("Interior World").transform;
            ConfigureWorldRendering();

            CreateExterior();
            CreateInterior();
        }

        private void ConfigureWorldRendering()
        {
            interiorRoot.position = new Vector3(InteriorWorldOffsetX, 0f, 0f);
            exteriorCamera.cullingMask = ~0;
            interiorCamera.cullingMask = ~0;
            interiorCamera.transform.position = new Vector3(InteriorWorldOffsetX, 0f, -10f);
        }

        private void LoadArtSprites()
        {
            LoadSpriteSheet("Art/LastStop_MvpSpritesheet_v1");
            LoadSpriteSheet("Art/LastStop_CombatVfx_v1");
            LoadBusMuzzleSprite();
            LoadSpriteSheet("Art/LastStop_InteriorCore_v1");
            LoadSpriteSheet("Art/LastStop_BusBody_v2");
            LoadSpriteSheet("Art/LastStop_BusWheels_v2");
            LoadBackdropSprite("Art/LastStop_NightRoad_v1", "NightRoad_Backdrop");
            LoadBackdropSprite("Art/LastStop_NightBusInterior_v1", "NightBusInterior_Backdrop");
        }

        private void LoadBackdropSprite(string resourcePath, string spriteName)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                artSprites[spriteName] = Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        private void LoadSpriteSheet(string resourcePath)
        {
            foreach (var sprite in Resources.LoadAll<Sprite>(resourcePath))
            {
                artSprites[sprite.name] = sprite;
            }
        }

        private void LoadBusMuzzleSprite()
        {
            var texture = Resources.Load<Texture2D>("Art/LastStop_CombatVfx_v1");
            if (texture == null || texture.width < 460 || texture.height < 807) return;
            texture.filterMode = FilterMode.Point;
            var muzzleSprite = Sprite.Create(texture,
                new Rect(310f, 632f, 150f, 175f), new Vector2(0.5f, 0.5f), 100f);
            muzzleSprite.name = "Vfx_Bus_Muzzle";
            artSprites[muzzleSprite.name] = muzzleSprite;
        }

        private void CreateExterior()
        {
            if (artSprites.TryGetValue("NightRoad_Backdrop", out var nightRoad))
            {
                var artworkHeight = 13.2f;
                var artworkWidth = artworkHeight * nightRoad.rect.width / nightRoad.rect.height;
                for (var index = -1; index <= 1; index++)
                {
                    var backdrop = CreateBlock("흐르는 야간 도로", exteriorRoot,
                        new Vector2(index * artworkWidth, 1.1f), new Vector2(artworkWidth, artworkHeight),
                        Color.white, -3, "NightRoad_Backdrop");
                    backdrop.gameObject.AddComponent<LastStopRoadMotion>()
                        .Configure(0.7f, -artworkWidth * 1.5f, artworkWidth * 1.5f);
                }
            }
            else
            {
                var backdropWidth = Mathf.Max(20f, exteriorCamera.orthographicSize * exteriorCamera.aspect * 2f + 2f);
                CreateBlock("Arena", exteriorRoot, Vector2.zero, new Vector2(backdropWidth, 12f), new Color(0.10f, 0.18f, 0.16f), -3);
                CreateBlock("Road", exteriorRoot, Vector2.zero, new Vector2(backdropWidth, 2.7f), new Color(0.13f, 0.13f, 0.15f), -2);
                CreateBlock("Road Edge North", exteriorRoot, new Vector2(0f, 1.35f), new Vector2(backdropWidth, 0.10f), new Color(0.25f, 0.30f, 0.28f), -1);
                CreateBlock("Road Edge South", exteriorRoot, new Vector2(0f, -1.35f), new Vector2(backdropWidth, 0.10f), new Color(0.25f, 0.30f, 0.28f), -1);
                CreateRoadPresentation();
            }
            CreateRoadMarkers();
            var animatedBusArt = TryGetAnimatedBusArt(out var wheelFrames);
            var busSpriteName = animatedBusArt ? "Bus_Body_NoWheels" : LastStopContentCatalog.YellowBus.ExteriorSprite;
            exteriorBus = CreateBlock("Bus", exteriorRoot, new Vector2(0f, 0f), new Vector2(4.8f, 1.8f), Color.white, animatedBusArt ? 1 : 0, busSpriteName);
            exteriorBusRenderer = exteriorBus.GetComponent<SpriteRenderer>();
            if (animatedBusArt)
                exteriorBus.gameObject.AddComponent<LastStopBusWheelAnimation>().Configure(wheelFrames, exteriorBusRenderer);
            if (artSprites.TryGetValue("Vfx_Enemy_Death", out var trailSprite))
                exteriorBus.gameObject.AddComponent<LastStopBusTrail>().Configure(trailSprite);

            for (var index = 0; index < 3; index++)
            {
                SpawnExteriorEnemy();
            }
        }

        private bool TryGetAnimatedBusArt(out Sprite[] wheelFrames)
        {
            wheelFrames = new Sprite[4];
            if (!artSprites.TryGetValue("Bus_Body_NoWheels", out var body)) return false;

            for (var index = 0; index < wheelFrames.Length; index++)
            {
                if (!artSprites.TryGetValue($"Bus_Wheels_{index}", out var frame) ||
                    frame.rect.size != body.rect.size || frame.pivot != body.pivot ||
                    Mathf.Abs(frame.pixelsPerUnit - body.pixelsPerUnit) > 0.01f)
                    return false;

                wheelFrames[index] = frame;
            }

            return true;
        }

        private void CreateRoadPresentation()
        {
            for (var index = 0; index < 12; index++)
            {
                var y = 2.15f + (index % 4) * 0.68f;
                var star = CreateBlock("흐르는 별빛", exteriorRoot, new Vector2(-10f + index * 1.8f, y), new Vector2(0.05f, 0.05f), new Color(0.76f, 0.88f, 1f), -2);
                star.gameObject.AddComponent<LastStopRoadMotion>().Configure(0.22f + (index % 3) * 0.06f, -10.8f, 10.8f);
                star.gameObject.AddComponent<LastStopSpriteMotion>().Configure(LastStopSpriteMotion.MotionStyle.GentleBreathing);
            }

            for (var index = 0; index < 5; index++)
            {
                var height = 0.85f + (index % 3) * 0.35f;
                var building = CreateBlock("흐르는 야경 건물", exteriorRoot, new Vector2(-9.5f + index * 4.6f, 2.05f), new Vector2(1.4f, height), new Color(0.08f, 0.13f, 0.21f), -2);
                building.gameObject.AddComponent<LastStopRoadMotion>().Configure(0.72f, -11.7f, 11.7f);
                for (var window = 0; window < 2; window++)
                {
                    CreateBlock("야경 창문", building, new Vector2(-0.32f + window * 0.62f, 0.05f), new Vector2(0.16f, 0.12f), new Color(1f, 0.74f, 0.29f), -1);
                }
            }

            for (var index = 0; index < 5; index++)
            {
                var bush = CreateBlock("흐르는 도로변 풀", exteriorRoot, new Vector2(-10f + index * 4.4f, 1.35f), new Vector2(0.9f, 0.34f), new Color(0.12f, 0.32f, 0.25f), -1);
                bush.gameObject.AddComponent<LastStopRoadMotion>().Configure(1.85f, -11.3f, 11.3f);
            }

            for (var index = 0; index < 4; index++)
            {
                var lamp = CreateBlock("흐르는 가로등", exteriorRoot, new Vector2(-8f + index * 5.3f, 3.25f), new Vector2(0.16f, 1.15f), new Color(0.4f, 0.53f, 0.62f), -1);
                lamp.gameObject.AddComponent<LastStopRoadMotion>().Configure(3.2f, -11.5f, 11.5f);
                var light = CreateBlock("가로등 불빛", lamp, new Vector2(0f, 0.62f), new Vector2(0.42f, 0.22f), new Color(1f, 0.78f, 0.27f), 0);
                light.gameObject.AddComponent<LastStopSpriteMotion>().Configure(LastStopSpriteMotion.MotionStyle.GentleBreathing);
            }
        }

        private void CreateRoadMarkers()
        {
            for (var index = 0; index < 13; index++)
            {
                var marker = CreateBlock("흐르는 차선", exteriorRoot,
                    new Vector2(-16.2f + index * 2.7f, -0.84f), new Vector2(1.1f, 0.08f),
                    new Color(1f, 0.86f, 0.38f), -1);
                marker.gameObject.AddComponent<LastStopRoadMotion>().Configure(4.8f, -18f, 18f);
            }
        }

        private void CreateInterior()
        {
            var backdropWidth = Mathf.Max(20f, interiorCamera.orthographicSize * interiorCamera.aspect * 2f + 2f);
            var hasInteriorArt = artSprites.ContainsKey("NightBusInterior_Backdrop");
            if (hasInteriorArt)
            {
                CreateBlock("Night Bus Interior", interiorRoot, Vector2.zero,
                    new Vector2(backdropWidth, 11.5f), Color.white, -2, "NightBusInterior_Backdrop");
            }
            else
            {
                CreateBlock("Interior Background", interiorRoot, Vector2.zero, new Vector2(backdropWidth, 12f), new Color(0.075f, 0.08f, 0.11f), -2);
                CreateBlock("Bus Floor", interiorRoot, new Vector2(0f, -2.1f), new Vector2(15.5f, 0.55f), new Color(0.22f, 0.18f, 0.14f), -1);
                CreateBlock("Bus Ceiling", interiorRoot, new Vector2(0f, 2.35f), new Vector2(15.5f, 0.32f), new Color(0.34f, 0.27f, 0.17f), -1);
                CreateBlock("Driver Cabin", interiorRoot, new Vector2(6.4f, -0.45f), new Vector2(0.65f, 3.8f), new Color(0.20f, 0.24f, 0.32f), 0);
            }
            interiorPlayer = CreateBlock("Interior Player", interiorRoot, new Vector2(5.8f, -1.55f), new Vector2(0.70f, 0.90f), Color.white, 2, LastStopContentCatalog.RookieGuard.InteriorSprite);
            ConfigureInteriorSwing();
            interiorCore = new GameObject("Interior Core").transform;
            interiorCore.SetParent(interiorRoot, false);
            interiorCore.localPosition = new Vector3(4.75f, -1.45f, 0f);
            coreVisual = CreateBlock("Core Visual", interiorCore, Vector2.zero, new Vector2(0.96f, 1.05f), Color.white, 3, "LastStop_InteriorCore_v1");
            coreVisualRenderer = coreVisual.GetComponent<SpriteRenderer>();
            coreVisualBaseScale = coreVisual.localScale;
        }

        private void UpdateCorePresentation()
        {
            if (coreVisualRenderer == null) return;
            coreFlashTimer = Mathf.Max(0f, coreFlashTimer - Time.deltaTime);
            var healthRatio = runState.SharedHealth / Mathf.Max(1f, runState.SharedMaxHealth);
            var baseColor = healthRatio <= 0.3f
                ? new Color(1f, 0.60f, 0.56f)
                : Color.white;
            coreVisualRenderer.color = coreFlashTimer > 0f ? new Color(1f, 0.48f, 0.40f) : baseColor;
            var pulse = 1f + 0.035f * Mathf.Sin(Time.time * (healthRatio <= 0.3f ? 8f : 2.5f));
            coreVisual.localScale = coreVisualBaseScale * (coreFlashTimer > 0f ? 1.12f : pulse);
        }

        private void CreateHud()
        {
            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("HUD Event System", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }

            statusPanel = CreateUiPanel(canvasObject.transform, "Status Panel", new Color(0.015f, 0.02f, 0.035f, 0.88f));
            SetAnchors(statusPanel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -124f), Vector2.zero);
            healthLabel = CreateUiText(statusPanel.transform, "Shared Health", TextAnchor.UpperLeft, 24);
            var healthBar = CreateUiPanel(statusPanel.transform, "Shared Health Bar", new Color(0.10f, 0.15f, 0.22f));
            sharedHealthFill = CreateUiPanel(healthBar.transform, "Shared Health Fill", new Color(0.25f, 0.92f, 0.79f));
            hitSourceLabel = CreateUiText(statusPanel.transform, "Hit Source", TextAnchor.MiddleLeft, 18);
            modeLabel = CreateUiText(statusPanel.transform, "Mode", TextAnchor.MiddleCenter, 20);
            experienceLabel = CreateUiText(statusPanel.transform, "Experience", TextAnchor.MiddleCenter, 20);
            runLabel = CreateUiText(statusPanel.transform, "Run Timer", TextAnchor.MiddleCenter, 26);
            breachLabel = CreateUiText(statusPanel.transform, "Breaches", TextAnchor.UpperRight, 24);
            SetAnchors(healthLabel.rectTransform, new Vector2(0.02f, 0.70f), new Vector2(0.32f, 0.98f), Vector2.zero, Vector2.zero);
            SetAnchors(healthBar.rectTransform, new Vector2(0.02f, 0.55f), new Vector2(0.31f, 0.68f), Vector2.zero, Vector2.zero);
            SetAnchors(sharedHealthFill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetAnchors(hitSourceLabel.rectTransform, new Vector2(0.02f, 0.30f), new Vector2(0.32f, 0.52f), Vector2.zero, Vector2.zero);
            SetAnchors(runLabel.rectTransform, new Vector2(0.35f, 0.66f), new Vector2(0.65f, 0.98f), Vector2.zero, Vector2.zero);
            SetAnchors(modeLabel.rectTransform, new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.63f), Vector2.zero, Vector2.zero);
            SetAnchors(experienceLabel.rectTransform, new Vector2(0.35f, 0.10f), new Vector2(0.65f, 0.35f), Vector2.zero, Vector2.zero);
            SetAnchors(breachLabel.rectTransform, new Vector2(0.68f, 0.16f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

            actionPanel = CreateUiPanel(canvasObject.transform, "Action Bar", new Color(0.015f, 0.02f, 0.035f, 0.88f));
            SetAnchors(actionPanel.rectTransform, new Vector2(0.12f, 0.205f), new Vector2(0.88f, 0.265f), Vector2.zero, Vector2.zero);
            objectiveLabel = CreateUiText(actionPanel.transform, "Objective", TextAnchor.MiddleCenter, 23);
            SetAnchors(objectiveLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-12f, 0f));

            loadoutPanel = CreateUiPanel(canvasObject.transform, "Loadout Panel", new Color(0.015f, 0.03f, 0.07f, 0.82f));
            SetAnchors(loadoutPanel.rectTransform, new Vector2(0.015f, 0.54f), new Vector2(0.22f, 0.76f), Vector2.zero, Vector2.zero);
            loadoutLabel = CreateUiText(loadoutPanel.transform, "Loadout", TextAnchor.UpperLeft, 21);
            SetAnchors(loadoutLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(14f, 4f), new Vector2(-12f, -4f));

            hintToastPanel = CreateUiPanel(canvasObject.transform, "Hint Toast Panel", new Color(0.01f, 0.025f, 0.04f, 0.86f));
            SetAnchors(hintToastPanel.rectTransform, new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.195f), Vector2.zero, Vector2.zero);
            hintToastLabel = CreateUiText(hintToastPanel.transform, "Hint Toast", TextAnchor.MiddleCenter, 27);
            hintToastLabel.color = new Color(1f, 0.93f, 0.62f);
            SetAnchors(hintToastLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-12f, -6f));
            hintToastPanel.gameObject.SetActive(false);

            resultDim = CreateUiPanel(canvasObject.transform, "Result Dim", new Color(0.01f, 0.02f, 0.05f, 0.78f));
            SetAnchors(resultDim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            resultDim.gameObject.SetActive(false);
            resultPanel = CreateUiPanel(canvasObject.transform, "Result Panel", new Color(0.035f, 0.06f, 0.13f, 0.96f));
            SetAnchors(resultPanel.rectTransform, new Vector2(0.2f, 0.31f), new Vector2(0.8f, 0.69f), Vector2.zero, Vector2.zero);
            gameOverLabel = CreateUiText(resultPanel.transform, "Result Text", TextAnchor.MiddleCenter, 44);
            SetAnchors(gameOverLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(28f, 20f), new Vector2(-28f, -20f));
            resultPanel.gameObject.SetActive(false);

            levelUpDim = CreateUiPanel(canvasObject.transform, "Level Up Dim", new Color(0.01f, 0.02f, 0.05f, 0.72f));
            SetAnchors(levelUpDim.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            levelUpDim.gameObject.SetActive(false);
            levelUpPanel = CreateUiPanel(canvasObject.transform, "Level Up Panel", new Color(0.02f, 0.03f, 0.08f, 0.98f));
            SetAnchors(levelUpPanel.rectTransform, new Vector2(0.20f, 0.14f), new Vector2(0.80f, 0.78f), Vector2.zero, Vector2.zero);
            levelUpText = CreateUiText(levelUpPanel.transform, "Level Up Title", TextAnchor.MiddleCenter, 28);
            SetAnchors(levelUpText.rectTransform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.95f), Vector2.zero, Vector2.zero);
            var levelUpHint = CreateUiText(levelUpPanel.transform, "Level Up Input Hint", TextAnchor.MiddleCenter, 18);
            levelUpHint.text = "카드를 클릭하거나 1 · 2 · 3 키로 선택";
            levelUpHint.color = new Color(0.73f, 0.79f, 0.83f);
            SetAnchors(levelUpHint.rectTransform, new Vector2(0.06f, 0.01f), new Vector2(0.94f, 0.10f), Vector2.zero, Vector2.zero);
            for (var choice = 1; choice <= 3; choice++)
            {
                var choiceIndex = choice;
                var buttonObject = new GameObject("Level Up Choice " + choice, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                buttonObject.transform.SetParent(levelUpPanel.transform, false);
                var buttonRect = buttonObject.GetComponent<RectTransform>();
                var left = choice == 1 ? 0.05f : choice == 2 ? 0.365f : 0.68f;
                var right = choice == 1 ? 0.32f : choice == 2 ? 0.635f : 0.95f;
                SetAnchors(buttonRect, new Vector2(left, 0.12f), new Vector2(right, 0.75f), Vector2.zero, Vector2.zero);
                var buttonImage = buttonObject.GetComponent<UnityEngine.UI.Image>();
                buttonImage.color = Color.white;
                buttonImage.raycastTarget = true;
                var button = buttonObject.GetComponent<UnityEngine.UI.Button>();
                button.targetGraphic = buttonImage;
                var cardColor = new Color(0.12f, 0.19f, 0.29f, 0.98f);
                var accentColor = choice == 1 ? new Color(0.48f, 0.76f, 1f)
                    : choice == 2 ? new Color(0.49f, 0.93f, 0.79f)
                    : new Color(1f, 0.78f, 0.42f);
                button.colors = new UnityEngine.UI.ColorBlock
                {
                    normalColor = cardColor,
                    highlightedColor = new Color(0.20f, 0.31f, 0.43f, 1f),
                    pressedColor = new Color(0.09f, 0.14f, 0.22f, 1f),
                    selectedColor = new Color(0.20f, 0.31f, 0.43f, 1f),
                    disabledColor = new Color(0.12f, 0.14f, 0.18f, 0.6f),
                    colorMultiplier = 1f,
                    fadeDuration = 0.12f
                };
                button.onClick.AddListener(() => ChooseLevelUp(choiceIndex));
                var cardOutline = buttonObject.AddComponent<UnityEngine.UI.Outline>();
                cardOutline.effectColor = accentColor;
                cardOutline.effectDistance = new Vector2(2f, -2f);
                var cardAccent = CreateUiPanel(buttonObject.transform, "Card Accent", accentColor);
                SetAnchors(cardAccent.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -6f), Vector2.zero);
                var keycap = CreateUiText(buttonObject.transform, "Keycap", TextAnchor.MiddleCenter, 26);
                keycap.text = "[" + choice + "]";
                keycap.color = accentColor;
                SetAnchors(keycap.rectTransform, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
                levelUpChoiceLabels[choice - 1] = CreateUiText(buttonObject.transform, "Choice Title", TextAnchor.MiddleCenter, 27);
                SetAnchors(levelUpChoiceLabels[choice - 1].rectTransform, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.72f), Vector2.zero, Vector2.zero);
                levelUpChoiceDetails[choice - 1] = CreateUiText(buttonObject.transform, "Choice Effect", TextAnchor.MiddleCenter, 22);
                SetAnchors(levelUpChoiceDetails[choice - 1].rectTransform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.42f), Vector2.zero, Vector2.zero);
            }
            levelUpPanel.gameObject.SetActive(false);
        }

        private static UnityEngine.UI.Image CreateUiPanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static UnityEngine.UI.Text CreateUiText(Transform parent, string name, TextAnchor alignment, int size)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<UnityEngine.UI.Text>();
            if (uiFont == null)
            {
                uiFont = Resources.Load<Font>("Fonts/NotoSansCJKkr-Regular");
                if (uiFont == null)
                {
                    Debug.LogError("LAST STOP UI font is missing from Resources/Fonts.");
                    uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            text.font = uiFont;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 minimum, Vector2 maximum, Vector2 minimumOffset, Vector2 maximumOffset)
        {
            rectTransform.anchorMin = minimum;
            rectTransform.anchorMax = maximum;
            rectTransform.offsetMin = minimumOffset;
            rectTransform.offsetMax = maximumOffset;
        }

        private Transform CreateBlock(string name, Transform parent, Vector2 position, Vector2 size, Color color, int order, string spriteName = null)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            var renderer = block.AddComponent<SpriteRenderer>();
            if (!string.IsNullOrEmpty(spriteName) && artSprites.TryGetValue(spriteName, out var artSprite))
            {
                renderer.sprite = artSprite;
                var spriteSize = artSprite.bounds.size;
                block.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            }
            else
            {
                renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                block.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            renderer.color = color;
            renderer.sortingOrder = order;
            AddPresentationMotion(block, spriteName);
            return block.transform;
        }

        private void ConfigureSprite(Transform target, string spriteName, Vector2 worldSize)
        {
            if (!artSprites.TryGetValue(spriteName, out var sprite)) return;
            var renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 2;
            var spriteSize = sprite.bounds.size;
            target.localScale = new Vector3(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y, 1f);
        }

        private static void AddPresentationMotion(GameObject block, string spriteName)
        {
            LastStopSpriteMotion.MotionStyle? motion = spriteName switch
            {
                "Player_Exterior_TopDown" or "Player_Interior_SideView" => LastStopSpriteMotion.MotionStyle.GentleBreathing,
                "Enemy_Exterior_TopDown" or "Enemy_Interior_SideView" => LastStopSpriteMotion.MotionStyle.EnemyBounce,
                "Bus_Exterior_Yellow" or "Bus_Body_NoWheels" => LastStopSpriteMotion.MotionStyle.BusSway,
                _ => null
            };

            if (motion.HasValue)
            {
                block.AddComponent<LastStopSpriteMotion>().Configure(motion.Value);
            }
        }

        private void SetView(bool interior)
        {
            var changed = showingInterior != interior;
            showingInterior = interior;
            exteriorCamera.enabled = !interior;
            interiorCamera.enabled = interior;
            if (changed)
            {
                LastStopAudio.TryPlay(LastStopAudioEvent.ViewChanged);
                RecordPlaytestEvent("view_changed", interior ? "view=interior" : "view=exterior");
            }

            RefreshHud();
        }

        private float GetExteriorSpawnInterval()
        {
            if (runElapsedSeconds < 150f)
            {
                return 4f;
            }

            if (runElapsedSeconds < 330f)
            {
                return 2.6f;
            }

            return runElapsedSeconds >= 420f ? 1.5f : 1.8f;
        }

        private int GetExteriorEnemyCap()
        {
            if (runElapsedSeconds < 150f)
            {
                return 6;
            }

            return runElapsedSeconds < 330f ? 10 : 14;
        }

        private void SimulateExterior()
        {
            UpdateBusImpactPresentation();
            var exteriorCap = GetExteriorEnemyCap();
            exteriorSpawnTimer += Time.deltaTime;
            if (exteriorSpawnTimer >= GetExteriorSpawnInterval() && exteriorEnemies.Count < exteriorCap)
            {
                exteriorSpawnTimer = 0f;
                SpawnExteriorEnemy();
            }

            if (runElapsedSeconds >= 150f && runElapsedSeconds < 330f && runElapsedSeconds >= nextMiddleWaveAt)
            {
                nextMiddleWaveAt += 45f;
                for (var waveEnemy = 0; waveEnemy < 2 && exteriorEnemies.Count < exteriorCap; waveEnemy++)
                {
                    SpawnExteriorEnemy();
                }
            }

            exteriorAttackTimer += Time.deltaTime;
            if (exteriorAttackTimer >= ExteriorAttackInterval / exteriorAttackSpeedMultiplier)
            {
                exteriorAttackTimer = 0f;
                FireBusProjectile();
            }

            for (var index = exteriorEnemies.Count - 1; index >= 0; index--)
            {
                var enemy = exteriorEnemies[index];
                if (enemy.IsDefeated || enemy.Transform == null)
                {
                    exteriorEnemies.RemoveAt(index);
                    continue;
                }

                MoveExteriorEnemyTowardsBus(enemy, index, Time.deltaTime);
            }
        }

        private void MoveExteriorEnemyTowardsBus(Enemy enemy, int exteriorIndex, float deltaTime)
        {
            var target = exteriorBus.position;
            enemy.SlowTimer = Mathf.Max(0f, enemy.SlowTimer - deltaTime);
            var speedMultiplier = enemy.SlowTimer > 0f ? 0.42f : 1f;
            enemy.Transform.position = Vector3.MoveTowards(enemy.Transform.position, target,
                deltaTime * LastStopContentCatalog.Rusher.ExteriorSpeed * speedMultiplier);
            var remaining = target - enemy.Transform.position;
            if (Mathf.Abs(remaining.x) < 2.15f && Mathf.Abs(remaining.y) < 0.85f)
            {
                TransferEnemyInside(enemy, exteriorIndex);
            }
        }

        private void TransferEnemyInside(Enemy enemy, int exteriorIndex)
        {
            exteriorEnemies.RemoveAt(exteriorIndex);
            enemy.IsInside = true;
            interiorEnemies.Add(enemy);
            busImpactFlashTimer = 0.24f;
            breachPulseUntil = Time.unscaledTime + 0.65f;
            if (!interiorHintShown)
            {
                interiorHintShown = true;
                ShowHint("버스에 닿은 적이 안으로 들어왔어요!\nTab으로 내부를 확인하고 Space로 막으세요.", 3.2f);
            }
            SpawnFeedback(exteriorRoot, exteriorBus.localPosition, new Vector2(1.15f, 1.15f), new Color(1f, 0.44f, 0.20f), 0.24f, "Vfx_Hit");
            PlayFeedbackTone(185f, 0.18f);
            enemy.Transform.name = "Interior Enemy";
            enemy.Transform.SetParent(interiorRoot, false);
            enemy.Transform.localPosition = new Vector3(-5.75f, -1.55f, 0f);
            SeparateInteriorEnemyVisual(enemy.Transform);
            enemy.DoorwayStaggerTimer = 0.6f;
            var entryFloor = enemy.Transform.localPosition + new Vector3(0f, -0.30f, 0f);
            SpawnFeedback(interiorRoot, entryFloor, new Vector2(0.42f, 0.14f),
                new Color(1f, 0.78f, 0.25f, 0.65f), 0.14f, "Vfx_Hit");
            RecordPlaytestEvent("breach");
        }

        private void SeparateInteriorEnemyVisual(Transform enemyTransform)
        {
            var originalRenderer = enemyTransform.GetComponent<SpriteRenderer>();
            if (originalRenderer == null) return;

            // Keep the root at its gameplay position; only the sprite is offset.
            var visual = new GameObject("Interior Enemy Visual").transform;
            visual.SetParent(enemyTransform, false);
            var lane = interiorVisualSequence++ % 15;
            var offsetX = (lane % 5 - 2) * 0.18f;
            var offsetY = (lane / 5 - 1) * 0.15f;
            visual.localPosition = new Vector3(offsetX / enemyTransform.localScale.x,
                offsetY / enemyTransform.localScale.y, 0f);

            var visualRenderer = visual.gameObject.AddComponent<SpriteRenderer>();
            visualRenderer.sprite = originalRenderer.sprite;
            visualRenderer.color = originalRenderer.color;
            visualRenderer.sortingLayerID = originalRenderer.sortingLayerID;
            visualRenderer.sortingOrder = originalRenderer.sortingOrder;
            visualRenderer.flipX = originalRenderer.flipX;
            originalRenderer.enabled = false;

            var motion = enemyTransform.GetComponent<LastStopSpriteMotion>();
            if (motion != null)
            {
                motion.SetPresentationRenderer(visualRenderer);
                motion.TriggerEntry();
            }
        }

        private void UpdateBusImpactPresentation()
        {
            if (exteriorBusRenderer == null) return;
            busImpactFlashTimer = Mathf.Max(0f, busImpactFlashTimer - Time.deltaTime);
            exteriorBusRenderer.color = busImpactFlashTimer > 0f
                ? Color.Lerp(Color.white, new Color(1f, 0.28f, 0.18f), busImpactFlashTimer / 0.24f)
                : Color.white;
        }

        private void SimulateInteriorThreats()
        {
            breaches = 0;
            for (var index = interiorEnemies.Count - 1; index >= 0; index--)
            {
                var enemy = interiorEnemies[index];
                if (enemy.Transform == null)
                {
                    interiorEnemies.RemoveAt(index);
                    continue;
                }

                breaches++;
                enemy.DoorwayStaggerTimer = Mathf.Max(0f, enemy.DoorwayStaggerTimer - Time.deltaTime);
                if (enemy.DoorwayStaggerTimer > 0f) continue;
                var playerInReach = Vector2.Distance(enemy.Transform.position, interiorPlayer.position) <= 0.58f;
                var coreInReach = Vector2.Distance(enemy.Transform.position, interiorCore.position) <= 0.70f;
                var attackTarget = playerInReach ? InteriorAttackTarget.Player
                    : coreInReach ? InteriorAttackTarget.Core : InteriorAttackTarget.None;
                if (enemy.CurrentAttackTarget != attackTarget)
                {
                    enemy.CurrentAttackTarget = attackTarget;
                    enemy.DamageTimer = 0f;
                }
                if (attackTarget != InteriorAttackTarget.None)
                {
                    DamageSharedHealth(enemy, attackTarget == InteriorAttackTarget.Player);
                    if (gameOver) break;
                    continue;
                }

                MoveInteriorEnemyTowardsCore(enemy.Transform, Time.deltaTime);
            }

            peakBreaches = Mathf.Max(peakBreaches, breaches);

            if (!breachWarningActive && breaches >= BreachWarningThreshold)
            {
                breachWarningActive = true;
                LastStopAudio.TryPlay(LastStopAudioEvent.BreachWarningOn);
            }
            else if (breachWarningActive && breaches < BreachWarningThreshold)
            {
                breachWarningActive = false;
            }

        }

        private void MoveInteriorEnemyTowardsCore(Transform enemyTransform, float deltaTime)
        {
            var position = enemyTransform.position;
            position.x = Mathf.MoveTowards(position.x, interiorCore.position.x,
                LastStopContentCatalog.Rusher.InteriorSpeed * deltaTime);
            enemyTransform.position = position;
        }

        private void MoveInteriorPlayerAndAttack()
        {
            var localPosition = interiorPlayer.localPosition;
            var previousX = localPosition.x;
            var playerMotion = interiorPlayer.GetComponent<LastStopSpriteMotion>();
            var movement = ReadMovement();
            if (!Mathf.Approximately(movement.x, 0f))
            {
                interiorFacing = Mathf.Sign(movement.x);
                playerMotion.SetFacing(interiorFacing);
            }

            localPosition += (Vector3)(new Vector2(movement.x, 0f) * LastStopContentCatalog.RookieGuard.MoveSpeed * Time.deltaTime);
            localPosition = new Vector3(Mathf.Clamp(localPosition.x, -5.8f, 5.8f), -1.55f, 0f);
            interiorPlayer.localPosition = localPosition;
            if (!playtestMovementLogged && !Mathf.Approximately(previousX, localPosition.x))
            {
                playtestMovementLogged = true;
                RecordPlaytestEvent("first_interior_move");
            }

            interiorAttackTimer += Time.deltaTime;
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed && interiorAttackTimer >= InteriorAttackInterval)
            {
                interiorAttackTimer = 0f;
                AdvanceInteriorCombo();
                playerMotion.TriggerAttack(interiorComboStep, interiorFacing);
                if (!playtestAttackLogged)
                {
                    playtestAttackLogged = true;
                    RecordPlaytestEvent("first_interior_attack");
                }
                var isFinisher = interiorComboStep == 3;
                SpawnInteriorAttackFeedback(interiorComboStep, playerMotion.HasSwingSprites);
                if (isFinisher)
                {
                    DamageInteriorSweep();
                }
                else
                {
                    var range = interiorComboStep == 1 ? 1.35f : 1.55f;
                    var knockback = interiorComboStep == 2 ? 0.25f : 0f;
                    DamageNearestInFacing(interiorEnemies, interiorPlayer.position, range, 1f, knockback);
                }
                TryFlameSkewer(interiorEnemies, interiorRoot);
            }

        }

        private void AdvanceInteriorCombo()
        {
            interiorComboStep = Time.time <= interiorComboExpiresAt ? interiorComboStep % 3 + 1 : 1;
            interiorComboExpiresAt = Time.time + InteriorComboWindow;
        }

        private void ConfigureInteriorSwing()
        {
            const int frameWidth = 448;
            var idle = interiorPlayer.GetComponent<SpriteRenderer>().sprite;
            var sheet = Resources.Load<Texture2D>("Art/LastStop_GuardSwing_v6");
            if (sheet == null || idle == null || sheet.width != frameWidth * 2 ||
                sheet.height != idle.rect.height || idle.rect.width != 322) return;

            sheet.filterMode = FilterMode.Point;
            var pivot = new Vector2(idle.pivot.x / frameWidth, idle.pivot.y / sheet.height);
            var contact = Sprite.Create(sheet, new Rect(0, 0, frameWidth, sheet.height), pivot,
                idle.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            var followThrough = Sprite.Create(sheet, new Rect(frameWidth, 0, frameWidth, sheet.height), pivot,
                idle.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            contact.name = "GuardSwing_Contact";
            followThrough.name = "GuardSwing_FollowThrough";
            interiorPlayer.GetComponent<LastStopSpriteMotion>().SetSwingSprites(contact, followThrough);
        }

        private void SpawnInteriorAttackFeedback(int comboStep, bool hasSwingTrail)
        {
            var finisher = comboStep == 3;
            if (!hasSwingTrail)
            {
                var reach = finisher ? 1.1f : comboStep == 2 ? 0.84f : 0.72f;
                var size = finisher ? new Vector2(1.55f, 1.05f) : new Vector2(0.95f, 0.68f);
                var color = finisher ? new Color(1f, 0.72f, 0.22f) : Color.white;
                var slashPosition = interiorPlayer.localPosition + Vector3.right * interiorFacing * reach;
                var slash = CreateBlock(finisher ? "내부 3타 마무리" : "내부 근접 타격", interiorRoot, slashPosition, size, color, 5, "Vfx_Hit");
                slash.GetComponent<SpriteRenderer>().flipX = interiorFacing < 0f;
                DestroyPresentation(slash.gameObject, finisher ? 0.2f : 0.12f);
            }
            PlayFeedbackTone(finisher ? 880f : comboStep == 2 ? 740f : 659f, finisher ? 0.115f : comboStep == 2 ? 0.08f : 0.07f);
        }

        private void SimulateBubbleLauncher()
        {
            if (!bubbleLauncherUnlocked) return;
            bubbleAttackTimer += Time.deltaTime;
            if (bubbleAttackTimer < 1.35f) return;
            bubbleAttackTimer = 0f;

            var target = FindNearest(exteriorEnemies, exteriorBus.position, 5.5f);
            if (target == null) return;
            var bubble = CreateBlock("비눗방울 탄환", exteriorRoot, exteriorBus.localPosition, new Vector2(0.36f, 0.20f), Color.white, 4, "Vfx_Bubble_Projectile");
            bubble.gameObject.AddComponent<LastStopProjectileVisual>().Configure(
                target.Transform,
                8f,
                () =>
                {
                    if (!IsActiveExteriorEnemy(target)) return;
                    target.SlowTimer = 1.4f;
                    var popPosition = target.Transform.localPosition;
                    DamageEnemy(target, 0.75f);
                    var pop = CreateBlock("비눗방울 팝", exteriorRoot, popPosition, new Vector2(0.60f, 0.60f), new Color(0.72f, 0.94f, 1f), 5, "Vfx_Hit");
                    DestroyPresentation(pop.gameObject, 0.22f);
                    PlayFeedbackTone(620f, 0.11f);
                }, () => IsActiveExteriorEnemy(target));
            PlayFeedbackTone(510f, 0.08f);
        }

        private void FireBusProjectile()
        {
            var target = FindNearest(exteriorEnemies, exteriorBus.position, 20f);
            if (target == null) return;

            var muzzlePosition = exteriorBus.localPosition + new Vector3(0.4f, 0.7f, 0f);
            if (artSprites.ContainsKey("Vfx_Bus_Muzzle"))
                SpawnFeedback(exteriorRoot, muzzlePosition,
                    new Vector2(0.62f, 0.72f), Color.white, 0.11f, "Vfx_Bus_Muzzle");
            var projectile = CreateBlock("콩알 탄환", exteriorRoot, muzzlePosition, new Vector2(0.48f, 0.18f), Color.white, 4, "Vfx_Bean_Projectile");
            projectile.gameObject.AddComponent<LastStopProjectileVisual>().Configure(
                target.Transform,
                13f,
                () =>
                {
                    if (!IsActiveExteriorEnemy(target)) return;
                    var triggerPopcorn = exteriorPiercingUnlocked && target.SlowTimer > 0f;
                    var impactPosition = target.Transform.localPosition;
                    DamageEnemy(target, exteriorDamage);
                    if (triggerPopcorn) SpawnPopcornBurst(impactPosition, target);
                }, () => IsActiveExteriorEnemy(target), orientToTarget: true);
            PlayFeedbackTone(780f, 0.055f);
        }

        private void SpawnPopcornBurst(Vector3 origin, Enemy primaryTarget)
        {
            var targets = new List<Enemy>();
            foreach (var enemy in exteriorEnemies)
            {
                if (enemy == primaryTarget || enemy.IsDefeated || enemy.Transform == null) continue;
                if (Vector2.Distance(enemy.Transform.localPosition, origin) <= 3f) targets.Add(enemy);
            }

            targets.Sort((left, right) => Vector2.Distance(left.Transform.localPosition, origin).CompareTo(Vector2.Distance(right.Transform.localPosition, origin)));
            var burst = Mathf.Min(3, targets.Count);
            SpawnFeedback(exteriorRoot, origin, new Vector2(0.65f, 0.65f), new Color(1f, 0.88f, 0.34f), 0.14f, "Vfx_Bean_Projectile");
            for (var index = 0; index < burst; index++)
            {
                var target = targets[index];
                var split = CreateBlock("팝콘 분열탄", exteriorRoot, origin, new Vector2(0.22f, 0.09f), Color.white, 4, "Vfx_Bean_Projectile");
                split.gameObject.AddComponent<LastStopProjectileVisual>().Configure(target.Transform, 12f, () =>
                {
                    if (IsActiveExteriorEnemy(target)) DamageEnemy(target, exteriorDamage * 0.65f);
                }, () => IsActiveExteriorEnemy(target));
            }

            PlayFeedbackTone(960f, 0.12f);
        }

        private void SimulateBusFlame()
        {
            if (!flameSkewerUnlocked) return;
            busFlameTimer += Time.deltaTime;
            if (busFlameTimer < 0.7f) return;
            busFlameTimer = 0f;

            var target = FindNearest(exteriorEnemies, exteriorBus.position, 2.6f);
            if (target == null) return;
            var direction = (target.Transform.localPosition - exteriorBus.localPosition).normalized;
            DamageEnemy(target, FlameDamage);
            var flame = CreateBlock("버스 화염 방사", exteriorRoot, exteriorBus.localPosition + direction * 1.3f, new Vector2(1.6f, 0.48f), Color.white, 4, "Vfx_Flame_Loop");
            DestroyPresentation(flame.gameObject, 0.16f);
            PlayFeedbackTone(330f, 0.12f);
        }

        private void TryFlameSkewer(List<Enemy> targets, Transform effectRoot)
        {
            if (!flameSkewerUnlocked) return;
            flameAttackTimer += Time.deltaTime;
            if (flameAttackTimer < 0.7f) return;
            flameAttackTimer = 0f;

            var source = showingInterior ? interiorPlayer.localPosition : exteriorPlayer.localPosition;
            foreach (var enemy in new List<Enemy>(targets))
            {
                if (enemy.IsDefeated || enemy.Transform == null) continue;
                if (Vector2.Distance(enemy.Transform.localPosition, source) <= 2.4f)
                {
                    DamageEnemy(enemy, FlameDamage);
                }
            }

            for (var side = -1; side <= 1; side += 2)
            {
                var flame = CreateBlock("화염 꼬치", effectRoot, source + Vector3.right * (1.2f * side), new Vector2(2.4f, 0.55f), Color.white, 4, "Vfx_Flame_Loop");
                flame.GetComponent<SpriteRenderer>().flipX = side < 0;
                DestroyPresentation(flame.gameObject, 0.18f);
            }
            PlayFeedbackTone(330f, 0.12f);
        }

        private static Vector2 ReadMovement()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            var direction = Vector2.zero;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) direction.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) direction.x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) direction.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) direction.y -= 1f;
            return direction.normalized;
        }

        private void SpawnExteriorEnemy()
        {
            var position = Random.insideUnitCircle.normalized * Random.Range(5f, 8f);
            var transform = CreateBlock("Exterior Enemy", exteriorRoot, position, new Vector2(0.60f, 0.60f), Color.white, 2, LastStopContentCatalog.Rusher.ExteriorSprite);
            exteriorEnemies.Add(new Enemy { Transform = transform, Health = LastStopContentCatalog.Rusher.MaxHealth });
        }

        private void SpawnInteriorEnemy()
        {
            var transform = CreateBlock("Interior Enemy", interiorRoot, new Vector2(-5.75f, -1.55f), new Vector2(0.60f, 0.60f), Color.white, 2, LastStopContentCatalog.Rusher.ExteriorSprite);
            interiorEnemies.Add(new Enemy { Transform = transform, Health = LastStopContentCatalog.Rusher.MaxHealth, IsInside = true });
        }

        private void DamageNearestInFacing(List<Enemy> targets, Vector3 source, float range, float damage, float knockback)
        {
            Enemy nearest = null;
            var nearestDistance = range;
            foreach (var enemy in targets)
            {
                if (!IsInInteriorAttackArc(enemy, source, range)) continue;
                var distance = Vector2.Distance(enemy.Transform.position, source);
                if (distance > nearestDistance) continue;
                nearest = enemy;
                nearestDistance = distance;
            }

            if (nearest == null) return;
            var doorwayCounter = nearest.DoorwayStaggerTimer > 0f;
            DamageEnemy(nearest, damage);
            if (nearest.Transform == null) return;

            if (doorwayCounter)
            {
                ApplyInteriorKnockback(nearest, source, 1.15f);
                nearest.DoorwayStaggerTimer = 0f;
                return;
            }

            if (knockback > 0f) ApplyInteriorKnockback(nearest, source, knockback);
        }

        private void DamageInteriorSweep()
        {
            const float range = 2f;
            const int maxTargets = 3;
            var source = interiorPlayer.position;
            var hitCount = 0;
            foreach (var enemy in new List<Enemy>(interiorEnemies))
            {
                if (hitCount >= maxTargets || !IsInInteriorAttackArc(enemy, source, range)) continue;
                var doorwayCounter = enemy.DoorwayStaggerTimer > 0f;
                DamageEnemy(enemy, 2f);
                if (enemy.Transform != null)
                {
                    ApplyInteriorKnockback(enemy, source, doorwayCounter ? 1.15f : 1f);
                    enemy.DoorwayStaggerTimer = 0f;
                }

                hitCount++;
            }

            if (hitCount > 0)
            {
                PlayFeedbackTone(1047f, 0.08f);
            }
        }

        private bool IsInInteriorAttackArc(Enemy enemy, Vector3 source, float range)
        {
            if (enemy == null || enemy.IsDefeated || enemy.Transform == null) return false;
            var delta = enemy.Transform.position - source;
            return Vector2.Distance(enemy.Transform.position, source) <= range && delta.x * interiorFacing >= -0.1f;
        }

        private static void ApplyInteriorKnockback(Enemy enemy, Vector3 source, float distance)
        {
            var direction = Mathf.Sign(enemy.Transform.position.x - source.x);
            if (Mathf.Approximately(direction, 0f)) direction = -1f;
            enemy.Transform.localPosition += Vector3.right * direction * distance;
        }

        private void DamageNearest(List<Enemy> targets, Vector3 source, float range, float damage)
        {
            var nearest = FindNearest(targets, source, range);
            if (nearest == null) return;
            var doorwayCounter = nearest.DoorwayStaggerTimer > 0f;
            DamageEnemy(nearest, damage);
            if (!doorwayCounter || nearest.Transform == null) return;

            var knockbackDirection = Mathf.Sign(nearest.Transform.localPosition.x - source.x);
            if (Mathf.Approximately(knockbackDirection, 0f)) knockbackDirection = -1f;
            nearest.Transform.localPosition += Vector3.right * knockbackDirection * 1.15f;
            nearest.DoorwayStaggerTimer = 0f;
            SpawnFeedback(interiorRoot, nearest.Transform.localPosition, new Vector2(0.7f, 0.7f), new Color(1f, 0.68f, 0.18f), 0.18f, "Vfx_Hit");
            PlayFeedbackTone(740f, 0.12f);
        }

        private static Enemy FindNearest(List<Enemy> targets, Vector3 source, float range)
        {
            Enemy nearest = null;
            var nearestDistance = range;
            foreach (var enemy in targets)
            {
                if (enemy.IsDefeated || enemy.Transform == null) continue;
                var distance = Vector2.Distance(enemy.Transform.position, source);
                if (distance > nearestDistance) continue;
                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void DamageEnemy(Enemy enemy, float damage)
        {
            if (enemy == null || enemy.IsDefeated || enemy.Transform == null) return;
            enemy.Health -= damage;
            if (enemy.IsInside)
                SpawnInteriorHitFeedback(enemy, enemy.Health > 0f);
            else
                SpawnFeedback(enemy.Transform.parent, enemy.Transform.localPosition, new Vector2(0.52f, 0.52f), Color.white, 0.16f, "Vfx_Hit");
            PlayFeedbackTone(enemy.Health > 0f ? 540f : 920f, enemy.Health > 0f ? 0.045f : 0.14f);
            if (enemy.Health > 0f)
            {
                var motion = enemy.Transform.GetComponent<LastStopSpriteMotion>();
                if (motion != null)
                {
                    if (enemy.IsInside)
                    {
                        var offset = enemy.Transform.position.x - interiorPlayer.position.x;
                        motion.TriggerInteriorHit(Mathf.Approximately(offset, 0f) ? interiorFacing : Mathf.Sign(offset));
                    }
                    else
                    {
                        motion.TriggerHit();
                    }
                }
                return;
            }
            enemy.IsDefeated = true;
            if (enemy.IsInside) interiorEnemies.Remove(enemy);
            else exteriorEnemies.Remove(enemy);
            kills++;
            if (enemy.IsInside) RecordPlaytestEvent("interior_kill");
            SpawnFeedback(enemy.Transform.parent, enemy.Transform.localPosition, new Vector2(0.84f, 0.84f), Color.white, 0.28f, "Vfx_Enemy_Death");
            GainExperience(2f);
            Destroy(enemy.Transform.gameObject);
        }

        private void SpawnInteriorHitFeedback(Enemy enemy, bool survives)
        {
            var visual = enemy.Transform.Find("Interior Enemy Visual");
            var renderer = (visual != null ? visual : enemy.Transform).GetComponent<SpriteRenderer>();
            if (renderer == null || !artSprites.ContainsKey("Vfx_Hit")) return;

            var side = interiorPlayer.position.x < renderer.bounds.center.x ? -1f : 1f;
            var effect = CreateBlock("Interior Hit Spark", interiorRoot, Vector2.zero,
                new Vector2(0.46f, 0.46f), new Color(1f, 0.88f, 0.66f), 6, "Vfx_Hit");
            effect.gameObject.AddComponent<LastStopUnscaledLifetime>().ConfigureHitSpark(0.10f, renderer, side, survives);
        }

        private bool IsActiveExteriorEnemy(Enemy enemy)
        {
            return enemy != null && !enemy.IsDefeated && !enemy.IsInside && enemy.Transform != null && exteriorEnemies.Contains(enemy);
        }

        private void SpawnFeedback(Transform parent, Vector3 position, Vector2 size, Color color, float lifetime, string spriteName = null)
        {
            var effect = CreateBlock("Combat Feedback", parent, position, size, color, 6, spriteName);
            DestroyPresentation(effect.gameObject, lifetime);
        }

        private static void DestroyPresentation(GameObject target, float lifetime)
        {
            target.AddComponent<LastStopUnscaledLifetime>().Configure(lifetime);
        }

        private void PlayFeedbackTone(float frequency, float duration)
        {
            if (feedbackAudio == null) return;
            var key = Mathf.RoundToInt(frequency) * 1000 + Mathf.RoundToInt(duration * 1000f);
            var now = Time.unscaledTime;
            if (feedbackLastPlayed.TryGetValue(key, out var lastPlayed) && now - lastPlayed < 0.05f) return;
            feedbackLastPlayed[key] = now;
            if (!feedbackClips.TryGetValue(key, out var clip))
            {
                const int sampleRate = 22050;
                var samples = Mathf.CeilToInt(sampleRate * duration);
                var data = new float[samples];
                var phase = 0f;
                for (var sample = 0; sample < samples; sample++)
                {
                    var progress = sample / (float)samples;
                    phase += 2f * Mathf.PI * frequency * (1.08f - 0.12f * progress) / sampleRate;
                    var attack = Mathf.Min(1f, progress * 30f);
                    var decay = (1f - progress) * (1f - progress);
                    data[sample] = (Mathf.Sin(phase) + 0.20f * Mathf.Sin(phase * 2f)) * attack * decay * 0.42f;
                }

                clip = AudioClip.Create("Chip Feedback " + key, samples, 1, sampleRate, false);
                clip.SetData(data, 0);
                feedbackClips.Add(key, clip);
            }

            var volumeScale = duration <= 0.08f ? 0.55f : 0.85f;
            feedbackAudio.PlayOneShot(clip, volumeScale);
        }

        private void OnDestroy()
        {
            foreach (var clip in feedbackClips.Values)
            {
                if (clip != null) Destroy(clip);
            }
        }

        private void GainExperience(float amount)
        {
            if (gameOver) return;
            runState.Experience += amount;
            TryOfferLevelUp();
        }

        private bool TryOfferLevelUp()
        {
            if (gameOver || levelUpOpen || runState.Experience < runState.ExperienceToNextLevel) return false;
            runState.Experience -= runState.ExperienceToNextLevel;
            runState.Level++;
            runState.ExperienceToNextLevel = Mathf.Ceil(runState.ExperienceToNextLevel * 1.35f);
            ShowLevelUp();
            return true;
        }

        private void ShowLevelUp()
        {
            levelUpOpen = true;
            RecordPlaytestEvent("level_up_offer");
            Time.timeScale = 0f;
            if (hintToastPanel != null) hintToastPanel.gameObject.SetActive(false);
            SetHudVisible(false);
            levelUpDim.transform.SetAsLastSibling();
            levelUpPanel.transform.SetAsLastSibling();
            statusPanel.gameObject.SetActive(true);
            statusPanel.transform.SetAsLastSibling();
            levelUpText.text = "버스 레벨 업 · 강화 하나 선택";
            levelUpChoiceLabels[0].text = bubbleLauncherUnlocked && !exteriorPiercingUnlocked ? "관통 탄환" : "콩알 연발포";
            levelUpChoiceDetails[0].text = bubbleLauncherUnlocked && !exteriorPiercingUnlocked ? "느려진 적 명중 시\n팝콘 분열탄" : "외부 자동 공격력 +1";
            levelUpChoiceLabels[1].text = flameSkewerUnlocked ? $"꼬치 화염 강화 {flameSkewerLevel + 1}" : bubbleLauncherUnlocked ? "꼬치 화염기" : "말랑 비눗방울";
            levelUpChoiceDetails[1].text = flameSkewerUnlocked ? "화염 피해 +1" : bubbleLauncherUnlocked ? "근거리 다수 적 정리" : "주변 적 감속";
            levelUpChoiceLabels[2].text = "차체 보강";
            levelUpChoiceDetails[2].text = "공용 생존력 최대치 +25";
            levelUpDim.gameObject.SetActive(true);
            levelUpPanel.gameObject.SetActive(true);
            RefreshHud();
        }

        private void HandleLevelUpInput()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                RecordPlaytestEvent("level_input", "choice=1");
                ChooseLevelUp(1);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                RecordPlaytestEvent("level_input", "choice=2");
                ChooseLevelUp(2);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                RecordPlaytestEvent("level_input", "choice=3");
                ChooseLevelUp(3);
            }
        }

        private void ChooseLevelUp(int choice)
        {
            if (!levelUpOpen || gameOver) return;
            string rewardMessage;
            switch (choice)
            {
                case 1:
                    if (bubbleLauncherUnlocked && !exteriorPiercingUnlocked)
                    {
                        exteriorPiercingUnlocked = true;
                        rewardMessage = "획득: 관통 탄환 — 느려진 적에게 팝콘 분열탄";
                    }
                    else
                    {
                        exteriorDamage += 1f;
                        rewardMessage = "강화: 콩알 연발포 — 외부 자동 공격력 +1";
                    }
                    break;
                case 2:
                    if (flameSkewerUnlocked)
                    {
                        flameSkewerLevel++;
                        rewardMessage = $"강화: 꼬치 화염 +{flameSkewerLevel} — 화염 피해 상승";
                    }
                    else if (bubbleLauncherUnlocked)
                    {
                        flameSkewerUnlocked = true;
                        rewardMessage = "획득: 꼬치 화염기 — 근거리 다수 적 정리";
                    }
                    else
                    {
                        bubbleLauncherUnlocked = true;
                        rewardMessage = "획득: 말랑 비눗방울 — 주변 적 감속";
                    }
                    break;
                case 3:
                    runState.SharedMaxHealth += 25f;
                    runState.SharedHealth += 25f;
                    rewardMessage = "강화: 차체 보강 — 공용 생존력 최대치 +25";
                    break;
                default:
                    return;
            }

            RecordPlaytestEvent("level_up_choice", "choice=" + choice);
            levelUpOpen = false;
            if (TryOfferLevelUp()) return;
            levelUpPanel.gameObject.SetActive(false);
            levelUpDim.gameObject.SetActive(false);
            Time.timeScale = 1f;
            SetHudVisible(true);
            ShowHint(rewardMessage, 1.8f);
        }

        private void ShowHint(string message, float duration)
        {
            if (hintToastPanel == null || gameOver) return;
            if (levelUpOpen)
            {
                pendingHintMessage = message;
                pendingHintDuration = duration;
                return;
            }
            hintToastLabel.text = message;
            hintToastPanel.gameObject.SetActive(true);
            hintToastUntil = Time.unscaledTime + duration;
        }

        private void DamageSharedHealth(Enemy enemy, bool playerHit)
        {
            if (gameOver) return;
            enemy.DamageTimer += Time.deltaTime;
            if (enemy.DamageTimer < 0.8f) return;
            enemy.DamageTimer = 0f;
            var damageTaken = Mathf.Min(7f, runState.SharedHealth);
            runState.SharedHealth = Mathf.Max(0f, runState.SharedHealth - 7f);
            RecordPlaytestEvent("shared_hit", playerHit ? "target=player" : "target=core");
            coreFlashTimer = 0.18f;
            if (Time.unscaledTime >= hitFeedbackWindowUntil)
            {
                hitFeedbackWindowUntil = Time.unscaledTime + 0.35f;
                hitFeedbackDamage = 0f;
                hitFeedbackCount = 0;
                hitFeedbackPlayerHit = false;
                hitFeedbackCoreHit = false;
            }
            hitFeedbackDamage += damageTaken;
            hitFeedbackCount++;
            if (playerHit) hitFeedbackPlayerHit = true;
            else hitFeedbackCoreHit = true;
            var hitTarget = hitFeedbackPlayerHit && hitFeedbackCoreHit ? "플레이어·코어 피격"
                : hitFeedbackPlayerHit ? "플레이어 피격" : "코어 피격";
            var hitCount = hitFeedbackCount > 1 ? $" ×{hitFeedbackCount}" : string.Empty;
            hitSourceMessage = $"{hitTarget}{hitCount} · 생존력 -{hitFeedbackDamage:0}";
            hitSourceLabelUntil = Time.unscaledTime + 1.1f;
            healthPulseUntil = Time.unscaledTime + 0.22f;
            combatDuckUntil = Time.unscaledTime + CombatDuckSeconds;
            if (feedbackAudio != null) feedbackAudio.volume = FeedbackVolume * CombatDuckVolume;
            var hitPosition = playerHit ? interiorPlayer.localPosition : interiorCore.localPosition;
            SpawnFeedback(interiorRoot, hitPosition, new Vector2(0.70f, 0.70f), Color.white, 0.15f, "Vfx_Hit");
            LastStopAudio.TryPlay(playerHit ? LastStopAudioEvent.PlayerDamaged : LastStopAudioEvent.CoreDamaged);
            if (!showingInterior && !interiorAttackHintShown)
            {
                interiorAttackHintShown = true;
                ShowHint("버스 내부가 공격받고 있어요!\nTab으로 내부를 확인하세요.", 2.5f);
            }
            if (runState.IsGameOver)
            {
                EndGameOver((playerHit ? "플레이어 피격" : "코어 피격") + " · 생존력 0");
            }
        }

        private void EndGameOver(string reason)
        {
            if (gameOver) return;
            gameOver = true;
            RecordPlaytestEvent("game_over", "reason=" + reason);
            levelUpOpen = false;
            levelUpPanel.gameObject.SetActive(false);
            levelUpDim.gameObject.SetActive(false);
            Time.timeScale = 0f;
            LastStopAudio.TryPlay(LastStopAudioEvent.GameOver);
            ShowResult("이번 정류장은 여기까지…\n" + reason + "\n[R] 다시 출발", new Color(1f, 0.34f, 0.25f));
        }

        private void EndArrival()
        {
            if (gameOver) return;
            gameOver = true;
            RecordPlaytestEvent("demo_arrival");
            levelUpOpen = false;
            levelUpPanel.gameObject.SetActive(false);
            levelUpDim.gameObject.SetActive(false);
            Time.timeScale = 0f;
            ShowResult($"다음 정류장 도착!\n버스를 지켜냈어요\n생존 시간 08:00 · 처치 {kills} · 최고 차내 {peakBreaches}\n레벨 {runState.Level}\n[R] 다시 출발", new Color(0.43f, 0.95f, 0.78f));
        }

        private void ShowResult(string message, Color textColor)
        {
            if (hintToastPanel != null) hintToastPanel.gameObject.SetActive(false);
            SetHudVisible(false);
            resultDim.gameObject.SetActive(true);
            resultPanel.gameObject.SetActive(true);
            gameOverLabel.color = textColor;
            gameOverLabel.text = message;
        }

        private void SetHudVisible(bool visible)
        {
            statusPanel.gameObject.SetActive(visible);
            loadoutPanel.gameObject.SetActive(visible);
            actionPanel.gameObject.SetActive(visible);
        }

        private void RefreshHud()
        {
            if (healthLabel == null) return;
            if (hintToastPanel != null && hintToastPanel.gameObject.activeSelf && Time.unscaledTime >= hintToastUntil)
            {
                hintToastPanel.gameObject.SetActive(false);
            }
            if (!levelUpOpen && !gameOver && pendingHintMessage != null && !hintToastPanel.gameObject.activeSelf)
            {
                var message = pendingHintMessage;
                var duration = pendingHintDuration;
                pendingHintMessage = null;
                ShowHint(message, duration);
            }
            healthLabel.text = $"생존력  {runState.SharedHealth:0} / {runState.SharedMaxHealth:0}";
            healthLabel.color = runState.SharedHealth <= runState.SharedMaxHealth * 0.3f
                ? new Color(1f, 0.38f, 0.28f)
                : runState.SharedHealth <= runState.SharedMaxHealth * 0.6f
                    ? new Color(1f, 0.76f, 0.28f)
                    : Color.white;
            sharedHealthFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(runState.SharedHealth / Mathf.Max(1f, runState.SharedMaxHealth)), 1f);
            var healthFillColor = runState.SharedHealth <= runState.SharedMaxHealth * 0.3f
                ? new Color(1f, 0.38f, 0.28f)
                : runState.SharedHealth <= runState.SharedMaxHealth * 0.6f
                    ? new Color(1f, 0.76f, 0.28f)
                    : new Color(0.25f, 0.92f, 0.79f);
            var hitFlash = Mathf.Clamp01((healthPulseUntil - Time.unscaledTime) / 0.22f);
            sharedHealthFill.color = Color.Lerp(healthFillColor, Color.white, hitFlash * 0.7f);
            statusPanel.color = Color.Lerp(new Color(0.015f, 0.02f, 0.035f, 0.88f),
                new Color(0.24f, 0.045f, 0.045f, 0.94f), levelUpOpen ? 0f : hitFlash);
            hitSourceLabel.text = !levelUpOpen && Time.unscaledTime < hitSourceLabelUntil ? hitSourceMessage : string.Empty;
            hitSourceLabel.color = new Color(1f, 0.72f, 0.45f);
            var armorLevel = Mathf.Max(0, Mathf.RoundToInt((runState.SharedMaxHealth - LastStopContentCatalog.YellowBus.MaxHealth) / 25f));
            var loadoutText = "장비\n" + $"콩알포 Lv {Mathf.RoundToInt(exteriorDamage)}";
            var loadoutRows = 2;
            if (bubbleLauncherUnlocked) { loadoutText += "\n말랑 비눗방울"; loadoutRows++; }
            if (flameSkewerUnlocked) { loadoutText += $"\n화염 꼬치 Lv {flameSkewerLevel + 1}"; loadoutRows++; }
            if (exteriorPiercingUnlocked) { loadoutText += "\n관통 탄환"; loadoutRows++; }
            if (armorLevel > 0) { loadoutText += $"\n차체 보강 x{armorLevel}"; loadoutRows++; }
            if (loadoutLabel.text != loadoutText)
            {
                loadoutLabel.text = loadoutText;
                SetAnchors(loadoutPanel.rectTransform, new Vector2(0.015f, 0.76f - loadoutRows * 0.07f), new Vector2(0.22f, 0.76f), Vector2.zero, Vector2.zero);
            }
            modeLabel.text = showingInterior
                ? "버스 내부 · 밖도 진행"
                : "외부 자동 전투";
            experienceLabel.text = $"Lv {runState.Level}  ·  XP {runState.Experience:0}/{runState.ExperienceToNextLevel:0}";
            var remainingSeconds = Mathf.CeilToInt(Mathf.Max(0f, DemoDurationSeconds - runElapsedSeconds));
            var timer = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
            runLabel.text = remainingSeconds <= 10 ? $"도착 임박 · {timer}" : $"정류장 {timer}";
            runLabel.color = remainingSeconds <= 10 ? new Color(1f, 0.56f, 0.22f) : remainingSeconds <= 60 ? new Color(1f, 0.86f, 0.32f) : Color.white;
            var dangerText = breaches >= BreachWarningThreshold
                ? "50 경고 · 내부 공격 위험"
                : breaches >= 40
                    ? "위험 · 코어 접근 다수"
                    : breaches >= 16 ? "주의 · 내부 적 증가" : breaches > 0 ? "침입 중 · 내부 방어" : "안전";
            breachLabel.text = $"차내 적 {breaches} · 경고 {BreachWarningThreshold}\n" + dangerText;
            var breachColor = breaches >= BreachWarningThreshold
                ? new Color(1f, 0.34f, 0.29f)
                : breaches >= 40
                ? new Color(1f, 0.58f, 0.28f)
                : breaches > 0 ? new Color(1f, 0.84f, 0.35f) : Color.white;
            var breachPulse = Mathf.Clamp01((breachPulseUntil - Time.unscaledTime) / 0.65f);
            breachLabel.color = Color.Lerp(breachColor, new Color(1f, 0.96f, 0.78f), breachPulse * 0.5f);
            var recentInteriorHit = Time.unscaledTime < hitSourceLabelUntil;
            actionPanel.color = !showingInterior && recentInteriorHit
                ? new Color(0.36f, 0.07f, 0.06f, 0.94f)
                : new Color(0.015f, 0.02f, 0.035f, 0.88f);
            objectiveLabel.text = showingInterior
                ? "코어 방어 · Space: 공격 · Tab: 외부"
                : recentInteriorHit
                    ? hitSourceMessage + " · Tab: 내부"
                    : "자동 전투 중   Tab: 버스 내부";
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void RecordPlaytestEvent(string eventName, string details = "")
        {
            Debug.Log($"LAST_STOP_QA event={eventName} t={runElapsedSeconds:0.00} hp={runState.SharedHealth:0} outside={exteriorEnemies.Count} inside={interiorEnemies.Count} peak={Mathf.Max(peakBreaches, interiorEnemies.Count)} kills={kills} level={runState.Level} xp={runState.Experience:0.#} {details}");
        }

        private float FlameDamage => 2f + flameSkewerLevel;
    }
}
