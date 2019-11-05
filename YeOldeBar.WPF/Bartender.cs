﻿using System.Threading;

namespace YeOldePub
{

    public class Bartender : Agent
    {
        // wait for patron
        // then go to Shelves and get glass (3 sec)
        // if no glass available, wait for clean glass, then get
        // fill glass with beer (3 sec)
        // give glass to patron
        // wait for next patron
        // when no more Patrons in pub, go home

        private bool hasGoneHome;
        private PintGlass pintGlass;
        private const int TimeSpentGettingGlass = 3000;
        private const int TimeSpentFillingGlassWithBeer = 3000;

        //Constructor
        public Bartender(YeOldePub yeOldePub)
        {

            YeOldePub = yeOldePub;
            DataManager = yeOldePub.DataManager;
            hasGoneHome = false;
        }

        public override void AgentCycle(YeOldePub yeOldePub)
        {
            while (hasGoneHome is false)
            {
                switch (CheckState(yeOldePub))
                {
                    case RunState.Idling:
                        //Wait before checking for new patron
                        Thread.Sleep(1000);
                        DataManager.RefreshList(yeOldePub, this, "Waiting for a patron");
                        break;
                    case RunState.Working:
                        //Identify patron in first in queue
                        Patron patronBeingServed = null;
                        while (patronBeingServed is null) yeOldePub.PatronsWaitingForBeer.TryPeek(out patronBeingServed);
                        DataManager.RefreshList(yeOldePub, this, $"Taking order from {patronBeingServed}");

                        //Get clean glass from Shelves
                        while (pintGlass is null) yeOldePub.Shelves.TryTake(out pintGlass);
                        Thread.Sleep(TimeSpentGettingGlass);
                        DataManager.RefreshList(yeOldePub, this, "Getting a glass from the shelves");

                        //Fill glass with beer
                        pintGlass.HasBeer = true;
                        pintGlass.IsClean = false;
                        Thread.Sleep(TimeSpentFillingGlassWithBeer);
                        DataManager.RefreshList(yeOldePub, this, "Filling glass with beer");

                        //Give glass to customer
                        patronBeingServed.pintGlass = pintGlass;
                        pintGlass = null;
                        DataManager.RefreshList(yeOldePub, this, $"Giving beer to {patronBeingServed}");
                        break;
                    case RunState.LeavingThePub:
                        DataManager.RefreshList(yeOldePub, this, "Going home");
                        hasGoneHome = true;
                        break;
                }
            }
        }

        public override RunState CheckState(YeOldePub yeOldePub)
        {
            //Check to see if bartender should work or go home
            if (yeOldePub.Patrons is null && yeOldePub.currentPubState is PubState.Closed) return RunState.LeavingThePub;
            if (yeOldePub.PatronsWaitingForBeer.IsEmpty == false && yeOldePub.Shelves.Count > 0) return RunState.Working;
            return RunState.Idling;
        }
    }
}
