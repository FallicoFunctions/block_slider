using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton pattern to access the GameManager from anywhere
    public static GameManager Instance { get; private set; }
    
    // Variables for tracking game state
    public bool isGameActive = false;
    public int currentLevel = 1;
    
    // Called when the script instance is being loaded
    private void Awake()
    {
        // Implement singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Makes this object persistent between scenes
        }
        else
        {
            Destroy(gameObject); // Prevents duplicate GameManagers
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Game Manager initialized!");
    }
    
    // Update is called once per frame
    void Update()
    {
        // Game loop logic will go here
    }
    
    // Function to start a new level
    public void StartLevel(int levelNumber)
    {
        currentLevel = levelNumber;
        isGameActive = true;
        Debug.Log("Starting level " + levelNumber);
        
        // Level setup code will go here
    }
    
    // Function to complete a level
    public void CompleteLevel()
    {
        isGameActive = false;
        Debug.Log("Level " + currentLevel + " completed!");
        
        // Level completion code will go here
    }
}