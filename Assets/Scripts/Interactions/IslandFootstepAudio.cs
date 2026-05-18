using UnityEngine;

namespace PrivateIsland
{
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(IslandCharacterController))]
    public sealed class IslandFootstepAudio : MonoBehaviour
    {
        private enum FootstepSurface
        {
            Grass,
            Snow,
            Wet
        }

        [Header("Timing")]
        [SerializeField] private float minimumStepSpeed = 1.15f;
        [SerializeField] private float walkStepInterval = 0.42f;
        [SerializeField] private float sprintStepInterval = 0.29f;
        [SerializeField] private float fullStrideSpeed = 10f;

        [Header("Mix")]
        [SerializeField] private float baseVolume = 0.18f;
        [SerializeField] private Vector2 volumeVariation = new Vector2(0.92f, 1.08f);
        [SerializeField] private Vector2 pitchVariation = new Vector2(0.96f, 1.05f);
        [SerializeField] private int clipVariantsPerSurface = 4;

        private IslandCharacterController playerController;
        private WorldEnvironmentManager worldEnvironmentManager;
        private AudioSource audioSource;
        private AudioClip[] grassStepClips;
        private AudioClip[] snowStepClips;
        private AudioClip[] wetStepClips;
        private float nextStepTime;
        private int grassStepIndex = -1;
        private int snowStepIndex = -1;
        private int wetStepIndex = -1;

        private void Awake()
        {
            playerController = GetComponent<IslandCharacterController>();
            audioSource = GetComponent<AudioSource>();
            ConfigureAudioSource();
            CreateRuntimeClips();
        }

        private void OnEnable()
        {
            ResolveEnvironmentManager();
            SubscribeToWorldState();
        }

        private void Start()
        {
            ResolveEnvironmentManager();
            SubscribeToWorldState();
        }

        private void OnDisable()
        {
            if (worldEnvironmentManager != null)
            {
                worldEnvironmentManager.WorldStateChanged -= HandleWorldStateChanged;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || playerController == null)
            {
                return;
            }

            if (!playerController.IsInputEnabled || !playerController.IsGrounded)
            {
                nextStepTime = Mathf.Max(nextStepTime, Time.time + 0.04f);
                return;
            }

            Vector3 planarVelocity = playerController.CurrentVelocity;
            planarVelocity.y = 0f;

            float speed = planarVelocity.magnitude;
            if (speed < minimumStepSpeed || Time.time < nextStepTime)
            {
                return;
            }

            PlayStep(ResolveSurface(), speed);

            float strideBlend = Mathf.InverseLerp(minimumStepSpeed, fullStrideSpeed, speed);
            nextStepTime = Time.time + Mathf.Lerp(walkStepInterval, sprintStepInterval, strideBlend);
        }

        private void HandleWorldStateChanged()
        {
            nextStepTime = Mathf.Max(nextStepTime, Time.time + 0.02f);
        }

        private void ResolveEnvironmentManager()
        {
            if (worldEnvironmentManager == null)
            {
                worldEnvironmentManager = FindAnyObjectByType<WorldEnvironmentManager>(FindObjectsInactive.Exclude);
            }
        }

        private void SubscribeToWorldState()
        {
            if (worldEnvironmentManager == null)
            {
                return;
            }

            worldEnvironmentManager.WorldStateChanged -= HandleWorldStateChanged;
            worldEnvironmentManager.WorldStateChanged += HandleWorldStateChanged;
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 12f;
        }

        private void CreateRuntimeClips()
        {
            int variantCount = Mathf.Max(2, clipVariantsPerSurface);
            grassStepClips = CreateSurfaceClips(FootstepSurface.Grass, variantCount);
            snowStepClips = CreateSurfaceClips(FootstepSurface.Snow, variantCount);
            wetStepClips = CreateSurfaceClips(FootstepSurface.Wet, variantCount);
        }

        private static AudioClip[] CreateSurfaceClips(FootstepSurface surface, int variantCount)
        {
            AudioClip[] clips = new AudioClip[variantCount];
            for (int i = 0; i < variantCount; i++)
            {
                clips[i] = CreateFootstepClip(surface, i);
            }

            return clips;
        }

        private static AudioClip CreateFootstepClip(FootstepSurface surface, int variantIndex)
        {
            int sampleRate = 22050;
            float duration = surface switch
            {
                FootstepSurface.Grass => 0.13f,
                FootstepSurface.Snow => 0.17f,
                FootstepSurface.Wet => 0.19f,
                _ => 0.15f
            };

            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
            float[] data = new float[sampleCount];
            int seed = (variantIndex + 1) * 173;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float progress = sampleCount <= 1 ? 1f : i / (float)(sampleCount - 1);

                float primaryNoise = SignedNoise(i + seed * 3, seed);
                float secondaryNoise = SignedNoise((i * 3) + 11, seed * 7);
                float crispNoise = primaryNoise - secondaryNoise;

                float sample = surface switch
                {
                    FootstepSurface.Grass => SynthesizeGrassStep(t, progress, variantIndex, primaryNoise, crispNoise),
                    FootstepSurface.Snow => SynthesizeSnowStep(t, progress, variantIndex, primaryNoise, crispNoise),
                    FootstepSurface.Wet => SynthesizeWetStep(t, progress, variantIndex, primaryNoise, crispNoise),
                    _ => 0f
                };

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create($"Footstep_{surface}_{variantIndex}", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float SynthesizeGrassStep(float time, float progress, int variantIndex, float primaryNoise, float crispNoise)
        {
            float bodyEnvelope = Envelope(progress, 0.01f, 2.9f);
            float crunchEnvelope = Envelope(progress, 0.02f, 4f);
            float thumpEnvelope = Envelope(progress, 0.005f, 8f);
            float lowTone = Mathf.Sin((68f + (variantIndex * 6f)) * Mathf.PI * 2f * time) * thumpEnvelope * 0.08f;
            float rustle = primaryNoise * bodyEnvelope * 0.18f;
            float crisp = crispNoise * crunchEnvelope * 0.07f;
            return rustle + crisp + lowTone;
        }

        private static float SynthesizeSnowStep(float time, float progress, int variantIndex, float primaryNoise, float crispNoise)
        {
            float bodyEnvelope = Envelope(progress, 0.012f, 2.3f);
            float crunchEnvelope = Envelope(progress, 0.03f, 3.3f);
            float squeakEnvelope = Envelope(progress, 0.018f, 5.4f);
            float crunch = primaryNoise * bodyEnvelope * 0.14f;
            float crisp = crispNoise * crunchEnvelope * 0.12f;
            float squeak = Mathf.Sin((980f + (variantIndex * 45f)) * Mathf.PI * 2f * time) * squeakEnvelope * 0.028f;
            return crunch + crisp + squeak;
        }

        private static float SynthesizeWetStep(float time, float progress, int variantIndex, float primaryNoise, float crispNoise)
        {
            float squishEnvelope = Envelope(progress, 0.012f, 2f);
            float splashEnvelope = Envelope(progress, 0.035f, 3.1f);
            float tailEnvelope = Envelope(progress, 0.04f, 6f);
            float lowSquish = Mathf.Sin((78f + (variantIndex * 5f)) * Mathf.PI * 2f * time) * squishEnvelope * 0.1f;
            float squelch = primaryNoise * squishEnvelope * 0.15f;
            float splash = crispNoise * splashEnvelope * 0.09f;
            float drip = Mathf.Sin((1250f + (variantIndex * 70f)) * Mathf.PI * 2f * time) * tailEnvelope * 0.018f;
            return lowSquish + squelch + splash + drip;
        }

        private static float Envelope(float progress, float attackPortion, float decayPower)
        {
            progress = Mathf.Clamp01(progress);
            float safeAttack = Mathf.Clamp(attackPortion, 0.001f, 0.999f);
            float attack = Mathf.Clamp01(progress / safeAttack);
            float decay = Mathf.Pow(1f - progress, Mathf.Max(0.01f, decayPower));
            return attack * decay;
        }

        private static float SignedNoise(int sampleIndex, int seed)
        {
            float value = Mathf.Sin((sampleIndex * 12.9898f) + (seed * 78.233f)) * 43758.5453f;
            return (value - Mathf.Floor(value) - 0.5f) * 2f;
        }

        private FootstepSurface ResolveSurface()
        {
            ResolveEnvironmentManager();
            if (worldEnvironmentManager == null)
            {
                return FootstepSurface.Grass;
            }

            if (worldEnvironmentManager.CurrentSeason == Season.Winter)
            {
                return FootstepSurface.Snow;
            }

            if (worldEnvironmentManager.CurrentWeather == WeatherType.Rain ||
                worldEnvironmentManager.CurrentWeather == WeatherType.Thunderstorm)
            {
                return FootstepSurface.Wet;
            }

            return FootstepSurface.Grass;
        }

        private void PlayStep(FootstepSurface surface, float speed)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = GetNextClip(surface);
            if (clip == null)
            {
                return;
            }

            float speedBlend = Mathf.InverseLerp(minimumStepSpeed, fullStrideSpeed, speed);
            audioSource.pitch = Random.Range(pitchVariation.x, pitchVariation.y);
            audioSource.volume = baseVolume * Random.Range(volumeVariation.x, volumeVariation.y) * Mathf.Lerp(0.82f, 1.14f, speedBlend);
            audioSource.PlayOneShot(clip);
        }

        private AudioClip GetNextClip(FootstepSurface surface)
        {
            AudioClip[] clips = GetClipSet(surface);
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            ref int lastIndex = ref GetLastIndex(surface);
            int nextIndex = Random.Range(0, clips.Length);
            if (clips.Length > 1 && nextIndex == lastIndex)
            {
                nextIndex = (nextIndex + 1) % clips.Length;
            }

            lastIndex = nextIndex;
            return clips[nextIndex];
        }

        private AudioClip[] GetClipSet(FootstepSurface surface)
        {
            return surface switch
            {
                FootstepSurface.Grass => grassStepClips,
                FootstepSurface.Snow => snowStepClips,
                FootstepSurface.Wet => wetStepClips,
                _ => grassStepClips
            };
        }

        private ref int GetLastIndex(FootstepSurface surface)
        {
            switch (surface)
            {
                case FootstepSurface.Snow:
                    return ref snowStepIndex;
                case FootstepSurface.Wet:
                    return ref wetStepIndex;
                default:
                    return ref grassStepIndex;
            }
        }
    }
}
