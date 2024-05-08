using Packages.Estenis.GameEvent_;
using Packages.Estenis.ScriptableObjectsData_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    public class StateMachine : EventMonoBehaviour
    {
        private bool _transitionDonePerFrame;  // only allow one transition per frame
        public State _currentState;
        public TransitionTable _transitionTable;
        [SerializeField] private GameEventObject _onStateChangedEvent;
        [SerializeField] private ObjectSOData _stateData;
        [SerializeField] private bool _logStateTransition;


        private void Awake()
        {
            _currentState = _transitionTable.InitialState;
            _transitionTable.Register(EventId, _transitionTable_OnTransition);
        }


        private void OnDisable()
        {
            _currentState = _transitionTable.InitialState; 
            _onStateChangedEvent.Raise(EventId, this, _currentState);
            _transitionTable.OnStateChanged(EventId, _transitionTable.InitialState, _transitionTable_OnTransition);
        }

        private void OnDestroy()
        {
            // unregister...
            _transitionTable.Unregister(EventId, _transitionTable_OnTransition);
        }

        private void _transitionTable_OnTransition(object sender, object data)
        {
            if (_transitionDonePerFrame) return;

            if (sender is not Transition transition)
            {
                Debug.LogError($"{nameof(sender)} needs to be of type, '{nameof(Transition)}'.");
                return;
            }

            if (_currentState.name == transition.NextState.name)
            {
                Debug.LogWarning($"Trying to transition through same states is NOT supported. Transition from {_currentState.name}->{transition.NextState.name} cancelled.");
                return;
            }

            if (_logStateTransition)
            {
                Debug.Log($"Transition [{Time.time}]: {transition.CurrentState.name} -> {transition.NextState.name}");
            }

            // Set state data
            if (_stateData != null)
            {
                _stateData.Data = data;
            }

            _currentState = transition.NextState;
            _onStateChangedEvent.Raise(EventId, this, _currentState);
            _transitionTable.OnStateChanged(EventId, transition.NextState, _transitionTable_OnTransition);
            _transitionDonePerFrame = true;

        }

        private void LateUpdate()
        {
            _transitionDonePerFrame = false;
        }

    }
}
