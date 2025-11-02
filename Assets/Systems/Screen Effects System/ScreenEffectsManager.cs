using System;
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
    [SerializeField] private RectTransform topBar, bottomBar;
    [SerializeField] private Sprite test;
    //Title queue system
    private Queue<IEnumerator> titleTextQueue = new Queue<IEnumerator>(); //Holds the queue of title text sequences
    private bool isTitleActive = false;
    private RectTransform canvasRectTransform;
    private Coroutine letterboxCoroutine;

    protected override void Awake()
    {
        base.Awake();
        canvasRectTransform = GetComponent<RectTransform>();
    }
    private void Start()
    {
        //screenCover.gameObject.SetActive(false);
        //notification.gameObject.SetActive(false);
        InitializeLetterboxBars();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SetLetterboxActive(true);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            SetLetterboxActive(false);
        }
    }
    /// <summary>
    /// Performs a full screen fade in and out
    /// </summary>
    /// <param name="actionToDo">Action to perform during the hold duration</param>
    /// <param name="fadeInDuration">Duration taken to fade in</param>
    /// <param name="fadeOutDuration">Duration taken to fade out</param>
    /// <param name="holdDuration">Duration that the fade will hold for</param>
    /// <param name="color">Color of the screen fade, leave as null for black</param>
    public void ScreenFade(float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, float holdDuration = 1f, Color? color = null, Action actionToDo = null)
    {
        //Assigns chosen values to the screen 
        screenCover.color = color ?? Color.black;
        
        StartCoroutine(FadeSequence(screenCover, fadeInDuration, fadeOutDuration, holdDuration, actionToDo));
    }
    /// <summary>
    /// Performs a full screen fade in and out
    /// </summary>
    /// <param name="actionToDo">Action to perform during the hold duration</param>
    /// <param name="fadeInDuration">Duration taken to fade in</param>
    /// <param name="fadeOutDuration">Duration taken to fade out</param>
    /// <param name="holdDuration">Duration that the fade will hold for</param>
    /// <param name="color">Color of the screen fade, leave as null for black</param>
    public void ScreenFade(Action actionToDo = null, float fadeInDuration = 0.5f, float fadeOutDuration = 0.5f, float holdDuration = 1f, Color? color = null)
    {
        //Assigns chosen values to the screen 
        screenCover.color = color ?? Color.black;

        StartCoroutine(FadeSequence(screenCover, fadeInDuration, fadeOutDuration, holdDuration, actionToDo));
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

        titleTextQueue.Enqueue(FadeSequence(notification, fadeInDuration, fadeOutDuration, holdDuration, null)); //Adds the sequence to the queue

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

        StartCoroutine(FadeSequence(textNotification.GetComponent<CanvasGroup>(), fadeInDuration, fadeOutDuration, holdDuration, null));
    }
    /// <summary>
    /// Used to fade a UI element in or out
    /// </summary>
    /// <param name="target">Target to fade</param>
    /// <param name="fadeInDuration">Duration of the fade in</param>
    /// <param name="fadeOutDuration">Duration of the fade out</param>
    /// <param name="holdDuration">How long to hold the fade in state</param>
    /// <returns></returns>
    private IEnumerator FadeSequence(Graphic target, float fadeInDuration, float fadeOutDuration, float holdDuration, Action actionToDo)
    {
        target.gameObject.SetActive(true);

        yield return Fade(target, 0f, 1f, fadeInDuration);
        actionToDo?.Invoke();
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
    private IEnumerator FadeSequence(CanvasGroup target, float fadeInDuration, float fadeOutDuration, float holdDuration, Action actionToDo)
    {
        target.gameObject.SetActive(true);

        yield return Fade(target, 0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(target, 1f, 0f, fadeOutDuration);

        target.gameObject.SetActive(false);
    }

    public IEnumerator Fade(Graphic target, float start, float end, float duration)
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
    public IEnumerator Fade(CanvasGroup target, float start, float end, float duration)
    {
        float timer = 0f;

        while (timer <= duration)
        {
            timer += Time.deltaTime;
            target.alpha = Mathf.Lerp(start, end, timer / duration);
            yield return null;
        }
    }
    private void InitializeLetterboxBars()
    {
        topBar.offsetMin = new Vector2(topBar.offsetMin.x, canvasRectTransform.rect.height);
        bottomBar.offsetMax = new Vector2(bottomBar.offsetMax.x, -canvasRectTransform.rect.height);
    }
    /// <summary>
    /// Starts the letterbox effect
    /// </summary>
    /// <param name="active">Activates/Deactivates the effect</param>
    /// <param name="size">Percentage of the screen that it'll cover</param>
    /// <param name="duration">Duration of the effect</param>
    /// <param name="color">Color of the effect</param>
    public void SetLetterboxActive(bool active, float size = 0.2f, float duration = 2f, Color? color = null)
    {
        if (letterboxCoroutine != null) StopCoroutine(letterboxCoroutine);
        letterboxCoroutine = StartCoroutine(SetLetterboxActiveCoroutine(active, size, duration, color));
    }
    public IEnumerator SetLetterboxActiveCoroutine(bool active, float size = 0.1f, float duration = 2f, Color? color = null)
    {
        color = color ?? Color.black;
        float timer = 0f;

        topBar.GetComponent<Image>().color = (Color)color;
        bottomBar.GetComponent<Image>().color = (Color)color;

        float canvasHeight = canvasRectTransform.rect.height;
        float startSize = topBar.offsetMin.y;
        float targetSize = active? canvasHeight - size * (canvasHeight / 2f) : canvasHeight;

        while (timer <= duration)
        {
            timer += Time.deltaTime;

            float offsetSize = Mathf.Lerp(startSize, targetSize, timer / duration);

            topBar.offsetMin = new Vector2(topBar.offsetMin.x, offsetSize);
            bottomBar.offsetMax = new Vector2(bottomBar.offsetMax.x, -offsetSize); // Not a typo, offsetMax is set to negative by unity so it has to be negative to counter it
            yield return null;
        }
   
        topBar.offsetMin = new Vector2(topBar.offsetMin.x, targetSize);
        bottomBar.offsetMax = new Vector2(bottomBar.offsetMax.x, -targetSize);
    }
}
