using System.Collections;
using TMPro;
using UnityEngine;

public class EndingTextSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Sequence")]
    [Tooltip("删光记忆结局（all_deleted）的段落")]
    [SerializeField] private string[] allDeletedParagraphs = new string[]
    {
        "Space cleaning completed",
        "You are awake",
        "The body remembers how to breathe",
        "The heart remembers how to beat",
        "But you don't remember them",
        "Nor do you remember yourself",
        "",
        "MEMORY SPACE: AVAILABLE",
        "",
        "You finally have enough space",
        "Because there is nothing inside.",
        "",
        "NO SPACE LEFT"
    };

    [Tooltip("删除 AI 结局（awakening）的段落")]
    [SerializeField] private string[] awakeningParagraphs = new string[]
    {
        "Space cleaning completed",
        "You are awake",
        "The body remembers how to breathe",
        "The heart remembers how to beat",
        "You still remember them",
        "And you still remember yourself",
        "",
        "MEMORY SPACE: OCCUPIED",
        "",
        "You finally have enough space",
        "Because the entity that occupied the space is no longer here.",
        "",
        "NO SPACE LEFT"
    };

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float paragraphHoldTime = 2.5f;

    private void Start()
    {
        if (text == null) return;

        string[] paragraphs = GameManager.LastEndingType == "awakening"
            ? awakeningParagraphs
            : allDeletedParagraphs;

        text.text = string.Empty;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        StartCoroutine(PlaySequence(paragraphs));
    }

    private IEnumerator PlaySequence(string[] paragraphs)
    {
        for (int i = 0; i < paragraphs.Length; i++)
        {
            text.text = paragraphs[i];
            yield return FadeTo(1f);

            // 最后一段停留不再前进，展示鼠标表示游戏结束
            if (i == paragraphs.Length - 1)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                yield break;
            }

            yield return new WaitForSeconds(paragraphHoldTime);
            yield return FadeTo(0f);
        }
    }

    private IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null)
        {
            yield return new WaitForSeconds(0.01f);
            yield break;
        }

        float start = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, progress));

            yield return null;
        }

        canvasGroup.alpha = target;
    }
}
