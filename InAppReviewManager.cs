using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using Google.Play.Review;
using System.Collections;
#endif

public class InAppReviewManager : Singleton<InAppReviewManager>
{
#if UNITY_ANDROID
    private ReviewManager reviewManager;
    private PlayReviewInfo playReviewInfo;
#endif

    private const string REVIEW_SHOWN_KEY = "InAppReviewShown";

    private void OnEnable()
    {
        // ✅ Only trigger once ever
        if (!PlayerPrefs.HasKey(REVIEW_SHOWN_KEY))
        {
            StartCoroutine(DelayAndRequestReview());
        }
    }

    // ✅ Wait 2 minutes before showing
    private IEnumerator DelayAndRequestReview()
    {
        yield return new WaitForSeconds(120f); // 2 minutes

        RequestReview();
    }

    public void RequestReview()
    {
#if UNITY_ANDROID
        StartCoroutine(RequestReviewFlow());
#else
        Debug.Log("In-App Review only works on Android.");
#endif
    }

#if UNITY_ANDROID
    private IEnumerator RequestReviewFlow()
    {
        reviewManager = new ReviewManager();

        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning("Review Request Failed: " + requestFlowOperation.Error);
            yield break;
        }

        playReviewInfo = requestFlowOperation.GetResult();

        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;

        playReviewInfo = null;

        // ✅ Save so it never shows again
        PlayerPrefs.SetInt(REVIEW_SHOWN_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("In-App Review shown successfully (one-time only).");
    }
#endif
}
