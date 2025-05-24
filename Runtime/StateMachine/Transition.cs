using Packages.Estenis.GameEvent_;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.Estenis.StateMachine_ {

  [Serializable]
  public readonly struct Transition : IEquatable<Transition>, IEqualityComparer<Transition> {
    public string CurrentState { get; }
    public string NextState { get; }
    public GameEventObject TransitionEvent { get; }
    public Guid Id { get; }

    public Transition( string currentState, string nextState, GameEventObject gameEvent ) {
      if ( gameEvent == null ) {
        Debug.LogWarning( $"transition from {currentState} to {nextState}" );
        throw new ArgumentNullException( $"{nameof( gameEvent )}" );
      }

      this.CurrentState = currentState;
      this.NextState = nextState;
      this.TransitionEvent = gameEvent;
      this.Id = Guid.NewGuid();
    }

    public static bool operator ==( Transition lhs, Transition rhs ) =>
        ( (IEquatable<Transition>) lhs ).Equals( rhs );

    public static bool operator !=( Transition lhs, Transition rhs ) =>
        !( lhs == rhs );

    public bool Equals( Transition x, Transition y ) =>
        x == y;

    public bool Equals( Transition other ) =>
        this.CurrentState == other.CurrentState &&
        this.NextState == other.NextState &&
        TransitionEvent.name == other.TransitionEvent.name;

    public override int GetHashCode( ) =>
        HashCode.Combine(
            this.CurrentState.GetHashCode(),
            this.NextState.GetHashCode(),
            this.TransitionEvent.GetHashCode() );

    public int GetHashCode( Transition obj ) =>
        obj.GetHashCode();

    public override bool Equals( object obj ) =>
        this.Equals( (Transition) obj );

    public override string ToString( ) =>
        $"From: {CurrentState}, To: {NextState}, When: {TransitionEvent.name}";
  }
}
