﻿using System.Threading.Tasks;

namespace YeOldePubSim
{
    public enum RunState
    {
        Idling,
        Working,
        WalkingToBar,
        WaitingForBeer,
        WaitingForChair,
        WalkingToChair,
        DrinkingBeer,
        LeavingThePub
    }

    public abstract class Agent
    {
        public bool hasGoneHome = false;
        public DataManager DataManager { get; set; }
        public YeOldePub YeOldePub { get; set; }
        public abstract void AgentCycle(YeOldePub yeOldePub);
        public void Run(YeOldePub yeOldePub)
        {
            Task.Run(() => AgentCycle(yeOldePub));
        }
    }
}