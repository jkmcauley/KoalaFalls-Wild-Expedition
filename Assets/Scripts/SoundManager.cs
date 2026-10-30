using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<SoundManager>();
            return _instance;
        }
    }

    [SerializeField] private AudioClip _snakeClip;
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] private AudioClip _killMachineClip;
    [SerializeField] private AudioClip _bearClip;
    [SerializeField] private AudioClip _cougarClip;
    [SerializeField] private AudioClip _bearTrapClip;
    [SerializeField] private AudioClip _gatorClip;
    [SerializeField] private AudioClip _coinClip;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _shootClip;
    [SerializeField] private AudioClip _skunkClip;
    [SerializeField] private AudioClip _spiderClip;
    [SerializeField] private AudioSource _backgroundSound;
    [SerializeField] private AudioClip _boulderClip;
    [SerializeField] private AudioClip _musicClip;
    [SerializeField] [Range(0f, 1f)] private float _musicVolume = 0.45f;

    AudioSource _sfxSource;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            _instance.Adopt(this);
            Destroy(this);
            return;
        }

        _instance = this;
        if (_backgroundSound == null)
            _backgroundSound = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        _sfxSource = _backgroundSound;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayMusic();
    }

    void Start()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        PlayMusic();
    }

    void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => PlayMusic();

    public void Adopt(SoundManager other)
    {
        if (other == null)
            return;
        if (_snakeClip == null) _snakeClip = other._snakeClip;
        if (_bearClip == null) _bearClip = other._bearClip;
        if (_cougarClip == null) _cougarClip = other._cougarClip;
        if (_bearTrapClip == null) _bearTrapClip = other._bearTrapClip;
        if (_gatorClip == null) _gatorClip = other._gatorClip;
        if (_coinClip == null) _coinClip = other._coinClip;
        if (_winClip == null) _winClip = other._winClip;
        if (_shootClip == null) _shootClip = other._shootClip;
        if (_explosionClip == null) _explosionClip = other._explosionClip;
        if (_killMachineClip == null) _killMachineClip = other._killMachineClip;
        if (_skunkClip == null) _skunkClip = other._skunkClip;
        if (_spiderClip == null) _spiderClip = other._spiderClip;
        if (_boulderClip == null) _boulderClip = other._boulderClip;
        if (_musicClip == null) _musicClip = other._musicClip;
        PlayMusic();
    }

    public void PlayMusic()
    {
        if (_backgroundSound == null)
            return;
        if (_musicClip == null && _backgroundSound.clip != null)
            _musicClip = _backgroundSound.clip;
        if (_musicClip == null)
            return;

        _backgroundSound.spatialBlend = 0f;
        _backgroundSound.loop = true;
        _backgroundSound.mute = false;
        _backgroundSound.playOnAwake = false;
        _backgroundSound.volume = _musicVolume;
        if (_backgroundSound.clip != _musicClip)
            _backgroundSound.clip = _musicClip;
        if (!_backgroundSound.isPlaying)
            _backgroundSound.Play();
    }

    void PlayOneShot(AudioClip audioClip)
    {
        if (audioClip == null || _sfxSource == null)
            return;
        _sfxSource.PlayOneShot(audioClip);
    }

    public void PlaySnake() => PlayOneShot(_snakeClip);
    public void PlayBear() => PlayOneShot(_bearClip);
    public void PlayCougar() => PlayOneShot(_cougarClip);
    public void PlayBearTrap() => PlayOneShot(_bearTrapClip);
    public void PlayGator() => PlayOneShot(_gatorClip);
    public void PlayCoin() => PlayOneShot(_coinClip);
    public void PlayWin() => PlayOneShot(_winClip);
    public void PlayShoot() => PlayOneShot(_shootClip);
    public void PlaySkunk() => PlayOneShot(_skunkClip);
    public void PlaySpider() => PlayOneShot(_spiderClip);
    public void PlayBoulder() => PlayOneShot(_boulderClip);
    public void PlayKillMachine() => PlayOneShot(_killMachineClip);
    public void PlayExplosion() => PlayOneShot(_explosionClip);
}
