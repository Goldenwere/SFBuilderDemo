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
        [SerializeField] private GoalContainer[]    goalPresetsEasy;
        [SerializeField] private GoalContainer[]    goalPresetsHard;
#pragma warning restore 0649
        /**************/ private bool               canMoveOn;
        /**************/ private bool               uiWasSetUp;
        #endregion
        #region Properties
        /// For the sake of cleaner code, make goals.Length - 1 and goals.Length + 1 properties
        private int                 GoalsLengthMinusOne { get { return goals.Length - 1; } }
        private int                 GoalsLengthPlusOne  { get { return goals.Length + 1; } }

        /// <summary>
        /// Calculates what the current goal viability is when the game first loads from a save
        /// </summary>
        private int                 LoadedGoalViability
        {
            get
            {
                if (CurrentGoal < goals.Length)
                    return goals[CurrentGoal].goalViability;
                else if (CurrentGoal <= (int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard))
                    return goals[GoalsLengthMinusOne].goalViability + ((CurrentGoal - GoalsLengthPlusOne) * GameConstants.InfiniPlayEasyViabilityIncrease);
                else
                    return goals[GoalsLengthMinusOne].goalViability
                        + (((int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard) - GoalsLengthPlusOne) * GameConstants.InfiniPlayEasyViabilityIncrease)
                        + ((CurrentGoal - ((int)(GoalsLengthMinusOne * GameConstants.InfiniPlayFromEasyToHard) + 1)) * GameConstants.InfiniPlayHardViabilityIncrease);
            }
        }
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

        /// <summary>
        /// The previous goal's viability
        /// </summary>
        public float                PreviousGoalViability
        {
            get
            {
                // no previous viability
                if (CurrentGoal == 0)
                    return 0;
                // From goal 1 to the first easy-infini, return saved goal viability
                else if (CurrentGoal <= goals.Length)
                    return goals[CurrentGoal - 1].goalViability;
                // From 2nd easy-infini to first hard-infini, return last goal viability + easy-infini * amount
                else if (CurrentGoal <= (int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard) + 1)
                    return goals[GoalsLengthMinusOne].goalViability + ((CurrentGoal - goals.Length) * GameConstants.InfiniPlayEasyViabilityIncrease);
                else
                    return goals[GoalsLengthMinusOne].goalViability
                        + (((int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard) - GoalsLengthPlusOne) * GameConstants.InfiniPlayEasyViabilityIncrease)
                        + ((CurrentGoal - ((int)(GoalsLengthMinusOne * GameConstants.InfiniPlayFromEasyToHard) + 1) - 1) * GameConstants.InfiniPlayHardViabilityIncrease);
            }
        }
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
            CurrentGoal = GameSave.Instance.currentGoal;
            if (CurrentGoal < goals.Length)
                CurrentGoalWorkingSet = GoalContainer.Copy(goals[CurrentGoal]);
            else
                if (CurrentGoal > (int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard))
                    CurrentGoalWorkingSet = GoalContainer.Copy(goalPresetsHard[GameSave.Instance.currentGoalSetIndex], LoadedGoalViability);
                else
                    CurrentGoalWorkingSet = GoalContainer.Copy(goalPresetsEasy[GameSave.Instance.currentGoalSetIndex], LoadedGoalViability);

            if (!GameSave.Instance.workingBetweenTransition)
            {
                for (int i = 0; i < GameSave.Instance.currentGoalSetCount.Length; i++)
                {
                    if (i < CurrentGoalWorkingSet.goalRequirements.Length)
                        CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount = GameSave.Instance.currentGoalSetCount[i];
                    else
                        CurrentGoalWorkingSet.goalExtras[i - CurrentGoalWorkingSet.goalRequirements.Length].goalStructureCount = GameSave.Instance.currentGoalSetCount[i];
                }
            }

            else
            {
                SetGameSaveOnNewGoal();
                GameSave.Instance.workingBetweenTransition = false;
            }

            uiWasSetUp = false;
            if (GameEventSystem.Instance.CurrentGameState == GameState.Gameplay)
                GameEventSystem.Instance.UpdatePlacementPanel();
            VerifyForNextGoal();
            GameEventSystem.Instance.NotifyLevelReadyState(CurrentGoal >= goals.Length);
        }

        /// <summary>
        /// On Enable, subscribe to events
        /// </summary>
        private void OnEnable()
        {
            GameEventSystem.GameStateChanged += OnGameStateChanged;
            GameEventSystem.GoalInfoChanged += OnGoalInfoChanged;
            GameEventSystem.LevelBanished += OnLevelBanished;
            GameEventSystem.GoalChanged += OnGoalChanged;
            GameEventSystem.LevelTransitioned += OnLevelTransitioned;
        }

        /// <summary>
        /// On Disable, unsubscribe from events
        /// </summary>
        private void OnDisable()
        {
            GameEventSystem.GameStateChanged -= OnGameStateChanged;
            GameEventSystem.GoalInfoChanged -= OnGoalInfoChanged;
            GameEventSystem.LevelBanished -= OnLevelBanished;
            GameEventSystem.GoalChanged -= OnGoalChanged;
            GameEventSystem.LevelTransitioned -= OnLevelTransitioned;
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
            {
                GameEventSystem.Instance.UpdatePlacementPanel();
                uiWasSetUp = true;
            }
        }

        /// <summary>
        /// When the NextGoal button is pressed, move to next goal if canMoveOn
        /// </summary>
        private void OnGoalChanged()
        {
            if (canMoveOn)
            {
                CurrentGoal++;
                GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoal, CurrentGoal + 1);
                GameAudioSystem.Instance.PlaySound(AudioClipDefinition.Button);
                if (CurrentGoal < goals.Length)
                {
                    CurrentGoalWorkingSet = GoalContainer.Copy(goals[CurrentGoal]);
                    GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoalMinimumViability, CurrentGoalWorkingSet.goalViability);
                    newGoal?.Invoke(CurrentGoal);
                    GameEventSystem.Instance.UpdatePlacementPanel();
                    SetGameSaveOnNewGoal();
                }
                else
                {
                    int index;
                    if (CurrentGoal > (int)(goals.Length * GameConstants.InfiniPlayFromEasyToHard))
                    {
                        index = Random.Range(0, goalPresetsHard.Length);
                        CurrentGoalWorkingSet = GoalContainer.Copy(
                            goalPresetsHard[index],
                            CurrentGoalWorkingSet.goalViability + GameConstants.InfiniPlayHardViabilityIncrease);
                    }
                    else
                    {
                        index = Random.Range(0, goalPresetsEasy.Length);
                        CurrentGoalWorkingSet = GoalContainer.Copy(
                            goalPresetsEasy[index],
                            CurrentGoalWorkingSet.goalViability + GameConstants.InfiniPlayEasyViabilityIncrease);
                    }
                    GameEventSystem.Instance.UpdatePlacementPanel();
                    GameEventSystem.Instance.NotifyLevelReadyState(true);
                    GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoalMinimumViability, CurrentGoalWorkingSet.goalViability);
                    newGoal?.Invoke(CurrentGoal);
                    SetGameSaveOnNewGoal(index);
                }
            }
        }

        /// <summary>
        /// On the GoalInfoChanged event, update the current working set
        /// </summary>
        /// <param name="isUndo">Whether to increase or decrease a goal value (undo increases, normal decreases)</param>
        /// <param name="id">The id of the BuilderObject associated with the goal</param>
        /// <param name="isRequired">Whether the BuilderObject was a requirement</param>
        private void OnGoalInfoChanged(bool isUndo, int id, bool isRequired)
        {
            if (isRequired)
            {
                int i = System.Array.IndexOf(
                    CurrentGoalWorkingSet.goalRequirements,
                    CurrentGoalWorkingSet.goalRequirements.First(g => (int)g.goalStructureID == id));
                if (isUndo)
                {
                    CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount++;
                    GameSave.Instance.currentGoalSetCount[i]++;
                }
                else
                {
                    CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount--;
                    GameSave.Instance.currentGoalSetCount[i]--;
                }
            }
            else
            {
                int i = System.Array.IndexOf(
                    CurrentGoalWorkingSet.goalExtras,
                    CurrentGoalWorkingSet.goalExtras.First(g => (int)g.goalStructureID == id));
                if (isUndo)
                {
                    CurrentGoalWorkingSet.goalExtras[i].goalStructureCount++;
                    GameSave.Instance.currentGoalSetCount[i + CurrentGoalWorkingSet.goalRequirements.Length]++;
                }
                else
                {
                    CurrentGoalWorkingSet.goalExtras[i].goalStructureCount--;
                    GameSave.Instance.currentGoalSetCount[i + CurrentGoalWorkingSet.goalRequirements.Length]--;
                }
            }
            VerifyForNextGoal();
        }

        /// <summary>
        /// On the LevelBanished event, reset GoalSystem back to first goal
        /// </summary>
        private void OnLevelBanished()
        {
            CurrentGoal = 0;
            CurrentGoalWorkingSet = GoalContainer.Copy(goals[CurrentGoal]);
            GameEventSystem.Instance.UpdatePlacementPanel();
            GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoal, CurrentGoal + 1);
            GameEventSystem.Instance.UpdateScoreUI(ScoreType.CurrentGoalMinimumViability, CurrentGoalWorkingSet.goalViability);
            SetGameSaveOnNewGoal();
        }

        /// <summary>
        /// Handler for the LevelTransitioned event
        /// </summary>
        /// <param name="isStart">Whether start of or end of transition</param>
        private void OnLevelTransitioned(bool isStart)
        {
            if (GameEventSystem.Instance.CurrentGameState == GameState.Gameplay)
            {
                GameSave.Instance.currentGoal = 0;
                GameSave.Instance.currentGoalSetIndex = 0;
                GameSave.Instance.currentHappiness = 0;
                GameSave.Instance.currentPower = 0;
                GameSave.Instance.currentSustenance = 0;
                GameSave.Instance.CurrentlyPlacedObjects.Clear();
                if (isStart)
                    GameSave.Instance.workingBetweenTransition = true;
            }
        }

        /// <summary>
        /// Sets GameSave info when a new goalset is made
        /// </summary>
        /// <param name="index">Indicates which index of corresponding preset collection when beyond the hard-coded goals (left at 0 if not beyond said goals)</param>
        private void SetGameSaveOnNewGoal(int index = 0)
        {
            int[] saveGoalSet = new int[CurrentGoalWorkingSet.goalRequirements.Length + CurrentGoalWorkingSet.goalExtras.Length];
            int lengthOfReq = CurrentGoalWorkingSet.goalRequirements.Length;
            int lengthOfExt = CurrentGoalWorkingSet.goalExtras.Length;
            for (int i = 0; i < lengthOfReq; i++)
                saveGoalSet[i] = CurrentGoalWorkingSet.goalRequirements[i].goalStructureCount;
            for (int i = lengthOfReq; i < lengthOfReq + lengthOfExt; i++)
                saveGoalSet[i] = CurrentGoalWorkingSet.goalExtras[i - lengthOfReq].goalStructureCount;
            GameSave.Instance.currentGoalSetCount = saveGoalSet;
            GameSave.Instance.currentGoal = CurrentGoal;
            GameSave.Instance.currentGoalSetIndex = index;
        }
        #endregion
    }
}