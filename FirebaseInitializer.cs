using UnityEngine;
using Firebase;
using Firebase.Analytics;
using Firebase.Messaging;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseInitializer : Singleton<FirebaseInitializer>
{
    private FirebaseApp app;

    private void Awake()
    {
        // Keep this object alive across scenes
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        Debug.Log("Checking Firebase dependencies...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                app = FirebaseApp.DefaultInstance;
                Debug.Log("Firebase is ready!");

                InitializeAnalytics();
                InitializeMessaging();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    private void InitializeAnalytics()
    {
       Debug.Log("Initializing Firebase Analytics...");
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventAppOpen);
    }

    private void InitializeMessaging()
    {
        Debug.Log("Initializing Firebase Messaging...");

        // Subscribe to default topic for global notifications
        FirebaseMessaging.SubscribeAsync("all").ContinueWithOnMainThread(task =>
        {
            Debug.Log("Subscribed to 'all' topic for push notifications.");
        });

        // Register message received callback
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenReceived += OnTokenReceived;
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("Firebase Message Received:");
        if (e.Message.Notification != null)
        {
            Debug.Log($"Title: {e.Message.Notification.Title}");
            Debug.Log($"Body: {e.Message.Notification.Body}");
        }
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
    {
        Debug.Log($"Firebase Registration Token: {e.Token}");
    }

    public void LogLevelWinEvent(int world, int level, long score, int stars)
    {
        Debug.Log($"Logging Level Win Event: World {world}, Level {level}, Score {score}, Stars {stars}");

        FirebaseAnalytics.LogEvent(
            "level_win",
            new Firebase.Analytics.Parameter[]
            {
            new Firebase.Analytics.Parameter("world", world),
            new Firebase.Analytics.Parameter("level", level),
            new Firebase.Analytics.Parameter("score", score),
            new Firebase.Analytics.Parameter("stars", stars)
            }
        );
    }

}
