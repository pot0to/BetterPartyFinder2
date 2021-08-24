using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace BetterPartyFinder {
    public class Commands : IDisposable {
        private static readonly Dictionary<string, string> CommandNames = new() {
            ["/betterpartyfinder"] = "Opens the main interface. Use with args \"c\" or \"config\" to open the settings.",
            ["/bpf"] = "Alias for /betterpartyfinder",
        };

        private Plugin Plugin { get; }

        internal Commands(Plugin plugin) {
            this.Plugin = plugin;

            foreach (var (name, help) in CommandNames) {
                this.Plugin.CommandManager.AddHandler(name, new CommandInfo(this.OnCommand) {
                    HelpMessage = help,
                });
            }
        }

        public void Dispose() {
            foreach (var name in CommandNames.Keys) {
                this.Plugin.CommandManager.RemoveHandler(name);
            }
        }

        private void OnCommand(string command, string args) {
            if (args is "c" or "config") {
                this.Plugin.Ui.SettingsVisible = !this.Plugin.Ui.SettingsVisible;
            } else {
                this.Plugin.Ui.Visible = !this.Plugin.Ui.Visible;
            }
        }
    }
}
