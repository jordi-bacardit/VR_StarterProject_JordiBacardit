using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Al "Cube Spawn Manager": genera els cubs per onades (waves). Cada onada té una dificultat ben definida
// (ritme, velocitat, tipus de cub, cubs dobles i cubs especials) i la dificultat puja d'una a la següent.
public class CubeSpawner : MonoBehaviour
{
    public enum CubeTypes { OnlyOneType, Both }

    [Serializable]
    public class Wave
    {
        public string name = "Wave";
        [Tooltip("Durada en segons (l'última onada dura fins al final de la partida)")]
        public float duration = 20f;

        [Header("Ritme")]
        [Tooltip("Segons entre aparicions (aleatori entre min i max)")]
        public float minInterval = 3f;
        public float maxInterval = 4f;
        [Tooltip("Probabilitat (0-1) que surtin dos cubs alhora")]
        [Range(0f, 1f)] public float doubleSpawnChance = 0f;

        [Header("Velocitat (nivell de moviment del CubePhysics)")]
        [Tooltip("Nivell a l'inici de l'onada; puja fins a 'endSpeedLevel' al llarg de l'onada")]
        public int startSpeedLevel = 1;
        public int endSpeedLevel = 1;

        [Header("Tipus de cub")]
        public CubeTypes cubeTypes = CubeTypes.Both;

        [Header("Cubs especials")]
        [Range(0f, 1f)] public float specialCubeChance = 0f;
        public bool allowSlowCube = false;
        public bool allowFastCube = false;
        public bool allowSwapCube = false;
    }

    [Header("Cubs")]
    [SerializeField] private GameObject[] cubePrefabs;
    [Tooltip("Si es deixa buit, agafa els fills que es diuen SpawnPoint...")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float startDelay = 2f;

    [Header("Onades")]
    [SerializeField] private Wave[] waves = DefaultWaves();

    public int CurrentWaveIndex { get; private set; } = -1;
    public Wave CurrentWave => CurrentWaveIndex >= 0 && CurrentWaveIndex < waves.Length ? waves[CurrentWaveIndex] : null;
    public int CurrentLevel { get; private set; } = 1;

    // Avisa quan comença una onada nova (index, onada)
    public event Action<int, Wave> OnWaveStarted;

    private Coroutine spawnRoutine;
    private int lastSpawnIndex = -1;
    private GameObject gunPrefab;
    private GameObject swordPrefab;
    private GameObject singleTypePrefab; // tipus únic de la wave 1, triat a l'atzar cada partida

    private static Wave[] DefaultWaves()
    {
        return new[]
        {
            new Wave
            {
                name = "Wave 1 - Introducció",
                duration = 20f,
                minInterval = 3f, maxInterval = 4f,
                doubleSpawnChance = 0f,
                startSpeedLevel = 1, endSpeedLevel = 1,
                cubeTypes = CubeTypes.OnlyOneType,
            },
            new Wave
            {
                name = "Wave 2 - Combinació",
                duration = 25f,
                minInterval = 2f, maxInterval = 2.8f,
                doubleSpawnChance = 0.35f,
                startSpeedLevel = 2, endSpeedLevel = 3,
                cubeTypes = CubeTypes.Both,
            },
            new Wave
            {
                name = "Wave 3 - Ritme i cubs nous",
                duration = 45f,
                minInterval = 1.2f, maxInterval = 1.8f,
                doubleSpawnChance = 0.5f,
                startSpeedLevel = 4, endSpeedLevel = 6,
                cubeTypes = CubeTypes.Both,
                specialCubeChance = 0.25f,
                allowSlowCube = true, allowFastCube = true, allowSwapCube = true,
            },
        };
    }

    void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            var found = new List<Transform>();
            foreach (Transform child in transform)
                if (child.name.StartsWith("SpawnPoint"))
                    found.Add(child);
            spawnPoints = found.ToArray();
        }

        if (waves == null || waves.Length == 0)
            waves = DefaultWaves();

