﻿using System.Linq;
using UnityEngine;

namespace SFBuilder.Gameplay
{
    public delegate void GoalDelegate(int newGoal);

    /// <summary>
    /// Manages the goals in each level
    /// </summary>
    /// <remarks>This is a singleton that can be referenced through GoalSystem.Instance; present in each level scene except the base scene</remarks>
    public class GoalSystem : MonoBehaviour
    {
        #region Fields
#pragma warning disable 0649
        [SerializeField] private GoalContainer[]    goals;
#pragma warning restore 0649
        /**************/ private bool               canMoveOn;
        /**************/ private bool               uiWasSetUp;
        #endregion
        #region Properties
        /// <summary>
        /// The current goal level (represents index in Goals)
        /// </summary>
        public int                  CurrentGoal { get; private set; }

        /// <summary>
        /// The current goal working set (copied from Goals for further manipulation)
        /// </summary>
        public GoalContainer        CurrentGoalWorkingSet { get; private set; }

        /// <summary>
        /// Goals defined in inspector at scene level
        /// </summary>
        public GoalContainer[]      Goals { get { return goals; } }

        /// <summary>
        /// Singleton instance of GoalSystem in the game level scene
        /// </summary>
        public static GoalSystem    Instance { get; private set; }
        #endregion
        #region Events
        public static GoalDelegate  newGoal;
        #endregion
        #region Methods
        /// <summary>
        /// Set singleton on Awake
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        /// <summary>
        /// Set current working set and setup UI on Start
        /// </summary>
        private void Start()
        {
            CurrentGoalWorkingSet = goals[CurrentGoal];
            uiWasSetUp = false;
            if (GameEventSystem.Instance.CurrentGameState == GameState.Gameplay)
                GameEventSystem.Instance.UpdatePlacementPanel();
        }

        /// <summary>
        /// On Enable, subscribe to events
        /// </summary>
        private void OnEnable()
        {
            GameEventSystem.GameStateChanged += OnGameStateChanged;
            GameEventSystem.GoalChanged += OnGoalChanged;
            GameEventSystem.LevelBanished += OnLevelBanished;
        }

        /// <summary>
        /// On Disable, unsubscribe from events
        /// </summary>
        private void OnDisable()
        {
            GameEventSystem.GameStateChanged -= OnGameStateChanged;
            GameEventSystem.GoalChanged -= OnGoalChanged;
            GameEventSystem.LevelBanished -= OnLevelBanished;
        }

        /// <summary>
        /// When the NextGoal button is pressed, move to next goal if canMoveOn
        /// </summary>
        public void OnNextGoalButtonPressed()
        {
            if (canMoveOn)
            {
                CurrentGoal++;
                GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoal, CurrentGoal + 1);
                GameAudioSystem.Instance.PlaySound(AudioClipDefinition.Button);
                if (CurrentGoal < goals.Length)
                {
                    CurrentGoalWorkingSet = goals[CurrentGoal];
                    GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoalMinimumViability, CurrentGoalWorkingSet.goalViability);
                    newGoal?.Invoke(CurrentGoal);
                    GameEventSystem.Instance.UpdatePlacementPanel();
                }
                else
                {
                    LevelingSystem.Instance.CurrentLevel++;
                    // will be made redundant as there will be a scene change 
                    CurrentGoal = 0;
                    GameEventSystem.Instance.UpdatePlacementPanel(true);
                }
            }
        }

        /// <summary>
        /// Verify if able to move on (called when a BuilderObject is placed, to enable/disable the next goal button
        /// </summary>
        public void VerifyForNextGoal()
        {
            bool test = true;
            foreach (GoalItem g in CurrentGoalWorkingSet.goalRequirements)
                if (g.goalStructureCount > 0)
                    test = false;
            canMoveOn = test && GameScoring.Instance.TotalViability >= CurrentGoalWorkingSet.goalViability &&
                GameScoring.Instance.TotalHappiness > 0 && GameScoring.Instance.TotalPower > 0 && GameScoring.Instance.TotalSustenance > 0;
            GameEventSystem.Instance.UpdateGoalMetState(canMoveOn);
        }

        /// <summary>
        /// Handler for the GameStateChanged event
        /// </summary>
        /// <param name="prevState">The previous GameState</param>
        /// <param name="newState">The new GameState</param>
        private void OnGameStateChanged(GameState prevState, GameState newState)
        {
            if (newState == GameState.Gameplay && !uiWasSetUp)
                GameEventSystem.Instance.UpdatePlacementPanel();
        }

        /// <summary>
        /// On the GoalChanged event, update the current working set
        /// </summary>
        /// <param name="isUndo">Whether to increase or decrease a goal value (undo increases, normal decreases)</param>
        /// <param name="id">The id of the BuilderObject associated with the goal</param>
        /// <param name="isRequired">Whether the BuilderObject was a requirement</param>
        private void OnGoalChanged(bool isUndo, int id, bool isRequired)
        {
            if (isRequired)
            {
                int i = System.Array.IndexOf(
                    CurrentGoalWorkingSet.goalRequirements,
                    CurrentGoalWorkingSet.goalRequirements.First(g => (int)g.goalStructureID == id));
                if (isUndo)
                    CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount++;
                else
                    CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount--;
            }
            else
            {
                int i = System.Array.IndexOf(
                    CurrentGoalWorkingSet.goalExtras,
                    CurrentGoalWorkingSet.goalExtras.First(g => (int)g.goalStructureID == id));
                if (isUndo)
                    CurrentGoalWorkingSet.goalExtras[i].goalStructureCount++;
                else
                    CurrentGoalWorkingSet.goalExtras[i].goalStructureCount--;
            }
        }

        /// <summary>
        /// On the LevelBanished event, reset GoalSystem back to first goal
        /// </summary>
        private void OnLevelBanished()
        {
            CurrentGoal = 0;
            CurrentGoalWorkingSet = goals[CurrentGoal];
            uiWasSetUp = false;
            GameEventSystem.Instance.UpdatePlacementPanel();
            GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoal, CurrentGoal + 1);
        }
        #endregion
    }
}