using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using SN = System.Numerics;

namespace AltarHelperV2
{
    /// <summary>Weights and alerts of one named profile.</summary>
    public class WeightProfile
    {
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, int> ModTiers = new(StringComparer.OrdinalIgnoreCase);

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, bool> ModAlerts = new(StringComparer.OrdinalIgnoreCase);

        public static WeightProfile CopyOf(WeightProfile source) => new()
        {
            ModTiers = new Dictionary<string, int>(source.ModTiers, StringComparer.OrdinalIgnoreCase),
            ModAlerts = new Dictionary<string, bool>(source.ModAlerts, StringComparer.OrdinalIgnoreCase),
        };

        internal void RestoreComparers()
        {
            ModTiers = new Dictionary<string, int>(ModTiers ?? new(), StringComparer.OrdinalIgnoreCase);
            ModAlerts = new Dictionary<string, bool>(ModAlerts ?? new(), StringComparer.OrdinalIgnoreCase);
        }
    }

    // Profiles: per-character sets of mod weights and alerts.
    //
    // ModTiers / ModAlerts stay the live dictionaries everything else reads and
    // writes. They always point at the active profile's dictionaries (same
    // object), so editing the table, presets or "Reset All Weights" edits the
    // active profile directly. Switching a profile just re-points them.
    public partial class Settings
    {
        public const string DefaultProfileName = "Default";

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, WeightProfile> Profiles;

        public string ActiveProfile = DefaultProfileName;

        public bool AutoSwapProfileByCharacter;

        [JsonIgnore]
        private string _newProfileName = "";

        [JsonIgnore]
        private string _pendingDeleteProfile;

        /// <summary>Character name the plugin last saw; used to prefill new profile names.</summary>
        [JsonIgnore]
        public string CurrentCharacterName = "";

        /// <summary>
        /// Make sure the profile store exists and ModTiers/ModAlerts point at the
        /// active profile. Safe to call any number of times.
        /// </summary>
        public void EnsureProfiles()
        {
            if (Profiles == null)
            {
                // First run, or a settings file from before profiles existed:
                // the current weights become the Default profile.
                Profiles = new Dictionary<string, WeightProfile>(StringComparer.OrdinalIgnoreCase)
                {
                    [DefaultProfileName] = new WeightProfile { ModTiers = ModTiers, ModAlerts = ModAlerts },
                };
            }
            else if (!ReferenceEquals(Profiles.Comparer, StringComparer.OrdinalIgnoreCase))
            {
                Profiles = new Dictionary<string, WeightProfile>(Profiles, StringComparer.OrdinalIgnoreCase);
            }

            if (!Profiles.ContainsKey(DefaultProfileName))
                Profiles[DefaultProfileName] = new WeightProfile { ModTiers = ModTiers, ModAlerts = ModAlerts };

            if (string.IsNullOrWhiteSpace(ActiveProfile) || !Profiles.ContainsKey(ActiveProfile))
                ActiveProfile = DefaultProfileName;

            BindActiveProfile();
        }

        private void BindActiveProfile()
        {
            var profile = Profiles[ActiveProfile];
            ModTiers = profile.ModTiers;
            ModAlerts = profile.ModAlerts;
        }

