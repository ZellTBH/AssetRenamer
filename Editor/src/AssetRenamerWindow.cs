using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace AssetRenamer.Editor
{
    /// <summary>
    /// Fenetre de renommage par lot. Le squelette vient du UXML (header, barre d'outils, sections,
    /// pied de page) ; les champs sont crees ou relies UNE fois dans CreateGUI. Ensuite, chaque
    /// changement met a jour l'etat puis appelle RefreshPreview, qui ne reconstruit que la liste de
    /// la preview, les compteurs et l'etat des boutons : le reste de l'arbre ne bouge pas, le focus
    /// et le scroll sont preserves.
    /// </summary>
    public class AssetRenamerWindow : EditorWindow
    {
        #region Unity API

        private void CreateGUI()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LAYOUT_PATH);
            Debug.Assert(layout != null, "the Asset Renamer layout is missing from the package");

            if (layout == null)
            {
                rootVisualElement.Add(new Label(MISSING_LAYOUT_MESSAGE));
                return;
            }

            layout.CloneTree(rootVisualElement);

            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH);

            if (style != null)
                rootVisualElement.styleSheets.Add(style);

            QueryElements();
            BindHeader();
            EnsurePrefixTable();
            BindToolbar();
            BindConventionSection();
            BindPrefixSection();
            BindTransformSection();
            BindPreviewSection();
            BindVersionBar();

            RefreshPrefixSection();
            RefreshResult();
            RefreshPreview();
            MaybeCheckForUpdate();
        }

        #endregion


        #region Main API

        [MenuItem(MENU_PATH)]
        public static void Open()
        {
            var window = GetWindow<AssetRenamerWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(560f, 420f);
            window.Show();
        }

        #endregion


        #region Tools and Utilities

        private void QueryElements()
        {
            VisualElement root = rootVisualElement;

            _addSelectionButton = root.Q<Button>("ar-add-selection");
            _clearButton = root.Q<Button>("ar-clear");
            _revertButton = root.Q<Button>("ar-revert");
            _applyButton = root.Q<Button>("ar-apply");
            _conventionRow = root.Q<VisualElement>("ar-convention-row");
            _helpButton = root.Q<Button>("ar-help-button");
            _helpPanel = root.Q<VisualElement>("ar-help-panel");
            _numberSlot = root.Q<VisualElement>("ar-number-slot");
            _tableSlot = root.Q<VisualElement>("ar-table-slot");
            _resetTableButton = root.Q<Button>("ar-reset-table");
            _missingTable = root.Q<VisualElement>("ar-missing-table");
            _transformFoldout = root.Q<Foldout>("ar-transform-foldout");
            _dropZone = root.Q<VisualElement>("ar-dropzone");
            _badge = root.Q<Label>("ar-badge");
            _previewEmpty = root.Q<Label>("ar-preview-empty");
            _previewList = root.Q<VisualElement>("ar-preview-list");
            _countsLabel = root.Q<Label>("ar-counts");
            _issuesLabel = root.Q<Label>("ar-counts-issues");
            _versionLabel = root.Q<Label>("ar-version");
            _updateButton = root.Q<Button>("ar-update");
            _resultLabel = root.Q<Label>("ar-result");

            Debug.Assert(_addSelectionButton != null && _clearButton != null && _revertButton != null && _applyButton != null &&
                _conventionRow != null && _helpButton != null && _helpPanel != null && _numberSlot != null &&
                _tableSlot != null && _resetTableButton != null && _missingTable != null && _transformFoldout != null &&
                _dropZone != null && _badge != null && _previewEmpty != null && _previewList != null &&
                _countsLabel != null && _issuesLabel != null && _versionLabel != null && _updateButton != null && _resultLabel != null,
                "the Asset Renamer layout does not carry the expected element names");
        }

        /// <summary>
        /// Meme logique que le header de Balance Forge : le logo est une copie LOCALE au package.
        /// Logo introuvable : l'Image est retiree plutot que laissee vide, le titre reste seul.
        /// </summary>
        private void BindHeader()
        {
            var logoImage = rootVisualElement.Q<Image>(LOGO_NAME);

            Debug.Assert(logoImage != null, "the Asset Renamer layout does not carry the header logo element");

            if (logoImage == null)
                return;

            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LOGO_PATH);

            if (logo == null)
            {
                logoImage.RemoveFromHierarchy();
                return;
            }

            logoImage.image = logo;
            logoImage.scaleMode = ScaleMode.ScaleToFit;
        }

        private void BindToolbar()
        {
            _addSelectionButton.clicked += AddSelection;
            _clearButton.clicked += ClearPaths;
            _revertButton.clicked += RevertLast;
            _applyButton.clicked += ApplyAll;
            ApplyTooltip(_addSelectionButton, "Add the assets currently selected in the Project window. Folders are expanded to their contents.");
        }

        private void BindConventionSection()
        {
            var conventionField = new EnumField("Convention", _convention);
            conventionField.AddToClassList("ar-field-grow");
            conventionField.RegisterValueChangedCallback(evt =>
            {
                _convention = (NamingConvention)evt.newValue;
                RefreshConventionHelp();
                RefreshPreview();
            });
            _conventionRow.Insert(0, conventionField);
            ApplyTooltip(conventionField, "Casing style applied to each name. Click the ? to preview every style.");

            _helpButton.clicked += ToggleConventionHelp;
            ApplyTooltip(_helpButton, "Preview every naming convention with a live example.");
            BuildConventionHelp();

            var numberChoices = new List<string> { "Off", "1", "01", "001", "Auto" };
            var numberField = new DropdownField("Number suffix", numberChoices, (int)_numberPadding);
            numberField.RegisterValueChangedCallback(evt =>
            {
                _numberPadding = (NumberPadding)numberChoices.IndexOf(evt.newValue);
                RefreshPreview();
            });
            _numberSlot.Add(numberField);
            ApplyTooltip(numberField, "Pads the trailing number of each name.\nOff: leave numbers as-is.\n1 / 01 / 001: force a fixed width.\nAuto: pad every number to the widest one in the dropped batch (e.g. 1..15 becomes 01..15) so they sort correctly.");

            BindToggle("ar-normalize", _normalize, value => _normalize = value,
                "Strip accents, copy markers like (1) / copie, and extra spaces before formatting.");
        }

        private void BindPrefixSection()
        {
            BindToggle("ar-apply-prefix", _applyPrefix, value => { _applyPrefix = value; RefreshPrefixSection(); },
                "Auto-prepend a type prefix (T_ textures, SM_ meshes, PF_ prefabs...) from the table below.");
            BindToggle("ar-strip-redundant", _stripRedundantTypeTokens, value => _stripRedundantTypeTokens = value,
                "Drop words that only repeat the type prefix, e.g. wall_mat -> M_Wall instead of M_WallMat. Needs Apply type prefix.");

            _tableField = new ObjectField("Type Prefix Table") { objectType = typeof(AssetTypePrefixTable), value = _prefixTable };
            _tableField.RegisterValueChangedCallback(evt =>
            {
                _prefixTable = evt.newValue as AssetTypePrefixTable;
                RefreshPrefixSection();
                RefreshPreview();
            });
            _tableSlot.Add(_tableField);
            ApplyTooltip(_tableField, "Maps file extensions and asset types to prefixes. Edit it to match your studio convention.");

            _resetTableButton.clicked += ResetPrefixTable;
            ApplyTooltip(_resetTableButton, "Replace this table's rules with the current built-in defaults (includes the latest type aliases). Custom rules are lost.");

            var createButton = rootVisualElement.Q<Button>("ar-create-table");
            Debug.Assert(createButton != null, "the Asset Renamer layout does not carry the create table button");
            if (createButton != null) createButton.clicked += CreatePrefixTable;
        }

        private void BindTransformSection()
        {
            _transformFoldout.SetValueWithoutNotify(_showAdvanced);
            _transformFoldout.RegisterValueChangedCallback(evt => _showAdvanced = evt.newValue);

            BindTextField("ar-find", _findText, value => _findText = value,
                "Text to search for in each name, applied before formatting. Case-insensitive. Leave empty to skip.");
            BindTextField("ar-replace", _replaceText, value => _replaceText = value,
                "Text that replaces every Find match. Leave empty to delete the matched text.");
            BindTextField("ar-custom-prefix", _customPrefix, value => _customPrefix = value,
                "Free text inserted in front of the formatted name, after the type prefix. Written verbatim, e.g. Boss_.");
            BindTextField("ar-custom-suffix", _customSuffix, value => _customSuffix = value,
                "Free text appended at the very end of the name. Written verbatim, e.g. _LOD0.");

            RefreshTransformIndicator();
        }

        private void BindPreviewSection()
        {
            _dropZone.RegisterCallback<DragEnterEvent>(_ => SetDragOver(isOver: true));
            _dropZone.RegisterCallback<DragLeaveEvent>(_ => SetDragOver(isOver: false));
            _dropZone.RegisterCallback<DragExitedEvent>(_ => SetDragOver(isOver: false));
            _dropZone.RegisterCallback<DragUpdatedEvent>(_ => DragAndDrop.visualMode = DragAndDropVisualMode.Copy);
            _dropZone.RegisterCallback<DragPerformEvent>(_ =>
            {
                SetDragOver(isOver: false);
                DragAndDrop.AcceptDrag();
                AddPaths(DragAndDrop.paths);
            });

            BindToggle("ar-hide-unchanged", _hideUnchanged, value => _hideUnchanged = value,
                "Hide rows that already conform, to focus on what will actually change.");
        }

        private void BindVersionBar()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            _versionLabel.text = info != null ? $"Asset Renamer v{info.version}" : "Asset Renamer (local)";
            _updateButton.clicked += UpdateTool;
            RefreshVersionBar();
        }

        /// <summary>
        /// Relie un toggle du UXML : valeur initiale, ecriture de l'etat, puis rafraichissement de la preview.
        /// </summary>
        private void BindToggle(string name, bool initial, System.Action<bool> write, string tooltip)
        {
            var toggle = rootVisualElement.Q<Toggle>(name);
            Debug.Assert(toggle != null, $"the Asset Renamer layout does not carry the toggle '{name}'");
            if (toggle == null) return;

            toggle.SetValueWithoutNotify(initial);
            toggle.RegisterValueChangedCallback(evt =>
            {
                write(evt.newValue);
                RefreshPreview();
            });
            ApplyTooltip(toggle, tooltip);
        }

        /// <summary>
        /// Relie un champ texte du foldout Transformations. isDelayed garde le comportement 0.4.0 :
        /// la valeur n'est ecrite qu'a la validation (Entree ou perte de focus).
        /// </summary>
        private void BindTextField(string name, string initial, System.Action<string> write, string tooltip)
        {
            var field = rootVisualElement.Q<TextField>(name);
            Debug.Assert(field != null, $"the Asset Renamer layout does not carry the text field '{name}'");
            if (field == null) return;

            field.isDelayed = true;
            field.SetValueWithoutNotify(initial);
            field.RegisterValueChangedCallback(evt =>
            {
                write(evt.newValue);
                RefreshTransformIndicator();
                RefreshPreview();
            });
            ApplyTooltip(field, tooltip);
        }

        private static void ApplyTooltip(VisualElement element, string text)
        {
            element.tooltip = text;
            element.Query().ForEach(child => { child.tooltip = text; });
        }

        private static void SetHidden(VisualElement element, bool hidden) => element.EnableInClassList(HIDDEN_CLASS, enable: hidden);

        private void SetDragOver(bool isOver) => _dropZone.EnableInClassList("ar-dropzone--dragover", enable: isOver);

        private void BuildConventionHelp()
        {
            _helpRows.Clear();
            _helpPanel.Clear();

            foreach (NamingConvention convention in System.Enum.GetValues(typeof(NamingConvention)))
            {
                var row = new VisualElement();
                row.AddToClassList("ar-help-row");

                var name = new Label(ObjectNames.NicifyVariableName(convention.ToString()));
                name.AddToClassList("ar-help-name");
                row.Add(name);

                var example = new Label(NameFormatter.Format(HELP_SAMPLE, convention));
                example.AddToClassList("ar-help-example");
                row.Add(example);

                _helpPanel.Add(row);
                _helpRows.Add(row);
            }

            RefreshConventionHelp();
        }

        private void ToggleConventionHelp()
        {
            _showConventionHelp = !_showConventionHelp;
            RefreshConventionHelp();
        }

        private void RefreshConventionHelp()
        {
            SetHidden(_helpPanel, hidden: !_showConventionHelp);

            int active = (int)_convention;
            for (int i = 0; i < _helpRows.Count; i++)
                _helpRows[i].EnableInClassList("ar-help-row--active", enable: i == active);
        }

        private void RefreshPrefixSection()
        {
            SetHidden(_resetTableButton, hidden: _prefixTable == null);
            SetHidden(_missingTable, hidden: !(_applyPrefix && _prefixTable == null));
        }

        private void RefreshTransformIndicator()
        {
            bool active = !string.IsNullOrEmpty(_findText) || !string.IsNullOrEmpty(_customPrefix) || !string.IsNullOrEmpty(_customSuffix);
            _transformFoldout.text = active ? TRANSFORM_TITLE + "  *" : TRANSFORM_TITLE;
        }

        private void RefreshResult()
        {
            _resultLabel.text = _lastResult;
            SetHidden(_resultLabel, hidden: string.IsNullOrEmpty(_lastResult));
        }

        private void RefreshVersionBar()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            string current = info != null ? info.version : null;
            bool isGit = info != null && info.source == PackageSource.Git;
            bool updateAvailable = isGit && !string.IsNullOrEmpty(_latestVersion) && IsNewer(_latestVersion, current);

            _updateButton.text = updateAvailable ? $"Update available: v{_latestVersion}" : string.Empty;
            SetHidden(_updateButton, hidden: !updateAvailable);
        }

        /// <summary>
        /// Seul rafraichissement apres un changement d'option : liste de la preview, badge,
        /// compteurs et etat des boutons. Le panneau d'options n'est jamais reconstruit.
        /// </summary>
        private void RefreshPreview()
        {
            List<AssetRenamePlan> plans = BuildPlans();

            int toRename = 0;
            int issues = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                if (plans[i].m_status == RenameStatus.Ok) toRename++;
                else if (plans[i].m_status == RenameStatus.Collision || plans[i].m_status == RenameStatus.Invalid) issues++;
            }

            _previewList.Clear();
            int visible = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                if (_hideUnchanged && plans[i].m_status == RenameStatus.Unchanged) continue;
                _previewList.Add(BuildRow(plans[i]));
                visible++;
            }

            _badge.text = visible.ToString();
            SetHidden(_previewEmpty, hidden: _paths.Count > 0);
            RefreshCounts(toRename, issues);
            RefreshToolbar(toRename);
        }

        private void RefreshCounts(int toRename, int issues)
        {
            _countsLabel.text = $"{toRename} to rename,";
            _issuesLabel.text = $"{issues} issues";
            _issuesLabel.EnableInClassList("ar-counts--issues", enable: issues > 0);
        }

        private void RefreshToolbar(int toRename)
        {
            int undoCount = _undo != null ? _undo.Count : 0;

            _applyButton.SetEnabled(toRename > 0);
            _clearButton.SetEnabled(_paths.Count > 0);
            _revertButton.SetEnabled(undoCount > 0);
            _revertButton.text = undoCount > 0 ? $"Revert ({undoCount})" : "Revert";
        }

        /// <summary>
        /// Une ligne de preview : [case] ancien -> nouveau ... [pastille statut]. La seconde ligne
        /// de message n'existe que pour Collision et Invalid : une ligne Ok tient sur une ligne.
        /// </summary>
        private VisualElement BuildRow(AssetRenamePlan plan)
        {
            var row = new VisualElement();
            row.AddToClassList("ar-row");
            ApplyStatusClass(row, plan.m_status);

            bool toggleable = plan.m_status == RenameStatus.Ok || plan.m_status == RenameStatus.Excluded;
            row.Add(toggleable ? BuildRowToggle(plan) : BuildRowToggleSpacer());

            var content = new VisualElement();
            content.AddToClassList("ar-row-content");
            content.Add(BuildRowNames(plan));

            if (HasRowMessage(plan))
            {
                var message = new Label(plan.m_message);
                message.AddToClassList("ar-row-message");
                content.Add(message);
            }

            row.Add(content);
            row.Add(BuildStatusPill(plan.m_status));
            return row;
        }

        private static bool HasRowMessage(AssetRenamePlan plan)
            => (plan.m_status == RenameStatus.Collision || plan.m_status == RenameStatus.Invalid) && !string.IsNullOrEmpty(plan.m_message);

        private static VisualElement BuildRowNames(AssetRenamePlan plan)
        {
            var names = new VisualElement();
            names.AddToClassList("ar-row-names");

            var oldName = new Label(plan.m_originalName + plan.m_extension);
            oldName.AddToClassList("ar-row-old");
            names.Add(oldName);

            var arrow = new Label("->");
            arrow.AddToClassList("ar-row-arrow");
            names.Add(arrow);

            var newName = new Label(plan.m_proposedName + plan.m_extension);
            newName.AddToClassList("ar-row-new");
            names.Add(newName);

            return names;
        }

        private static VisualElement BuildRowToggleSpacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList("ar-row-toggle-spacer");
            return spacer;
        }

        private static VisualElement BuildStatusPill(RenameStatus status)
        {
            var pill = new VisualElement();
            pill.AddToClassList("ar-pill");

            var dot = new VisualElement();
            dot.AddToClassList("ar-pill-dot");
            pill.Add(dot);

            var label = new Label(status.ToString());
            label.AddToClassList("ar-pill-label");
            pill.Add(label);

            return pill;
        }

        private Toggle BuildRowToggle(AssetRenamePlan plan)
        {
            string path = plan.m_assetPath;
            var include = new Toggle { value = plan.m_status != RenameStatus.Excluded };
            include.AddToClassList("ar-toggle");
            include.AddToClassList("ar-row-toggle");
            include.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) _excluded.Remove(path);
                else if (!_excluded.Contains(path)) _excluded.Add(path);
                RefreshPreview();
            });
            return include;
        }

        /// <summary>
        /// Pose la classe USS de l'etat et retire celles des autres etats : la couleur vient
        /// uniquement de la palette du USS.
        /// </summary>
        private static void ApplyStatusClass(VisualElement element, RenameStatus status)
        {
            for (int i = 0; i < STATUS_ORDER.Length; i++)
                element.EnableInClassList(StatusClass(STATUS_ORDER[i]), enable: STATUS_ORDER[i] == status);
        }

        private static string StatusClass(RenameStatus status) => status switch
        {
            RenameStatus.Ok => "ar-status--ok",
            RenameStatus.Unchanged => "ar-status--unchanged",
            RenameStatus.Collision => "ar-status--collision",
            RenameStatus.Invalid => "ar-status--invalid",
            RenameStatus.Excluded => "ar-status--excluded",
            _ => "ar-status--unchanged"
        };

        private void EnsurePrefixTable()
        {
            if (_prefixTable != null) return;

            var guids = AssetDatabase.FindAssets("t:AssetTypePrefixTable");
            if (guids.Length > 0)
                _prefixTable = AssetDatabase.LoadAssetAtPath<AssetTypePrefixTable>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void ResetPrefixTable()
        {
            if (_prefixTable == null) return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Reset Prefix Table",
                "Replace all rules in this table with the up-to-date defaults? Any custom rules will be lost.",
                "Reset",
                "Cancel");
            if (!confirmed) return;

            Undo.RecordObject(_prefixTable, "Reset Prefix Table");
            _prefixTable.LoadDefaults();
            EditorUtility.SetDirty(_prefixTable);
            AssetDatabase.SaveAssets();
            RefreshPreview();
        }

        private void CreatePrefixTable()
        {
            _prefixTable = PrefixTableAssets.CreateOrLoad(DEFAULT_TABLE_PATH);
            _tableField.SetValueWithoutNotify(_prefixTable);
            RefreshPrefixSection();
            RefreshPreview();
        }

        private void UpdateTool()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            string current = info != null ? info.version : "current";
            string target = string.IsNullOrEmpty(_latestVersion) ? "latest" : _latestVersion;

            bool confirmed = EditorUtility.DisplayDialog(
                "Update Asset Renamer",
                $"Update from v{current} to v{target}? This re-resolves the package from GitHub and may trigger a recompile and reopen the window.",
                "Update",
                "Cancel");
            if (!confirmed) return;

            Client.Add(GIT_URL);
        }

        private void MaybeCheckForUpdate()
        {
            if (_updateChecked) return;
            _updateChecked = true;

            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AssetRenamerWindow).Assembly);
            if (info == null || info.source != PackageSource.Git) return;

            var request = UnityWebRequest.Get(MANIFEST_URL);
            var operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var manifest = JsonUtility.FromJson<ManifestVersion>(request.downloadHandler.text);
                        if (manifest != null && !string.IsNullOrEmpty(manifest.version)) _latestVersion = manifest.version;
                    }
                    catch { /* malformed manifest, ignore */ }
                }

                request.Dispose();
                if (this != null && _updateButton != null) RefreshVersionBar();
            };
        }

        private static bool IsNewer(string latest, string current)
        {
            int[] l = ParseVersion(latest);
            int[] c = ParseVersion(current);
            for (int i = 0; i < 3; i++)
                if (l[i] != c[i]) return l[i] > c[i];
            return false;
        }

        private static int[] ParseVersion(string version)
        {
            var parts = new int[3];
            if (string.IsNullOrEmpty(version)) return parts;

            string core = version.Split('-')[0];
            string[] segments = core.Split('.');
            for (int i = 0; i < 3 && i < segments.Length; i++)
                int.TryParse(segments[i], out parts[i]);

            return parts;
        }

        private void AddPaths(string[] paths)
        {
            if (paths == null) return;
            _lastResult = string.Empty;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.StartsWith("Assets/")) continue;
                if (AssetDatabase.IsValidFolder(path)) AddFolderContents(path);
                else AddSingle(path);
            }

            RefreshResult();
            RefreshPreview();
        }

        private void AddFolderContents(string folder)
        {
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!AssetDatabase.IsValidFolder(path)) AddSingle(path);
            }
        }

        private void AddSingle(string path)
        {
            if (!_paths.Contains(path)) _paths.Add(path);
        }

        private void AddSelection()
        {
            var paths = new List<string>();
            foreach (var obj in Selection.objects)
            {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(assetPath)) paths.Add(assetPath);
            }
            AddPaths(paths.ToArray());
        }

        private void ClearPaths()
        {
            _paths.Clear();
            _excluded.Clear();
            _lastResult = string.Empty;
            RefreshResult();
            RefreshPreview();
        }

        private List<AssetRenamePlan> BuildPlans()
        {
            var options = new RenameOptions
            {
                m_convention = _convention,
                m_normalize = _normalize,
                m_applyPrefix = _applyPrefix,
                m_stripRedundantTypeTokens = _stripRedundantTypeTokens,
                m_prefixTable = _prefixTable,
                m_numberPadding = _numberPadding,
                m_customPrefix = _customPrefix,
                m_customSuffix = _customSuffix,
                m_findText = _findText,
                m_replaceText = _replaceText
            };

            var plans = AssetRenamerEngine.BuildPlans(_paths, options);
            for (int i = 0; i < plans.Count; i++)
                if (plans[i].m_status == RenameStatus.Ok && _excluded.Contains(plans[i].m_assetPath))
                    plans[i].m_status = RenameStatus.Excluded;
            return plans;
        }

        private void ApplyAll()
        {
            var plans = BuildPlans();
            _undo.Clear();
            int applied = AssetRenamerEngine.ApplyAll(plans, _undo, out int failed);
            _lastResult = failed > 0 ? $"Renamed {applied}, {failed} failed (see Console)." : $"Renamed {applied} asset(s).";

            KeepUnresolved(plans);
            RefreshResult();
            RefreshPreview();
        }

        private void RevertLast()
        {
            if (_undo == null || _undo.Count == 0) return;

            int reverted = 0;
            for (int i = _undo.Count - 1; i >= 0; i--)
            {
                string error = AssetDatabase.RenameAsset(_undo[i].m_newPath, _undo[i].m_originalName);
                if (string.IsNullOrEmpty(error)) reverted++;
                else Debug.LogError($"[Asset Renamer] Revert failed for '{_undo[i].m_newPath}': {error}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _lastResult = $"Reverted {reverted} asset(s).";
            _undo.Clear();
            RefreshResult();
            RefreshPreview();
        }

        private void KeepUnresolved(List<AssetRenamePlan> plans)
        {
            var survivors = new List<string>();
            for (int i = 0; i < plans.Count; i++)
                if (plans[i].m_status != RenameStatus.Ok) survivors.Add(plans[i].m_assetPath);

            _paths = survivors;
        }

        #endregion


        #region Private and Protected

        private const string MENU_PATH = "Breaking Frame/Asset Renamer";
        private const string WINDOW_TITLE = "Asset Renamer";
        private const string MISSING_LAYOUT_MESSAGE = "Asset Renamer layout not found. Reinstall the package.";
        private const string LAYOUT_PATH = "Packages/com.tools.assetrenamer/Editor/Resources/AssetRenamerWindow.uxml";
        private const string STYLE_PATH = "Packages/com.tools.assetrenamer/Editor/Resources/AssetRenamerTheme.uss";
        private const string LOGO_PATH = "Packages/com.tools.assetrenamer/Editor/Resources/BreakingFrameLogo.png";
        private const string LOGO_NAME = "ar-logo";
        private const string HIDDEN_CLASS = "ar-hidden";
        private const string TRANSFORM_TITLE = "TRANSFORMATIONS";
        private const string DEFAULT_TABLE_PATH = "Assets/AssetTypePrefixTable.asset";
        private const string GIT_URL = "https://github.com/ZellTBH/asset-renamer.git";
        private const string MANIFEST_URL = "https://raw.githubusercontent.com/ZellTBH/asset-renamer/main/package.json";
        private static readonly List<string> HELP_SAMPLE = new List<string> { "wall", "torch" };
        private static readonly RenameStatus[] STATUS_ORDER =
        {
            RenameStatus.Ok, RenameStatus.Unchanged, RenameStatus.Collision, RenameStatus.Invalid, RenameStatus.Excluded
        };

        [SerializeField] private NamingConvention _convention = NamingConvention.PascalCase;
        [SerializeField] private NumberPadding _numberPadding = NumberPadding.Off;
        [SerializeField] private bool _showConventionHelp;
        [SerializeField] private bool _normalize = true;
        [SerializeField] private bool _applyPrefix = true;
        [SerializeField] private bool _stripRedundantTypeTokens = true;
        [SerializeField] private AssetTypePrefixTable _prefixTable;
        [SerializeField] private List<string> _paths = new List<string>();
        [SerializeField] private List<RenameRecord> _undo = new List<RenameRecord>();
        [SerializeField] private string _lastResult = string.Empty;
        [SerializeField] private bool _hideUnchanged;
        [SerializeField] private List<string> _excluded = new List<string>();
        [SerializeField] private string _customPrefix = string.Empty;
        [SerializeField] private string _customSuffix = string.Empty;
        [SerializeField] private string _findText = string.Empty;
        [SerializeField] private string _replaceText = string.Empty;
        [SerializeField] private bool _showAdvanced;

        private Button _addSelectionButton;
        private Button _clearButton;
        private Button _revertButton;
        private Button _applyButton;
        private VisualElement _conventionRow;
        private Button _helpButton;
        private VisualElement _helpPanel;
        private readonly List<VisualElement> _helpRows = new List<VisualElement>();
        private VisualElement _numberSlot;
        private VisualElement _tableSlot;
        private ObjectField _tableField;
        private Button _resetTableButton;
        private VisualElement _missingTable;
        private Foldout _transformFoldout;
        private VisualElement _dropZone;
        private Label _badge;
        private Label _previewEmpty;
        private VisualElement _previewList;
        private Label _countsLabel;
        private Label _issuesLabel;
        private Label _versionLabel;
        private Button _updateButton;
        private Label _resultLabel;

        private static bool _updateChecked;
        private static string _latestVersion;

        [System.Serializable]
        private class ManifestVersion
        {
            public string version;
        }

        #endregion
    }
}
