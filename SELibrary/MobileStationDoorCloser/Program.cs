#region pre-script

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace SELibrary.MobileStationDoorCloser
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