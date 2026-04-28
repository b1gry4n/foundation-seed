using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FoundationSeed.Diagnostics;
using FoundationSeed.Editor.Diagnostics;
using FoundationSeed.Input;
using UnityEditor;
using UnityEngine;

namespace FoundationSeed.Editor.Tools
{
    internal static class FoundationSeedMenu
    {
        private const string PlantRoot = "Assets/Plant";
        private const string PlantRuntime = PlantRoot + "/Runtime";
        private const string PlantEditor = PlantRoot + "/Editor";
        private const string PlantDesign = PlantRoot + "/Design";
        private const string PlantDesignDoctrine = PlantDesign + "/Doctrine";
        private const string PlantDesignSystems = PlantDesign + "/Systems";
        private const string PlantDesignWorkflows = PlantDesign + "/Workflows";
        private const string PlantDoctrineRoot = PlantRoot + "/Doctrine";
        private const string PlantDoctrineDecisions = PlantDoctrineRoot + "/Decisions";
        private const string PlantSystemMapRoot = PlantRoot + "/SystemMap";

        [MenuItem("Tools/Foundation Seed/Setup/Run Initial Setup", false, 10)]
        private static void RunInitialSetup()
        {
            bool loggingCreated = EnsureLoggingConfigAsset(selectCreatedAsset: false);
            bool inputCreated = EnsureInputConfigAsset(selectCreatedAsset: false);
            bool codexLoggingCreated = EnsureCodexLoggingConfigAsset(selectCreatedAsset: false);
            bool intentLoggingCreated = EnsureIntentLoggingConfigAsset(selectCreatedAsset: false);
            bool plantScaffoldChanged = EnsurePlantProjectScaffold();
            ReportInputReadiness();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Foundation Seed initial setup complete. Logging config " + (loggingCreated ? "created" : "already existed") +
                ", input config " + (inputCreated ? "created" : "already existed") +
                ", codex logging config " + (codexLoggingCreated ? "created" : "already existed") +
                ", intent logging config " + (intentLoggingCreated ? "created" : "already existed") +
                ", plant scaffold " + (plantScaffoldChanged ? "created/updated" : "already existed") +
                ".");
        }

        [MenuItem("Tools/Foundation Seed/Docs/Open README", false, 30)]
        private static void OpenReadme() => OpenAssetByRelativePath("README.md");

        [MenuItem("Tools/Foundation Seed/Docs/Copy AGENTS Foundation Setup Blurb", false, 32)]
        private static void CopyAgentsFoundationSetupBlurb()
        {
            EditorGUIUtility.systemCopyBuffer = BuildAgentsSetupBlurb();
            Debug.Log("Foundation Seed AGENTS setup blurb copied to clipboard.");
            EditorUtility.DisplayDialog("Foundation Seed", "AGENTS setup blurb copied to clipboard.", "OK");
        }


