using GoogleMobileAds.Api;
using GoogleMobileAds.Api.Mediation.UnityAds;
using GoogleMobileAds.Ump;
using GoogleMobileAds.Ump.Api;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AdsManager : Singleton<AdsManager>
{
    public static AdsManager Instance;


    // ===========================================================
    //   ███   User Ids Setup
    // ===========================================================
    #region  User IDs 
    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    [Header("AdMob Ad Unit IDs")]
     string rewardedAdUnitId = "ca-app-pub-7230095769442277/6080200211";  // Add your Rewarded ID
     string interstitialAdUnitId = "ca-app-pub-7230095769442277/1627101262";  // Add your interstitial AdUnitId
     string bannerAdUnitId = "ca-app-pub-7230095769442277/7393281888";       //   Add your banner AdUnitId
#endregion

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (ConnectivityManager.Instance.isConnected)
        {
            RequestUserConsent();  // MUST BE CALLED FIRST (GDPR compliance)
        }
    }

    // ===========================================================
    //   ███   UMP CONSENT (GDPR / CCPA)
    // ===========================================================
    #region UMP CONSENT
    public void RequestUserConsent()
    {
        Debug.Log("📌 Requesting UMP Consent...");

        ConsentRequestParameters request = new ConsentRequestParameters();

        ConsentInformation.Update(request, (FormError updateError) =>
        {
            if (updateError != null)
            {
                Debug.LogError("❌ UMP Update Error: " + updateError.Message);
                InitializeMobileAds();
                return;
            }

            if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
            {
                ConsentForm.Load((ConsentForm form, FormError loadError) =>
                {
                    if (loadError != null)
                    {
                        Debug.LogError("❌ UMP Load Error: " + loadError.Message);
                        InitializeMobileAds();
                        return;
                    }

                    form.Show((FormError showError) =>
                    {
                        if (showError != null)
                            Debug.LogError("⚠️ UMP Show Error: " + showError.Message);

                        FinalizeConsent();
                    });
                });
            }
            else
            {
                FinalizeConsent();
            }
        });
    }

    private void FinalizeConsent()
    {
        bool consentGiven =
            ConsentInformation.ConsentStatus == ConsentStatus.Obtained ||
            ConsentInformation.ConsentStatus == ConsentStatus.NotRequired;

        Debug.Log("📝 Consent Given = " + consentGiven);

        UnityAds.SetConsentMetaData("gdpr.consent", consentGiven);
        UnityAds.SetConsentMetaData("privacy.consent", consentGiven);

        InitializeMobileAds();
    }
    #endregion

    // ===========================================================
    //   ███   ADMOB INITIALIZATION
    // ===========================================================
    #region INITIALIZATION
    private void InitializeMobileAds()
    {
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("✅ AdMob initialized.");

            var adapters = initStatus.getAdapterStatusMap();
            foreach (var kvp in adapters)
            {
                Debug.Log($"Adapter {kvp.Key}: {kvp.Value.InitializationState} - {kvp.Value.Description}");
            }

            LoadRewardedAd();
            LoadInterstitialAd();
            LoadBannerAd();
            LoadNativeAd();

            Invoke(nameof(ShowInterstitialAd), 3f);
            StartCoroutine(ShowBanner());
        });
    }
    #endregion

    // ===========================================================
    //   ███   REWARDED ADS
    // ===========================================================
    #region REWARDED ADS
    public void LoadRewardedAd()
    {
        RewardedAd.Load(rewardedAdUnitId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("❌ Rewarded failed to load: " + error);
                return;
            }

            rewardedAd = ad;
            Debug.Log("✅ Rewarded ad loaded.");

            rewardedAd.OnAdPaid += adValue =>
                Debug.Log($"💰 Rewarded Paid: {adValue.Value} {adValue.CurrencyCode}");

            rewardedAd.OnAdImpressionRecorded += () =>
                Debug.Log("👁️ Rewarded Impression Recorded");

            rewardedAd.OnAdClicked += () =>
                Debug.Log("🖱️ Rewarded Clicked");

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("❌ Rewarded Closed – Reloading");
                LoadRewardedAd();
            };

            rewardedAd.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("⚠️ Rewarded Failed: " + error);
                LoadRewardedAd();
            };
        });
    }

    public void ShowRewardedAds()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show(reward =>
            {
                Debug.Log($"🏆 User Rewarded: {reward.Amount} {reward.Type}");
            });
        }
        else
        {
            Debug.LogWarning("⚠️ Rewarded not ready. Reloading...");
            LoadRewardedAd();
        }
    }

    /// <summary>
    /// Shows a rewarded ad and calls onAdCompleted only if the ad is watched fully.
    /// If ad fails or is skipped, shows a debug warning and does NOT call the callback.
    /// </summary>
    public void ShowRewardedAd(System.Action onAdCompleted)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show(reward =>
            {
                Debug.Log($"🏆 User Rewarded: {reward.Amount} {reward.Type}");

                // Call the callback only if the ad was watched fully
                onAdCompleted?.Invoke();
            });

            // Optional: subscribe to full screen content events for fail/skip
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("⚠️ Rewarded ad closed.");
            };

            rewardedAd.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("❌ Rewarded ad failed: " + error);
            };
        }
        else
        {
            Debug.LogWarning("⚠️ Rewarded ad not ready. Loading now...");
            LoadRewardedAd();
        }
    }

    /*    public void ShowRewardedAd(System.Action onAdCompleted)
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                rewardedAd.Show(reward =>
                {
                    Debug.Log($"🏆 User Rewarded: {reward.Amount} {reward.Type}");

                    // Call the callback after ad is watched fully
                    onAdCompleted?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning("⚠️ Rewarded ad not ready. Loading now...");
                LoadRewardedAd();
            }
        }*/


    /*  private SkillButton pendingSkillButton;

      public void ShowRewardedAdForSkill(SkillButton skillButton)
      {
          pendingSkillButton = skillButton;

          if (rewardedAd != null && rewardedAd.CanShowAd())
          {
              rewardedAd.Show(reward =>
              {
                  Debug.Log("🏆 Rewarded – Skill Unlocked!");
                  pendingSkillButton.UseSkillAfterAd();  // <-- CUSTOM METHOD
                  pendingSkillButton = null;
              });
          }
          else
          {
              Debug.LogWarning("Rewarded ad not ready! Reloading...");
              LoadRewardedAd();
          }
      }*/

    /*   public void ShowRewardedAdForGrid(UnlockableGrid grid)
       {
           if (grid == null)
               return;

           if (rewardedAd != null && rewardedAd.CanShowAd())
           {
               rewardedAd.Show(reward =>
               {
                   if (grid != null) // <-- Check if grid still exists
                   {
                       Debug.Log("🏆 Rewarded – Grid Unlocked!");
                       grid.UnlockAfterAd();
                   }
               });
           }
           else
           {
               Debug.LogWarning("Rewarded ad not ready! Reloading...");
               LoadRewardedAd();
           }
       }*/


    #endregion

    // ===========================================================
    //   ███   INTERSTITIAL ADS
    // ===========================================================
    #region INTERSTITIAL ADS
    public void LoadInterstitialAd()
    {
        InterstitialAd.Load(interstitialAdUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("❌ Interstitial failed to load: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("✅ Interstitial loaded.");

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("❌ Interstitial closed – Reloading");
                LoadInterstitialAd();
            };

            interstitialAd.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogError("⚠️ Interstitial Failed: " + error);
                LoadInterstitialAd();
            };
        });
    }

    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("⚠️ Interstitial not ready!");
            LoadInterstitialAd();
        }
    }
    #endregion

    // ===========================================================
    //   ███   BANNER ADS (AUTO REFRESH BY ADMOB)
    // ===========================================================
    #region BANNER ADS
    public void LoadBannerAd()
    {
        if (bannerView != null)
            return;

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
            Debug.Log("📌 Banner Loaded");

        bannerView.OnBannerAdLoadFailed += (error) =>
            Debug.LogError("❌ Banner Failed: " + error.GetMessage());

        bannerView.LoadAd(new AdRequest());
    }

    IEnumerator  ShowBanner()
    {
        if (bannerView == null)
            LoadBannerAd();

        bannerView?.Show();
        yield return new WaitForSeconds(35f);
        StartCoroutine(ShowBanner());
    }

    public void HideBanner()
    {
        bannerView?.Hide();
    }

    public void DestroyBanner()
    {
        bannerView?.Destroy();
        bannerView = null;
    }
    #endregion

    // ===========================================================
    //   ███   EXAMPLE SCENE HANDLER
    // ===========================================================
    #region Native Ads
