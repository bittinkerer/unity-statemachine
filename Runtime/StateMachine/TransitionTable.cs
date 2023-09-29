using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Packages.Estenis.UnityExts_;
using System;

namespace Packages.Estenis.StateMachine_
{
    [CreateAssetMenu(menuName = "EventsFSM/TransitionTable")]
    public sealed class TransitionTable : TransitionTableBase
    {
        public State AnyState { get; private set; }
        public State InitialState { get; private set; }
        private Dictionary<State, HashSet<Transition>> _transitions = new();

        protected override void Initialize(State initialState, List<StateToStateTransition> stateToState)
        {
            InitialState = initialState;

            foreach (var entry in stateToState?.Where(e => e.IsValid()))
            {
                State from      = entry.FromState;
                State to        = entry.ToState;
                var gameEvent   = entry.GameEvent;

                var transition  = new Transition(from, to, gameEvent);
                AddTransition(from, transition);
            }
        }

        public void Register(int clientId, Action<object, object> action)
        {
            foreach (var transition in _transitions.SelectMany(t => t.Value).Where(t => t.CurrentState == InitialState || t.CurrentState == _anyState))
            {
                // NOTE: must override sender (original object that triggered event) with Transition for StateMachine
                transition.TransitionEvent.Register(clientId, (sender,data) => action(transition, data)); 
            } 
        }

        /// <summary>
        /// Remove the client handler from all transitions 
        /// </summary>
        /// <param name="clientId"></param>
        public void Unregister(int clientId, Action<object, object> action)
        {
            foreach (var transition in _transitions.SelectMany(t => t.Value))
            {
                transition.TransitionEvent.Unregister(clientId, (sender, data) => action(transition, data));
            }
        }

        public void OnStateChanged(int clientId, State newState, Action<object, object> action)
        {
            // Deactivate all except AnyState
            foreach (var transition in _transitions.Where(t => t.Key != _anyState).Select(kvp => kvp.Value).SelectMany(v => v))
            {
                transition.TransitionEvent.Unregister(clientId, (sender, data) => action(transition, data));
            }

            // Activate current
            _transitions
                .Where(kvp => kvp.Key == newState)
                .SelectMany(kvp => kvp.Value)
                .ForEach(t => t.TransitionEvent.Register(clientId, (sender, data) => action(t, data)));
            
        }

        private void AddTransition(State from, Transition transition)
        {
            if (!_transitions.TryGetValue(from, out var transitions))
            {
                transitions = new HashSet<Transition>();
                _transitions[from] = transitions;
            }

            transitions.Add(transition); // transitions is a reference to the HashSet inside the _transitions dictionary
        }
    }

}