        public bool SwitchProfile(string name)
        {
            EnsureProfiles();
            if (string.IsNullOrWhiteSpace(name) || !Profiles.TryGetValue(name, out _))
                return false;

            // Use the stored spelling of the key.
            ActiveProfile = Profiles.Keys.First(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            BindActiveProfile();
            return true;
        }

        /// <summary>
        /// Called when the logged-in character changes. Selects the profile named
        /// like the character, otherwise Default.
        /// </summary>
        public string OnCharacterChanged(string characterName)
        {
            CurrentCharacterName = characterName ?? "";
            if (!AutoSwapProfileByCharacter) return null;

            var target = !string.IsNullOrWhiteSpace(characterName) && Profiles.ContainsKey(characterName)
                ? characterName
                : DefaultProfileName;
            if (string.Equals(target, ActiveProfile, StringComparison.OrdinalIgnoreCase)) return null;
            SwitchProfile(target);
            return ActiveProfile;
        }

        private void DrawProfiles()
        {
#pragma warning disable CA1416
            EnsureProfiles();
            if (!ImGui.TreeNode($"Profiles (active: {ActiveProfile})")) return;

            // Profile selector
            ImGui.SetNextItemWidth(250);
            if (ImGui.BeginCombo("Active profile", ActiveProfile))
            {
                foreach (var name in Profiles.Keys
                             .OrderBy(k => k.Equals(DefaultProfileName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                             .ThenBy(k => k, StringComparer.OrdinalIgnoreCase))
                {
                    bool selected = string.Equals(name, ActiveProfile, StringComparison.OrdinalIgnoreCase);
                    if (ImGui.Selectable(name, selected))
                    {
                        SwitchProfile(name);
                        _pendingDeleteProfile = null;
                    }
                    if (selected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Delete (never the Default profile)
            bool isDefault = string.Equals(ActiveProfile, DefaultProfileName, StringComparison.OrdinalIgnoreCase);
            if (!isDefault)
            {
                ImGui.SameLine();
                if (_pendingDeleteProfile == null)
                {
                    if (ImGui.Button("Delete profile"))
                        _pendingDeleteProfile = ActiveProfile;
                }
                else
                {
                    ImGui.TextColored(new SN.Vector4(1f, 0.5f, 0.4f, 1f), $"Delete '{_pendingDeleteProfile}'?");
                    ImGui.SameLine();
                    if (ImGui.Button("Yes, delete"))
                    {
                        Profiles.Remove(_pendingDeleteProfile);
                        _pendingDeleteProfile = null;
                        SwitchProfile(DefaultProfileName);
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel")) _pendingDeleteProfile = null;
                }
            }

            // New profile (always a copy of Default)
            if (string.IsNullOrEmpty(_newProfileName) && !string.IsNullOrEmpty(CurrentCharacterName) &&
                !Profiles.ContainsKey(CurrentCharacterName))
                _newProfileName = CurrentCharacterName;

            ImGui.SetNextItemWidth(250);
            ImGui.InputTextWithHint("##NewProfileName", "New profile name...", ref _newProfileName, 64);
            ImGui.SameLine();
            var newName = _newProfileName?.Trim() ?? "";
            bool nameValid = newName.Length > 0 && !Profiles.ContainsKey(newName);
            if (!nameValid) ImGui.BeginDisabled();
            if (ImGui.Button("Create (copy of Default)"))
            {
                Profiles[newName] = WeightProfile.CopyOf(Profiles[DefaultProfileName]);
                SwitchProfile(newName);
                _newProfileName = "";
                _pendingDeleteProfile = null;
            }
            if (!nameValid) ImGui.EndDisabled();
            if (newName.Length > 0 && Profiles.ContainsKey(newName))
            {
                ImGui.SameLine();
                ImGui.TextColored(new SN.Vector4(1f, 0.5f, 0.4f, 1f), "already exists");
            }

            // Auto swap
            if (ImGui.Checkbox("Automatically swap based on character name", ref AutoSwapProfileByCharacter) &&
                AutoSwapProfileByCharacter && !string.IsNullOrEmpty(CurrentCharacterName))
            {
                // Apply right away for the character that is logged in now.
                var name = CurrentCharacterName;
                CurrentCharacterName = "";
                OnCharacterChanged(name);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("The profile must be named exactly like the character (case-insensitive).\n" +
                                 "Characters without a matching profile use the Default profile.\n" +
                                 "Swaps when the logged-in character changes; a manual selection\n" +
                                 "stays active until then.");
            if (!string.IsNullOrEmpty(CurrentCharacterName))
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"(current character: {CurrentCharacterName})");
            }

            ImGui.TreePop();
#pragma warning restore CA1416
        }
    }
}
