#  Centered Trachea – VR Medical Training Module

This project is a Unity-based VR training and evaluation experience developed for **medical learners** to locate and evaluate key neck landmarks like the **trachea**, **cricoid cartilage**, and **thyroid cartilage**.

It supports two modes:
- 🧪 **Practice Mode** – guided experience with audio and visual cues.
- 📝 **Evaluation Mode** – structured assessment with logging and no hints.

---

##  Session Flow Overview

| Phase                  | Evaluation Mode             | Practice Mode                       |
|------------------------|-----------------------------|-------------------------------------|
| Start Button Pressed   | Starts guided assessment    | Starts practice with visual/audio   |
| Locate Landmark        | Must find the correct one   | Visual/audio feedback on touch      |
| Audio Classification   | Choose between options      | Shown but not scored/logged         |
| MCQ                    | Answer quiz about landmark  | Not shown                           |
| Session Completion     | Uploads score + log         | Ends manually via "End Session"     |

---

##  Core Scripts Overview

This module primarily uses the following scripts:

###  `Start_script.cs`

**Role:**  
Configures the scene at the beginning based on whether the session is in **practice** or **evaluation** mode.

**How It Works:**
- `practiceMode` is a public flag set by `IntentManager_tt.cs`.
- On `Start()`, it:
  - Hides UI panels (like test panel)
  - Enables/disables **skin1** or **skin2**
  - Toggles markers and end session button
- On `OnTriggerEnter`, it:
  - Switches visibility and layout depending on the mode.

**Key Fields:**
- `panel`: contains the evaluation UI (Eval_Script)
- `robe`: robe hiding toggle
- `selector`: appears in practice mode
- `endsession`: enabled immediately in practice mode

---

###  `IntentManager_tt.cs`

**Role:**  
Reads the **Android Intent** to check whether the session is in **practice mode**.

**What It Does:**
- On `Start()`, it:
  - Reads a JSON payload passed from the MedVR portal.
  - Extracts the `practiceMode` boolean.
  - Finds the `Start_script` component and sets its `practiceMode`.

**Purpose:**  
Determines whether user launched a **practice session** or an **evaluation** from the MedVR Portal.

---

###  `Eval_Script.cs`

**Role:**  
Manages the **evaluation flow**, including:
1. Landmark detection
2. Audio classification
3. MCQs
4. Logging each step
5. Sending results to Supabase

**Key Functions:**
- `SubmitLandmark(areaName)` – called by `SimpleTriggerDetector` when a landmark is touched.
- `OnOptionSelected(answer)` – triggered when a user picks a classification or MCQ answer.
- `LoadQuestionsFromSupabase()` – fetches questions dynamically.
- `EndSession()` – uploads log and ends session.

**Audio Feedback:**
- Plays:
  -  `questionCompleteAudio` when correct
  -  `incorrectAnswerAudio` when incorrect

**UI Interaction:**
- Buttons are shown/hidden depending on state.
- Session ends after all 3 regions are evaluated fully (Landmark + Audio + MCQ).

---

###  `SimpleTriggerDetector.cs`

**Role:**  
Attached to **Trachea**, **Cricoid**, and **Thyroid** game objects to detect VR controller collisions.

**Behavior:**

| Mode          | Vibration | Audio        | Highlight        | Calls `Eval_Script`|
|---------------|-----------|--------------|------------------|--------------------|
| Practice      | Yes       |  Plays clip  |  Yellow color    |  (no eval logic)   |
| Evaluation    | No        |  Silent      |  No highlight    |  (logs result)     |

**How It Works:**
- Uses tags like `newtag1L` / `newtag1R` to detect controller.
- Reads `practiceMode` from linked `Start_script`.
- Calls `SubmitLandmark(areaName)` on `Eval_Script` in eval mode.

**Custom Settings:**
- Assign:
  - `audioClip`
  - `highlightMaterial`
  - `areaName` ("Trachea", etc.)
  - `startScript` and `evalScript` references

---

## 📊 Supabase Integration

### 🎯 Fetching Questions

From:  
```

[https://twwgbdwnrsntinfhpplr.supabase.co/functions/v1/questions](https://twwgbdwnrsntinfhpplr.supabase.co/functions/v1/questions)

````

Sends:
```json
{
  "package_name": "com.cavelabspesurr.CenteredTrachea"
}
````

Receives:

* MCQs (question, options, answer) per anatomical area.

### 📤 Uploading Logs

Uses `LogUploader` to send:

* Step-by-step logs
* Score
* Total duration

Logs each:

* Landmark detection result
* Audio classification result
* Final MCQ result

---

## 🎮 GameObject Setup in Unity

Each **landmark** (Trachea, Cricoid, Thyroid) should:

* Have a `Collider` (set as `IsTrigger`)
* Have an `AudioSource` with assigned clip
* Have `SimpleTriggerDetector` script:

  * Assign `areaName`, `audioClip`, `highlightMaterial`, `evalScript`, `startScript`

The scene should include:

* `Start Button` with `Start_script.cs`
* `Panel` with `Eval_Script.cs`
* `IntentManager_tt.cs` attached to a separate GameObject

---

## 🎧 Audio Behavior Summary

| Context          | Audio Source Triggered         |
| ---------------- | ------------------------------ |
| Landmark Touched | Only in Practice mode          |
| Correct Answer   | `questionCompleteAudio.Play()` |
| Incorrect Answer | `incorrectAnswerAudio.Play()`  |

Ensure both audio sources are assigned in `Eval_Script`.

---

## 🧪 Testing Checklist

### ✅ Practice Mode

* [ ] End session button shows immediately
* [ ] Triggers play sound and highlight
* [ ] No MCQs shown
* [ ] No Supabase submission

### ✅ Evaluation Mode

* [ ] Panel visible after start
* [ ] Each step: Locate → Audio → MCQ
* [ ] Supabase questions load
* [ ] Supabase logs upload after test

---

## 🛠 Tips for Developers

* ✅ Always assign `startScript` and `evalScript` in each `SimpleTriggerDetector`.
* ✅ Ensure all objects have their AudioSources and Renderers set.
* ✅ Use `practiceMode` flag ONLY from `Start_script`.
* ✅ Keep audio sources and materials modular for reuse.
* ✅ Use consistent naming: `"Trachea"`, `"Cricoid cartilage"`.

---

## 📁 Project File Structure

```
Assets/
├── Scripts/
│   ├── Eval_Script.cs
│   ├── SimpleTriggerDetector.cs
│   ├── Start_script.cs
│   ├── IntentManager_tt.cs
│   └── LogUploader.cs
├── Materials/
│   └── HighlightYellow.mat
├── Prefabs/
│   └── Trachea.prefab
├── Audio/
│   ├── Trachea.mp3
    ├── Cricoid Cartilage.mp3
    ├── Thyroid Trachea.mp3
│   ├── CorrectDing.wav
│   └── IncorrectBuzz.wav
└── Scenes/
    └── CenteredTrachea.unity
```

---
---

```

