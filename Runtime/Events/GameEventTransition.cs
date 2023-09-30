using Packages.Estenis.GameEvent_;
using UnityEngine;

namespace Packages.Estenis.StateMachine_
{
    [CreateAssetMenu(menuName = "GameEvent/GameEventTransition")]
    public class GameEventTransition : GameEvent<Transition>
    {
    }
}