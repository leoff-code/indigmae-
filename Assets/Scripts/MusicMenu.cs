using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrystalSprint
{
    [DefaultExecutionOrder(-1100)]
    public sealed class MusicMenu : MonoBehaviour
    {
        public const string VolumePreference = "CrystalSprint.MusicVolume";
        public static MusicMenu Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.open;
        private int closedFrame=-1;
        public static bool JustClosed => Instance!=null && Instance.closedFrame==Time.frameCount;
        [SerializeField] private AudioSource music;
        [SerializeField] private GameObject panel;
        [SerializeField] private Slider slider;
        [SerializeField] private Text percentage;
        private CursorLockController cursor;
        private bool open;
        private float resumeScale = 1f, fade;
        public float Volume { get; private set; }
        public AudioSource Source => music;
        public Slider VolumeSlider => slider;
        public void Configure(AudioSource source, GameObject ui, Slider control, Text label)
        { music = source; panel = ui; slider = control; percentage = label; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            cursor = FindAnyObjectByType<CursorLockController>();
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePreference, .35f));
            panel.SetActive(false); slider.SetValueWithoutNotify(Volume);
            slider.onValueChanged.AddListener(SetVolume);
            music.loop = true; music.spatialBlend = 0; music.playOnAwake = false; music.volume = 0;
            if (music.clip != null) music.Play();
            RefreshLabel();
        }
        private void Update()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.mKey.wasPressedThisFrame) SetOpen(!open);
                else if (open && Keyboard.current.escapeKey.wasPressedThisFrame) SetOpen(false, false);
            }
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 1.8f);
            music.volume = Volume * fade;
        }
        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value); music.volume = Volume * fade;
            slider.SetValueWithoutNotify(Volume); RefreshLabel();
            PlayerPrefs.SetFloat(VolumePreference, Volume);
        }
        private void RefreshLabel() => percentage.text = $"Musik  {Mathf.RoundToInt(Volume * 100)} %";
        public void SetOpen(bool value, bool recapture = true)
        {
            if (open == value) return;
            open = value; panel.SetActive(value);
            if (value) { resumeScale = Time.timeScale; Time.timeScale = 0; cursor?.ReleaseCursor(); }
            else
            {
                Time.timeScale = resumeScale; closedFrame=Time.frameCount;PlayerPrefs.Save();
                if (recapture) cursor?.LockCursor(); else cursor?.ReleaseCursor();
            }
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            if (open) Time.timeScale = resumeScale;
            slider?.onValueChanged.RemoveListener(SetVolume);
            Instance = null;
        }
    }
}
