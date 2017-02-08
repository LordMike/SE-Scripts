#region pre-script

using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace SELibrary.Welder
{
    public class Program : MyGridProgram
    {
        #endregion
        const string PanelName = "Welder StatusPanel";
        const string LightName = "Welder StatusLight";
        const long CutoffPoint = 100;
        const long CriticalPoint = 20;
        const long Scale = 1000000;   // Values is in millions 

        readonly IMyTextPanel _panel;
        readonly IMyInteriorLight _light;
        readonly long _myGridId;

        public Program()
        {
            _myGridId = Me.CubeGrid.EntityId;
            _panel = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(PanelName);
            _light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName(LightName);
        }

        void Main()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, s => s.CubeGrid.EntityId == _myGridId);

            // Clear panel
            _panel.WritePublicTitle("");
            _panel.WritePublicText("");

            Dictionary<string, long> amounts = new Dictionary<string, long>();

            // Always watch these: 
            amounts.Add("Construction Component", 0);
            amounts.Add("Metal Grid", 0);
            amounts.Add("Interior Plate", 0);
            amounts.Add("Steel Plate", 0);
            amounts.Add("Small Steel Tube", 0);
            amounts.Add("Large Steel Tube", 0);
            amounts.Add("Bulletproof Glass", 0);
            amounts.Add("Reactor Component", 0);
            amounts.Add("Radio Component", 0);
            amounts.Add("Detector Component", 0);
            amounts.Add("Solar Cell", 0);
            amounts.Add("Power Cell", 0);

            foreach (IMyTerminalBlock block in blocks)
            {
                IMyInventory inventory = block.GetInventory();
                if (inventory == null) continue;

                List<IMyInventoryItem> items = inventory.GetItems();

                foreach (IMyInventoryItem item in items)
                {
                    long amount;
                    string displayName = DecodeItemName(item.Content.SubtypeName, item.Content.TypeId.ToString());

                    amounts.TryGetValue(displayName, out amount);
                    amounts[displayName] = amount + item.Amount.RawValue / Scale;
                }
            }

            List<KeyValuePair<string, long>> critical = amounts.OrderBy(s => s.Value).Where(s => s.Value <= CutoffPoint).ToList();
            long lowest = long.MaxValue;

            if (critical.Count == 0)
            {
                _panel.WritePublicTitle("All ok", true);
                _panel.WritePublicText("All ok\n", true);

                IEnumerable<KeyValuePair<string, long>> warnings = amounts.OrderBy(s => s.Value).Take(5);

                foreach (KeyValuePair<string, long> inventoryItem in warnings)
                {
                    _panel.WritePublicText($"{inventoryItem.Key} - {inventoryItem.Value:N0} Kg\n", true);
                }
            }
            else
            {
                lowest = critical.First().Value;

                _panel.WritePublicTitle("Some items are critical", true);
                foreach (KeyValuePair<string, long> inventoryItem in critical)
                {
                    _panel.WritePublicText($"{inventoryItem.Key} - {inventoryItem.Value:N0} Kg\n", true);
                }
            }

            if (lowest < CriticalPoint)
            {
                _light.BlinkLength = 30f;    // 30% 
                _light.BlinkIntervalSeconds = 0.5f;  // 0.5s 
                _light.Color = Color.Red;
            }
            else if (lowest <= CutoffPoint)
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