        [MenuItem("Tools/Foundation Seed/Diagnostics/Reveal Latest Session Log", false, 50)]
        private static void RevealLatestSessionLog()
        {
            if (!TryResolveLatestSessionLogPath(out string path))
            {
                Debug.LogWarning("No session log found yet.");
                return;
            }

            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Tools/Foundation Seed/Diagnostics/Reveal Codex Change Log", false, 51)]
        private static void RevealCodexChangeLog()
        {
            string path = FoundationSeedCodexChangeLogger.ResolveLogPath();
            if (!File.Exists(path))
            {
                Debug.LogWarning("Codex change log not found yet.");
                return;
            }

            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Tools/Foundation Seed/Diagnostics/Validate Foundation Setup", false, 52)]
        private static void ValidateFoundationSetup() => RunValidation(requireSessionEvidence: true, includeCleanupAudit: true);

        [MenuItem("Tools/Foundation Seed/Release/Export Client-Safe Package", false, 70)]
        private static void ExportClientSafePackage()
        {
            string packagePath = ResolvePackagePath();
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                Debug.LogWarning("Foundation Seed package path could not be resolved.");
                return;
            }

            string defaultRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string destinationParent = EditorUtility.OpenFolderPanel("Choose Export Parent Folder", defaultRoot, string.Empty);
            if (string.IsNullOrWhiteSpace(destinationParent))
            {
                return;
            }

            string packageFolderName = Path.GetFileName(packagePath.Replace('\\', '/').TrimEnd('/'));
            string destinationPath = Path.Combine(destinationParent, packageFolderName);
            if (Directory.Exists(destinationPath))
            {
                Directory.Delete(destinationPath, true);
            }

            Directory.CreateDirectory(destinationPath);
            ExportPackageTree(packagePath, destinationPath);
            WriteExportManifest(destinationPath);
            EditorUtility.RevealInFinder(destinationPath);
        }

        private static void RunValidation(bool requireSessionEvidence, bool includeCleanupAudit)
        {
            List<string> structuralIssues = BuildStructuralIssues();
            List<string> guardrailIssues = BuildBoundaryGuardrailIssues();
            List<string> freshnessIssues = BuildSystemMapFreshnessIssues();
            List<string> cleanupIssues = includeCleanupAudit ? BuildCleanupIssues() : new List<string>();

            List<string> passed = new List<string>();
            List<string> failed = new List<string>();
            if (requireSessionEvidence)
            {
                if (!TryResolveLatestSessionLogPath(out string logPath))
                {
                    Debug.LogWarning("No session log found. Run Play Mode first.");
                    return;
                }

                string[] lines = File.ReadAllLines(logPath);
                ValidationCheck[] checks =
                {
                    new ValidationCheck("Session logging enabled", "Session", "Enabled"),
                    new ValidationCheck("Bootstrap initialized", "Foundation", "BootstrapReady"),
                    new ValidationCheck("Game time ready", "Time", "ServiceReady"),
                    new ValidationCheck("Timed runtime driver ready", "Time", "TimedDriverReady"),
                    new ValidationCheck("Input router ready", "Input", "RouterReady"),
                    new ValidationCheck("Save service ready", "Save", "ServiceReady"),
                    new ValidationCheck("Developer console ready", "DevConsole", "ServiceReady"),
                    new ValidationCheck("Console toggle bridge ready", "DevConsole", "ToggleBridgeReady")
                };

                for (int i = 0; i < checks.Length; i += 1)
                {
                    bool ok = ContainsEvidence(lines, checks[i].Category, checks[i].EventName);
                    (ok ? passed : failed).Add(checks[i].Label);
                }
            }

            string summary =
                "Mode: " + (requireSessionEvidence ? "Full" : "Quick") + "\n\n" +
                "Structure issues: " + structuralIssues.Count + "\n" + JoinPrefixed(structuralIssues, "  - ") + "\n\n" +
                "Boundary issues: " + guardrailIssues.Count + "\n" + JoinPrefixed(guardrailIssues, "  - ") + "\n\n" +
                "System map freshness issues: " + freshnessIssues.Count + "\n" + JoinPrefixed(freshnessIssues, "  - ") + "\n\n" +
                "Cleanup issues: " + cleanupIssues.Count + "\n" + JoinPrefixed(cleanupIssues, "  - ") + "\n\n" +
                "Evidence passed: " + passed.Count + "\n" + JoinPrefixed(passed, "  + ") + "\n\n" +
                "Evidence missing: " + failed.Count + "\n" + JoinPrefixed(failed, "  - ");

            bool success = structuralIssues.Count == 0 && guardrailIssues.Count == 0 && freshnessIssues.Count == 0 && cleanupIssues.Count == 0 && failed.Count == 0;
            if (success) Debug.Log("Validation passed.\n" + summary);
            else Debug.LogWarning("Validation incomplete.\n" + summary);
        }

        private static List<string> BuildStructuralIssues()
        {
            List<string> issues = new List<string>();
            if (!File.Exists("Assets/Plant/Resources/FoundationSeedLoggingConfig.asset")) issues.Add("Missing FoundationSeedLoggingConfig.asset");
            if (!File.Exists("Assets/Plant/Resources/FoundationSeedInputConfig.asset")) issues.Add("Missing FoundationSeedInputConfig.asset");
            if (!File.Exists("Assets/Plant/Resources/FoundationSeedCodexLoggingConfig.asset")) issues.Add("Missing FoundationSeedCodexLoggingConfig.asset");
            if (!File.Exists("Assets/Plant/Resources/FoundationSeedIntentLoggingConfig.asset")) issues.Add("Missing FoundationSeedIntentLoggingConfig.asset");
            if (!AssetDatabase.IsValidFolder(PlantDoctrineDecisions)) issues.Add("Missing Assets/Plant/Doctrine/Decisions");
            if (!File.Exists(PlantDoctrineRoot + "/CleanupDoctrine.md")) issues.Add("Missing Assets/Plant/Doctrine/CleanupDoctrine.md");
            if (!File.Exists(PlantSystemMapRoot + "/DeprecatedSystems.md")) issues.Add("Missing Assets/Plant/SystemMap/DeprecatedSystems.md");
            if (!File.Exists(PlantDoctrineDecisions + "/ADR-Template.md")) issues.Add("Missing ADR template");
            return issues;
        }

        private static List<string> BuildBoundaryGuardrailIssues()
        {
            List<string> issues = new List<string>();
            if (!Directory.Exists("Assets")) return issues;

            string[] allScripts = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < allScripts.Length; i += 1)
            {
                string normalized = allScripts[i].Replace('\\', '/');
                bool underPlantRuntime = normalized.StartsWith(PlantRuntime + "/", StringComparison.OrdinalIgnoreCase);
                bool underPlantEditor = normalized.StartsWith(PlantEditor + "/", StringComparison.OrdinalIgnoreCase);
                bool underIgnore = normalized.Contains("/Plugins/") || normalized.Contains("/ThirdParty/");
                if (!underPlantRuntime && !underPlantEditor && !underIgnore)
                {
                    issues.Add("Project script outside plant boundaries: " + normalized);
                }

                if (underPlantRuntime || underPlantEditor)
                {
                    string text = File.ReadAllText(allScripts[i]);
                    if (text.Contains("namespace FoundationSeed")) issues.Add("Plant script using FoundationSeed namespace: " + normalized);
                    if (!text.Contains("namespace Plant")) issues.Add("Plant script missing Plant namespace: " + normalized);
                }
            }

            return issues;
        }

