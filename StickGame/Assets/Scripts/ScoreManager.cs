using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    enum PerfectnessType {
        MEH,
        OK,
        GOOD,
        GREAT,
        PERFECT,
        MARVELOUS
    }

    [Serializable] class PerfectnessPair {
        public PerfectnessType type;
        public float score;
        public string displayText;
    }

    public static ScoreManager instance;
    [Header("Prefabs")]
    [SerializeField] GameObject floatingTextPrefab;
    [Header("Objects")]
    [SerializeField] GameObject player;
    [SerializeField] Canvas canvas;
    [SerializeField] TextMeshProUGUI scoreText;
    [Header("Setting")]
    [SerializeField] int scorePerMove = 100;
    [SerializeField] List<PerfectnessPair> perfectnessAmounts;
    [SerializeField] Vector2 floatingTextSpawnPosition = new Vector2(-160, 130);
    [Header("Debug")]
    [SerializeField] int score = 0;
    void Start()
    {
        if (instance != null) throw new System.Exception("Multiple instances of ScoreManager created");
        instance = this;
        UpdateScoreText();
    }

    private void OnDestroy() {
        instance = null;
    }

    public void SubmitMove(float distance, float currentPlayerSpeedFactor, bool firstPass) {
        float maxDist = LevelManager.instance.GetMaxDistance();
        float perfectness = (maxDist - distance) / maxDist;
        

        int scoreGained = (int)Mathf.Ceil((.5f + perfectness / 2) * scorePerMove);
        if (firstPass) scoreGained *= 2;
        scoreGained = (int)Mathf.Ceil(scoreGained * currentPlayerSpeedFactor);
        

        PerfectnessPair perfectnessPair = GetPerfectness(perfectness);

        Debug.Log("Move Made:" +
            "\nScoreGained:" + scoreGained + 
            "\nSpeedFactor:" + currentPlayerSpeedFactor +
            "\nPerfectness:" + perfectness +
            "\nPerfectnessType:" + perfectnessPair.type);

        score += scoreGained;

        UpdateScoreText();
        SpawnText(perfectnessPair, scoreGained);

        
    }

    private PerfectnessPair GetPerfectness(float perfectness) {
        float highestFound = -1;
        PerfectnessPair typeFound = null;
        foreach (PerfectnessPair pair in perfectnessAmounts) {
            if (pair.score > perfectness || pair.score< highestFound) continue;
            highestFound = pair.score;
            typeFound = pair;
        }
        return typeFound;
    }

    private void UpdateScoreText() {
        scoreText.text = "SCORE:" + score;
    }

    private void SpawnText(PerfectnessPair perfectness, int scoreGained) {
        GameObject spawned = Instantiate(floatingTextPrefab, canvas.transform);
        spawned.transform.localPosition = floatingTextSpawnPosition;

        spawned.GetComponent<TextMeshProUGUI>().text = 
            perfectness.displayText + 
            "\n+"+scoreGained;
    }
}