        // Identifica quin prefab és de pistola i quin d'espasa
        if (cubePrefabs != null)
        {
            foreach (var prefab in cubePrefabs)
            {
                if (prefab == null) continue;
                if (prefab.GetComponent<CubeExplode>() != null) gunPrefab = prefab;
                else if (prefab.GetComponent<CubeSlice>() != null) swordPrefab = prefab;
            }
        }
    }

    void Start()
    {
        StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        if (cubePrefabs == null || cubePrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: falten prefabs o spawn points al CubeSpawner.", this);
            yield break;
        }

        // Wave 1: un sol tipus de cub, triat a l'atzar
        var available = new List<GameObject>();
        if (gunPrefab != null) available.Add(gunPrefab);
        if (swordPrefab != null) available.Add(swordPrefab);
        singleTypePrefab = available.Count > 0 ? available[Random.Range(0, available.Count)] : cubePrefabs[0];

        yield return new WaitForSeconds(startDelay);

        for (int w = 0; w < waves.Length; w++)
        {
            CurrentWaveIndex = w;
            Wave wave = waves[w];
            bool lastWave = w == waves.Length - 1;
            OnWaveStarted?.Invoke(w, wave);

            float elapsed = 0f;
            while (lastWave || elapsed < wave.duration)
            {
                // La velocitat puja al llarg de l'onada
                float progress = wave.duration > 0f ? Mathf.Clamp01(elapsed / wave.duration) : 1f;
                CurrentLevel = Mathf.RoundToInt(Mathf.Lerp(wave.startSpeedLevel, wave.endSpeedLevel, progress));

                int count = Random.value < wave.doubleSpawnChance ? 2 : 1;
                SpawnGroup(wave, count);

                // Els cubs especials de velocitat també afecten el ritme d'aparició
                float interval = Random.Range(wave.minInterval, wave.maxInterval) / Mathf.Max(0.1f, GameSpeed.Multiplier);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }
    }

    private void SpawnGroup(Wave wave, int count)
    {
        int firstIndex = -1;
        for (int i = 0; i < count; i++)
        {
            int index = PickSpawnPoint(firstIndex);
            if (i == 0) firstIndex = index;
            SpawnCube(wave, spawnPoints[index]);
        }
    }

    // Spawn point a l'atzar, sense repetir l'anterior ni el de l'altre cub del mateix grup
    private int PickSpawnPoint(int exclude)
    {
        if (spawnPoints.Length == 1)
            return 0;

        int index = Random.Range(0, spawnPoints.Length);
        for (int tries = 0; tries < spawnPoints.Length && (index == lastSpawnIndex || index == exclude); tries++)
            index = (index + 1) % spawnPoints.Length;
        lastSpawnIndex = index;
        return index;
    }

    private void SpawnCube(Wave wave, Transform point)
    {
        GameObject prefab = wave.cubeTypes == CubeTypes.OnlyOneType
            ? singleTypePrefab
            : cubePrefabs[Random.Range(0, cubePrefabs.Length)];

        var cube = Instantiate(prefab, point.position, point.rotation);

        var physics = cube.GetComponent<CubePhysics>();
        if (physics != null)
            physics.movementLevel = CurrentLevel;

        // Cub especial
        if (wave.specialCubeChance > 0f && Random.value < wave.specialCubeChance)
        {
            var effect = PickSpecialEffect(wave);
            if (effect != SpecialEffect.None)
                cube.AddComponent<SpecialCube>().Init(effect);
        }
    }

    private static SpecialEffect PickSpecialEffect(Wave wave)
    {
        var options = new List<SpecialEffect>(3);
        if (wave.allowSlowCube) options.Add(SpecialEffect.SlowMotion);
        if (wave.allowFastCube) options.Add(SpecialEffect.FastForward);
        if (wave.allowSwapCube) options.Add(SpecialEffect.SwapWeapons);
        return options.Count > 0 ? options[Random.Range(0, options.Count)] : SpecialEffect.None;
    }
}
