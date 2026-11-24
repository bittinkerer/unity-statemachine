using Packages.Estenis.GameEvent_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_ {
  /// <summary>
  /// Runs one instance of a TransitionTable. It owns only its current state (a string), so a
  /// state change is one assignment. It knows nothing about what the states do.
  ///
  /// Why it is shaped this way (performance first): it registers one listener per transition
  /// in Awake and keeps them for its lifetime. When an event arrives the listener checks
  /// whether the transition applies to the current state. The old version unregistered and
  /// re-registered every transition (LINQ plus a new delegate and lambda each) on every state
  /// change.
  /// </summary>
  public class StateMachine : EventMonoBehaviour {
    // Bound to one transition at registration time so no lambda is created per event.
    private sealed class Listener {
      private readonly StateMachine _machine;
      private readonly Transition _transition;
      public readonly ActionWrapper<GameObject, object> Wrapper;

      public Listener( StateMachine machine, Transition transition ) {
        _machine = machine;
        _transition = transition;
        Wrapper = new ActionWrapper<GameObject, object>( transition.Id, Handle );
      }

      private void Handle( GameObject sender, object data ) => _machine.OnTransitionEvent( _transition, data );
    }

    public string                             _currentState;
    public TransitionTable                    _transitionTable;
    [SerializeField] private GameEventObject  _onStateChangedEvent;
    [SerializeField] private GameEventObject  _afterStateChangedEvent;
    [SerializeField] private bool             _logStateTransition;

    private bool                              _transitionDonePerFrame;  // only allow one transition per frame
    [System.NonSerialized] private Listener[] _listeners;
    [System.NonSerialized] private int         _registeredEventId;

    private void Awake( ) {
      _currentState = _transitionTable.InitState;
      var transitions = _transitionTable.Transitions;
      _registeredEventId = EventId;
      _listeners = new Listener[transitions.Length];
      for ( int i = 0; i < transitions.Length; i++ ) {
        _listeners[i] = new Listener( this, transitions[i] );
        transitions[i].TransitionEvent.Register( _registeredEventId, _listeners[i].Wrapper );
      }
    }

    private void OnTransitionEvent( in Transition transition, object data ) {
      if ( _transitionDonePerFrame ) return; // only one transition per frame allowed
      if ( !_transitionTable.AppliesTo( _currentState, transition ) ) return; // not from the current state

      if ( _currentState == transition.NextState ) {
        Debug.LogWarning(
          $"Trying to transition through same states is NOT supported. Transition from {_currentState}->{transition.NextState} cancelled." );
        return;
      }

      if ( _logStateTransition ) {
        Debug.Log(
          $"Transition [{Time.time}]: {transition.TransitionEvent.name} : ({transition.CurrentState} -> {transition.NextState})" );
      }

      _currentState = transition.NextState;
      _onStateChangedEvent.Raise( EventId, this.gameObject, _currentState );
      if ( _afterStateChangedEvent != null ) {
        _afterStateChangedEvent.Raise( EventId, this.gameObject, data ); //< event with data
      }
      _transitionDonePerFrame = true;
    }

    private void LateUpdate( ) {
      _transitionDonePerFrame = false;
    }

    private void OnDisable( ) {
      _currentState = _transitionTable.InitState;
      _onStateChangedEvent.Raise( EventId, this.gameObject, _currentState );
    }

    private void OnDestroy( ) {
      if ( _listeners == null ) {
        return;
      }
      var transitions = _transitionTable.Transitions;
      for ( int i = 0; i < _listeners.Length; i++ ) {
        transitions[i].TransitionEvent.Unregister( _registeredEventId, _listeners[i].Wrapper );
      }
      _listeners = null;
    }
  }
}
