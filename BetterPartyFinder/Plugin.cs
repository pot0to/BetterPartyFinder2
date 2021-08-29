using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.IoC;
using Dalamud.Plugin;
using XivCommon;

namespace BetterPartyFinder {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        public string Name => "Better Party Finder";

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        internal ChatGui ChatGui { get; init; } = null!;

        [PluginService]
        internal ClientState ClientState { get; init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal DataManager DataManager { get; init; } = null!;

        [PluginService]
        internal GameGui GameGui { get; init; } = null!;

        [PluginService]
        internal PartyFinderGui PartyFinderGui { get; init; } = null!;

        internal Configuration Config { get; }
        private Filter Filter { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }
        internal XivCommonBase Common { get; }
        private JoinHandler JoinHandler { get; }

        public Plugin() {
            this.Config = Configuration.Load(this) ?? new Configuration();
            this.Config.Initialise(this);

            this.Common = new XivCommonBase(Hooks.PartyFinder);
            this.Filter = new Filter(this);
            this.JoinHandler = new JoinHandler(this);
            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            // start task to determine maximum item level (based on max chestpiece)
            Util.CalculateMaxItemLevel(this.DataManager);
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.JoinHandler.Dispose();
            this.Filter.Dispose();
            this.Common.Dispose();
        }
    }
}
