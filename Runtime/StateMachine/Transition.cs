using Packages.Estenis.GameData_;
using Packages.Estenis.GameEvent_;
using System;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    [Serializable]
    public class Transition : Transition<GameData>
    {
        public Transition(State currentState, State nextState, GameEvent<GameData> gameEvent, EventHandlerWrapper<GameData> onTransition)
            : base(currentState, nextState, gameEvent, onTransition)
        { }
    }

    [Serializable]
    public class Transition<T> where T : GameData
    {
        public State CurrentState { get; }
        public State NextState { get; }
        public GameEvent<T> TransitionEvent { get; }
        public EventHandlerWrapper<T> OnTransition { get; }

        public Transition(State currentState, State nextState, GameEvent<T> gameEvent, EventHandlerWrapper<T> onTransition)
        {
            if (gameEvent == null)
            {
                Debug.LogWarning($"transition from {currentState.name} to {nextState.name}");
                throw new ArgumentNullException($"{nameof(gameEvent)}");
            }

            this.CurrentState = currentState;
            this.NextState = nextState;
            this.TransitionEvent = gameEvent;
            this.OnTransition = onTransition;
        }

        ~Transition() =>
            DeactivateEvent();

        public void Activate(int clientId) =>
            this.TransitionEvent.Register(clientId, OnEventRaised);

        public void Deactivate(int clientId) =>
            this.TransitionEvent.Unregister(clientId, OnEventRaised);

        private void DeactivateEvent() =>
            this.TransitionEvent.Unregister(OnEventRaised);

        /// <summary>
        /// This will remove the event handler completely
        /// </summary>
        /// <param name="clientId"></param>
        public void Remove(int clientId)
        {
            OnTransition.RemoveHandler(clientId);
        }

        private void OnEventRaised(T data)
        {
            OnTransition?.Invoke(this, data);
        }

        public bool IsActive(int clientId) =>
            this.TransitionEvent.IsActive(clientId, OnEventRaised);
    }
}