#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/2247696110";
#elif UNITY_IOS
    private string _adUnitId = "ca-app-pub-3940256099942544/3986624511";
#else
    private string _adUnitId = "unused";
#endif

    private NativeOverlayAd _nativeOverlayAd;
    private bool _adReadyToShow = false;

    /// <summary>
    /// Loads a Native Overlay Ad
    /// </summary>
    public void LoadNativeAd()
    {
        if (_nativeOverlayAd != null)
        {
            DestroyAd();
        }

        Debug.Log("Loading Native Overlay Ad...");

        AdRequest request = new AdRequest();

        NativeAdOptions options = new NativeAdOptions
        {
            AdChoicesPlacement = AdChoicesPlacement.TopRightCorner,
            MediaAspectRatio = MediaAspectRatio.Any
        };

        NativeOverlayAd.Load(_adUnitId, request, options, (ad, error) =>
        {
            if (error != null)
            {
                Debug.LogError("Error loading native overlay ad: " + error);
                _adReadyToShow = false;
                return;
            }

            _nativeOverlayAd = ad;
            _adReadyToShow = true; // mark ad as ready
            Debug.Log("Native Overlay loaded: " + ad.GetResponseInfo());

            RegisterAdEvents(_nativeOverlayAd);

            // Optional: show automatically after load
            // ShowAd();
        });
    }

    /// <summary>
    /// Shows the loaded native overlay ad if ready
    /// </summary>
    public void ShowNativeAd()
    {
        if (_adReadyToShow && _nativeOverlayAd != null)
        {
            _nativeOverlayAd.Show();
            _adReadyToShow = false; // reset flag after showing
        }
        else
        {
            Debug.LogWarning("Native Overlay Ad not ready to show.");
        }
    }

    /// <summary>
    /// Returns true if ad is loaded and ready
    /// </summary>
    public bool IsAdReady()
    {
        return _adReadyToShow && _nativeOverlayAd != null;
    }

    /// <summary>
    /// Destroys the ad to free resources
    /// </summary>
    private void DestroyAd()
    {
        if (_nativeOverlayAd != null)
        {
            _nativeOverlayAd.Destroy();
            _nativeOverlayAd = null;
        }
    }

    /// <summary>
    /// Registers events for ad lifecycle
    /// </summary>
    private void RegisterAdEvents(NativeOverlayAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Native Overlay Ad closed.");
            DestroyAd();
            LoadNativeAd(); // preload next ad automatically
        };

        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Native Overlay Ad opened.");
        };

        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Native Overlay Ad impression recorded.");
        };

        ad.OnAdClicked += () =>
        {
            Debug.Log("Native Overlay Ad clicked.");
        };
    }


    #endregion
}

