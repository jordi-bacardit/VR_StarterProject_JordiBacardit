using UnityEngine;

// Cub especial (wave 3): el CubeSpawner l'afegeix a un GunCube o SwordCube normal.
// Es veu de color diferent, brilla i fa pulsacions; quan es destrueix amb l'arma correcta activa el seu efecte.
//   Verd    → el joc va més lent
//   Groc    → el joc va més ràpid
//   Magenta → s'intercanvien l'espasa i la pistola de mà durant uns segons
// La icona del cub continua indicant quina arma el destrueix.
public class SpecialCube : MonoBehaviour
{
    // El shader multiplica l'hologram per _HologramEmissionPower (7.88) i sense HDR cada canal es talla a 1.
    // Per això tots els colors tenen canals a 0: així mantenen el to i no es tornen blancs.
    // Colors dels cubs normals (_HologramColor i _FrameColor de GunCube.mat i SwordCubeMat.mat)
    public static readonly Color GunColor = new Color(1f, 0f, 0f);
    public static readonly Color SwordColor = new Color(0f, 1f, 1f);

    public SpecialEffect Effect { get; private set; }

    private Vector3 baseScale;
    private Transform pulseTarget;

    public static Color ColorFor(SpecialEffect effect)
    {
        switch (effect)
        {
            case SpecialEffect.SlowMotion: return new Color(0f, 1f, 0f);
            case SpecialEffect.FastForward: return new Color(1f, 1f, 0f);
            case SpecialEffect.SwapWeapons: return new Color(1f, 0f, 1f);
            default: return Color.white;
        }
    }

    public void Init(SpecialEffect effect)
    {
        Effect = effect;
        Color color = ColorFor(effect);

        // Tint de la part hologràfica del cub (icona de pistola/espasa)
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in r.materials)
            {
                if (mat.HasProperty("_HologramColor"))
                    mat.SetColor("_HologramColor", color);
                if (mat.HasProperty("_HologramEmissionPower"))
                    mat.SetFloat("_HologramEmissionPower", Mathf.Max(mat.GetFloat("_HologramEmissionPower"), 4f));
                // El marc també agafa el color (l'alfa indica que substitueix el taronja de la textura)
                if (mat.HasProperty("_FrameColor"))
                    mat.SetColor("_FrameColor", color);
            }
        }

        // Llum de color perquè es distingeixi de lluny
        var lightGo = new GameObject("SpecialLight");
        lightGo.transform.SetParent(transform, false);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = 1.5f;
        light.intensity = 3f;

        // Pulsació sobre el model visual (no sobre l'arrel, per no tocar el collider)
        pulseTarget = transform.childCount > 0 && GetComponent<MeshRenderer>() == null ? transform.GetChild(0) : transform;
        baseScale = pulseTarget.localScale;
    }

    void Update()
    {
        if (pulseTarget == null)
            return;
        float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.06f;
        pulseTarget.localScale = baseScale * pulse;
    }

    // El criden CubeExplode / CubeSlice quan el cub es destrueix amb l'arma correcta
    public void Activate()
    {
        if (Effect != SpecialEffect.None)
            SpecialEffects.Instance.Activate(Effect, transform.position);
        Effect = SpecialEffect.None;
    }

    // Els materials tintats no es destrueixen aquí: les meitats tallades del cub els continuen fent servir.
    // Unity els allibera en canviar d'escena.
}
