using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace BetterPartyFinder {
    public class Configuration : IPluginConfiguration {
        private Plugin? Plugin { get; set; }

        public int Version { get; set; } = 1;

        public Dictionary<Guid, ConfigurationFilter> Presets { get; } = new();
        public Guid? SelectedPreset { get; set; } = null;

        internal void Initialise(Plugin plugin) {
            this.Plugin = plugin;
        }

        internal void Save() {
            this.Plugin?.Interface.SavePluginConfig(this);
        }
    }

    public class ConfigurationFilter {
        public string Name { get; set; } = "<unnamed preset>";
        public ListMode ListMode { get; set; } = ListMode.Blacklist;
        public List<uint> Duties { get; set; } = new();
        public List<JobFlags> Jobs { get; set; } = new();
        // default to true because that's the PF's default
        // use nosol if trying to avoid spam
        public bool AllowHugeItemLevel { get; set; } = true;
        public uint? MinItemLevel { get; set; }
        public uint? MaxItemLevel { get; set; }
    }

    public enum ListMode {
        Whitelist,
        Blacklist,
    }
}
