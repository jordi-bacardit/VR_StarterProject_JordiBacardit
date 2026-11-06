using System.Collections;
using UnityEngine;

public enum SpecialEffect { None, SlowMotion, FastForward, SwapWeapons }

// Velocitat global del joc (cubs i ritme d'aparició). No toca Time.timeScale perquè les mans
// d'AutoHand van per física i es tornarien lentes i estranyes.
public static class GameSpeed
{
    private static float multiplier = 1f;
    private static float endTime = 0f;

    public static float Multiplier => Time.time < endTime ? multiplier : 1f;
    public static float RemainingTime => Mathf.Max(0f, endTime - Time.time);

    public static void Apply(float value, float duration)
    {
        multiplier = value;
        endTime = Time.time + duration;
    }

    public static void Reset()
    {
        multiplier = 1f;
        endTime = 0f;
    }
}

// Aplica els efectes dels cubs especials. Es crea sol la primera vegada que es fa servir.
public class SpecialEffects : MonoBehaviour
{
    private static SpecialEffects instance;

    public static SpecialEffects Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("SpecialEffects").AddComponent<SpecialEffects>();
            return instance;
        }
    }

    [Header("Velocitat")]
    public float slowMultiplier = 0.5f;
    public float fastMultiplier = 1.6f;
    public float speedEffectDuration = 6f;

    [Header("Intercanvi d'armes")]
    public float swapDuration = 8f;

    private Coroutine swapRoutine;
    private bool swapped = false;

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void Activate(SpecialEffect effect, Vector3 position)
    {
        switch (effect)
        {
            case SpecialEffect.SlowMotion:
                GameSpeed.Apply(slowMultiplier, speedEffectDuration);
                AudioManager.Play(SfxEvent.SlowMotion, position);
                break;

            case SpecialEffect.FastForward:
                GameSpeed.Apply(fastMultiplier, speedEffectDuration);
                AudioManager.Play(SfxEvent.FastForward, position);
                break;

            case SpecialEffect.SwapWeapons:
                AudioManager.Play(SfxEvent.SwapWeapons, position);
                if (swapRoutine != null)
                    StopCoroutine(swapRoutine);
                swapRoutine = StartCoroutine(SwapForSeconds());
                break;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ShowEffect(effect);
    }

    // Intercanvia la pistola i l'espasa de mà durant uns segons i després les torna on eren
    private IEnumerator SwapForSeconds()
    {
        if (!swapped)
        {
            SwapAllWeapons();
            swapped = true;
        }

        yield return new WaitForSeconds(swapDuration);

        SwapAllWeapons();
        swapped = false;
        swapRoutine = null;
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ShowEffect(SpecialEffect.None);
    }

    private static void SwapAllWeapons()
    {
        var attachers = FindObjectsByType<HandAttacher>(FindObjectsSortMode.None);
        // Primer es decideix on va cadascuna i després es fan les agafades
        var targets = new HandSide[attachers.Length];
        for (int i = 0; i < attachers.Length; i++)
            targets[i] = attachers[i].CurrentHand == HandSide.Right ? HandSide.Left : HandSide.Right;
        for (int i = 0; i < attachers.Length; i++)
            attachers[i].SetHand(targets[i]);
    }

    // Deixa-ho tot normal (inici de partida)
    public void ResetAll()
    {
        GameSpeed.Reset();
        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
            swapRoutine = null;
            if (swapped)
                SwapAllWeapons();
        }
        swapped = false;
    }
}
