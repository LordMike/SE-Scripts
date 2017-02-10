#region pre-script

using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SELibrary.Grinder
{
    public class Program : MyGridProgram
    {
        #endregion
        const string PanelName = "Grinder StatusPanel";
        const string LightName = "Grinder StatusLight";
        const long Scale = 1000000;

        readonly IMyTextPanel _panel;
        readonly IMyInteriorLight _light;
        readonly long _myGridId;

        readonly List<IMyBatteryBlock> _batteries;
        readonly List<IMyTerminalBlock> _storages;

        public Program()
        {
            _myGridId = Me.CubeGrid.EntityId;
            _panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(PanelName);
            _light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName(LightName);

            // Locate blocks
            _batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(_batteries, s => s.CubeGrid.EntityId == _myGridId);

            _storages = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(_storages, s => s.CubeGrid.EntityId == _myGridId && s.HasInventory);
        }

        public void Main(string argument)
        {
            bool isCritical = false, isWarning = false;

            // Calculate battery status
            float batteryMax = _batteries.Sum(s => s.MaxStoredPower);
            float batteryCur = _batteries.Sum(s => s.CurrentStoredPower);
            float batteryPct = batteryCur / batteryMax;

            if (batteryPct < 0.20f)
                isCritical = true;
            else if (batteryPct < 0.30f)
                isWarning = true;

            // Calculate cargo status
            long cargoCap = 0, cargoCur = 0;
            Dictionary<string, long> amounts = new Dictionary<string, long>();

            foreach (IMyTerminalBlock block in _storages)
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    IMyInventory myInventory = block.GetInventory(i);

                    cargoCap += myInventory.MaxVolume.RawValue;
                    cargoCur += myInventory.CurrentVolume.RawValue;

                    foreach (IMyInventoryItem item in myInventory.GetItems())
                    {
                        long amount;
                        string displayName = DecodeItemName(item.Content.SubtypeName, item.Content.TypeId.ToString());

                        amounts.TryGetValue(displayName, out amount);
                        amounts[displayName] = amount + item.Amount.RawValue / Scale;
                    }
                }
            }

            float storagePct = cargoCur * 1f / cargoCap;

            // Update panel
            _panel.WritePublicText($"Battery: {batteryPct:P2}\n");
            _panel.WritePublicText($"Storage {storagePct:P2}\n", true);

            foreach (KeyValuePair<string, long> pair in amounts.OrderByDescending(s => s.Value))
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

        string DecodeItemName(String name, String typeId)
        {
            if (name.Equals("Construction")) { return "Construction Component"; }
            if (name.Equals("MetalGrid")) { return "Metal Grid"; }
            if (name.Equals("InteriorPlate")) { return "Interior Plate"; }
            if (name.Equals("SteelPlate")) { return "Steel Plate"; }
            if (name.Equals("SmallTube")) { return "Small Steel Tube"; }
            if (name.Equals("LargeTube")) { return "Large Steel Tube"; }
            if (name.Equals("BulletproofGlass")) { return "Bulletproof Glass"; }
            if (name.Equals("Reactor")) { return "Reactor Component"; }
            if (name.Equals("Thrust")) { return "Thruster Component"; }
            if (name.Equals("GravityGenerator")) { return "GravGen Component"; }
            if (name.Equals("Medical")) { return "Medical Component"; }
            if (name.Equals("RadioCommunication")) { return "Radio Component"; }
            if (name.Equals("Detector")) { return "Detector Component"; }
            if (name.Equals("SolarCell")) { return "Solar Cell"; }
            if (name.Equals("PowerCell")) { return "Power Cell"; }
            if (name.Equals("AutomaticRifleItem")) { return "Rifle"; }
            if (name.Equals("AutomaticRocketLauncher")) { return "Rocket Launcher"; }
            if (name.Equals("WelderItem")) { return "Welder"; }
            if (name.Equals("AngleGrinderItem")) { return "Grinder"; }
            if (name.Equals("HandDrillItem")) { return "Hand Drill"; }
            if (typeId.EndsWith("_Ore"))
            {
                if (name.Equals("Stone"))
                {
                    return name;
                }
                return name + " Ore";
            }
            if (typeId.EndsWith("_Ingot"))
            {
                if (name.Equals("Stone"))
                {
                    return "Gravel";
                }
                if (name.Equals("Magnesium"))
                {
                    return name + " Powder";
                }
                if (name.Equals("Silicon"))
                {
                    return name + " Wafer";
                }
                return name + " Ingot";
            }
            return name;
        }

        #region post-script
    }
}
#endregion