        private static List<string> BuildSystemMapFreshnessIssues()
        {
            List<string> issues = new List<string>();
            DateTime latestCodeUtc = DateTime.MinValue;
            if (Directory.Exists(PlantRuntime))
            {
                foreach (string file in Directory.GetFiles(PlantRuntime, "*.cs", SearchOption.AllDirectories))
                {
                    DateTime utc = File.GetLastWriteTimeUtc(file);
                    if (utc > latestCodeUtc) latestCodeUtc = utc;
                }
            }

            if (Directory.Exists(PlantEditor))
            {
                foreach (string file in Directory.GetFiles(PlantEditor, "*.cs", SearchOption.AllDirectories))
                {
                    DateTime utc = File.GetLastWriteTimeUtc(file);
                    if (utc > latestCodeUtc) latestCodeUtc = utc;
                }
            }

            DateTime latestMapUtc = DateTime.MinValue;
            if (Directory.Exists(PlantSystemMapRoot))
            {
                foreach (string file in Directory.GetFiles(PlantSystemMapRoot, "*.md", SearchOption.AllDirectories))
                {
                    DateTime utc = File.GetLastWriteTimeUtc(file);
                    if (utc > latestMapUtc) latestMapUtc = utc;
                }
            }

            if (latestCodeUtc != DateTime.MinValue && latestMapUtc != DateTime.MinValue)
            {
                double gap = (latestCodeUtc - latestMapUtc).TotalDays;
                if (gap > 7d) issues.Add("System maps stale by " + gap.ToString("0.0") + " day(s).");
            }

            return issues;
        }

        private static List<string> BuildCleanupIssues()
        {
            List<string> issues = new List<string>();
            string deprecatedPath = PlantSystemMapRoot + "/DeprecatedSystems.md";
            if (File.Exists(deprecatedPath))
            {
                string text = File.ReadAllText(deprecatedPath);
                if (!text.Contains("SunsetDateUtc") || !text.Contains("RemovedDateUtc"))
                {
                    issues.Add("DeprecatedSystems.md missing SunsetDateUtc/RemovedDateUtc fields.");
                }
            }

            return issues;
        }

        private static bool EnsureLoggingConfigAsset(bool selectCreatedAsset)
        {
            EnsureResourcesFolder();
            const string path = "Assets/Plant/Resources/FoundationSeedLoggingConfig.asset";
            FoundationSeedLoggingConfig existing = AssetDatabase.LoadAssetAtPath<FoundationSeedLoggingConfig>(path);
            if (existing != null) return false;
            FoundationSeedLoggingConfig created = ScriptableObject.CreateInstance<FoundationSeedLoggingConfig>();
            AssetDatabase.CreateAsset(created, path);
            if (selectCreatedAsset) Selection.activeObject = created;
            return true;
        }

