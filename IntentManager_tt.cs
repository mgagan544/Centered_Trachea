using UnityEngine;

// This script listens for Android intents sent by the hub app and forwards the received data
// to other components like `hideRobe` and `LogUploader` in Unity.
public class Intent_Manager_tt : MonoBehaviour
{
    // Reference to hideRobe script where the practiceMode flag is set
    public Start_script Start_Script;

    // Reference to LogUploader script which needs session code and JWT
    public LogUploader logUploader;

    // Called when the scene or GameObject starts
    void Start()
    {
        // Only run the code below on an actual Android device, not inside the Unity Editor
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Access the Unity Android activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity"
            );

            // Get the Intent object that started this activity
            AndroidJavaObject intent = currentActivity.Call<AndroidJavaObject>("getIntent");

            // Check if the intent is valid (not null)
            if (intent != null)
            {
                // Read the intent extras: session code, practice mode flag, evaluation mode flag, and JWT
                string sessionCode = intent.Call<string>("getStringExtra", "session_code"); // unique session identifier
                bool isPractice = intent.Call<bool>("getBooleanExtra", "is_practice", false); // training vs evaluation
                bool isEvaluation = intent.Call<bool>("getBooleanExtra", "is_evaluation", false); // can be used in other parts
                string jwt = intent.Call<string>("getStringExtra", "jwt"); // secure token for Supabase API
                Debug.Log($"[IntentManager_tt] JWT from Intent: {jwt}");

                // Log the received intent values to the Unity console for debugging
                Debug.Log(
                    $"Received Intent Data:\nSession Code: {sessionCode}\nIs Practice: {isPractice}\nIs Evaluation: {isEvaluation}\nJWT: {jwt}"
                );

                // Pass the received data to relevant components

                // 1. Set practice mode in the hideRobe script
                Start_Script.practiceMode = isPractice;

                // 2. Set the session code and JWT in LogUploader to prepare it for data upload
                logUploader.session_code = sessionCode;
                logUploader.jwt_secret = jwt;
            }
        }
        catch (System.Exception e)
        {
            // Catch and display any errors that occur while handling the intent
            Debug.LogError($"Error processing intent: {e.Message}");
        }
#endif
    }
}
