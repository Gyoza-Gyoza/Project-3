using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ScreenEffectsManager : Singleton<ScreenEffectsManager>
{
    [SerializeField]
    private Image screenCover;
    [SerializeField]
    private TextMeshProUGUI notification;
    [SerializeField]
    private Transform quickNotifications;
    [SerializeField]
    private GameObject quickNotificationPrefab;
    [SerializeField] private Sprite test;
    //Title queue system
    private Queue<IEnumerator> titleTextQueue = new Queue<IEnumerator>(); //Holds the queue of title text sequences
    private bool isTitleActive = false;

    private void Start()
    {
        screenCover.gameObject.SetActive(false);
        notification.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I)) CreateQuickTextNotification(test, "Level 1 Start");
        if (Input.GetKeyDown(KeyCode.U)) CreateTitleTextNotification("Level 1 Start");
        if (Input.GetKeyDown(KeyCode.Y)) ScreenFade(0.5f, 0.5f, 1f, Color.black);
    }
    /// <summary>
    /// Performs a full screen fade in and out
    /// </summary>
    /// <param name="fadeInDuration">Duration taken to fade in</param>
    /// <param name="fadeOutDuration">Duration taken to fade out</param>
    /// <param name="holdDuration">Duration that the fade will hold for</param>
    /// <param name="color">Color of the screen fade, leave as null for black</param>
    public void ScreenFade(float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, float holdDuration = 1f, Color? color = null)
    {
        //Assigns chosen values to the screen 
        screenCover.color = color ?? Color.black;
        
        StartCoroutine(FadeSequence(screenCover, fadeInDuration, fadeOutDuration, holdDuration));
    }
    /// <summary>
    /// Creates a title text notification 
    /// </summary>
    /// <param name="notificationText">Text to display</param>
    /// <param name="fadeInDuration">Duration taken to fade in</param>
    /// <param name="fadeOutDuration">Duration taken to fade out</param>
    /// <param name="holdDuration">Duration that the fade will hold for</param>
    public void CreateTitleTextNotification(string notificationText, float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, float holdDuration = 1f)
    {
        notification.text = notificationText; //Sets the text of the notification

        titleTextQueue.Enqueue(FadeSequence(notification, fadeInDuration, fadeOutDuration, holdDuration)); //Adds the sequence to the queue

        StartCoroutine(TryRunNextNotification());
    }
    private IEnumerator TryRunNextNotification()
    {
        if (isTitleActive || titleTextQueue.Count == 0) yield break; //If there is no notification to run, return

        isTitleActive = true;
        yield return StartCoroutine(titleTextQueue.Dequeue()); //Get the next notification in the queue
        isTitleActive = false;

        StartCoroutine(TryRunNextNotification()); //Recursively call the next notification
    }

    /// <summary>
    /// Clears the title notification queue
    /// </summary>
    public void ClearTitleNotification()
    {
        titleTextQueue.Clear(); //Clears the queue of title text sequences
        notification.gameObject.SetActive(false); //Disables the notification text
    }
    public void CreateQuickTextNotification(Sprite icon, string notificationText, float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, float holdDuration = 1f)
    {
        GameObject textNotification = GameObjectPool.GetObject(quickNotificationPrefab); 

        textNotification.GetComponentInChildren<Image>().sprite = icon; 
        textNotification.GetComponentInChildren<TextMeshProUGUI>().text = notificationText;
        textNotification.transform.SetParent(quickNotifications, false);
        textNotification.transform.SetAsLastSibling();

        CanvasGroup canvasGroup = textNotification.GetComponent<CanvasGroup>();

        StartCoroutine(FadeSequence(textNotification.GetComponent<CanvasGroup>(), fadeInDuration, fadeOutDuration, holdDuration));
    }
    /// <summary>
    /// Used to fade a UI element in or out
    /// </summary>
    /// <param name="target">Target to fade</param>
    /// <param name="fadeInDuration">Duration of the fade in</param>
    /// <param name="fadeOutDuration">Duration of the fade out</param>
    /// <param name="holdDuration">How long to hold the fade in state</param>
    /// <returns></returns>
    private IEnumerator FadeSequence(Graphic target, float fadeInDuration, float fadeOutDuration, float holdDuration)
    {
        target.gameObject.SetActive(true);

        yield return Fade(target, 0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(target, 1f, 0f, fadeOutDuration);

        target.gameObject.SetActive(false);
    }
    /// <summary>
    /// Used to fade a UI element in or out
    /// </summary>
    /// <param name="target">Target to fade</param>
    /// <param name="fadeInDuration">Duration of the fade in</param>
    /// <param name="fadeOutDuration">Duration of the fade out</param>
    /// <param name="holdDuration">How long to hold the fade in state</param>
    /// <returns></returns>
    private IEnumerator FadeSequence(CanvasGroup target, float fadeInDuration, float fadeOutDuration, float holdDuration)
    {
        target.gameObject.SetActive(true);

        yield return Fade(target, 0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(target, 1f, 0f, fadeOutDuration);

        target.gameObject.SetActive(false);
    }

    private IEnumerator Fade(Graphic target, float start, float end, float duration)
    {
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            target.color =
                new Color(target.color.r,
                target.color.g,
                target.color.b,
                Mathf.Lerp(start, end, timer / duration));
            yield return null;
        }
    }
    private IEnumerator Fade(CanvasGroup target, float start, float end, float duration)
    {
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            target.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
    }

    //private Task[] FadeSequence(CanvasGroup target, float fadeInDuration, float fadeOutDuration, float holdDuration)
    //{
    //    return new Task[]
    //    {
    //        InstantTask.Create(() => target.gameObject.SetActive(true)),
    //        CreateFadeRoutine(target, fadeInDuration, fadeOutDuration, true),
    //        Wait.Create(holdDuration),
    //        CreateFadeRoutine(target, fadeInDuration, fadeOutDuration, false),
    //        InstantTask.Create(() => GameObjectPool.ReturnObject(target.gameObject))
    //    };
    //}
    ///// <summary>
    ///// Used to fade a UI element in or out
    ///// </summary>
    ///// <param name="target">Target to fade</param>
    ///// <param name="fadeInDuration">Duration of the fade in</param>
    ///// <param name="fadeOutDuration">Duration of the fade out</param>
    ///// <param name="fadeIn">True to fade in, False to fade out</param>
    ///// <returns></returns>
    //private void Fade(Graphic target, float fadeInDuration, float fadeOutDuration, bool fadeIn)
    //{
    //    timerManager.Tasks.Add(CreateFadeRoutine(target, fadeInDuration, fadeOutDuration, fadeIn));
    //}
    //private RoutineTask CreateFadeRoutine(Graphic target, float fadeInDuration, float fadeOutDuration, bool fadeIn)
    //{
    //    float timer = 0f;

    //    float startAlpha = fadeIn ? 0f : 1f;
    //    float endAlpha = fadeIn ? 1f : 0f;
    //    float duration = fadeIn ? fadeInDuration : fadeOutDuration;

    //    if (duration <= 0f) //Prevent lerp issues when dividing by 0
    //    {
    //        target.color =
    //                new Color(target.color.r,
    //                target.color.g,
    //                target.color.b,
    //                endAlpha);
    //        return RoutineTask.Create(0f, null);
    //    }

    //    return RoutineTask.Create(duration, () =>
    //    {
    //        if (timer <= duration)
    //        {
    //            timer += Time.deltaTime;

    //            target.color =
    //                new Color(target.color.r,
    //                target.color.g,
    //                target.color.b,
    //                Mathf.Lerp(startAlpha, endAlpha, timer / duration));
    //        }
    //    });
    //}

    ///// <summary>
    ///// Used to fade a Canvas Group in or out
    ///// </summary>
    ///// <param name="target">Target to fade</param>
    ///// <param name="fadeInDuration">Duration of the fade in</param>
    ///// <param name="fadeOutDuration">Duration of the fade out</param>
    ///// <param name="fadeIn">True to fade in, False to fade out</param>
    ///// <returns></returns>
    //private void Fade(CanvasGroup target, float fadeInDuration, float fadeOutDuration, bool fadeIn)
    //{
    //    timerManager.Tasks.Add(CreateFadeRoutine(target, fadeInDuration, fadeOutDuration, fadeIn));
    //}
    //private RoutineTask CreateFadeRoutine(CanvasGroup target, float fadeInDuration, float fadeOutDuration, bool fadeIn)
    //{
    //    float startAlpha = fadeIn ? 0f : 1f;
    //    float endAlpha = fadeIn ? 1f : 0f;
    //    float duration = fadeIn ? fadeInDuration : fadeOutDuration;

    //    if (duration <= 0f) //Prevent lerp issues when dividing by 0
    //    {
    //        target.alpha = endAlpha;
    //        return RoutineTask.Create(0f, null);
    //    }

    //    float timer = 0f;

    //    return RoutineTask.Create(duration, () =>
    //    {
    //        if (timer <= duration)
    //        {
    //            timer += Time.deltaTime;

    //            target.alpha = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
    //        }
    //    });
    //}
}
