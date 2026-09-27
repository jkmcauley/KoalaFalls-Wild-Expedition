using UnityEngine;

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

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        if (_backgroundSound == null)
            _backgroundSound = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        if (_backgroundSound != null && _backgroundSound.clip != null && !_backgroundSound.isPlaying)
            _backgroundSound.Play();
    }

    void PlayOneShot(AudioClip audioClip)
    {
        if (audioClip == null || _backgroundSound == null)
            return;
        _backgroundSound.PlayOneShot(audioClip);
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
   
}
