using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidBreaker.Core;

namespace VoidBreaker.Audio
{
    /// <summary>
    /// Central audio manager - handles all game audio including music, SFX, and ambient sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private int sfxPoolSize = 10;
        [SerializeField] private List<AudioSource> sfxPool = new List<AudioSource>();

        [Header("Music Tracks")]
        [SerializeField] private AudioClip mainMenuMusic;
        [SerializeField] private AudioClip explorationMusic;
        [SerializeField] private AudioClip combatMusic;
        [SerializeField] private AudioClip combatIntenseMusic;
        [SerializeField] private AudioClip bossMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip defeatMusic;
        [SerializeField] private AudioClip shopMusic;

        [Header("Ambience")]
        [SerializeField] private AudioClip shipAmbience;
        [SerializeField] private AudioClip nebulaAmbience;
        [SerializeField] private AudioClip combatAmbience;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip buttonHover;
        [SerializeField] private AudioClip panelOpen;
        [SerializeField] private AudioClip panelClose;
        [SerializeField] private AudioClip notification;
        [SerializeField] private AudioClip error;

        [Header("Gameplay Sounds")]
        [SerializeField] private AudioClip weaponFire;
        [SerializeField] private AudioClip laserHit;
        [SerializeField] private AudioClip missileHit;
        [SerializeField] private AudioClip shieldHit;
        [SerializeField] private AudioClip shieldDown;
        [SerializeField] private AudioClip shieldRecharge;
        [SerializeField] private AudioClip hullDamage;
        [SerializeField] private AudioClip systemDamage;
        [SerializeField] private AudioClip crewDeath;
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip doorClose;
        [SerializeField] private AudioClip fireLoop;
        [SerializeField] private AudioClip breachLoop;
        [SerializeField] private AudioClip lowOxygenWarning;
        [SerializeField] private AudioClip ftlCharge;
        [SerializeField] private AudioClip ftlJump;
        [SerializeField] private AudioClip cloakActivate;
        [SerializeField] private AudioClip teleport;

        [Header("Evolution Sounds")]
        [SerializeField] private AudioClip mutationUnlock;
        [SerializeField] private AudioClip evolutionComplete;
        [SerializeField] private AudioClip shipTransform;

