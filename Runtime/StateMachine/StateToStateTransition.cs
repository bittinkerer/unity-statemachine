using Packages.Estenis.GameEvent_;
using System;

namespace Packages.Estenis.StateMachine_
{
    [Serializable]
    public struct StateToStateTransition
    {
        public int Index;
        public State FromState;
        public State ToState;
        public GameEventGameData GameEvent;

        //Check if a entry has been left incomplete in the inspector
        public bool IsValid() => FromState != null && ToState != null;
    }

    
}
