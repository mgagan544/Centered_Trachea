using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class Eval_Script : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text displayText;

    [Header("MCQ Buttons")]
    public MCQButtonHandler_tt optionA;
    public MCQButtonHandler_tt optionB;
    public MCQButtonHandler_tt optionC;
    public MCQButtonHandler_tt optionD;

    [Header("Uploader Reference")]
    public LogUploader logUploader;

    [Header("Audio")]
    public AudioSource correctAudioSource;
    public AudioSource incorrectAudioSource;

    private string[] allPoints = { "Thyroid cartilage", "Cricoid cartilage", "Trachea" };

    private class MCQ
    {
        public string question;
        public string correctAnswer;
        public List<string> options;

        public MCQ(string q, string correct, List<string> opts)
        {
            question = q;
            correctAnswer = correct;
            options = opts;
        }
    }

    private class StepLog
    {
        public string name;
        public string status;
        public float time;
        public string correctAnswer;
        public string selectedAnswer;
    }

    private Dictionary<string, List<MCQ>> mcqBank = new();
    private Dictionary<string, MCQ> defaultQuestions = new();
    private Dictionary<string, string[]> audioOptions = new();
    private HashSet<string> completedPoints = new();
    private string currentTarget = "";
    private MCQ currentMCQ;
    private string lastCorrectLandmark = "";

    private int score = 0;
    private bool awaitingMCQ = false;
    private bool awaitingAudioClassification = false;
    private bool sessionEnded = false;

    private List<StepLog> logs = new();
    private Stopwatch sessionStopwatch;
    private Stopwatch questionStopwatch;

    [System.Serializable]
    private class SupabaseResponse
    {
        public List<QuestionData> questions;
    }

    [System.Serializable]
    private class QuestionData
    {
        public string id;
        public string question;
        public string option_a;
        public string option_b;
        public string option_c;
        public string option_d;
        public string correct_answer;
        public string area;
    }

    [System.Serializable]
    private class Wrapper
    {
        public string package_name;
    }

    IEnumerator Start()
    {
        HideMCQButtons();
        sessionStopwatch = Stopwatch.StartNew();
        questionStopwatch = new Stopwatch();

        InitDefaultQuestions();
        InitAudioOptions();
        yield return StartCoroutine(LoadQuestionsFromSupabase());

        SelectNextPoint();
        questionStopwatch.Start();
    }

    void InitDefaultQuestions()
    {
        defaultQuestions["Thyroid cartilage"] = new MCQ(
            "Which of the following cartilages is commonly known as the 'Adam's Apple'?",
            "Thyroid cartilage",
            new List<string> { "Cricoid cartilage", "Thyroid cartilage", "Epiglottis", "Arytenoid cartilage" }
        );

        defaultQuestions["Cricoid cartilage"] = new MCQ(
            "Signet ring cartilage of larynx is?",
            "Cricoid cartilage",
            new List<string> { "Cricoid cartilage", "Thyroid cartilage", "Hyoid bone", "All of the above" }
        );

        defaultQuestions["Trachea"] = new MCQ(
            "Evaluate the position of trachea",
            "Centered",
            new List<string> { "Centered", "Deviated", "Collapsed", "Obstructed" }
        );
    }

    void InitAudioOptions()
    {
        audioOptions["Trachea"] = new[] { "Centered", "Deviated" };
        audioOptions["Thyroid cartilage"] = new[] { "Normal", "Abnormal" };
        audioOptions["Cricoid cartilage"] = new[] { "Normal", "Abnormal" };
    }

    IEnumerator LoadQuestionsFromSupabase()
    {
        string supabaseURL = "https://twwgbdwnrsntinfhpplr.supabase.co/functions/v1/questions";
        string packageName = "com.cavelabspesurr.chestsounds";

        string jsonPayload = JsonUtility.ToJson(new Wrapper { package_name = packageName });

        using var request = new UnityWebRequest(supabaseURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonPayload));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            SupabaseResponse response = JsonUtility.FromJson<SupabaseResponse>(request.downloadHandler.text);
            foreach (var q in response.questions)
            {
                if (!mcqBank.ContainsKey(q.area))
                    mcqBank[q.area] = new List<MCQ>();

                mcqBank[q.area].Add(new MCQ(q.question, q.correct_answer, new List<string> { q.option_a, q.option_b, q.option_c, q.option_d }));
            }

            Debug.Log("✅ MCQs loaded from Supabase");
        }
        else
        {
            Debug.LogError("❌ Supabase error: " + request.error);
        }
    }

    void SelectNextPoint()
    {
        if (sessionEnded || completedPoints.Count >= allPoints.Length)
        {
            EndSession();
            return;
        }

        string next;
        do next = allPoints[Random.Range(0, allPoints.Length)];
        while (completedPoints.Contains(next));

        currentTarget = next;
        displayText.text = $"Locate: {currentTarget}";
        questionStopwatch.Restart();
    }

    void EndSession()
    {
        if (sessionEnded) return;

        sessionEnded = true;
        sessionStopwatch.Stop();

        var payload = logs.Select(l => new LogUploader.StepResult
        {
            name = l.name,
            status = l.status,
            time = l.time,
            correctAnswer = l.correctAnswer,
            selectedAnswer = l.selectedAnswer
        }).ToList();

        logUploader?.UploadLog(payload, score, (float)sessionStopwatch.Elapsed.TotalSeconds);

        displayText.text = $"Test Complete\nScore: {score}/{allPoints.Length * 3}";
        StartCoroutine(QuitAfterDelay(5f));
    }

    public void SubmitLandmark(string landmarkName)
    {
        if (awaitingMCQ || awaitingAudioClassification || sessionEnded) return;

        float timeTaken = (float)questionStopwatch.Elapsed.TotalSeconds;
        logs.Add(new StepLog
        {
            name = $"Locate {currentTarget}",
            status = landmarkName == currentTarget ? "Correct" : "Incorrect",
            time = timeTaken,
            correctAnswer = currentTarget,
            selectedAnswer = landmarkName
        });

        if (landmarkName == currentTarget)
        {
            score++;
            lastCorrectLandmark = landmarkName;
            awaitingAudioClassification = true;

            var options = audioOptions.ContainsKey(landmarkName)
                ? audioOptions[landmarkName]
                : new[] { "Normal", "Abnormal" };

            displayText.text = $"Is the {landmarkName} {options[0]} or {options[1]}?";
            optionA.SetOption(options[0]);
            optionB.SetOption(options[1]);
            optionA.gameObject.SetActive(true);
            optionB.gameObject.SetActive(true);
            correctAudioSource?.Play();
        }
        else
        {
            displayText.text = $"Incorrect. Expected {currentTarget}";
            incorrectAudioSource?.Play();
        }
    }

    public void OnOptionSelected(string selectedAnswer)
    {
        if (sessionEnded) return;

        float timeTaken = (float)questionStopwatch.Elapsed.TotalSeconds;

        if (awaitingAudioClassification)
        {
            var expected = audioOptions.ContainsKey(lastCorrectLandmark)
                ? audioOptions[lastCorrectLandmark][0]
                : "Normal";

            bool correct = selectedAnswer == expected;

            logs.Add(new StepLog
            {
                name = $"Audio Classification at {lastCorrectLandmark}",
                status = correct ? "Correct" : "Incorrect",
                time = timeTaken,
                correctAnswer = expected,
                selectedAnswer = selectedAnswer
            });

            if (correct)
            {
                score++;
                correctAudioSource?.Play();
            }
            else
            {
                incorrectAudioSource?.Play();
            }

            awaitingAudioClassification = false;

            currentMCQ = GetRandomMCQ(lastCorrectLandmark);
            displayText.text = currentMCQ.question;
            ShowMCQButtons(currentMCQ);
            awaitingMCQ = true;
            questionStopwatch.Restart();
            return;
        }

        if (!awaitingMCQ) return;

        bool mcqCorrect = selectedAnswer == currentMCQ.correctAnswer;

        logs.Add(new StepLog
        {
            name = $"MCQ: {currentMCQ.question}",
            status = mcqCorrect ? "Correct" : "Incorrect",
            time = timeTaken,
            correctAnswer = currentMCQ.correctAnswer,
            selectedAnswer = selectedAnswer
        });

        if (mcqCorrect)
        {
            score++;
            displayText.text = "Correct Answer!";
            correctAudioSource?.Play();
        }
        else
        {
            displayText.text = $"Wrong. Correct: {currentMCQ.correctAnswer}";
            incorrectAudioSource?.Play();
        }

        awaitingMCQ = false;
        HideMCQButtons();
        completedPoints.Add(currentTarget);
        SelectNextPoint();
    }

    MCQ GetRandomMCQ(string point)
    {
        if (mcqBank.ContainsKey(point) && mcqBank[point].Count > 0)
            return mcqBank[point][Random.Range(0, mcqBank[point].Count)];

        return defaultQuestions.ContainsKey(point)
            ? defaultQuestions[point]
            : new MCQ("No question", "", new List<string> { "", "", "", "" });
    }

    void ShowMCQButtons(MCQ mcq)
    {
        var opts = new List<string>(mcq.options);
        opts.shuffled1();
        optionA.SetOption(opts[0]);
        optionB.SetOption(opts[1]);
        optionC.SetOption(opts[2]);
        optionD.SetOption(opts[3]);
        optionA.gameObject.SetActive(true);
        optionB.gameObject.SetActive(true);
        optionC.gameObject.SetActive(true);
        optionD.gameObject.SetActive(true);
    }

    void HideMCQButtons()
    {
        optionA.gameObject.SetActive(false);
        optionB.gameObject.SetActive(false);
        optionC.gameObject.SetActive(false);
        optionD.gameObject.SetActive(false);
    }

    IEnumerator QuitAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

public static class shuffled1d1
{
    public static void shuffled1<T>(this IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
