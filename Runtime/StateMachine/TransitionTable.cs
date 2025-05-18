using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Packages.Estenis.UnityExts_;
using System;

namespace Packages.Estenis.StateMachine_ {
  [CreateAssetMenu( menuName = "EventsFSM/TransitionTable" )]
  public sealed class TransitionTable : TransitionTableBase {
    public State                                      AnyState { get; private set; }
    public State                                      InitialState { get; private set; }
    private Dictionary<State, HashSet<Transition>>    _transitions  = new();

    public override void Initialize( State initialState, List<StateToStateTransition> stateToState ) {
      InitialState = initialState;

      // Add main transition-table transitions
      foreach ( var entry in stateToState?.Where( e => e.IsValid() ) ) {
        AddTransition( entry );
      }

      // Add parent transition-table's transitions
      foreach ( var baseTable in _baseTables ) {
        foreach ( var entry in baseTable._stateToStateEntries ) {
          AddTransition( entry );
        }
      }
    }

    private void AddTransition( StateToStateTransition stateTransition ) {
      State from      = stateTransition.FromState;
      State to        = stateTransition.ToState;
      var gameEvent   = stateTransition.GameEvent;

      var transition  = new Transition(from, to, gameEvent);
      AddTransition( from, transition );
    }

    private void AddTransition( State from, Transition transition ) {
      if ( !_transitions.TryGetValue( from, out var transitions ) ) {
        transitions = new HashSet<Transition>();
        _transitions[from] = transitions;
      }

      transitions.Add( transition ); // transitions is a reference to the HashSet inside the _transitions dictionary
    }

    /// <summary>
    /// Remove the client handler from all transitions 
    /// </summary>
    /// <param name="instanceId"></param>
    public void Unregister( int instanceId, Action<object, object> action ) {
      foreach ( var transition in _transitions.SelectMany( t => t.Value ) ) {
        transition.TransitionEvent.Unregister(
            instanceId,
            new ActionWrapper<GameObject, object>(
                transition.Id,
                ( sender, data ) => action( transition, data ) ) );
      }
    }

    public void OnStateChanged( int instanceId, string newState, Action<object, object> action ) {
      // Deactivate all except AnyState
      foreach ( var transition in _transitions
              .Where( t => t.Key.name != ANY_STATE_NAME )
              .Select( kvp => kvp.Value )
              .SelectMany( v => v ) ) {
        transition.TransitionEvent.Unregister(
            instanceId,
            new ActionWrapper<GameObject, object>(
                transition.Id,
                ( sender, data ) => action( transition, data ) ) );
      }

      // Activate current
      _transitions
          .Where( kvp => kvp.Key.name == newState || kvp.Key.name == ANY_STATE_NAME )
          .SelectMany( kvp => kvp.Value )
          .ForEach( t => t.TransitionEvent.Register(
              instanceId,
              new ActionWrapper<GameObject, object>( t.Id, ( sender, data ) => action( t, data ) ) ) );
    }

    public void AddBaseTable( TransitionTable baseTable ) {
      _baseTables.Add( baseTable );
    }
  }
}