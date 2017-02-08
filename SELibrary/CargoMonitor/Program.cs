#region pre-script

using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace SELibrary.CargoMonitor
{
    public class Program : MyGridProgram
    {
        #endregion
        const string LightName = "SmallMiner Light";
        readonly IMyInteriorLight _light;

        public Program()
        {
            _light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName(LightName);
        }

        public void Save()
        {

        }

        void Main()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            // Dictionary<string, long> amounts = new Dictionary<string, long>();

            long current = 0;
            long max = 0;

            foreach (var block in blocks)
            {
                var inventory = block.GetInventory();
                if (inventory == null) continue;

                current += inventory.CurrentVolume.RawValue;
                max += inventory.MaxVolume.RawValue;
            }

            float used = current * 100f / max;

            Echo($"Used {used:P1}: {current} of {max}: ");

            if (used > 95f)
            {
                _light.BlinkLength = 30f;    // 30%
                _light.BlinkIntervalSeconds = 0.5f;  // 0.5s
                _light.Color = Color.Red;
            }
            else if (used > 85f)
            {
                _light.BlinkLength = 20f;    // 20%
                _light.BlinkIntervalSeconds = 1.0f;  // 1s
                _light.Color = Color.Yellow;
            }
            else
            {
                _light.BlinkLength = 20f;    // 20%
                _light.BlinkIntervalSeconds = 1.5f;  // 1.5s
                _light.Color = Color.DarkGreen;
            }
        }
        #region post-script
    }
}
#endregion