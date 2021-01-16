using System.Threading.Tasks;
using Dalamud.Plugin;

namespace BetterPartyFinder {
    public class Plugin : IDalamudPlugin {
        public string Name => "Better Party Finder";

        internal DalamudPluginInterface Interface { get; private set; } = null!;
        internal Configuration Config { get; private set; } = null!;
        internal GameFunctions Functions { get; private set; } = null!;
        private Filter Filter { get; set; } = null!;
        internal PluginUi Ui { get; set; } = null!;
        private Commands Commands { get; set; } = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface;

            this.Config = Configuration.Load(this) ?? new Configuration();
            this.Config.Initialise(this);

            this.Functions = new GameFunctions(this);
            this.Filter = new Filter(this);
            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            // start task to determine maximum item level (based on max chestpiece)
            Task.Run(() => Util.CalculateMaxItemLevel(this.Interface.Data));
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Filter.Dispose();
            this.Functions.Dispose();
        }
    }
}
