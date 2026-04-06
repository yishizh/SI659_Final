using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach to MinimapCanvas (or any active GameObject that can receive Unity
/// UI pointer events).
///
/// Click anywhere inside the MinimapImage RawImage to teleport the Player
/// CharacterController to the corresponding world X/Z position.
///
/// No Meta/Oculus SDK required — pure Unity UI + CharacterController.
/// </summary>
public class MinimapTeleport : MonoBehaviour, IPointerClickHandler
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Feature Toggle")]
    [Tooltip("Uncheck to disable click-to-teleport while keeping the minimap visible.")]
    public bool teleportEnabled = true;

    [Header("Scene References")]
    [Tooltip("The MinimapImage RawImage whose rect defines the clickable minimap area.")]
    public RawImage minimapImage;

    [Tooltip("The MinimapCamera used for the top-down view. Provides orthographic size and world position.")]
    public Camera minimapCamera;

    [Tooltip("The Player GameObject that has the CharacterController. Auto-found by 'Player' tag if empty.")]
    public GameObject player;

    [Header("Click Feedback")]
    [Tooltip("Duration (seconds) for the white-circle click indicator to fade out.")]
    public float feedbackFadeDuration = 0.5f;

    // ── Private state ─────────────────────────────────────────────────────────

    CharacterController _cc;
    RectTransform       _mapRect;
    GameObject          _feedbackDot;
    Image               _feedbackImg;
    Coroutine           _fadeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        // Auto-find player
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[MinimapTeleport] No GameObject tagged 'Player' found. " +
                               "Assign it in the Inspector.");
                enabled = false;
                return;
            }
        }

        _cc = player.GetComponent<CharacterController>();
        if (_cc == null)
        {
            Debug.LogError($"[MinimapTeleport] '{player.name}' has no CharacterController. " +
                            "Teleport disabled.");
            enabled = false;
            return;
        }

        // Auto-find MinimapImage
        if (minimapImage == null)
        {
            minimapImage = GetComponentInChildren<RawImage>();
            if (minimapImage == null)
            {
                Debug.LogError("[MinimapTeleport] MinimapImage not found. Assign it in the Inspector.");
                enabled = false;
                return;
            }
        }
        _mapRect = minimapImage.rectTransform;

        // Auto-find MinimapCamera
        if (minimapCamera == null)
        {
            GameObject camGO = GameObject.Find("MinimapCamera");
            if (camGO != null) minimapCamera = camGO.GetComponent<Camera>();
            if (minimapCamera == null)
            {
                Debug.LogError("[MinimapTeleport] MinimapCamera not found. Assign it in the Inspector.");
                enabled = false;
                return;
            }
        }

        CreateFeedbackDot();
    }

    // ── IPointerClickHandler ──────────────────────────────────────────────────

    /// <summary>
    /// Fires when the user clicks anywhere on a UI Graphic that is a child of
    /// (or is) the GameObject this script is attached to.
    /// MinimapImage must have Raycast Target = true for this to trigger.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!teleportEnabled) return;

        // ── Convert screen click → normalised position inside minimap rect ───
        // RectTransformUtility handles the CanvasScaler for us.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _mapRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            return;

        // localPoint is in the RectTransform's local space.
        // The pivot is (0.5, 0.5) by default so localPoint is in [-half, +half].
        Rect rect      = _mapRect.rect;
        float normX    = (localPoint.x - rect.x) / rect.width;   // 0 … 1
        float normY    = (localPoint.y - rect.y) / rect.height;

        // Clamp: reject clicks outside the rect
        if (normX < 0f || normX > 1f || normY < 0f || normY > 1f) return;

        // ── Convert normalised UV → world position ─────────────────────────
        // MinimapCamera is orthographic.
        // Camera right = world +X, camera up = world +Z (rotation Euler 90,0,0).
        // orthographicSize = half-height in world units.
        float halfH = minimapCamera.orthographicSize;
        float halfW = halfH * minimapCamera.aspect;

        // Camera anchor world position (it follows the player via MinimapFollow)
        Vector3 camPos = minimapCamera.transform.position;

        float worldX = camPos.x + Mathf.Lerp(-halfW, halfW, normX);
        float worldZ = camPos.z + Mathf.Lerp(-halfH, halfH, normY);

        // ── Teleport ────────────────────────────────────────────────────────
        TeleportPlayer(new Vector3(worldX, 0f, worldZ));

        // ── Visual feedback ─────────────────────────────────────────────────
        ShowFeedback(localPoint);
    }

    // ── Teleport ──────────────────────────────────────────────────────────────

    void TeleportPlayer(Vector3 destination)
    {
        // CharacterController.Move ignores physics; we must disable/re-enable
        // the collider to warp without the controller fighting the move.
        _cc.enabled = false;
        player.transform.position = destination;
        _cc.enabled = true;

        Debug.Log($"[MinimapTeleport] Teleported '{player.name}' to {destination:F1}");
    }

    // ── Click feedback ────────────────────────────────────────────────────────

    void CreateFeedbackDot()
    {
        _feedbackDot = new GameObject("MinimapClickFeedback");
        _feedbackDot.transform.SetParent(_mapRect, false);

        RectTransform rt   = _feedbackDot.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(14f, 14f);

        _feedbackImg          = _feedbackDot.AddComponent<Image>();
        _feedbackImg.raycastTarget = false;  // don't intercept future clicks

        // Re-use the circle sprite if it was already created by MinimapUISetup;
        // fall back to the default white square if not found.
        Sprite circle = Resources.Load<Sprite>("MinimapCircle");
        if (circle == null)
        {
            // Try by path at runtime (sprite must be in Resources or already loaded)
            // Graceful fallback: built-in white square is fine for a tiny dot.
        }
        _feedbackImg.sprite = circle;
        _feedbackImg.color  = new Color(1f, 1f, 1f, 0f);  // fully transparent initially

        _feedbackDot.SetActive(false);
    }

    void ShowFeedback(Vector2 localPoint)
    {
        if (_feedbackDot == null) return;

        _feedbackDot.GetComponent<RectTransform>().anchoredPosition = localPoint;
        _feedbackImg.color = Color.white;
        _feedbackDot.SetActive(true);

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeFeedback());
    }

    IEnumerator FadeFeedback()
    {
        float elapsed = 0f;
        while (elapsed < feedbackFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / feedbackFadeDuration);
            _feedbackImg.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        _feedbackDot.SetActive(false);
    }
}
