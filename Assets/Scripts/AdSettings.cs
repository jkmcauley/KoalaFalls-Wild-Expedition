using UnityEngine;

[CreateAssetMenu(menuName = "Koala Falls/Ad Settings", fileName = "AdSettings")]
public class AdSettings : ScriptableObject
{
    [Header("From Unity dashboard > Monetization > Apps")]
    public string androidAppKey;
    public string iosAppKey;

    [Header("From Unity dashboard > Monetization > Ad Units > Rewarded")]
    public string androidRewardedAdUnitId;
    public string iosRewardedAdUnitId;

    [Header("From Unity dashboard > Monetization > Ad Units > Interstitial")]
    public string androidInterstitialAdUnitId;
    public string iosInterstitialAdUnitId;
}
