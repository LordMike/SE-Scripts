#region pre-script

using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace SELibrary.MobileStationStatus
{
    public class Program : MyGridProgram
    {
        #endregion
        const string PanelPrefix = "MobileStation Status.";
        const long Scale = 1000000;

        readonly IMyTextPanel[] _panels;

        readonly long _myGridId;
        readonly List<IMyTerminalBlock> _storages;
        readonly List<IMyBatteryBlock> _batteries;
        readonly List<IMyRefinery> _refineries;
        readonly List<IMyReactor> _reactors;
        readonly List<IMySolarPanel> _solarCells;

        public Program()
        {
            _myGridId = Me.CubeGrid.EntityId;

            _panels = new IMyTextPanel[3];
            for (int i = 0; i < _panels.Length; i++)
                _panels[i] = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(PanelPrefix + i);

            // Locate blocks
            _storages = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(_storages, s => s.CubeGrid.EntityId == _myGridId && s.InventoryCount > 0);

            _batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(_batteries, s => s.CubeGrid.EntityId == _myGridId);

            _refineries = new List<IMyRefinery>();
            GridTerminalSystem.GetBlocksOfType(_refineries, s => s.CubeGrid.EntityId == _myGridId);
            _refineries = _refineries.OrderBy(s => s.CustomName).ToList();

            _reactors = new List<IMyReactor>();
            GridTerminalSystem.GetBlocksOfType(_reactors, s => s.CubeGrid.EntityId == _myGridId);

            _solarCells = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(_solarCells, s => s.CubeGrid.EntityId == _myGridId);

            _refineries.Sort((a, b) => string.Compare(a.CustomName, b.CustomName, StringComparison.Ordinal));
            _reactors.Sort((a, b) => string.Compare(a.CustomName, b.CustomName, StringComparison.Ordinal));
            _solarCells.Sort((a, b) => string.Compare(a.CustomName, b.CustomName, StringComparison.Ordinal));
        }

        private void HandleEnergy()
        {
            IMyTextPanel panel = _panels[0];

            panel.WritePublicText("");

            // Calculate battery status
            if (_batteries.Any())
            {
                float batteryMax = _batteries.Sum(s => s.MaxStoredPower);
                float batteryCur = _batteries.Sum(s => s.CurrentStoredPower);
                float batteryPct = batteryCur / batteryMax;

                panel.WritePublicText($"Battery: {batteryPct:P2} ({_batteries.Count}pc, {batteryMax:N2} MWh)\n", true);
            }
            else
            {
                panel.WritePublicText("Battery: <none>\n", true);
            }

            List<IMyBatteryBlock> batteriesExternal = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteriesExternal, s => s.CubeGrid.EntityId != _myGridId);

            if (batteriesExternal.Any())
            {
                float batteryMax = batteriesExternal.Sum(s => s.MaxStoredPower);
                float batteryCur = batteriesExternal.Sum(s => s.CurrentStoredPower);
                float batteryPct = batteryCur / batteryMax;

                panel.WritePublicText($"Battery EXT: {batteryPct:P2} ({batteriesExternal.Count}pc, {batteryMax:N2} MWh)\n", true);
            }
            else
            {
                panel.WritePublicText("Battery EXT: <none>\n", true);
            }

            float produceCur = 0;
            float produceMax = 0;

            // Output individual sources
            foreach (IMyReactor source in _reactors)
            {
                IMyInventory inventory = source.GetInventory();
                long mass = inventory.CurrentMass.RawValue / Scale;

                float pct = source.CurrentOutput / source.MaxOutput;

                panel.WritePublicText($"- {source.CustomName}, {mass:N0} Kg ({source.CurrentOutput:N1} MW, {pct:P2})\n", true);

                produceCur += source.CurrentOutput;
                produceMax += source.MaxOutput;
            }

            foreach (IMySolarPanel source in _solarCells)
            {
                float pct = source.CurrentOutput / source.MaxOutput;

                panel.WritePublicText($"- {source.CustomName}, ({source.CurrentOutput:N1} MW, {pct:P2})\n", true);

                produceCur += source.CurrentOutput;
                produceMax += source.MaxOutput;
            }

            // Total energy
            panel.WritePublicText($"Total {produceCur:N2} MW / {produceMax:N2} MW ({produceCur / produceMax:P2})\n", true);
        }

        private void HandleOreCargo()
        {
            IMyTextPanel panel = _panels[1];

            // Calculate ore status
            Dictionary<string, long> oreInventory = new Dictionary<string, long>();
            long volumeCap = 0, volumeCur = 0;
            long massTotal = 0;

            foreach (IMyTerminalBlock block in _storages)
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    IMyInventory myInventory = block.GetInventory(i);

                    volumeCap += myInventory.MaxVolume.RawValue;
                    volumeCur += myInventory.CurrentVolume.RawValue;

                    foreach (IMyInventoryItem item in myInventory.GetItems())
                    {
                        string itemId = item.Content.TypeId.ToString();
                        string itemName = item.Content.SubtypeName;

                        massTotal += item.Amount.RawValue / Scale;

                        if (!itemId.EndsWith("_Ore"))
                            continue;

                        long tmp;
                        oreInventory.TryGetValue(itemName, out tmp);
                        tmp += item.Amount.RawValue / Scale;

                        oreInventory[itemName] = tmp;
                    }
                }
            }

            float storagePct = volumeCur * 1f / volumeCap;

            // Update panel
            panel.WritePublicText($"Storage {storagePct:P2}\n");
            panel.WritePublicText("Ores:\n", true);

            foreach (KeyValuePair<string, long> pair in oreInventory)
            {
                panel.WritePublicText($"- {pair.Key}: {pair.Value:N0} Kg\n", true);
            }

            panel.WritePublicText($"Combined mass: {massTotal:N2} Kg", true);
        }

        private void HandleRefineries()
        {
            List<MyProductionItem> queue = new List<MyProductionItem>();
            IMyTextPanel panel = _panels[2];

            panel.WritePublicText("Refineries\n");

            // Calculate refinery status
            foreach (IMyRefinery refinery in _refineries.OrderBy(s => s.CustomName))
            {
                string status;
                if (!refinery.IsWorking)
                    status = "Out of order";
                else if (!refinery.IsProducing)
                    status = "Not producing";
                else
                {
                    refinery.GetQueue(queue);

                    if (queue.Any())
                    {
                        MyProductionItem queueItem = queue[0];
                        MyDefinitionId nextItemBlueprint = queueItem.BlueprintId;
                        string operation = nextItemBlueprint.SubtypeName;

                        // Operation ex. CobaltOreToIngot

                        status = operation.Substring(0, operation.IndexOf("To"));

                        status += $" {queueItem.Amount.RawValue / Scale:N0} Kg";
                    }
                    else
                    {
                        status = "???";
                    }

                    queue.Clear();
                }


                panel.WritePublicText($"- {refinery.CustomName}, {status}\n", true);
            }
        }

        public void Main(string argument)
        {
            HandleEnergy();

            HandleOreCargo();

            HandleRefineries();
        }

        public void Save()
        {

        }

        #region post-script
    }
}
#endregion