﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace YeOldePub
{
    public class Patron : Agent
    {
        //FIRST go to bar (1 sec)
        //THEN wait for bartender to serve beer
        //THEN search and go to empty chair (4 sec)
        //IF no empty chair, IDLE
        //WHEN seated, drink beer (20-30 se)
        //WHEN beer is finished, LEAVE chair and pub
        private static List<string> PatronNames = new List<string>()
        {
            "Adam", "Bob", "Caesar", "David",
            "Eric", "Francis", "George", "Harold",
            "Ivan", "Joe", "Ken", "Leroy", "Mandrake",
            "Newman", "Otto", "Peter", "Quinn", "Rick",
            "Steven", "Ty", "Urban", "Victor", "Willy"
        };
        public Enum CurrentState { get; set; }
        public Enum PreviousState { get; set; }
        public string Name { get; set; }
        private int _timeSpentDrinkingBeer;
        public int TimeSpentDrinkingBeer
        {
            get { return _timeSpentDrinkingBeer; }
            set
            {
                Random rnd = new Random();
                int time = 1000 * (rnd.Next(20, 31));
                _timeSpentDrinkingBeer = time;
            }
        }
        private const int TimeSpentWalkingToChair = 4000;
        private const int TimeSpentWaiting = 3000;
        private bool hasGoneHome;
        private bool isThirsty;
        private bool isSitting;
        public Chair chair;
        public PintGlass pintGlass;
        private ConcurrentQueue<Patron> CurrentQueue { get; set; }

        //Constructor
        public Patron(YeOldePub yeOldePub)
        {
            YeOldePub = yeOldePub;
            DataManager = yeOldePub.DataManager;
            Name = GetRandomPatronName();
            hasGoneHome = false;
            isThirsty = true;
            isSitting = false;
            CurrentState = RunState.WalkingToBar;
            
            Run(yeOldePub);
        }

        public static string GetRandomPatronName()
        {
            Random rnd = new Random();
            int randomIndex = rnd.Next(0, PatronNames.Count);
            string randomName = PatronNames.ElementAt(randomIndex);
            PatronNames.RemoveAt(randomIndex);
            return randomName;
        }

        public override void AgentCycle(YeOldePub yeOldePub)
        {
            while (hasGoneHome is false)
            {
                switch (CurrentState = CheckState(yeOldePub))
                {
                    case RunState.WalkingToBar:
                        if (PreviousState != CurrentState)
                        {
                            DataManager.RefreshList(yeOldePub, this, $"{Name} entered and is walking to the bar");
                        }
                        Thread.Sleep(1000);
                        yeOldePub.PatronsWaitingForBeer.Enqueue(this);
                        CurrentQueue = yeOldePub.PatronsWaitingForBeer;
                        break;
                    case RunState.WaitingForBeer:
                        if (pintGlass is null)
                        {
                            if (PreviousState != CurrentState)
                            {
                                DataManager.RefreshList(yeOldePub, this, $"{Name} is waiting for a pint of beer");
                            }
                            Thread.Sleep(TimeSpentWaiting); // Give the bartender som time before trying again
                        }
                        else
                        {
                            DataManager.RefreshList(yeOldePub, this, $"{Name} got a pint of beer");

                            DequeuePatron(CurrentQueue, this);
                        }

                        break;
                    case RunState.WaitingForChair:
                        yeOldePub.PatronsWaitingForChair.Enqueue(this);
                        CurrentQueue = yeOldePub.PatronsWaitingForChair;
                        DataManager.RefreshList(yeOldePub, this, $"{Name} is waiting for an available chair");
                        //Check to see if patron is first in line
                        var isFirstInQueue = false;
                        while (isFirstInQueue is false)
                        {
                            //Spend time checking
                            Thread.Sleep(TimeSpentWaiting);
                            isFirstInQueue = yeOldePub.PatronsWaitingForChair.TryPeek(out var result);
                            if (this != result) continue;
                            foreach (var chair in yeOldePub.Chairs)
                            {
                                if (!(chair.IsAvailable)) continue;
                                this.chair = chair; //Dibs on available chair
                                chair.IsAvailable = false;
                                DequeuePatron(CurrentQueue, this);
                            }
                        }
                        break;
                    case RunState.WalkingToChair:
                        DataManager.RefreshList(yeOldePub, this, $"{Name} is walking to a chair");
                        Thread.Sleep(TimeSpentWalkingToChair);
                        isSitting = true;
                        CurrentState = RunState.DrinkingBeer;
                        break;
                    case RunState.DrinkingBeer:
                        //Drink beer
                        DataManager.RefreshList(yeOldePub, this, $"{Name} is drinking a pint of beer");
                        Thread.Sleep(TimeSpentDrinkingBeer);
                        pintGlass.HasBeer = false;
                        isThirsty = false;

                        //Place empty glass on table
                        DataManager.RefreshList(yeOldePub, this, $"{Name} is done drinking and is getting ready to leave");
                        yeOldePub.Tables.Add(pintGlass);
                        isSitting = false;
                        chair.IsAvailable = true;
                        CurrentState = RunState.LeavingThePub;
                        break;
                    case RunState.LeavingThePub:
                        //Remove patron from pub
                        DataManager.RefreshList(yeOldePub, this, $"{Name} is going home");
                        while (hasGoneHome is false) hasGoneHome = yeOldePub.Patrons.TryRemove(Name, out _);
                        hasGoneHome = true;
                        break;
                }
            }
        }

        public Patron DequeuePatron(ConcurrentQueue<Patron> currentQueue, Patron patronTryingToLeave)
        {
            Patron patron = null;
            while (patron is null) _ = currentQueue.TryDequeue(out patron);
            return patron;
        }

        public override RunState CheckState(YeOldePub yeOldePub)
        {
            PreviousState = CurrentState;
            //if (pintGlass == null && isThirsty == true && chair == null && CurrentQueue == null) return RunState.WalkingToBar;
            if (pintGlass == null && isThirsty == true && chair == null && CurrentQueue != null) return RunState.WaitingForBeer;
            else if (pintGlass != null && isThirsty == true && chair == null && CurrentQueue != null) return RunState.WaitingForChair;
            else if (pintGlass != null && isThirsty == true && chair != null && CurrentQueue == null) return RunState.WalkingToChair;
            else if (pintGlass != null && isThirsty == true && chair != null && isSitting == true) return RunState.DrinkingBeer;
            else if (pintGlass != null && isThirsty == false && chair != null && isSitting == true) return RunState.LeavingThePub;
            else return RunState.WalkingToBar;
        }
        //public override string ToString()
        //{
        //    return this.Name;
        //}
    }
}
