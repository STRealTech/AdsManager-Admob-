using Sych.ShareAssets.Runtime;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sych.ShareAssets.Runtime;
public class ShareManager : MonoBehaviour
{
    [Header("UI Button")]
    public Button shareButton;

    [Header("Game Share Settings")]
    [TextArea]
    string shareMessage =
"Play Animal Match 3 Puzzle! 🐾🧩 Match cute animals, solve fun match-3 puzzles, and enjoy relaxing casual gameplay. Download now and start matching!";



    string gameLink =
        " https://play.google.com/store/apps/details?id=com.strealtech.animalmatchpuzzle";

    private void Start()
    {
        shareButton.onClick.AddListener(ShareGame);
    }

    private void OnDestroy()
    {
        shareButton.onClick.RemoveListener(ShareGame);
    }

    private void ShareGame()
    {
        if (!Share.IsPlatformSupported)
        {
            Debug.LogError("Sharing is not supported on this platform.");
            return;
        }

        List<string> items = new List<string>
        {
            shareMessage + gameLink
        };

        Share.Items(items, success =>
        {
            if (success)
                Debug.Log("Share window opened successfully.");
            else
                Debug.LogWarning("Failed to open share window.");
        });
    }
}
