using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A [UI]/UI_Legend de la GameScene: panell sempre visible que explica cada cub segons el seu color.
// Els colors, els punts i les durades es llegeixen del joc, així la llegenda no es queda desfasada.
// El panell es crea en iniciar la partida; per moure'l, mou aquest objecte (a l'escena es veu com un requadre groc).
// S'executa abans que CanvasCameraSetup perquè aquest també configuri la càmera d'aquest Canvas.
[DefaultExecutionOrder(-50)]
public class CubeLegendUI : MonoBehaviour
{
    private const float Width = 560f;
    private const float Height = 570f;
    private const float Padding = 24f;
    private const float TitleHeight = 60f;
    private const float RowHeight = 78f;
    private const float SwatchSize = 48f;

    [SerializeField] private float canvasScale = 0.006f;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.79f);

    void Start()
    {
        var score = ScoreManager.Instance;
        int correct = score != null ? score.CorrectPoints : 0;
        int miss = score != null ? score.MissPenalty : 0;
        int wrong = score != null ? score.WrongPenalty : 0;

        var effects = SpecialEffects.Instance;
        float speedTime = effects.speedEffectDuration;
        float swapTime = effects.swapDuration;

        RectTransform panel = CreateCanvas();

        AddText(panel, "LLEGENDA", Padding, Padding, Width - Padding * 2f, TitleHeight, 42f, Color.white, TextAlignmentOptions.Center);

        float y = Padding + TitleHeight;
        AddRow(panel, ref y, SpecialCube.GunColor, "CUB PISTOLA", $"Dispara-li amb la pistola: +{correct}");
        AddRow(panel, ref y, SpecialCube.SwordColor, "CUB ESPASA", $"Talla'l amb l'espasa: +{correct}");
        AddRow(panel, ref y, SpecialCube.ColorFor(SpecialEffect.SlowMotion), "ESPECIAL: MÉS LENT", $"Els cubs van més lents durant {speedTime:0} s");
        AddRow(panel, ref y, SpecialCube.ColorFor(SpecialEffect.FastForward), "ESPECIAL: MÉS RÀPID", $"Els cubs van més ràpid durant {speedTime:0} s");
        AddRow(panel, ref y, SpecialCube.ColorFor(SpecialEffect.SwapWeapons), "ESPECIAL: ARMES CANVIADES", $"Pistola i espasa canvien de mà {swapTime:0} s");

        AddText(panel, $"Als especials, la icona indica l'arma correcta.\nArma equivocada: -{wrong}   Cub perdut: -{miss}",
            Padding, y + 4f, Width - Padding * 2f, Height - y - Padding, 24f, Color.white, TextAlignmentOptions.Center);
    }

    private RectTransform CreateCanvas()
    {
        var go = new GameObject("LegendCanvas");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(Width, Height);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * canvasScale;

        var background = go.AddComponent<Image>();
        background.sprite = backgroundSprite;
        background.type = Image.Type.Sliced;
        background.color = backgroundColor;
        background.raycastTarget = false;

        return rect;
    }

    private void AddRow(RectTransform parent, ref float y, Color color, string title, string description)
    {
        var swatch = new GameObject("Color").AddComponent<Image>();
        Place(swatch.rectTransform, parent, Padding, y + (RowHeight - SwatchSize) * 0.5f, SwatchSize, SwatchSize);
        swatch.color = color;
        swatch.raycastTarget = false;

        float textX = Padding + SwatchSize + 16f;
        float textWidth = Width - textX - Padding;
        AddText(parent, title, textX, y + 4f, textWidth, 36f, 30f, color, TextAlignmentOptions.Left);
        AddText(parent, description, textX, y + 40f, textWidth, 32f, 26f, Color.white, TextAlignmentOptions.Left);

        y += RowHeight;
    }

    private void AddText(RectTransform parent, string value, float x, float y, float width, float height,
                         float size, Color color, TextAlignmentOptions alignment)
    {
        var text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        Place(text.rectTransform, parent, x, y, width, height);
        if (font != null)
            text.font = font;
        text.text = value;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        // Si un text no hi cap, es redueix en lloc de sortir del panell
        text.enableAutoSizing = true;
        text.fontSizeMax = size;
        text.fontSizeMin = size * 0.6f;
    }

    // Col·loca un element mesurant x i y des de la cantonada superior esquerra del panell
    private static void Place(RectTransform rect, RectTransform parent, float x, float y, float width, float height)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(Width, Height, 0f) * canvasScale);
    }
}