        private static bool EnsureInputConfigAsset(bool selectCreatedAsset)
        {
            EnsureResourcesFolder();
            const string path = "Assets/Plant/Resources/FoundationSeedInputConfig.asset";
            FoundationSeedInputConfig existing = AssetDatabase.LoadAssetAtPath<FoundationSeedInputConfig>(path);
            if (existing != null) return false;
            FoundationSeedInputConfig created = ScriptableObject.CreateInstance<FoundationSeedInputConfig>();
            AssetDatabase.CreateAsset(created, path);
            if (selectCreatedAsset) Selection.activeObject = created;
            return true;
        }

        private static bool EnsureCodexLoggingConfigAsset(bool selectCreatedAsset)
        {
            EnsureResourcesFolder();
            const string path = "Assets/Plant/Resources/FoundationSeedCodexLoggingConfig.asset";
            FoundationSeedCodexLoggingConfig existing = AssetDatabase.LoadAssetAtPath<FoundationSeedCodexLoggingConfig>(path);
            if (existing != null) return false;
            FoundationSeedCodexLoggingConfig created = ScriptableObject.CreateInstance<FoundationSeedCodexLoggingConfig>();
            AssetDatabase.CreateAsset(created, path);
            if (selectCreatedAsset) Selection.activeObject = created;
            return true;
        }

        private static bool EnsureIntentLoggingConfigAsset(bool selectCreatedAsset)
        {
            EnsureResourcesFolder();
            const string path = "Assets/Plant/Resources/FoundationSeedIntentLoggingConfig.asset";
            FoundationSeedIntentLoggingConfig existing = AssetDatabase.LoadAssetAtPath<FoundationSeedIntentLoggingConfig>(path);
            if (existing != null) return false;
            FoundationSeedIntentLoggingConfig created = ScriptableObject.CreateInstance<FoundationSeedIntentLoggingConfig>();
            AssetDatabase.CreateAsset(created, path);
            if (selectCreatedAsset) Selection.activeObject = created;
            return true;
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder(PlantRoot))
            {
                AssetDatabase.CreateFolder("Assets", "Plant");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Plant/Resources"))
            {
                AssetDatabase.CreateFolder(PlantRoot, "Resources");
            }
        }

        private static void ReportInputReadiness()
        {
            bool hasInputSystem = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem", false) != null;
            Debug.Log(hasInputSystem ? "Input System detected." : "Input System not detected; legacy fallback remains.");
        }

