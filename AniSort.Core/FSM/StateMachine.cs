using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AniSort.Core.FSM
{
    /// <summary>
    /// Functional Finite State Machine that allows configuring of state changes and actions to perform in each state as well as an entry feed for values
    /// </summary>
    /// <typeparam name="TState">Enum representing the state of the machine</typeparam>
    /// <typeparam name="TIngest">Ingest value of the state machine</typeparam>
    public class IngestingStateMachine<TState, TIngest> where TState : Enum
    {
        /// <summary>
        /// Ingest processing action delegate
        /// </summary>
        /// <param name="ingest">Value to ingest</param>
        /// <returns>The state of the machine after this ingest</returns>
        public delegate TState OnIngest(TIngest ingest);

        /// <summary>
        /// State entry action delegate
        /// </summary>
        /// <param name="previousState">The previous state that the FSM is transitioning from</param>
        public delegate void OnEntry(TState previousState);

        /// <summary>
        /// State exit action delegate
        /// </summary>
        /// <param name="nextState">The next state that the FSM is about to transition to</param>
        public delegate void OnExit(TState nextState);
        
        private TState currentState;

        private Dictionary<TState, OnIngest> ingestActions = new Dictionary<TState, OnIngest>();
        private Dictionary<TState, OnEntry> entryActions = new Dictionary<TState, OnEntry>();
        private Dictionary<TState, OnExit> exitActions = new Dictionary<TState, OnExit>();

        /// <summary>
        /// Default action to execute if no handler for the state is found. If this isn't set the FSM will throw when it doesn't have an appropriate action setup for a state
        /// </summary>
        private Func<TIngest, TState> DefaultAction { get; set; } = null;

        public IngestingStateMachine(TState currentState)
        {
            this.currentState = currentState;
        }

        public void Ingest(TIngest ingest)
        {
            if (ingestActions.ContainsKey(currentState))
            {
                var newState = ingestActions[currentState](ingest);

                if (!Equals(newState, currentState))
                {
                    

                    currentState = newState;
                }
            }
        }

        /// <summary>
        /// Register an action to be run for a specific state
        /// </summary>
        /// <param name="state">The state to register for</param>
        /// <param name="action">The action to execute for the state</param>
        public void RegisterAction(TState state, [NotNull] OnIngest action)
        {
            ingestActions[state] = action;
        }

        /// <summary>
        /// Register an entry action for a state
        /// </summary>
        /// <param name="state">The state to register the entry action for</param>
        /// <param name="action">The action to execute</param>
        public void RegisterEntryAction(TState state, [NotNull] OnEntry action)
        {
            entryActions[state] = action;
        }

        /// <summary>
        /// Register an exit action for a state
        /// </summary>
        /// <param name="state">The state to register the exit action for</param>
        /// <param name="action">The action to execute</param>
        public void RegisterExitAction(TState state, [NotNull] OnExit action)
        {
            exitActions[state] = action;
        }
    }
}