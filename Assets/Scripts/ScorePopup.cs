using UnityEngine;

// Número de puntos que sube flotando donde Mario consiguió puntos ("100", "200", "1UP"...)
public class ScorePopup : MonoBehaviour
{
    public string text;
    public float duration = 0.8f;
    public float riseSpeed = 2.5f;

    private float elapsed;
    private static GUIStyle style;

    public static void Show(int amount, Vector3 worldPosition)
    {
        Show(amount.ToString(), worldPosition);
    }

    public static void Show(string text, Vector3 worldPosition)
    {
        GameObject go = new GameObject("Score Popup " + text);
        go.transform.position = worldPosition + Vector3.up * 0.75f;

        ScorePopup popup = go.AddComponent<ScorePopup>();
        popup.text = text;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        if (elapsed >= duration) {
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;

        if (camera == null) {
            return;
        }

        if (style == null) {
            style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }

        style.fontSize = Mathf.Max(10, Screen.height / 36);

        Vector3 screen = camera.WorldToScreenPoint(transform.position);
        Rect rect = new Rect(screen.x - 60f, Screen.height - screen.y - 20f, 120f, 40f);
        float alpha = 1f - Mathf.Clamp01((elapsed - duration * 0.6f) / (duration * 0.4f));

        style.normal.textColor = new Color(0f, 0f, 0f, alpha);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);

        style.normal.textColor = new Color(1f, 1f, 1f, alpha);
        GUI.Label(rect, text, style);
    }

}
