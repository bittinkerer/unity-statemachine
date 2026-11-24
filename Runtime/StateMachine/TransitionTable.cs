using System.Collections.Generic;
using UnityEngine;
using System;

namespace Packages.Estenis.StateMachine_ {
  /// <summary>
  /// Authoring asset: states (plain strings) and the game event that moves between them,
  /// edited with the TransitionTable inspector. At runtime it is only a read-only, flat list of
  /// transitions shared by every StateMachine that uses it (its own plus its base tables');
  /// it owns no per-instance state.
  ///
  /// Why it is shaped this way (performance first): the old table re-registered a fresh set of
  /// wrapper delegates and lambdas, through LINQ, on every state change of every machine.
  /// Now each machine registers its handlers once and filters by its current state when an
  /// event arrives, so a state change is a single string assignment. The table's job is
  /// just to hand out the flat <see cref="Transitions"/> array, built once in Initialize.
  /// </summary>
  [CreateAssetMenu( menuName = "EventsFSM/TransitionTable" )]
  public sealed class TransitionTable : TransitionTableBase {
    public State InitialState { get; private set; }
    public string InitState { get; private set; }

    /// <summary>Every transition from every state, base tables included. Read-only; do not modify.</summary>
    public Transition[] Transitions { get; private set; } = Array.Empty<Transition>();

    public override void Initialize( string initialState, List<StateToStateTransition2> stateToState ) {
      InitState = initialState;

      var list = new List<Transition>();
      var seen = new HashSet<Transition>();

      // main transition-table transitions
      Add( stateToState, list, seen );

      // parent transition-table's transitions
      for ( int i = 0; i < _baseTables.Count; i++ ) {
        if ( _baseTables[i] != null ) {
          Add( _baseTables[i]._stateToStateEntries2, list, seen );
        }
      }
      Transitions = list.ToArray();
    }

    private void Add( List<StateToStateTransition2> entries, List<Transition> into, HashSet<Transition> seen ) {
      if ( entries == null ) {
        return;
      }
      for ( int i = 0; i < entries.Count; i++ ) {
        var entry = entries[i];
        if ( !entry.IsValid() ) {
          continue;
        }
        if ( entry.GameEvent == null ) {
          // the old code threw here and left the whole table half-built
          Debug.LogWarning( $"[{name}] transition {entry.FromState} -> {entry.ToState} has no GameEvent; skipped.", this );
          continue;
        }
        var transition = new Transition( entry.FromState, entry.ToState, entry.GameEvent );
        if ( seen.Add( transition ) ) { // same de-duplication the per-state HashSet gave
          into.Add( transition );
        }
      }
    }

    /// <summary>True when a machine in <paramref name="current"/> may take <paramref name="t"/>.</summary>
    public bool AppliesTo( string current, in Transition t ) =>
      t.CurrentState == current || t.CurrentState == ANY_STATE_NAME;

    /// <summary>Remove the client's handlers from every transition.</summary>
    public void Unregister( int instanceId, Action<object, object> action ) {
      for ( int i = 0; i < Transitions.Length; i++ ) {
        var t = Transitions[i];
        t.TransitionEvent.Unregister(
          instanceId,
          new ActionWrapper<GameObject, object>( t.Id, ( sender, data ) => action( t, data ) ) );
      }
    }

    /// <summary>
    /// Kept for source compatibility; does nothing now. Handlers stay registered and the
    /// client filters by its current state, so there is nothing to re-register on a state change.
    /// </summary>
    public void OnStateChanged( int instanceId, string newState, Action<object, object> action ) { }

    public void AddBaseTable( TransitionTable baseTable ) {
      _baseTables.Add( baseTable );
    }
  }
}
