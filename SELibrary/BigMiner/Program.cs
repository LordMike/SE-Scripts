#region pre-script
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using VRage.Library.Collections;

namespace ClassLibrary1.BigMiner
{
    public class Program : MyGridProgram
    {
        #endregion
        const string PanelName = "BigMiner StatusPanel";
        const string LightName = "BigMiner StatusLight";
        const string ConnectorName = "BigMiner Connector";
        const long Scale = 1000000;

        readonly IMyTextPanel _panel;
        readonly IMyInteriorLight _light;
        readonly IMyInventory _connector;
        readonly long _myGridId;

        public Program()
        {
            _myGridId = Me.CubeGrid.EntityId;
            _panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(PanelName);
            _light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName(LightName);
            _connector = ((IMyShipConnector)GridTerminalSystem.GetBlockWithName(ConnectorName)).GetInventory();
        }

        public void Main(string argument)
        {
            // Locate blocks
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteries, s => s.CubeGrid.EntityId == _myGridId);

            List<IMyTerminalBlock> storages = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(storages, s => s.CubeGrid.EntityId == _myGridId && s.HasInventory);

            bool isCritical = false, isWarning = false;

            // Calculate battery status
            float batteryMax = batteries.Sum(s => s.MaxStoredPower);
            float batteryCur = batteries.Sum(s => s.CurrentStoredPower);
            float batteryPct = batteryCur / batteryMax;

            if (batteryPct < 0.20f)
                isCritical = true;
            else if (batteryPct < 0.30f)
                isWarning = true;

            // Calculate ore status
            Dictionary<string, long> oreInventory = new Dictionary<string, long>();
            long cargoCap = 0, cargoCur = 0;

            foreach (IMyTerminalBlock block in storages)
            {
                IMyInventory myInventory = block.GetInventory();
                if (!myInventory.IsConnectedTo(_connector))
                    continue;

                cargoCap += myInventory.MaxVolume.RawValue;
                cargoCur += myInventory.CurrentVolume.RawValue;

                foreach (IMyInventoryItem item in myInventory.GetItems())
                {
                    string itemId = item.Content.TypeId.ToString();
                    string itemName = item.Content.SubtypeName;

                    if (!itemId.EndsWith("_Ore"))
                        continue;

                    long tmp;
                    oreInventory.TryGetValue(itemName, out tmp);
                    tmp += item.Amount.RawValue / Scale;

                    oreInventory[itemName] = tmp;
                }
            }

            float storagePct = cargoCur * 1f / cargoCap;

            // Update panel
            _panel.WritePublicText($"Battery: {batteryPct:P2}\n");
            _panel.WritePublicText($"Storage {storagePct:P2}\n", true);

            foreach (KeyValuePair<string, long> pair in oreInventory)
            {
                _panel.WritePublicText($"{pair.Key}: {pair.Value:N0} Kg\n", true);
            }

            if (storagePct > 0.90f)
                isCritical = true;
            else if (storagePct > 0.80f)
                isWarning = true;

            // Update light
            if (isCritical)
            {
                _light.BlinkLength = 30f;    // 30%
                _light.BlinkIntervalSeconds = 0.5f;  // 0.5s
                _light.Color = Color.Red;
            }
            else if (isWarning)
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

        public void Save()
        {

        }

        #region post-script
    }
}
#endregion