        private static bool EnsurePlantProjectScaffold()
        {
            bool changed = false;
            changed |= EnsureAssetFolder("Assets", "Plant");
            changed |= EnsureAssetFolder(PlantRoot, "Runtime");
            changed |= EnsureAssetFolder(PlantRoot, "Editor");
            changed |= EnsureAssetFolder(PlantRoot, "Design");
            changed |= EnsureAssetFolder(PlantRoot, "Doctrine");
            changed |= EnsureAssetFolder(PlantDoctrineRoot, "Decisions");
            changed |= EnsureAssetFolder(PlantRoot, "SystemMap");
            changed |= EnsureAssetFolder(PlantDesign, "Doctrine");
            changed |= EnsureAssetFolder(PlantDesign, "Systems");
            changed |= EnsureAssetFolder(PlantDesign, "Workflows");

            changed |= WriteTextAssetIfMissing(PlantDoctrineRoot + "/CleanupDoctrine.md",
                "# Cleanup Doctrine\n\n" +
                "1. Mark deprecated in `Assets/Plant/SystemMap/DeprecatedSystems.md`.\n" +
                "2. Add SunsetDateUtc and migration owner.\n" +
                "3. Remove code/docs and log RemovedDateUtc + evidence.\n");
            changed |= WriteTextAssetIfMissing(PlantSystemMapRoot + "/DeprecatedSystems.md",
                "# Deprecated Systems Ledger\n\n" +
                "- System:\n" +
                "  - Status: Deprecated | Sunset | Removed\n" +
                "  - SunsetDateUtc:\n" +
                "  - RemovedDateUtc:\n" +
                "  - Replacement:\n" +
                "  - Evidence:\n");
            changed |= WriteTextAssetIfMissing(PlantDoctrineDecisions + "/ADR-Template.md",
                "# ADR-XXXX Title\n\n" +
                "- DateUtc:\n" +
                "- Status: Proposed | Accepted | Superseded\n" +
                "- Context:\n" +
                "- Decision:\n" +
                "- Consequences:\n" +
                "- Validation:\n");

            changed |= WriteTextAssetIfMissing(PlantRuntime + "/Plant.Runtime.asmdef",
                "{\n" +
                "  \"name\": \"Plant.Runtime\",\n" +
                "  \"rootNamespace\": \"Plant\",\n" +
                "  \"references\": [\n" +
                "    \"FoundationSeed.Runtime\"\n" +
                "  ],\n" +
                "  \"includePlatforms\": [],\n" +
                "  \"excludePlatforms\": [],\n" +
                "  \"allowUnsafeCode\": false,\n" +
                "  \"overrideReferences\": false,\n" +
                "  \"precompiledReferences\": [],\n" +
                "  \"autoReferenced\": true,\n" +
                "  \"defineConstraints\": [],\n" +
                "  \"versionDefines\": [],\n" +
                "  \"noEngineReferences\": false\n" +
                "}\n");

            changed |= WriteTextAssetIfMissing(PlantEditor + "/Plant.Editor.asmdef",
                "{\n" +
                "  \"name\": \"Plant.Editor\",\n" +
                "  \"rootNamespace\": \"Plant\",\n" +
                "  \"references\": [\n" +
                "    \"Plant.Runtime\",\n" +
                "    \"FoundationSeed.Editor\"\n" +
                "  ],\n" +
                "  \"includePlatforms\": [\n" +
                "    \"Editor\"\n" +
                "  ],\n" +
                "  \"excludePlatforms\": [],\n" +
                "  \"allowUnsafeCode\": false,\n" +
                "  \"overrideReferences\": false,\n" +
                "  \"precompiledReferences\": [],\n" +
                "  \"autoReferenced\": true,\n" +
                "  \"defineConstraints\": [],\n" +
                "  \"versionDefines\": [],\n" +
                "  \"noEngineReferences\": false\n" +
                "}\n");

            changed |= WriteTextAssetIfMissing(PlantDoctrineRoot + "/EntryDoctrine.md",
                "# Entry Doctrine\n\nProject-level entry doctrine and read flow.\n");
            changed |= WriteTextAssetIfMissing(PlantDoctrineRoot + "/SystemMappingDoctrine.md",
                "# System Mapping Doctrine\n\nKeep project map files current with implementation changes.\n");
            changed |= WriteTextAssetIfMissing(PlantSystemMapRoot + "/SystemMapIndex.md",
                "# System Map Index\n\nTrack all plant system maps here.\n");
            changed |= WriteTextAssetIfMissing(PlantSystemMapRoot + "/ProjectSystemMap.md",
                "# Project System Map\n\nHigh-level project map.\n");
            changed |= WriteTextAssetIfMissing(PlantDesignDoctrine + "/DesignDoctrine.md",
                "# Design Doctrine\n\nProject design intent.\n");
            changed |= WriteTextAssetIfMissing(PlantDesignDoctrine + "/ArchitectureDoctrine.md",
                "# Architecture Doctrine\n\nProject runtime/editor architecture rules.\n");
            changed |= WriteTextAssetIfMissing(PlantDesignSystems + "/SystemMapIndex.md",
                "# Design System Map Index\n");
            changed |= WriteTextAssetIfMissing(PlantDesignWorkflows + "/ImplementationAndTesting.md",
                "# Implementation And Testing Workflow\n");

            return changed;
        }

        private static bool EnsureAssetFolder(string parentPath, string folderName)
        {
            string combined = parentPath + "/" + folderName;
            if (AssetDatabase.IsValidFolder(combined)) return false;
            AssetDatabase.CreateFolder(parentPath, folderName);
            return true;
        }

        private static bool WriteTextAssetIfMissing(string assetPath, string content)
        {
            if (File.Exists(assetPath)) return false;
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(assetPath, content, Encoding.UTF8);
            return true;
        }