        [Header("Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float ambienceVolume = 0.5f;
        [SerializeField] private float crossfadeDuration = 1.5f;

        private MusicState currentMusicState = MusicState.None;
        private Coroutine musicFadeCoroutine;
        private Dictionary<string, AudioClip> sfxLibrary = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            BuildSFXLibrary();
        }

        private void Start()
        {
            SubscribeToEvents();
            PlayMusic(MusicState.MainMenu);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ==================== INITIALIZATION ====================

        private void InitializeAudioSources()
        {
            // Create music source
            if (musicSource == null)
            {
                var musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            // Create ambience source
            if (ambienceSource == null)
            {
                var ambObj = new GameObject("AmbienceSource");
                ambObj.transform.SetParent(transform);
                ambienceSource = ambObj.AddComponent<AudioSource>();
                ambienceSource.loop = true;
                ambienceSource.playOnAwake = false;
            }

            // Create UI source
            if (uiSource == null)
            {
                var uiObj = new GameObject("UISource");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }

            // Create SFX pool
            for (int i = 0; i < sfxPoolSize; i++)
            {
                var sfxObj = new GameObject($"SFXSource_{i}");
                sfxObj.transform.SetParent(transform);
                var source = sfxObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            UpdateVolumes();
        }

        private void BuildSFXLibrary()
        {
            sfxLibrary["weapon_fire"] = weaponFire;
            sfxLibrary["laser_hit"] = laserHit;
            sfxLibrary["missile_hit"] = missileHit;
            sfxLibrary["shield_hit"] = shieldHit;
            sfxLibrary["shield_down"] = shieldDown;
            sfxLibrary["shield_recharge"] = shieldRecharge;
            sfxLibrary["hull_damage"] = hullDamage;
            sfxLibrary["system_damage"] = systemDamage;
            sfxLibrary["crew_death"] = crewDeath;
            sfxLibrary["door_open"] = doorOpen;
            sfxLibrary["door_close"] = doorClose;
            sfxLibrary["fire"] = fireLoop;
            sfxLibrary["breach"] = breachLoop;
            sfxLibrary["low_oxygen"] = lowOxygenWarning;
            sfxLibrary["ftl_charge"] = ftlCharge;
            sfxLibrary["ftl_jump"] = ftlJump;
            sfxLibrary["cloak"] = cloakActivate;
            sfxLibrary["teleport"] = teleport;
            sfxLibrary["mutation"] = mutationUnlock;
            sfxLibrary["evolution"] = evolutionComplete;
            sfxLibrary["transform"] = shipTransform;
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnCombatStarted += HandleCombatStarted;
            GameEvents.OnCombatEnded += HandleCombatEnded;
            GameEvents.OnGamePaused += HandleGamePaused;
            GameEvents.OnGameResumed += HandleGameResumed;
            GameEvents.OnShieldChanged += HandleShieldChanged;
            GameEvents.OnHullDamaged += HandleHullDamaged;
            GameEvents.OnProjectileFired += HandleProjectileFired;
            GameEvents.OnMutationUnlocked += HandleMutationUnlocked;
            GameEvents.OnBossPhaseChanged += HandleBossPhaseChanged;
            GameEvents.OnFTLJumpStarted += HandleFTLJump;
            GameEvents.OnCloakActivated += HandleCloakActivated;
        }

        private void UnsubscribeFromEvents()
        {
            GameEvents.OnCombatStarted -= HandleCombatStarted;
            GameEvents.OnCombatEnded -= HandleCombatEnded;
            GameEvents.OnGamePaused -= HandleGamePaused;
            GameEvents.OnGameResumed -= HandleGameResumed;
            GameEvents.OnShieldChanged -= HandleShieldChanged;
            GameEvents.OnHullDamaged -= HandleHullDamaged;
            GameEvents.OnProjectileFired -= HandleProjectileFired;
            GameEvents.OnMutationUnlocked -= HandleMutationUnlocked;
            GameEvents.OnBossPhaseChanged -= HandleBossPhaseChanged;
            GameEvents.OnFTLJumpStarted -= HandleFTLJump;
            GameEvents.OnCloakActivated -= HandleCloakActivated;
        }

        // ==================== MUSIC ====================

        // Public volume properties
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        /// <summary>
        /// Alias for PlayMusic for compatibility.
        /// </summary>
        public void SetMusicState(MusicState state) => PlayMusic(state);

        public void PlayMusic(MusicState state)
        {
            if (currentMusicState == state) return;
            currentMusicState = state;

            AudioClip newClip = state switch
            {
                MusicState.MainMenu => mainMenuMusic,
                MusicState.Exploration => explorationMusic,
                MusicState.Combat => combatMusic,
                MusicState.CombatIntense => combatIntenseMusic,
                MusicState.Boss => bossMusic,
                MusicState.Victory => victoryMusic,
                MusicState.Defeat => defeatMusic,
                MusicState.Shop => shopMusic,
                _ => null
            };

            if (newClip != null)
            {
                if (musicFadeCoroutine != null)
                    StopCoroutine(musicFadeCoroutine);

                musicFadeCoroutine = StartCoroutine(CrossfadeMusic(newClip));
            }
        }

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float timer = 0f;
            float startVolume = musicSource.volume;

            // Fade out
            while (timer < crossfadeDuration / 2f)
            {
                timer += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (crossfadeDuration / 2f));
                yield return null;
            }

            // Switch clip
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            timer = 0f;
            while (timer < crossfadeDuration / 2f)
            {
                timer += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, timer / (crossfadeDuration / 2f));
                yield return null;
            }

            musicSource.volume = musicVolume * masterVolume;
        }

        public void StopMusic()
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);

