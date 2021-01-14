using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;

namespace BetterPartyFinder {
    public class Configuration : IPluginConfiguration {
        private Plugin? Plugin { get; set; }

        public int Version { get; set; } = 1;

        public Dictionary<Guid, ConfigurationFilter> Presets { get; } = new();
        public Guid? SelectedPreset { get; set; }

        internal static Configuration? Load(Plugin plugin) {
            return (Configuration?) plugin.Interface.GetPluginConfig();
        }

        internal void Initialise(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            this.Plugin?.Interface.SavePluginConfig(this);
        }
    }

    public class ConfigurationFilter {
        public string Name { get; set; } = "<unnamed preset>";

        public ListMode DutiesMode { get; set; } = ListMode.Blacklist;
        public HashSet<uint> Duties { get; set; } = new();

        public HashSet<UiCategory> Categories { get; set; } = new();

        public List<JobFlags> Jobs { get; set; } = new();
        // default to true because that's the PF's default
        // use nosol if trying to avoid spam

        public SearchAreaFlags SearchArea { get; set; } = (SearchAreaFlags) ~(uint) 0;
        public LootRuleFlags LootRule { get; set; } = ~LootRuleFlags.None;
        public DutyFinderSettingsFlags DutyFinderSettings { get; set; } = ~DutyFinderSettingsFlags.None;
        public ConditionFlags Conditions { get; set; } = (ConditionFlags) ~(uint) 0;
        public ObjectiveFlags Objectives { get; set; } = ~ObjectiveFlags.None;

        public bool AllowHugeItemLevel { get; set; } = true;
        public uint? MinItemLevel { get; set; }
        public uint? MaxItemLevel { get; set; }

        internal bool this[SearchAreaFlags flags] {
            get => (this.SearchArea & flags) > 0;
            set {
                if (value) {
                    this.SearchArea |= flags;
                } else {
                    this.SearchArea &= ~flags;
                }
            }
        }

        internal bool this[LootRuleFlags flags] {
            get => (this.LootRule & flags) > 0;
            set {
                if (value) {
                    this.LootRule |= flags;
                } else {
                    this.LootRule &= ~flags;
                }
            }
        }

        internal bool this[DutyFinderSettingsFlags flags] {
            get => (this.DutyFinderSettings & flags) > 0;
            set {
                if (value) {
                    this.DutyFinderSettings |= flags;
                } else {
                    this.DutyFinderSettings &= ~flags;
                }
            }
        }

        internal bool this[ConditionFlags flags] {
            get => (this.Conditions & flags) > 0;
            set {
                if (value) {
                    this.Conditions |= flags;
                } else {
                    this.Conditions &= ~flags;
                }
            }
        }

        internal bool this[ObjectiveFlags flags] {
            get => (this.Objectives & flags) > 0;
            set {
                if (value) {
                    this.Objectives |= flags;
                } else {
                    this.Objectives &= ~flags;
                }
            }
        }

        internal static ConfigurationFilter Create() {
            return new() {
                Categories = Enum.GetValues(typeof(UiCategory))
                    .Cast<UiCategory>()
                    .ToHashSet(),
            };
        }
    }

    public enum ListMode {
        Whitelist,
        Blacklist,
    }
}
