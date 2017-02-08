#region pre-script
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace ClassLibrary1.MobileStationDoorCloser
{
    public class Program : MyGridProgram
    {
        #endregion
        public Program()
        {
        }

        public void Main(string argument)
        {
            List<IMyDoor> outerDoors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(outerDoors);

            foreach (IMyDoor door in outerDoors)
                door.CloseDoor();
        }

        #region post-script
    }
}
#endregion