            musicFadeCoroutine = StartCoroutine(FadeOutMusic());
        }

        private IEnumerator FadeOutMusic()
        {
            float startVolume = musicSource.volume;
            float timer = 0f;

            while (timer < crossfadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / crossfadeDuration);
                yield return null;
            }

            musicSource.Stop();
        }

        // ==================== AMBIENCE ====================

        public void PlayAmbience(AmbienceType type)
        {
            AudioClip clip = type switch
            {
                AmbienceType.Ship => shipAmbience,
                AmbienceType.Nebula => nebulaAmbience,
                AmbienceType.Combat => combatAmbience,
                _ => null
            };

            if (clip != null)
            {
                ambienceSource.clip = clip;
                ambienceSource.volume = ambienceVolume * masterVolume;
                ambienceSource.Play();
            }
        }

        public void StopAmbience()
        {
            ambienceSource.Stop();
        }

        // ==================== SFX ====================

        public void PlaySFX(string sfxId, float volumeMultiplier = 1f)
        {
            if (sfxLibrary.TryGetValue(sfxId, out AudioClip clip))
            {
                PlaySFX(clip, volumeMultiplier);
            }
        }

        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetAvailableSFXSource();
            if (source != null)
            {
                source.clip = clip;
                source.volume = sfxVolume * masterVolume * volumeMultiplier;
                source.Play();
            }
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
        {
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume * volumeMultiplier);
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var source in sfxPool)
            {
                if (!source.isPlaying)
                    return source;
            }

            // All sources busy, return first one (will interrupt)
            return sfxPool.Count > 0 ? sfxPool[0] : null;
        }

        // ==================== UI SOUNDS ====================

        public void PlayUIClick()
        {
            if (buttonClick != null)
            {
                uiSource.PlayOneShot(buttonClick, sfxVolume * masterVolume);
            }
        }

        public void PlayUIHover()
        {
            if (buttonHover != null)
            {
                uiSource.PlayOneShot(buttonHover, sfxVolume * masterVolume * 0.5f);
            }
        }

        public void PlayPanelOpen()
        {
            if (panelOpen != null)
            {
                uiSource.PlayOneShot(panelOpen, sfxVolume * masterVolume);
            }
        }

        public void PlayPanelClose()
        {
            if (panelClose != null)
            {
                uiSource.PlayOneShot(panelClose, sfxVolume * masterVolume);
            }
        }

        public void PlayNotification()
        {
            if (notification != null)
            {
                uiSource.PlayOneShot(notification, sfxVolume * masterVolume);
            }
        }

        public void PlayError()
        {
            if (error != null)
            {
                uiSource.PlayOneShot(error, sfxVolume * masterVolume);
            }
        }

        // ==================== EVENT HANDLERS ====================

        private void HandleCombatStarted(CombatStartedArgs args)
        {
            PlayMusic(MusicState.Combat);
            PlayAmbience(AmbienceType.Combat);
        }

        private void HandleCombatEnded(CombatEndedArgs args)
        {
            if (args.playerWon)
            {
                PlayMusic(MusicState.Victory);
            }
            else
            {
                PlayMusic(MusicState.Exploration);
            }
            PlayAmbience(AmbienceType.Ship);
        }

        private void HandleGamePaused()
        {
            musicSource.volume *= 0.3f;
        }

        private void HandleGameResumed()
        {
            musicSource.volume = musicVolume * masterVolume;
        }

        private void HandleShieldChanged(ShieldChangedArgs args)
        {
            if (args.newLayers < args.previousLayers)
            {
                PlaySFX(shieldHit);

                if (args.newLayers == 0)
                {
                    PlaySFX(shieldDown);
                }
            }
            else if (args.newLayers > args.previousLayers)
            {
                PlaySFX(shieldRecharge);
            }
        }

        private void HandleHullDamaged(HullDamagedArgs args)
        {
            PlaySFX(hullDamage);
        }

        private void HandleProjectileFired(ProjectileFiredArgs args)
        {
            PlaySFX(weaponFire);
        }

        private void HandleMutationUnlocked(MutationUnlockedArgs args)
        {
            PlaySFX(mutationUnlock);
        }

        private void HandleBossPhaseChanged(BossPhaseChangedArgs args)
        {
            PlayMusic(MusicState.Boss);
        }

        private void HandleFTLJump(FTLJumpStartedArgs args)
        {
            PlaySFX(ftlJump);
        }

        private void HandleCloakActivated(CloakActivatedArgs args)
        {
            PlaySFX(cloakActivate);
        }

        // ==================== VOLUME CONTROL ====================

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetAmbienceVolume(float volume)
        {
            ambienceVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (musicSource) musicSource.volume = musicVolume * masterVolume;
            if (ambienceSource) ambienceSource.volume = ambienceVolume * masterVolume;
        }
    }

    public enum MusicState
    {
        None,
        MainMenu,
        Exploration,
        Combat,
        CombatIntense,
        Boss,
        Victory,
        Defeat,
        Shop
    }

    public enum AmbienceType
    {
        None,
        Ship,
        Nebula,
        Combat
    }
}