        private static bool TryResolveLatestSessionLogPath(out string path)
        {
            path = string.Empty;
            string jsonPath = FoundationSeedSessionLogRuntime.LatestJsonlPath;
            if (!string.IsNullOrWhiteSpace(jsonPath) && File.Exists(jsonPath))
            {
                path = jsonPath;
                return true;
            }

            string transcriptPath = FoundationSeedSessionLogRuntime.LatestTranscriptPath;
            if (!string.IsNullOrWhiteSpace(transcriptPath) && File.Exists(transcriptPath))
            {
                path = transcriptPath;
                return true;
            }

            string sessionRoot = ResolveSessionRootFromConfig();
            if (string.IsNullOrWhiteSpace(sessionRoot) || !Directory.Exists(sessionRoot)) return false;
            FileInfo[] candidates = new DirectoryInfo(sessionRoot).GetFiles("session_*.*", SearchOption.TopDirectoryOnly);
            if (candidates.Length == 0) return false;
            Array.Sort(candidates, (a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
            path = candidates[0].FullName;
            return true;
        }

        private static string ResolveSessionRootFromConfig()
        {
            FoundationSeedLoggingConfig config = AssetDatabase.LoadAssetAtPath<FoundationSeedLoggingConfig>("Assets/Plant/Resources/FoundationSeedLoggingConfig.asset");
            if (config == null || config.writeToProjectFolderInEditor)
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
                string folder = config != null && !string.IsNullOrWhiteSpace(config.projectSessionFolderName) ? config.projectSessionFolderName.Trim() : "PlantLogs/SessionLogs";
                return Path.Combine(projectRoot, folder);
            }

            string runtimeFolder = !string.IsNullOrWhiteSpace(config.runtimeSessionFolderName) ? config.runtimeSessionFolderName.Trim() : "PlantLogs/SessionLogs";
            return Path.Combine(Application.persistentDataPath, runtimeFolder);
        }

        private static bool ContainsEvidence(string[] lines, string category, string eventName)
        {
            string jsonCategory = "\"category\":\"" + category + "\"";
            string jsonEvent = "\"event\":\"" + eventName + "\"";
            string transcriptPattern = "[" + category + "/" + eventName + "]";

            for (int i = 0; i < lines.Length; i += 1)
            {
                string line = lines[i] ?? string.Empty;
                if ((line.Contains(jsonCategory) && line.Contains(jsonEvent)) || line.Contains(transcriptPattern)) return true;
            }

            return false;
        }

        private static string JoinPrefixed(List<string> items, string prefix)
        {
            if (items == null || items.Count == 0) return prefix + "none";
            return string.Join("\n", items.Select(item => prefix + item).ToArray());
        }

        private static string BuildAgentsSetupBlurb()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## Foundation Startup Read Order");
            sb.AppendLine("1. `AGENTS.md` (project root, one level above `Assets/`)");
            sb.AppendLine("2. `Packages/com.ryans.foundation.seed/Documentation~/Doctrine/CoreDoctrine.md`");
            sb.AppendLine("3. `Packages/com.ryans.foundation.seed/Documentation~/Doctrine/ObservabilityDoctrine.md`");
            sb.AppendLine("4. `Packages/com.ryans.foundation.seed/Documentation~/Doctrine/PackageStructureDoctrine.md`");
            sb.AppendLine("5. `Packages/com.ryans.foundation.seed/Documentation~/Doctrine/SystemMapDoctrine.md`");
            sb.AppendLine("6. `Packages/com.ryans.foundation.seed/Documentation~/Systems/BootstrapSystemMap.md`");
            sb.AppendLine("7. `Packages/com.ryans.foundation.seed/Documentation~/Systems/TimeAndTickSystemMap.md`");
            sb.AppendLine("8. `Packages/com.ryans.foundation.seed/Documentation~/Systems/ObservabilitySystemMap.md`");
            sb.AppendLine("9. `Packages/com.ryans.foundation.seed/Documentation~/Systems/ConsoleSystemMap.md`");
            sb.AppendLine("10. `Packages/com.ryans.foundation.seed/Documentation~/Systems/InputSystemMap.md`");
            sb.AppendLine("11. `Packages/com.ryans.foundation.seed/Documentation~/Systems/SaveSystemMap.md`");
            sb.AppendLine("12. `Assets/Plant/Doctrine/EntryDoctrine.md`");
            sb.AppendLine("13. `Assets/Plant/Doctrine/SystemMappingDoctrine.md`");
            sb.AppendLine("14. `Assets/Plant/Doctrine/CleanupDoctrine.md`");
            sb.AppendLine("15. `Assets/Plant/Doctrine/Decisions/*`");
            sb.AppendLine("16. `Assets/Plant/SystemMap/SystemMapIndex.md`");
            sb.AppendLine("17. `Assets/Plant/SystemMap/DeprecatedSystems.md`");
            sb.AppendLine("18. `Assets/Plant/Design/Doctrine/*`");
            sb.AppendLine("19. `Assets/Plant/Design/Systems/*`");
            sb.AppendLine();
            sb.AppendLine("## Codex Rules");
            sb.AppendLine("- Treat foundation docs as infrastructure truth.");
            sb.AppendLine("- Treat plant doctrine/system maps as project truth.");
            sb.AppendLine("- Keep runtime scripts under `Assets/Plant/Runtime/`.");
            sb.AppendLine("- Keep editor scripts under `Assets/Plant/Editor/`.");
            sb.AppendLine("- Keep design intent docs under `Assets/Plant/Design/` and `Assets/Plant/Doctrine/`.");
            sb.AppendLine("- When adding/changing systems, update `Assets/Plant/SystemMap/` in the same pass.");
            sb.AppendLine("- Log phased-out systems in `Assets/Plant/SystemMap/DeprecatedSystems.md` before removal.");
            sb.AppendLine("- Ignore `Packages/com.ryans.foundation.seed/OperatorSetup~/` unless explicitly asked by a human.");
            return sb.ToString();
        }

