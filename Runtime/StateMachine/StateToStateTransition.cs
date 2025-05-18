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
  public struct StateToStateTransition2 {
    public int Index;
    public int    FromStateIndex; // For use by Editor Only
    public string FromState;      // Use this for state decisions
    public int    ToStateIndex;   // For use by Editor Only
    public string ToState;
    public GameEventObject GameEvent;

    //Check if a entry has been left incomplete in the inspector
    public bool IsValid( ) => FromState != null && !string.IsNullOrEmpty(ToState);
  }
}
