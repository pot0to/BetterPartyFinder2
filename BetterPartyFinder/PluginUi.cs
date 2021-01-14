using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace BetterPartyFinder {
    public class PluginUi : IDisposable {
        private static readonly uint[] AllowedContentTypes = {
            2,
            3,
            4,
            5,
            6,
            16,
            21,
            26,
            28,
        };

        private Plugin Plugin { get; }

        private string DutySearchQuery { get; set; } = string.Empty;

        private string PresetName { get; set; } = string.Empty;

        internal PluginUi(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.OnBuildUi += this.Draw;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnBuildUi -= this.Draw;
        }

        private static bool IconButton(FontAwesomeIcon icon, string? id = null) {
            ImGui.PushFont(UiBuilder.IconFont);

            var text = icon.ToIconString();
            if (id != null) {
                text += $"##{id}";
            }

            var result = ImGui.Button(text);

            ImGui.PopFont();

            return result;
        }

        private void Draw() {
            if (!ImGui.Begin($"{this.Plugin.Name} settings")) {
                return;
            }

            var selected = this.Plugin.Config.SelectedPreset;

            string selectedName;
            if (selected == null) {
                selectedName = "<none>";
            } else {
                if (this.Plugin.Config.Presets.TryGetValue(selected.Value, out var preset)) {
                    selectedName = preset.Name;
                } else {
                    this.Plugin.Config.SelectedPreset = null;
                    selectedName = "<invalid preset>";
                }
            }

            ImGui.TextUnformatted("Preset");
            if (ImGui.BeginCombo("###preset", selectedName)) {
                if (ImGui.Selectable("<none>")) {
                    this.Plugin.Config.SelectedPreset = null;
                    this.Plugin.Config.Save();
                }

                foreach (var preset in this.Plugin.Config.Presets) {
                    if (!ImGui.Selectable(preset.Value.Name)) {
                        continue;
                    }

                    this.Plugin.Config.SelectedPreset = preset.Key;
                    this.Plugin.Config.Save();
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            if (IconButton(FontAwesomeIcon.Plus, "add-preset")) {
                var id = Guid.NewGuid();

                this.Plugin.Config.Presets.Add(id, new ConfigurationFilter());
                this.Plugin.Config.SelectedPreset = id;
                this.Plugin.Config.Save();
            }

            ImGui.SameLine();

            if (IconButton(FontAwesomeIcon.Trash, "delete-preset") && selected != null) {
                this.Plugin.Config.Presets.Remove(selected.Value);
                this.Plugin.Config.Save();
            }

            ImGui.SameLine();

            if (IconButton(FontAwesomeIcon.PencilAlt, "edit-preset") && selected != null) {
                if (this.Plugin.Config.Presets.TryGetValue(selected.Value, out var editPreset)) {
                    this.PresetName = editPreset.Name;

                    ImGui.OpenPopup("###rename-preset");
                }
            }

            if (ImGui.BeginPopupModal("Rename preset###rename-preset")) {
                if (selected != null && this.Plugin.Config.Presets.TryGetValue(selected.Value, out var editPreset)) {
                    ImGui.TextUnformatted("Preset name");
                    ImGui.PushItemWidth(-1f);
                    var name = this.PresetName;
                    if (ImGui.InputText("###preset-name", ref name, 1_000)) {
                        this.PresetName = name;
                    }

                    ImGui.PopItemWidth();

                    if (ImGui.Button("Save") && this.PresetName.Trim().Length > 0) {
                        editPreset.Name = this.PresetName;
                        this.Plugin.Config.Save();
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.EndPopup();
                }
            }

            ImGui.Separator();

            if (selected != null && this.Plugin.Config.Presets.TryGetValue(selected.Value, out var filter)) {
                this.DrawPresetConfiguration(filter);
            }

            ImGui.End();
        }

        private void DrawPresetConfiguration(ConfigurationFilter filter) {
            if (!ImGui.BeginTabBar("bpf-tabs")) {
                return;
            }

            this.DrawCategoriesTab(filter);

            this.DrawDutiesTab(filter);

            this.DrawItemLevelTab(filter);

            this.DrawJobsTab(filter);

            this.DrawRestrictionsTab(filter);

            ImGui.EndTabBar();
        }

        private void DrawCategoriesTab(ConfigurationFilter filter) {
            if (!ImGui.BeginTabItem("Categories")) {
                return;
            }

            ImGui.TextUnformatted("Nothing here yet");

            ImGui.EndTabItem();
        }

        private void DrawDutiesTab(ConfigurationFilter filter) {
            if (!ImGui.BeginTabItem("Duties")) {
                return;
            }

            var listModeStrings = new[] {
                "Show ONLY these duties",
                "Do NOT show these duties",
            };
            var listModeIdx = filter.ListMode == ListMode.Blacklist ? 1 : 0;
            ImGui.TextUnformatted("List mode");
            ImGui.PushItemWidth(-1);
            if (ImGui.Combo("###list-mode", ref listModeIdx, listModeStrings, listModeStrings.Length)) {
                filter.ListMode = listModeIdx == 0 ? ListMode.Whitelist : ListMode.Blacklist;
                this.Plugin.Config.Save();
            }

            ImGui.PopItemWidth();

            var query = this.DutySearchQuery;
            ImGui.TextUnformatted("Search");
            if (ImGui.InputText("###search", ref query, 1_000)) {
                this.DutySearchQuery = query;
            }

            ImGui.SameLine();
            if (ImGui.Button("Clear list")) {
                filter.Duties.Clear();
                this.Plugin.Config.Save();
            }

            if (ImGui.BeginChild("duty-selection", new Vector2(-1f, -1f))) {
                var duties = this.Plugin.Interface.Data.GetExcelSheet<ContentFinderCondition>()
                    .Where(cf => cf.Unknown29)
                    .Where(cf => AllowedContentTypes.Contains(cf.ContentType.Row));

                var searchQuery = this.DutySearchQuery.Trim();
                if (searchQuery.Trim() != "") {
                    duties = duties.Where(duty => {
                        var sestring = this.Plugin.Interface.SeStringManager.Parse(duty.Name.RawData.ToArray());
                        return sestring.TextValue.ContainsIgnoreCase(searchQuery);
                    });
                }

                foreach (var cf in duties) {
                    var sestring = this.Plugin.Interface.SeStringManager.Parse(cf.Name.RawData.ToArray());
                    var selected = filter.Duties.Contains(cf.RowId);
                    var name = sestring.TextValue;
                    name = char.ToUpperInvariant(name[0]) + name.Substring(1);
                    if (!ImGui.Selectable(name, ref selected)) {
                        continue;
                    }

                    if (selected) {
                        filter.Duties.Add(cf.RowId);
                    } else {
                        filter.Duties.Remove(cf.RowId);
                    }

                    this.Plugin.Config.Save();
                }

                ImGui.EndChild();
            }

            ImGui.EndTabItem();
        }

        private void DrawItemLevelTab(ConfigurationFilter filter) {
            if (!ImGui.BeginTabItem("Item level")) {
                return;
            }

            var hugePfs = filter.AllowHugeItemLevel;
            if (ImGui.Checkbox("Show PFs above maximum item level", ref hugePfs)) {
                filter.AllowHugeItemLevel = hugePfs;
                this.Plugin.Config.Save();
            }

            var minLevel = (int?) filter.MinItemLevel ?? 0;
            ImGui.TextUnformatted("Minimum item level (0 to disable)");
            ImGui.PushItemWidth(-1);
            if (ImGui.InputInt("###min-ilvl", ref minLevel)) {
                filter.MinItemLevel = minLevel == 0 ? null : (uint) minLevel;
                this.Plugin.Config.Save();
            }

            ImGui.PopItemWidth();

            var maxLevel = (int?) filter.MaxItemLevel ?? 0;
            ImGui.TextUnformatted("Maximum item level (0 to disable)");
            ImGui.PushItemWidth(-1);
            if (ImGui.InputInt("###max-ilvl", ref maxLevel)) {
                filter.MaxItemLevel = maxLevel == 0 ? null : (uint) maxLevel;
                this.Plugin.Config.Save();
            }

            ImGui.PopItemWidth();

            ImGui.EndTabItem();
        }

        private void DrawJobsTab(ConfigurationFilter filter) {
            if (!ImGui.BeginTabItem("Jobs")) {
                return;
            }

            ImGui.TextUnformatted("Nothing here yet");

            ImGui.EndTabItem();
        }

        private void DrawRestrictionsTab(ConfigurationFilter filter) {
            if (!ImGui.BeginTabItem("Restrictions")) {
                return;
            }

            ImGui.TextUnformatted("Nothing here yet");

            ImGui.EndTabItem();
        }
    }
}