        private readonly struct ValidationCheck
        {
            public readonly string Label;
            public readonly string Category;
            public readonly string EventName;
            public ValidationCheck(string label, string category, string eventName)
            {
                Label = label;
                Category = category;
                EventName = eventName;
            }
        }

        private static void ExportPackageTree(string sourceRoot, string destinationRoot)
        {
            foreach (string sourceDirectory in Directory.GetDirectories(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = sourceDirectory.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (ShouldExcludeRelativePath(relative, isDirectory: true))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.Combine(destinationRoot, relative));
            }

            foreach (string sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relative = sourceFile.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (ShouldExcludeRelativePath(relative, isDirectory: false))
                {
                    continue;
                }

                string destinationFile = Path.Combine(destinationRoot, relative);
                string destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(sourceFile, destinationFile, overwrite: true);
            }
        }

        private static bool ShouldExcludeRelativePath(string relativePath, bool isDirectory)
        {
            string normalized = relativePath.Replace('\\', '/');
            string[] segments = normalized.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> excludedRootFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "OperatorSetup~",
                "Documentation~",
                "Tests"
            };

            if (segments.Length > 0 && excludedRootFolders.Contains(segments[0])) return true;
            if (normalized.Equals("AGENTS.md", StringComparison.OrdinalIgnoreCase)) return true;
            if (!isDirectory && normalized.EndsWith(".user", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static void WriteExportManifest(string destinationRoot)
        {
            string manifestPath = Path.Combine(destinationRoot, "CLIENT_SAFE_EXPORT.txt");
            string content =
                "Foundation Seed Client-Safe Export\n" +
                "GeneratedUtc: " + DateTime.UtcNow.ToString("o") + "\n\n" +
                "Excluded folders:\n" +
                "- Documentation~\n" +
                "- OperatorSetup~\n" +
                "- Tests\n\n" +
                "Excluded files:\n" +
                "- AGENTS.md\n";
            File.WriteAllText(manifestPath, content);
        }

        private static void OpenAssetByRelativePath(string relativePath)
        {
            string packagePath = ResolvePackagePath();
            if (string.IsNullOrWhiteSpace(packagePath))
            {
                Debug.LogWarning("Foundation Seed package path could not be resolved.");
                return;
            }

            string fullPath = packagePath + "/" + relativePath;
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath);
            if (asset == null)
            {
                Debug.LogWarning("Asset not found: " + fullPath);
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }

        private static string ResolvePackagePath()
        {
            string[] guids = AssetDatabase.FindAssets("FoundationSeedMenu t:Script");
            if (guids == null || guids.Length == 0) return string.Empty;
            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            const string marker = "/Editor/Tools/FoundationSeedMenu.cs";
            int markerIndex = scriptPath.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0) return string.Empty;
            return scriptPath.Substring(0, markerIndex);
        }
    }
}
