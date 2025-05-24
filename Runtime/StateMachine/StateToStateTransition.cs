using Packages.Estenis.GameEvent_;
using System;

namespace Packages.Estenis.StateMachine_ {
  [Serializable]
  public struct StateToStateTransition {
    public int Index;
    public State FromState;
    public State ToState;
    public GameEventObject GameEvent;

    //Check if a entry has been left incomplete in the inspector
    public bool IsValid( ) => FromState != null && ToState != null;
  }

  [Serializable]
  public class StateToStateTransition2 {
    public string             FromState;
    public string             ToState;
    public GameEventObject    GameEvent;

    //Check if a entry has been left incomplete in the inspector
    public bool IsValid( ) => FromState != null && !string.IsNullOrEmpty(ToState) && ToState != "_AnyState";
  }
}
