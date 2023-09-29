using Packages.Estenis.GameEvent_;
using System;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{

    [Serializable]
    public class Transition
    {
        public State CurrentState { get; }
        public State NextState { get; }
        public GameEventObject TransitionEvent { get; }

        public Transition(State currentState, State nextState, GameEventObject gameEvent)
        {
            if (gameEvent == null)
            {
                Debug.LogWarning($"transition from {currentState.name} to {nextState.name}");
                throw new ArgumentNullException($"{nameof(gameEvent)}");
            }

            this.CurrentState = currentState;
            this.NextState = nextState;
            this.TransitionEvent = gameEvent;
        }

        
        //public bool IsActive(int clientId) =>
        //    this.TransitionEvent.IsActive(clientId, OnEventRaised);
    }
}
