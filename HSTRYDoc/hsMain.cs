using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace HSTRYDoc
{
    public partial class hsMain : Form
    {

        private enum KeyResolveOutcome
        {
            Success,     // session key chosen
            Fallback     // do not use drive keys, use stored path / setup
        }

        private sealed class DriveKeyCandidate
        {
            public string DriveRoot { get; init; } = string.Empty;   // "E:\\"
            public string FolderPath { get; init; } = string.Empty;  // "E:\\HSTRY_KEY"
            public string[] PrivateKeys { get; init; } = Array.Empty<string>();
        }

        private HSTRYDoc.colorPicker? _colorPopup;
        private bool _suppressBlockSelectionChanged = false;

        // ---- Hash compute (on-demand for large containers) ----
        private CancellationTokenSource? _hashCts;


        // Background hash filling for the block list
        private CancellationTokenSource? _hashFillCts;


        private bool _updatingFontSizeUi = false;
        private bool _updatingParagraphUi = false;

        private readonly AppState _appState = AppState.Load();

        // Private key used to open the current container (V2-only)
        private string? _privateKeyPath;

        // Auto-complete
        private RtfAutoComplete? _rtfAutoComplete;
        private readonly System.Windows.Forms.Timer _acRebuildTimer = new();
        private HashSet<string> _acLastWords = new(StringComparer.OrdinalIgnoreCase);

        private const float H1_SIZE = 24f;
        private const float H2_SIZE = 18f;
        private const float H3_SIZE = 14f;

        // ---- Container/Editor State ----
        private HSTRYContainer? _container;
        private string? _containerPath;

        private int _currentBlockIndex = -1;
        private bool _loadingBlockIntoEditor = false;
        private bool _blockDirty = false;
        private bool _containerDirty = false;

        // ---- Search State ----
        private string _lastFindText = string.Empty;
        private bool _lastFindMatchCase = false;
        private bool _lastFindWholeWord = false;
        private bool _lastFindWrap = true;

        public hsMain()
        {
            InitializeComponent();
            if (Global.Testmode)
            {
                this.toolStripSeparator5.Visible = true;
                this.btnTestblocks.Visible = true;
            }

        }

        private void UiKeyManagement()
        {
            if (_container == null || string.IsNullOrWhiteSpace(_containerPath))
            {
                MessageBox.Show(this, "No container is currently open.", "Key Management",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new KeyManagerDialog(_container, _containerPath!, _privateKeyPath);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            // Update key path from dialog
            _privateKeyPath = dlg.SelectedPrivateKeyPath;

            // Persist into AppState
            if (!string.IsNullOrWhiteSpace(_privateKeyPath) && File.Exists(_privateKeyPath))
            {
                _appState.PrivateKeyPath = _privateKeyPath;
                _appState.Save();
            }

            if (dlg.ContainerChanged)
            {
                try
                {
                    if (!MaybeCommitCurrentBlock())
                        return;

                    _container.Save(_containerPath!);
                    _containerDirty = false;
                    UpdateUiState();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Key Management", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        #region test_blocks

        // ...

        private async Task CreateTestBlocksAsync(int count, int minWordsPerBlock = 80, int maxWordsPerBlock = 250)
        {
            if (count <= 0) return;

            if (_container == null)
            {
                MessageBox.Show(this, "No container is currently open.", "Test blocks",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!MaybeCommitCurrentBlock())
                return;

            // Keep range sane
            if (minWordsPerBlock < 1) minWordsPerBlock = 1;
            if (maxWordsPerBlock < minWordsPerBlock) maxWordsPerBlock = minWordsPerBlock;

            try
            {
                // Optional: progress dialog (works with the reporterDiag you already have)
                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Creating test blocks",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress
                        {
                            Message = "Preparing…",
                            Indeterminate = false,
                            Maximum = count,
                            Value = 0
                        });

                        await Task.Run(() =>
                        {
                            // Pre-generate dictionary words once (fast + stable)
                            string[] words = BuildWordPool();

                            for (int i = 0; i < count; i++)
                            {
                                token.ThrowIfCancellationRequested();

                                // Unique title (no duplicates)
                                string title = _container!.GenerateUniqueTitle();

                                // Build random text
                                int wCount = RandomNumberGenerator.GetInt32(minWordsPerBlock, maxWordsPerBlock + 1);
                                string plain = BuildRandomParagraphs(words, wCount);

                                // Wrap as RTF (minimal formatting; safe escaping)
                                string rtf = BuildSimpleRtf(title, plain);

                                _container!.AddRtfDocument(title, rtf);

                                progress.Report(new UiProgress
                                {
                                    Message = $"Created block {i + 1}/{count}",
                                    Value = i + 1
                                });
                            }
                        }, token);
                    });

                _containerDirty = true;
                RefreshBlockList(selectIndex: _container.Blocks.Count - 1);
                LoadBlockIntoEditor(_container.Blocks.Count - 1);
                UpdateUiState();

                MessageBox.Show(this, $"Created {count} test block(s).", "Test blocks",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this, "Operation cancelled.", "Test blocks",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Test blocks",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string[] BuildWordPool()
        {
            // Mix of generic words; feel free to expand
            return new[]
            {
        "alpha","beta","gamma","delta","epsilon","zeta","eta","theta","iota","kappa",
        "document","container","block","editor","format","title","content","random","test","sample",
        "encryption","signature","header","recipient","private","public","hash","nonce","cipher","tag",
        "method","class","function","variable","string","integer","boolean","object","thread","async",
        "update","save","open","close","search","index","value","progress","status","dialog",
        "system","design","performance","memory","validation","integrity","version","encoding","unicode",
        "note","section","paragraph","line","word","sentence","example","dummy","placeholder","draft"
    };
        }

        private static string BuildRandomParagraphs(string[] words, int wordCount)
        {
            // Create 2–6 paragraphs depending on size
            int paraCount = Math.Clamp(wordCount / 60, 2, 6);

            int remaining = wordCount;
            StringBuilder sb = new(wordCount * 6);

            for (int p = 0; p < paraCount; p++)
            {
                int thisPara = (p == paraCount - 1)
                    ? remaining
                    : Math.Max(10, remaining / (paraCount - p));

                remaining -= thisPara;

                sb.Append(BuildRandomSentences(words, thisPara));
                if (p < paraCount - 1)
                    sb.Append("\n\n");
            }

            return sb.ToString();
        }

        private static string BuildRandomSentences(string[] words, int wordCount)
        {
            StringBuilder sb = new(wordCount * 6);

            int remaining = wordCount;

            while (remaining > 0)
            {
                // sentence length 8..18 words
                int sLen = Math.Min(remaining, RandomNumberGenerator.GetInt32(8, 19));
                remaining -= sLen;

                for (int i = 0; i < sLen; i++)
                {
                    string w = words[RandomNumberGenerator.GetInt32(0, words.Length)];

                    if (i == 0)
                        w = char.ToUpperInvariant(w[0]) + w.Substring(1);

                    sb.Append(w);

                    if (i < sLen - 1)
                        sb.Append(' ');
                }

                sb.Append(". ");
            }

            return sb.ToString().Trim();
        }

        private static string BuildSimpleRtf(string title, string plainText)
        {
            // Minimal RTF (Arial 10). Convert newlines to \par, escape RTF special chars.
            string Esc(string s) => EscapeRtf(s);

            string body = Esc(plainText).Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n\n", @"\par\par ")
                                        .Replace("\n", @"\par ");

            return @"{\rtf1\ansi\deff0" +
                   @"{\fonttbl{\f0 Arial;}}" +
                   @"\fs20 " +
                   @"\b " + Esc(title) + @"\b0\par\par " +
                   body +
                   @"}";
        }

        private static string EscapeRtf(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;

            StringBuilder sb = new(s.Length + 16);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '{': sb.Append(@"\{"); break;
                    case '}': sb.Append(@"\}"); break;
                    default:
                        if (c <= 0x7f)
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            // RTF unicode escape
                            sb.Append(@"\u");
                            sb.Append((short)c);
                            sb.Append('?');
                        }
                        break;
                }
            }
            return sb.ToString();
        }



        #endregion

        private async void hsMain_Load(object sender, EventArgs e)
        {
            ApplySavedWindowPlacement();

            WireUiEvents();
            InitAutoComplete();

            // Sync menu checkbox with AppState (no dialogs here)
            if (keyLookupExternalDriveToolStripMenuItem != null)
                keyLookupExternalDriveToolStripMenuItem.Checked = _appState.UseDriveKeySearch;

            // IMPORTANT: Do NOT ask for drive scan / key config on startup anymore.
            // Key resolution happens only when opening a container or creating a new container.

            if (!await TryOpenFromCommandLineOrShellAsync())
            {
                if (!await RunChooserStartupAsync())
                {
                    Close();
                    return;
                }
            }

            UpdateUiState();
            UpdateRtfUiFromSelection();
            RebuildAutoCompleteWordsFromEditor();
        }

        private static List<DriveKeyCandidate> FindDriveKeyCandidates()
        {
            var result = new List<DriveKeyCandidate>();

            DriveInfo[] drives;
            try { drives = DriveInfo.GetDrives(); }
            catch { return result; }

            // Prefer removable first, then fixed
            var ordered = drives
                .Where(d => d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed)
                .OrderBy(d => d.DriveType == DriveType.Removable ? 0 : 1)
                .ToList();

            foreach (var d in ordered)
            {
                try
                {
                    if (!d.IsReady) continue;

                    string root = d.RootDirectory.FullName;                 // "E:\\"
                    string folder = Path.Combine(root, "HSTRY_KEY");         // "E:\\HSTRY_KEY"

                    if (!Directory.Exists(folder))
                        continue;

                    string[] privs = Directory.GetFiles(folder, "*.hstrypriv", SearchOption.TopDirectoryOnly);
                    if (privs.Length == 0)
                        continue;

                    result.Add(new DriveKeyCandidate
                    {
                        DriveRoot = root,
                        FolderPath = folder,
                        PrivateKeys = privs
                    });
                }
                catch
                {
                    // ignore drive errors
                }
            }

            return result;
        }

        private static string SelectPreferredPrivateKey(string folderPath, string[] privKeys)
        {
            if (privKeys == null || privKeys.Length == 0)
                return string.Empty;

            // Prefer default_owner.hstrypriv then owner.hstrypriv
            string p1 = Path.Combine(folderPath, "default_owner.hstrypriv");
            if (File.Exists(p1))
                return p1;

            string p2 = Path.Combine(folderPath, "owner.hstrypriv");
            if (File.Exists(p2))
                return p2;

            // Otherwise: stable choice (first ordered by filename)
            return privKeys
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .First();
        }

        private KeyResolveOutcome TryResolvePrivateKeyFromDrivesInteractive(out string? sessionPrivateKeyPath)
        {
            sessionPrivateKeyPath = null;

            // Ask every start (only when UseDriveKeySearch is enabled)
            var ask = MessageBox.Show(
                this,
                "Search connected drives for a folder named 'HSTRY_KEY' now?",
                "Drive key search",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (ask != DialogResult.Yes)
                return KeyResolveOutcome.Fallback;

            var candidates = FindDriveKeyCandidates();
            if (candidates.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No 'HSTRY_KEY' folder with private keys (*.hstrypriv) was found on connected drives.",
                    "Drive key search",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return KeyResolveOutcome.Fallback;
            }

            foreach (var c in candidates)
            {
                // Preselect best key
                string preferred = SelectPreferredPrivateKey(c.FolderPath, c.PrivateKeys);
                string preferredName = string.IsNullOrWhiteSpace(preferred) ? "<none>" : Path.GetFileName(preferred);

                var use = MessageBox.Show(
                    this,
                    $"Found key folder:\n{c.FolderPath}\n\n" +
                    $"Preferred key: {preferredName}\n\n" +
                    $"Use keys from drive '{c.DriveRoot}' for this session?",
                    "Drive key found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (use != DialogResult.Yes)
                    continue;

                // If only one or preferred exists -> take preferred
                if (!string.IsNullOrWhiteSpace(preferred) && File.Exists(preferred))
                {
                    sessionPrivateKeyPath = preferred;
                    return KeyResolveOutcome.Success;
                }

                // Fallback: ask user to pick one in folder
                using var ofd = new OpenFileDialog
                {
                    InitialDirectory = c.FolderPath,
                    Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                    CheckFileExists = true,
                    Title = $"Select private key from {c.DriveRoot}"
                };

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    continue;

                if (!File.Exists(ofd.FileName))
                    continue;

                sessionPrivateKeyPath = ofd.FileName;
                return KeyResolveOutcome.Success;
            }

            // user declined all drives
            return KeyResolveOutcome.Fallback;
        }



        // ============================================================
        // Interactive key selection dialog (Default vs USB(HSTRY_KEY) vs Manual)
        // ============================================================
        private bool ResolvePrivateKeyInteractive(string containerPath, out string privateKeyPath)
        {
            privateKeyPath = string.Empty;

            // 1) ONLY when opening a container: optionally scan drives
            if (_appState.UseDriveKeySearch)
            {
                var outcome = TryResolvePrivateKeyFromDrivesInteractive(out string? sessionKey);

                if (outcome == KeyResolveOutcome.Success &&
                    !string.IsNullOrWhiteSpace(sessionKey) &&
                    File.Exists(sessionKey))
                {
                    // Session-only: do NOT overwrite AppState.PrivateKeyPath
                    _privateKeyPath = sessionKey;
                    privateKeyPath = sessionKey;
                    return true;
                }
                // If user declines / none found -> fallback below
            }

            // 2) Fallback: stored path / create-select flow
            if (!EnsurePrivateKeyConfiguredInteractive())
                return false;

            if (string.IsNullOrWhiteSpace(_privateKeyPath) || !File.Exists(_privateKeyPath))
                return false;

            privateKeyPath = _privateKeyPath!;
            return true;
        }


        // ============================================================
        // Auto-complete (dynamic word suggestions)
        // ============================================================
        private void InitAutoComplete()
        {
            _rtfAutoComplete?.Dispose();
            _rtfAutoComplete = new RtfAutoComplete(rtfMainText, Enumerable.Empty<string>())
            {
                MinPrefixLength = 2,
                MaxItems = 25
            };

            _acRebuildTimer.Stop();
            _acRebuildTimer.Interval = 400; // debounce ms
            _acRebuildTimer.Tick -= AcRebuildTimer_Tick;
            _acRebuildTimer.Tick += AcRebuildTimer_Tick;
        }

        private void AcRebuildTimer_Tick(object? sender, EventArgs e)
        {
            _acRebuildTimer.Stop();
            RebuildAutoCompleteWordsFromEditor();
        }

        private void ScheduleAutoCompleteRebuild()
        {
            if (_loadingBlockIntoEditor) return;

            _acRebuildTimer.Stop();
            _acRebuildTimer.Start();
        }

        private void RebuildAutoCompleteWordsFromEditor()
        {
            if (_rtfAutoComplete == null) return;

            string text = rtfMainText.Text ?? string.Empty;

            if (text.Length == 0)
            {
                if (_acLastWords.Count != 0)
                {
                    _acLastWords.Clear();
                    _rtfAutoComplete.SetSource(Array.Empty<string>());
                }
                return;
            }

            HashSet<string> words = ExtractWords(text, minLen: 3, maxUniqueWords: 5000, maxReturned: 2000);

            if (SetsEqual(words, _acLastWords))
                return;

            _acLastWords = words;
            _rtfAutoComplete.SetSource(words);
        }

        private static HashSet<string> ExtractWords(string text, int minLen, int maxUniqueWords, int maxReturned)
        {
            Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

            int i = 0;
            while (i < text.Length)
            {
                while (i < text.Length && !IsWordCharForAutoComplete(text[i]))
                    i++;

                int start = i;

                while (i < text.Length && IsWordCharForAutoComplete(text[i]))
                    i++;

                int len = i - start;
                if (len >= minLen)
                {
                    string w = text.Substring(start, len);

                    bool allDigits = true;
                    for (int k = 0; k < w.Length; k++)
                    {
                        if (!char.IsDigit(w[k]))
                        {
                            allDigits = false;
                            break;
                        }
                    }
                    if (allDigits) continue;

                    if (counts.TryGetValue(w, out int c))
                        counts[w] = c + 1;
                    else
                        counts[w] = 1;

                    if (counts.Count >= maxUniqueWords)
                        break;
                }
            }

            return counts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Take(maxReturned)
                .Select(kv => kv.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsWordCharForAutoComplete(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' ||
                   c == 'Ä' || c == 'Ö' || c == 'Ü' ||
                   c == 'ä' || c == 'ö' || c == 'ü' || c == 'ß';
        }

        private static bool SetsEqual(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (string x in a)
                if (!b.Contains(x)) return false;
            return true;
        }

        // ============================================================
        // Window placement persistence
        // ============================================================
        private void ApplySavedWindowPlacement()
        {
            WindowPlacement wp = _appState.MainWindow;
            if (wp == null) return;

            if (wp.Width > 100 && wp.Height > 100 &&
                wp.X != int.MinValue && wp.Y != int.MinValue)
            {
                StartPosition = FormStartPosition.Manual;

                Rectangle desired = new(wp.X, wp.Y, wp.Width, wp.Height);
                Rectangle wa = Screen.FromRectangle(desired).WorkingArea;

                int x = desired.X;
                int y = desired.Y;
                int w = desired.Width;
                int h = desired.Height;

                if (w > wa.Width) w = wa.Width;
                if (h > wa.Height) h = wa.Height;

                if (x < wa.Left) x = wa.Left;
                if (y < wa.Top) y = wa.Top;

                if (x + w > wa.Right) x = wa.Right - w;
                if (y + h > wa.Bottom) y = wa.Bottom - h;

                Bounds = new Rectangle(x, y, w, h);
            }

            FormWindowState state = (FormWindowState)wp.WindowState;
            if (state == FormWindowState.Minimized)
                state = FormWindowState.Normal;

            WindowState = state;
        }

        private void SaveWindowPlacement()
        {
            WindowPlacement wp = _appState.MainWindow ??= new WindowPlacement();

            Rectangle r = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;

            wp.X = r.X;
            wp.Y = r.Y;
            wp.Width = r.Width;
            wp.Height = r.Height;
            wp.WindowState = (int)WindowState;
        }

        // ============================================================
        // Chooser Startup (ASYNC)
        // ============================================================
        private async Task<bool> RunChooserStartupAsync()
        {
            while (_container == null)
            {
                using Chooser chooser = new()
                {
                    StartPosition = FormStartPosition.CenterParent
                };

                chooser.SetRecent(_appState.GetRecentExisting());

                DialogResult dr = chooser.ShowDialog(this);

                if (dr == DialogResult.Cancel)
                    return false;

                if (dr == DialogResult.Yes)
                {
                    if (await UiCreateNewContainerAsync(initialStartup: true))
                        return true;

                    continue;
                }

                if (dr == DialogResult.No)
                {
                    if (!string.IsNullOrWhiteSpace(chooser.SelectedRecentPath))
                    {
                        string p = chooser.SelectedRecentPath;

                        if (!File.Exists(p))
                        {
                            _appState.RemoveRecentFile(p);
                            continue;
                        }

                        if (!LooksLikeHstryFile(p))
                        {
                            _appState.RemoveRecentFile(p);
                            continue;
                        }

                        if (await OpenContainerFromPathAsync(p))
                            return true;

                        continue;
                    }

                    if (await TryOpenContainerInteractiveAsync())
                        return true;

                    continue;
                }

                return false;
            }

            return true;
        }

        // ============================================================
        // Startup Open-With (ASYNC)
        // ============================================================
        private async Task<bool> TryOpenFromCommandLineOrShellAsync()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length < 2) return false;

                string? path = args.Skip(1).FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
                if (string.IsNullOrWhiteSpace(path)) return false;

                if (!LooksLikeHstryFile(path)) return false;

                return await OpenContainerFromPathAsync(path);
            }
            catch
            {
                return false;
            }
        }

        private static bool LooksLikeHstryFile(string path)
        {
            try
            {
                using FileStream fs = File.OpenRead(path);
                if (fs.Length < Global.FileMagic.Length) return false;

                byte[] buf = new byte[Global.FileMagic.Length];
                int read = fs.Read(buf, 0, buf.Length);
                if (read != buf.Length) return false;

                return buf.SequenceEqual(Global.FileMagic);
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // V2-only open (private key file)  [UPDATED: uses KeySourceDialog] (ASYNC)
        // ============================================================
        private async Task<bool> OpenContainerFromPathAsync(string path)
        {
            if (!ResolvePrivateKeyInteractive(path, out string privPath))
                return false;

            try
            {
                HSTRYContainer c = await reporterDiag.RunAsync(
                    owner: this,
                    title: "Opening container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress
                        {
                            Message = "Opening container…",
                            Indeterminate = true
                        });

                        HSTRYContainer loaded = await Task.Run(() => HSTRYContainer.LoadWithPrivateKeyFile(path, privPath), token);
                        return loaded;
                    });

                // IMPORTANT: stop running hash jobs before switching container/list
                CancelHashWorkers();

                _container?.CloseKeyMaterial();
                _container = c;
                _containerPath = path;
                _privateKeyPath = privPath;

                _currentBlockIndex = -1;
                _blockDirty = false;
                _containerDirty = false;

                RefreshBlockList();
                ClearEditor();
                UpdateUiState();

                _appState.TouchRecentFile(path);
                _appState.Save();

                RebuildAutoCompleteWordsFromEditor();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Open container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        // ============================================================
        // UI wiring
        // ============================================================
        private void WireUiEvents()
        {
            // Top Toolstrip
            newBlockToolStripButton.Click += (_, __) => UiNewBlock();
            openContainerToolStripButton.Click += async (_, __) => await UiOpenContainerAsync();
            saveContainerToolStripButton.Click += async (_, __) => await UiSaveContainerAsync();

            // File Menu
            newToolStripMenuItem.Click += (_, __) => UiNewBlock();
            openContainerToolStripMenuItem.Click += async (_, __) => await UiOpenContainerAsync();
            saveContainerToolStripMenuItem.Click += async (_, __) => await UiSaveContainerAsync();
            saveContainerAsToolStripMenuItem.Click += async (_, __) => await UiSaveContainerAsAsync();

            // Tools Menu: Search
            searchInBlockToolStripMenuItem.Click += (_, __) => UiSearchInBlock();
            searchInContainerToolStripMenuItem.Click += async (_, __) => await UiSearchInContainerAsync();

            // Blocks context menu
            newBlockToolStripMenuItem.Click += (_, __) => UiNewBlock();
            renameBlockToolStripMenuItem.Click += (_, __) => UiRenameBlock();
            removeBlockToolStripMenuItem.Click += (_, __) => UiRemoveBlock();

            // ListView selection
            lvwBlocks.SelectedIndexChanged += (_, __) => UiSelectBlockFromList();
            lvwBlocks.DoubleClick += (_, __) => UiSelectBlockFromList();
            // Hash on-demand when selection changes (large containers)
            lvwBlocks.ItemSelectionChanged += async (_, e) =>
            {
                if (e.IsSelected && e.Item != null)
                    await EnsureHashForListItemAsync(e.Item);
            };


            // RichTextBox context menu clipboard
            copyToolStripMenuItem1.Click += (_, __) => rtfMainText.Copy();
            pasteToolStripMenuItem1.Click += (_, __) => rtfMainText.Paste();
            cutToolStripMenuItem1.Click += (_, __) => rtfMainText.Cut();
            selectAllToolStripMenuItem1.Click += (_, __) => rtfMainText.SelectAll();

            // RichTextBox context menu format
            boldToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Bold);
            italicToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Italic);
            underlineToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Underline);
            strikeToolStripMenuItem.Click += (_, __) => ToggleSelectionStyle(FontStyle.Strikeout);

            // Context menu colors
            forecolorToolStripMenuItem.Click += (_, __) => foreColorToolButton_Click(foreColorToolButton, EventArgs.Empty);
            textBackgroundcolorToolStripMenuItem.Click += (_, __) => toolStripButton1_Click(backgroundColorToolButton, EventArgs.Empty);

            // RTF toolbar format
            boldToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Bold);
            ItalicToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Italic);
            UnderlineToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Underline);
            StrikeTroughToolButton.Click += (_, __) => ToggleSelectionStyle(FontStyle.Strikeout);

            // Font size combo
            FontSizeComboBox.TextUpdate += FontSizeComboBox_TextUpdate;
            FontSizeComboBox.Click += FontSizeComboBox_Click;
            FontSizeComboBox.TextChanged += FontSizeComboBox_TextChanged;

            // Color toolbar
            foreColorToolButton.Click += foreColorToolButton_Click;
            backgroundColorToolButton.Click += toolStripButton1_Click;

            // Clipboard toolbar
            toolButtonCopy.Click += toolButtonCopy_Click;
            toolButtonPaste.Click += toolButtonPaste_Click;
            toolButtonCut.Click += toolButtonCut_Click;
            toolButtonSelectAll.Click += toolButtonSelectAll_Click;

            // Editor dirty tracking + auto-complete rebuild scheduling
            rtfMainText.TextChanged += (_, __) =>
            {
                if (_loadingBlockIntoEditor) return;

                if (_container != null && _currentBlockIndex >= 0)
                {
                    _blockDirty = true;
                    _containerDirty = true;
                    UpdateUiState();
                }

                ScheduleAutoCompleteRebuild();
            };

            // Update toolbar state when selection changes
            rtfMainText.SelectionChanged += (_, __) => UpdateRtfUiFromSelection();

            // Shortcuts in editor
            rtfMainText.KeyDown += RtfMainText_KeyDown;

            // Closing
            FormClosing += hsMain_FormClosing;

            keyLookupExternalDriveToolStripMenuItem.CheckedChanged += (_, __) =>
            {
                _appState.UseDriveKeySearch = keyLookupExternalDriveToolStripMenuItem.Checked;
                _appState.Save();
            };

        }

        // ============================================================
        // UI state
        // ============================================================
        private void UpdateUiState()
        {
            bool hasContainer = _container != null;

            saveContainerToolStripButton.Enabled = hasContainer;
            saveContainerToolStripMenuItem.Enabled = hasContainer;
            saveContainerAsToolStripMenuItem.Enabled = hasContainer;

            newBlockToolStripButton.Enabled = hasContainer;
            newToolStripMenuItem.Enabled = hasContainer;
            newBlockToolStripMenuItem.Enabled = hasContainer;

            renameBlockToolStripMenuItem.Enabled = hasContainer && _currentBlockIndex >= 0;

            ContainerSizeLabel.Visible = hasContainer;
            ContainerSizeLabel.Text = hasContainer
                ? $"Container: {ByteFormat.ToHumanSize(_container!.GetStoredSizeBytes())}"
                : "<Container_Size>";

            // NEW: Title logic centralized
            UpdateWindowTitle();
        }


        private void hsMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!MaybeCommitCurrentBlock())
            {
                e.Cancel = true;
                return;
            }

            if (_containerDirty)
            {
                DialogResult res = MessageBox.Show(
                    this,
                    "The container has unsaved changes. Save now?",
                    Global.AppName,
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (res == DialogResult.Yes)
                {
                    if (!UiSaveContainerBlocking())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            SaveWindowPlacement();
            _appState.Save();

            _acRebuildTimer.Stop();
            _rtfAutoComplete?.Dispose();

            // IMPORTANT: stop background hash work before the form/control is disposed
            CancelHashWorkers();

            _container?.CloseKeyMaterial();
        }

        // ============================================================
        // Container operations (V2-only create/open/save) (ASYNC)
        // ============================================================
        private async Task<bool> UiCreateNewContainerAsync(bool initialStartup = false)
        {
            // DO NOT auto-generate keys anymore
            if (!EnsurePrivateKeyConfiguredInteractive())
                return false;

            if (string.IsNullOrWhiteSpace(_privateKeyPath) || !File.Exists(_privateKeyPath))
            {
                MessageBox.Show(this, "No private key is configured.", "Create container",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            using SaveFileDialog sfd = new()
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|All files (*.*)|*.*",
                DefaultExt = "hstry",
                AddExtension = true,
                FileName = "container.hstry",
                Title = "Create container"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return false;

            string containerPath = sfd.FileName;


            try
            {
                HSTRYContainer created = await reporterDiag.RunAsync(
                    owner: this,
                    title: "Create container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Creating container…", Indeterminate = true });

                        return await Task.Run(() =>
                        {
                            using RSA ownerPriv = HSTRYContainer.RsaKeyFiles.LoadPrivateKeyPkcs8(_privateKeyPath);

                            HSTRYContainer c = HSTRYContainer.CreateNewForRecipients(
                                ownerPrivateKey: ownerPriv,
                                recipientPublicKeys: new[] { ownerPriv },
                                encoding: Global.CurrentEditorEncoding);

                            c.Save(containerPath);
                            return c;
                        }, token);
                    });

                // IMPORTANT: stop running hash jobs before switching container/list
                CancelHashWorkers();

                _container?.CloseKeyMaterial();
                _container = created;
                _containerPath = containerPath;

                _currentBlockIndex = -1;
                _loadingBlockIntoEditor = false;
                _blockDirty = false;
                _containerDirty = false;

                lvwBlocks.Items.Clear();
                ClearEditor();
                UpdateUiState();

                _appState.TouchRecentFile(_containerPath);
                _appState.Save();

                RebuildAutoCompleteWordsFromEditor();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> TryOpenContainerInteractiveAsync()
        {
            if (!MaybeCommitCurrentBlock()) return false;

            if (_containerDirty)
            {
                DialogResult res = MessageBox.Show(
                    this,
                    "The container has unsaved changes. Continue without saving?",
                    "Open container",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (res != DialogResult.Yes) return false;
            }

            using OpenFileDialog ofd = new()
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (ofd.ShowDialog(this) != DialogResult.OK) return false;

            if (!LooksLikeHstryFile(ofd.FileName))
            {
                MessageBox.Show(this, "The selected file is not a valid HSTRY container file.", "Open container",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return await OpenContainerFromPathAsync(ofd.FileName);
        }

        private async Task UiOpenContainerAsync() => _ = await TryOpenContainerInteractiveAsync();

        private async Task<bool> UiSaveContainerAsync()
        {
            if (_container == null) return false;
            if (!MaybeCommitCurrentBlock()) return false;

            if (string.IsNullOrWhiteSpace(_containerPath))
                return await UiSaveContainerAsAsync();

            try
            {
                await reporterDiag.RunAsync<object>(
                    owner: this,
                    title: "Save container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Saving container…", Indeterminate = true });

                        await Task.Run(() => _container.Save(_containerPath!), token);
                        return new object();
                    });

                _containerDirty = false;
                UpdateUiState();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool UiSaveContainerBlocking()
        {
            // used only in FormClosing (no async)
            return UiSaveContainerAsync().GetAwaiter().GetResult();
        }

        private async Task<bool> UiSaveContainerAsAsync()
        {
            if (_container == null) return false;
            if (!MaybeCommitCurrentBlock()) return false;

            using SaveFileDialog sfd = new()
            {
                Filter = "HSTRY Container (*.hstry)|*.hstry|All files (*.*)|*.*",
                DefaultExt = "hstry",
                AddExtension = true,
                FileName = string.IsNullOrWhiteSpace(_containerPath) ? "container.hstry" : Path.GetFileName(_containerPath)
            };

            if (sfd.ShowDialog(this) != DialogResult.OK) return false;

            try
            {
                string target = sfd.FileName;

                await reporterDiag.RunAsync<object>(
                    owner: this,
                    title: "Save container as",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Saving container…", Indeterminate = true });

                        await Task.Run(() => _container.Save(target), token);
                        return new object();
                    });

                _containerPath = target;
                _containerDirty = false;
                UpdateUiState();

                _appState.TouchRecentFile(_containerPath!);
                _appState.Save();

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save container as", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // ============================================================
        // Blocks / Editor
        // ============================================================
        private void UiNewBlock()
        {
            if (_container == null)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Yield();
                    BeginInvoke(new Action(async () =>
                    {
                        if (!await UiCreateNewContainerAsync()) return;
                        UiNewBlock();
                    }));
                });
                return;
            }

            if (!MaybeCommitCurrentBlock()) return;

            string title = _container.GenerateUniqueTitle();
            using (TextPromptDialog dlg = new("New block", "Block name:", title))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                title = dlg.InputText;
            }

            string emptyRtf;
            _loadingBlockIntoEditor = true;
            try
            {
                rtfMainText.Clear();
                emptyRtf = rtfMainText.Rtf ?? string.Empty;
            }
            finally
            {
                _loadingBlockIntoEditor = false;
            }

            try
            {
                // Stop hashing before mutation + list rebuild
                CancelHashWorkers();

                Block b = _container.AddRtfDocument(title, emptyRtf);
                _containerDirty = true;

                RefreshBlockList(selectIndex: b.Index);
                LoadBlockIntoEditor(b.Index);
                UpdateUiState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "New block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiRenameBlock()
        {
            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (!MaybeCommitCurrentBlock()) return;

            Block b = _container.Blocks[idx];

            using TextPromptDialog dlg = new("Rename block", "New name:", b.Title);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // Stop hashing before mutation + list rebuild
                CancelHashWorkers();

                _container.RenameBlock(idx, dlg.InputText);
                _containerDirty = true;

                RefreshBlockList(selectIndex: idx);
                LoadBlockIntoEditor(idx);
                UpdateUiState();

                RebuildAutoCompleteWordsFromEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Rename block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void UiRemoveBlock()
        {
            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (!MaybeCommitCurrentBlock()) return;

            Block b = _container.Blocks[idx];

            DialogResult result = MessageBox.Show(
                this,
                $"Do you really want to delete this block?\n\n{b.Title}",
                "Delete Block",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return;

            try
            {
                // Stop hashing before removal (indices shift) + list rebuild
                CancelHashWorkers();

                _container.RemoveBlock(idx);
                _containerDirty = true;

                int newIndex = -1;
                if (_container.Blocks.Count > 0)
                    newIndex = Math.Min(idx, _container.Blocks.Count - 1);

                RefreshBlockList(selectIndex: newIndex);

                if (newIndex >= 0)
                    LoadBlockIntoEditor(newIndex);
                else
                {
                    rtfMainText.Clear();
                    splitContainer1.Panel2.Enabled = false;
                }

                UpdateUiState();
                RebuildAutoCompleteWordsFromEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete Block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UiSelectBlockFromList()
        {
            if (_suppressBlockSelectionChanged) return;

            splitContainer1.Panel2.Enabled = false;

            if (_container == null) return;

            int idx = GetSelectedBlockIndex();
            if (idx < 0) return;

            if (idx == _currentBlockIndex)
            {
                splitContainer1.Panel2.Enabled = true;
                return;
            }

            if (!MaybeCommitCurrentBlockBeforeSwitch())
            {
                _suppressBlockSelectionChanged = true;
                try { SelectListIndex(_currentBlockIndex); }
                finally { _suppressBlockSelectionChanged = false; }
                return;
            }

            splitContainer1.Panel2.Enabled = true;
            rtfMainText.Focus();

            LoadBlockIntoEditor(idx);
            UpdateUiState();
        }

        private void LoadBlockIntoEditor(int index)
        {
            if (_container == null) return;

            try
            {
                _loadingBlockIntoEditor = true;

                string rtf = _container.GetRtfDocument(index);
                rtfMainText.Rtf = rtf ?? string.Empty;

                _currentBlockIndex = index;
                _blockDirty = false;

                UpdateRtfUiFromSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load block", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loadingBlockIntoEditor = false;
                RebuildAutoCompleteWordsFromEditor();
            }
        }

        private bool MaybeCommitCurrentBlock()
        {
            if (_container == null) return true;
            if (_currentBlockIndex < 0) return true;
            if (!_blockDirty) return true;

            DialogResult res = MessageBox.Show(
                this,
                "The current block has been modified. Apply changes?",
                Global.AppName,
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.No)
            {
                LoadBlockIntoEditor(_currentBlockIndex);
                return true;
            }

            try
            {
                // Stop hashing before heavy update/re-encrypt and list rebuild
                CancelHashWorkers();

                _container.UpdateRtfDocument(_currentBlockIndex, rtfMainText.Rtf ?? string.Empty);
                _blockDirty = false;
                _containerDirty = true;

                RefreshBlockList(selectIndex: _currentBlockIndex);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Apply changes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool MaybeCommitCurrentBlockBeforeSwitch()
        {
            if (_container == null) return true;
            if (_currentBlockIndex < 0) return true;
            if (!_blockDirty) return true;

            DialogResult res = MessageBox.Show(
                this,
                "This block has unsaved changes. Save changes before switching blocks?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.No)
            {
                LoadBlockIntoEditor(_currentBlockIndex);
                return true;
            }

            try
            {
                // Stop hashing before heavy update/re-encrypt and list rebuild
                CancelHashWorkers();

                _container.UpdateRtfDocument(_currentBlockIndex, rtfMainText.Rtf ?? string.Empty);
                _blockDirty = false;
                _containerDirty = true;

                RefreshBlockList(selectIndex: _currentBlockIndex);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save Changes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void RefreshBlockList(int? selectIndex = null)
        {
            if (_container == null)
            {
                lvwBlocks.Items.Clear();
                return;
            }

            // IMPORTANT: stop any running hash jobs before rebuilding the list
            CancelHashWorkers();

            int total = _container.Blocks.Count;
            bool computeSync = total <= 500;

            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.Items.Clear();

                foreach (Block b in _container.Blocks)
                {
                    string hashText = computeSync
                        ? Convert.ToHexString(_container.ComputeBlockHash(b))
                        : "Computing…";

                    string size = ByteFormat.ToHumanSize(b.StoredSizeBytes);

                    string created = b.CreatedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");
                    string changed = b.ModifiedUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss:ffffff");

                    ListViewItem item = new(b.Title);
                    item.SubItems.Add(hashText);
                    item.SubItems.Add(size);
                    item.SubItems.Add(created);
                    item.SubItems.Add(changed);
                    item.Tag = b.Index;

                    lvwBlocks.Items.Add(item);
                }
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }

            UpdateUiState();

            if (selectIndex.HasValue)
                SelectListIndex(selectIndex.Value);

            if (!computeSync && total > 0)
                StartHashFillAsync();
        }


        private void StartHashFillAsync()
        {
            if (_container == null) return;
            if (lvwBlocks.IsDisposed) return;

            _hashFillCts?.Cancel();
            _hashFillCts?.Dispose();
            _hashFillCts = new CancellationTokenSource();

            CancellationToken token = _hashFillCts.Token;

            int count = _container.Blocks.Count;

            _ = Task.Run(() =>
            {
                const int batchSize = 25;
                var batch = new List<(int index, string hex)>(batchSize);

                for (int i = 0; i < count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    string hex = Convert.ToHexString(_container!.ComputeBlockHash(_container.Blocks[i]));
                    batch.Add((i, hex));

                    if (batch.Count >= batchSize)
                    {
                        // IMPORTANT: snapshot copy to avoid "Collection was modified"
                        var snapshot = batch.ToArray();
                        PostBatchToUi(snapshot);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    var snapshot = batch.ToArray();
                    PostBatchToUi(snapshot);
                }

            }, token);

            void PostBatchToUi((int index, string hex)[] updates)
            {
                if (token.IsCancellationRequested) return;
                if (IsDisposed) return;

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        if (_container == null) return;
                        if (lvwBlocks.IsDisposed) return;

                        int itemCount = lvwBlocks.Items.Count;

                        for (int k = 0; k < updates.Length; k++)
                        {
                            int idx = updates[k].index;
                            string hex = updates[k].hex;

                            if (idx < 0 || idx >= itemCount) continue;

                            var it = lvwBlocks.Items[idx];
                            if (it.SubItems.Count < 2) continue;

                            if (it.SubItems[1].Text == "Computing…" || string.IsNullOrWhiteSpace(it.SubItems[1].Text))
                                it.SubItems[1].Text = hex;
                        }
                    }));
                }
                catch
                {
                    // Ignore UI race (form closing etc.)
                }
            }
        }

        private async Task EnsureHashForListItemAsync(ListViewItem item)
        {
            if (_container == null) return;
            if (item == null) return;

            // Hash column index 1 (Name is 0)
            if (item.SubItems.Count < 2) return;

            // Already computed?
            if (!string.IsNullOrWhiteSpace(item.SubItems[1].Text)) return;

            if (item.Tag is not int idx) return;
            if (idx < 0 || idx >= _container.Blocks.Count) return;

            // Cancel previous ongoing hash
            _hashCts?.Cancel();
            _hashCts?.Dispose();
            _hashCts = new CancellationTokenSource();
            var token = _hashCts.Token;

            item.SubItems[1].Text = "Computing…";

            try
            {
                string hex = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    byte[] h = _container.ComputeBlockHash(_container.Blocks[idx]);
                    return Convert.ToHexString(h);
                }, token);

                if (!token.IsCancellationRequested)
                    item.SubItems[1].Text = hex;
            }
            catch
            {
                if (!token.IsCancellationRequested)
                    item.SubItems[1].Text = string.Empty;
            }
        }


        private int GetSelectedBlockIndex()
        {
            if (lvwBlocks.SelectedItems.Count == 0) return -1;
            ListViewItem item = lvwBlocks.SelectedItems[0];
            return item.Tag is int i ? i : item.Index;
        }

        private void SelectListIndex(int index)
        {
            if (index < 0 || index >= lvwBlocks.Items.Count) return;

            lvwBlocks.BeginUpdate();
            try
            {
                lvwBlocks.SelectedItems.Clear();
                lvwBlocks.Items[index].Selected = true;
                lvwBlocks.Items[index].Focused = true;
                lvwBlocks.EnsureVisible(index);
            }
            finally
            {
                lvwBlocks.EndUpdate();
            }
        }

        private void ClearEditor()
        {
            _loadingBlockIntoEditor = true;
            try
            {
                rtfMainText.Clear();
                _currentBlockIndex = -1;
                _blockDirty = false;
            }
            finally
            {
                _loadingBlockIntoEditor = false;
                RebuildAutoCompleteWordsFromEditor();
            }
        }

        // ============================================================
        // Search
        // ============================================================
        private void UiSearchInBlock()
        {
            if (_container == null || _currentBlockIndex < 0)
            {
                MessageBox.Show(this, "No block is currently open.", "Search in block",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FindDialog dlg = new()
            {
                StartPosition = FormStartPosition.CenterParent,
                QueryText = string.IsNullOrWhiteSpace(_lastFindText) ? (rtfMainText.SelectedText ?? string.Empty) : _lastFindText,
                MatchCase = _lastFindMatchCase,
                WholeWord = _lastFindWholeWord,
                Wrap = _lastFindWrap
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _lastFindText = dlg.QueryText ?? string.Empty;
            _lastFindMatchCase = dlg.MatchCase;
            _lastFindWholeWord = dlg.WholeWord;
            _lastFindWrap = dlg.Wrap;

            FindNextInEditor(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, _lastFindWrap);
        }

        private async Task UiSearchInContainerAsync()
        {
            if (_container == null)
            {
                MessageBox.Show(this, "No container is currently open.", "Search in container",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FindDialog dlg = new()
            {
                StartPosition = FormStartPosition.CenterParent,
                Text = "Search in container",
                QueryText = _lastFindText,
                MatchCase = _lastFindMatchCase,
                WholeWord = _lastFindWholeWord,
                Wrap = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            string query = dlg.QueryText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
                return;

            _lastFindText = query;
            _lastFindMatchCase = dlg.MatchCase;
            _lastFindWholeWord = dlg.WholeWord;
            _lastFindWrap = true;

            try
            {
                // offload search; RTF->Text uses RichTextBox => run on STA thread
                List<ContainerSearchHit> results = await reporterDiag.RunAsync(
                    owner: this,
                    title: "Search in container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress
                        {
                            Message = "Searching…",
                            Maximum = _container.Blocks.Count,
                            Value = 0,
                            Indeterminate = false
                        });

                        return await StaTask.RunAsync(() =>
                        {
                            List<ContainerSearchHit> hits = new();
                            using RichTextBox tmp = new();

                            for (int i = 0; i < _container.Blocks.Count; i++)
                            {
                                token.ThrowIfCancellationRequested();

                                progress.Report(new UiProgress
                                {
                                    Message = $"Searching block {i + 1}/{_container.Blocks.Count}…",
                                    Value = i + 1
                                });

                                string rtf = _container.GetRtfDocument(i);
                                tmp.Rtf = rtf ?? string.Empty;
                                string text = tmp.Text ?? string.Empty;

                                int idx = FindIndex(text, query, dlg.MatchCase, dlg.WholeWord);
                                if (idx >= 0)
                                {
                                    string snippet = BuildSnippet(text, idx, query.Length, 40);
                                    hits.Add(new ContainerSearchHit(i, _container.Blocks[i].Title, idx, snippet));
                                }
                            }

                            return hits;
                        }, token);
                    });

                if (results.Count == 0)
                {
                    MessageBox.Show(this, "No matches were found in the container.", "Search in container",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using ContainerSearchResultsDialog resDlg = new();
                resDlg.SetResults(results);

                if (resDlg.ShowDialog(this) != DialogResult.OK)
                    return;

                ContainerSearchHit? hit = resDlg.SelectedHit;
                if (hit == null) return;

                if (!MaybeCommitCurrentBlock())
                    return;

                LoadBlockIntoEditor(hit.BlockIndex);
                FindNextInEditor(query, dlg.MatchCase, dlg.WholeWord, wrap: true);
            }
            catch (OperationCanceledException)
            {
                // user canceled search
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Search in container", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FindNextInEditor(string query, bool matchCase, bool wholeWord, bool wrap)
        {
            if (string.IsNullOrEmpty(query))
                return;

            RichTextBoxFinds opts = RichTextBoxFinds.None;
            if (matchCase) opts |= RichTextBoxFinds.MatchCase;
            if (wholeWord) opts |= RichTextBoxFinds.WholeWord;

            int start = rtfMainText.SelectionStart + rtfMainText.SelectionLength;
            int idx = rtfMainText.Find(query, start, opts);

            if (idx < 0 && wrap)
                idx = rtfMainText.Find(query, 0, opts);

            if (idx < 0)
            {
                MessageBox.Show(this, "No matches found.", "Search",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            rtfMainText.Select(idx, query.Length);
            rtfMainText.ScrollToCaret();
            rtfMainText.Focus();
        }

        private static int FindIndex(string haystack, string needle, bool matchCase, bool wholeWord)
        {
            if (string.IsNullOrEmpty(needle)) return -1;

            StringComparison comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
            int idx = haystack.IndexOf(needle, comparison);
            if (idx < 0) return -1;

            if (!wholeWord) return idx;

            static bool IsWordChar(char c) =>
                char.IsLetterOrDigit(c) || c == '_' ||
                c == 'Ä' || c == 'Ö' || c == 'Ü' ||
                c == 'ä' || c == 'ö' || c == 'ü' || c == 'ß';

            int left = idx - 1;
            int right = idx + needle.Length;

            if (left >= 0 && IsWordChar(haystack[left])) return -1;
            if (right < haystack.Length && IsWordChar(haystack[right])) return -1;

            return idx;
        }

        private static string BuildSnippet(string text, int index, int length, int context)
        {
            int start = Math.Max(0, index - context);
            int end = Math.Min(text.Length, index + length + context);
            string snippet = text.Substring(start, end - start).Replace("\r", " ").Replace("\n", " ");
            if (start > 0) snippet = "…" + snippet;
            if (end < text.Length) snippet = snippet + "…";
            return snippet;
        }

        // ============================================================
        // RTF Formatting + Clipboard + Shortcuts
        // ============================================================
        private void RtfMainText_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && !e.Shift && e.KeyCode == Keys.F)
            {
                UiSearchInBlock();
                e.Handled = true;
                return;
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                _ = UiSearchInContainerAsync();
                e.Handled = true;
                return;
            }

            if (!e.Control && !e.Shift && e.KeyCode == Keys.F3)
            {
                if (!string.IsNullOrWhiteSpace(_lastFindText))
                    FindNextInEditor(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, _lastFindWrap);

                e.Handled = true;
            }
        }

        private void UpdateRtfUiFromSelection()
        {
            if (_updatingParagraphUi) return;

            Font f = rtfMainText.SelectionFont ?? rtfMainText.Font;

            boldToolButton.Checked = f.Bold;
            ItalicToolButton.Checked = f.Italic;
            UnderlineToolButton.Checked = f.Underline;
            StrikeTroughToolButton.Checked = f.Strikeout;

            _updatingFontSizeUi = true;
            try
            {
                FontSizeComboBox.Text = ((int)Math.Round(f.Size)).ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                _updatingFontSizeUi = false;
            }

            // Paragraph UI (alignment + bullets + numbering)
            _updatingParagraphUi = true;
            try
            {
                HorizontalAlignment al = rtfMainText.SelectionAlignment;

                if (toolButtonAlignLeft != null) toolButtonAlignLeft.Checked = (al == HorizontalAlignment.Left);
                if (toolButtonAlignCenter != null) toolButtonAlignCenter.Checked = (al == HorizontalAlignment.Center);
                if (toolButtonAlignRight != null) toolButtonAlignRight.Checked = (al == HorizontalAlignment.Right);

                if (toolStripButtonBullets != null) toolStripButtonBullets.Checked = rtfMainText.SelectionBullet;

                // Numeric list state (read from RichEdit paragraph format)
                ushort numbering = GetCurrentParagraphNumbering(rtfMainText);
                bool numericOn = numbering != PFN_NONE;

                if (toolButtonBulletsNumeric != null) toolButtonBulletsNumeric.Checked = numericOn;
            }
            finally
            {
                _updatingParagraphUi = false;
            }
        }

        private void FontSizeComboBox_Click(object? sender, EventArgs e) { }

        private void FontSizeComboBox_TextUpdate(object? sender, EventArgs e)
        {
            if (_updatingFontSizeUi) return;
            if (sender is not ToolStripComboBox cb) return;

            string raw = (cb.Text ?? string.Empty).Trim()
                .Replace("pt", "", StringComparison.OrdinalIgnoreCase);

            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out float size) &&
                !float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                return;
            }

            if (size < 1f) size = 1f;
            if (size > 200f) size = 200f;

            ApplySelectionFontSize(rtfMainText, size);
            UpdateRtfUiFromSelection();
        }

        private void FontSizeComboBox_TextChanged(object? sender, EventArgs e)
        {
            if (_updatingFontSizeUi) return;
            if (sender is not ToolStripComboBox cb) return;

            string raw = (cb.Text ?? string.Empty).Trim()
                .Replace("pt", "", StringComparison.OrdinalIgnoreCase);

            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out float size) &&
                !float.TryParse(raw.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                return;
            }

            if (size < 1f) size = 1f;
            if (size > 200f) size = 200f;

            ApplySelectionFontSize(rtfMainText, size);
            UpdateRtfUiFromSelection();
        }

        private static void ApplySelectionFontSize(RichTextBox rtb, float newSize)
        {
            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                rtb.SelectionFont = new Font(baseFont.FontFamily, newSize, baseFont.Style);
                return;
            }

            if (rtb.SelectionFont != null)
            {
                Font f = rtb.SelectionFont;
                rtb.SelectionFont = new Font(f.FontFamily, newSize, f.Style);
                rtb.Select(start, len);
                return;
            }

            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);
                    Font f = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(f.FontFamily, newSize, f.Style);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
            }
        }

        private void ToggleSelectionStyle(FontStyle styleToToggle)
        {
            RichTextBox rtb = rtfMainText;

            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                FontStyle newStyle = baseFont.Style ^ styleToToggle;
                rtb.SelectionFont = new Font(baseFont, newStyle);
                rtb.Focus();
                UpdateRtfUiFromSelection();
                return;
            }

            if (rtb.SelectionFont != null)
            {
                Font f = rtb.SelectionFont;
                FontStyle newStyle = f.Style ^ styleToToggle;
                rtb.SelectionFont = new Font(f.FontFamily, f.Size, newStyle);
                rtb.Select(start, len);
                rtb.Focus();
                UpdateRtfUiFromSelection();
                return;
            }

            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);

                    Font f = rtb.SelectionFont ?? rtb.Font;
                    FontStyle newStyle = f.Style ^ styleToToggle;

                    rtb.SelectionFont = new Font(f.FontFamily, f.Size, newStyle);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
                UpdateRtfUiFromSelection();
            }
        }

        private void foreColorToolButton_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;
            Color current = item.Tag is Color c ? c : Color.Red;

            ShowColorPickerPopup(item, current, newColor =>
            {
                item.Tag = newColor;

                int selStart = rtfMainText.SelectionStart;
                int selLen = rtfMainText.SelectionLength;

                rtfMainText.SelectionColor = newColor;

                rtfMainText.Focus();
                rtfMainText.Select(selStart, selLen);
            });
        }

        private void toolStripButton1_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripItem item) return;
            Color current = item.Tag is Color c ? c : Color.Yellow;

            ShowColorPickerPopup(item, current, newColor =>
            {
                item.Tag = newColor;

                int selStart = rtfMainText.SelectionStart;
                int selLen = rtfMainText.SelectionLength;

                rtfMainText.SelectionBackColor = newColor;

                rtfMainText.Focus();
                rtfMainText.Select(selStart, selLen);
            });
        }

        private static Point AdjustToWorkingArea(Point desired, Size size)
        {
            Rectangle wa = Screen.FromPoint(desired).WorkingArea;

            int x = desired.X;
            int y = desired.Y;

            if (x + size.Width > wa.Right) x = wa.Right - size.Width;
            if (x < wa.Left) x = wa.Left;

            if (y + size.Height > wa.Bottom) y = wa.Bottom - size.Height;
            if (y < wa.Top) y = wa.Top;

            return new Point(x, y);
        }

        private void ShowColorPickerPopup(ToolStripItem ownerItem, Color currentColor, Action<Color> onOk)
        {
            if (_colorPopup != null && !_colorPopup.IsDisposed)
            {
                _colorPopup.Close();
                _colorPopup = null;
                return;
            }

            ToolStrip? ts = ownerItem.Owner;
            if (ts == null) return;

            Rectangle rect = ownerItem.Bounds;
            Point screenPos = ts.PointToScreen(new Point(rect.Left, rect.Bottom));

            colorPicker dlg = new(currentColor)
            {
                StartPosition = FormStartPosition.Manual,
                Location = AdjustToWorkingArea(screenPos, new Size(490, 288)),
                CloseOnDeactivate = true,
                TopMost = true,
                ShowInTaskbar = false
            };

            dlg.FormClosed += (s, args) =>
            {
                if (dlg.DialogResult == DialogResult.OK)
                    onOk(dlg.SelectedColor);

                _colorPopup = null;
                dlg.Dispose();
            };

            _colorPopup = dlg;
            dlg.Show(this);
            dlg.BringToFront();
            dlg.Activate();
        }

        private void toolButtonCopy_Click(object? sender, EventArgs e) => rtfMainText.Copy();
        private void toolButtonPaste_Click(object? sender, EventArgs e) => rtfMainText.Paste();
        private void toolButtonCut_Click(object? sender, EventArgs e) => rtfMainText.Cut();
        private void toolButtonSelectAll_Click(object? sender, EventArgs e) => rtfMainText.SelectAll();

        // ============================================================
        // Required by designer (Exit)
        // ============================================================
        private void closeToolStripMenuItem_Click(object sender, EventArgs e) => Close();

        private void exportBlockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_container == null)
            {
                MessageBox.Show(this, "No container is currently open.", "Export blocks",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!MaybeCommitCurrentBlock())
                return;

            using exporterDiag dlg = new(_container)
            {
                StartPosition = FormStartPosition.CenterParent
            };

            dlg.ShowDialog(this);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aboutDiag abt = new();
            abt.ShowDialog(this);
        }

        private void hilfeToolStripButton_Click(object sender, EventArgs e)
        {
            aboutToolStripMenuItem.PerformClick();
        }

        // ============================================================
        // Headings + Table insert
        // ============================================================
        private void heading1ToolStripMenuItem_Click(object sender, EventArgs e) => ApplyHeadingSize(rtfMainText, H1_SIZE, FontStyle.Bold);
        private void heading2ToolStripMenuItem_Click(object sender, EventArgs e) => ApplyHeadingSize(rtfMainText, H2_SIZE, FontStyle.Bold);
        private void heading3ToolStripMenuItem_Click(object sender, EventArgs e) => ApplyHeadingSize(rtfMainText, H3_SIZE, FontStyle.Bold);

        // SAFE: preserves mixed formatting by applying per-character
        private static void ApplyHeadingSize(RichTextBox rtb, float size, FontStyle addStyle)
        {
            if (rtb == null) return;

            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                rtb.SelectionFont = new Font(baseFont.FontFamily, size, baseFont.Style | addStyle);
                return;
            }

            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);
                    Font f = rtb.SelectionFont ?? rtb.Font;
                    rtb.SelectionFont = new Font(f.FontFamily, size, f.Style | addStyle);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
            }
        }

        private void toolTableInsert_Click(object sender, EventArgs e)
        {
            using InsertTableDialog dlg = new(rtfMainText);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (!string.IsNullOrWhiteSpace(dlg.ResultRtf))
                rtfMainText.SelectedRtf = dlg.ResultRtf;
        }

        private static bool IsCaretInTable(RichTextBox rtb)
        {
            if (rtb.TextLength == 0) return false;

            int origStart = rtb.SelectionStart;
            int origLen = rtb.SelectionLength;

            int probeStart = origStart;
            int probeLen = origLen;

            if (probeLen == 0)
            {
                if (probeStart < rtb.TextLength) probeLen = 1;
                else if (probeStart > 0) { probeStart--; probeLen = 1; }
                else return false;
            }

            rtb.Select(probeStart, probeLen);
            string frag = rtb.SelectedRtf ?? string.Empty;
            rtb.Select(origStart, origLen);

            return frag.Contains(@"\intbl", StringComparison.Ordinal) ||
                   frag.Contains(@"\trowd", StringComparison.Ordinal);
        }

        private void UiTableAddRow() => _ = TryModifyTableAtCaret(TableOp.AddRow);
        private void UiTableRemoveRow() => _ = TryModifyTableAtCaret(TableOp.RemoveRow);
        private void UiTableAddColumn() => _ = TryModifyTableAtCaret(TableOp.AddColumn);
        private void UiTableRemoveColumn() => _ = TryModifyTableAtCaret(TableOp.RemoveColumn);

        private enum TableOp { AddRow, RemoveRow, AddColumn, RemoveColumn }

        private sealed class TableContext
        {
            public string Marker = string.Empty;
            public int MarkerIndex;

            public int RowStart;
            public int RowEndTokenEnd;

            public int TableStart;
            public int TableEnd;

            public int ColIndex;
            public int ColCount;
        }

        private bool TryModifyTableAtCaret(TableOp op)
        {
            if (!IsCaretInTable(rtfMainText))
                return false;

            int caretPlain = rtfMainText.SelectionStart;
            string marker = "HSTRY_CARET_" + Guid.NewGuid().ToString("N");

            int origStart = rtfMainText.SelectionStart;
            rtfMainText.Select(origStart, 0);
            rtfMainText.SelectedText = marker;

            string rtfWithMarker = rtfMainText.Rtf ?? string.Empty;

            int markerIndex = rtfWithMarker.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                RemoveInsertedMarker(marker, caretPlain);
                return false;
            }

            if (!TryGetTableContextFromRtf(rtfWithMarker, marker, markerIndex, out TableContext ctx))
            {
                RemoveInsertedMarker(marker, caretPlain);
                return false;
            }

            string modified = rtfWithMarker;

            switch (op)
            {
                case TableOp.AddRow:
                    modified = AddRow(modified, ctx);
                    break;
                case TableOp.RemoveRow:
                    modified = RemoveRow(modified, ctx);
                    break;
                case TableOp.AddColumn:
                    modified = AddOrRemoveColumn(modified, ctx, add: true);
                    break;
                case TableOp.RemoveColumn:
                    modified = AddOrRemoveColumn(modified, ctx, add: false);
                    break;
            }

            modified = modified.Replace(marker, "", StringComparison.Ordinal);

            rtfMainText.Rtf = modified;

            int restore = Math.Min(caretPlain, rtfMainText.TextLength);
            rtfMainText.Select(restore, 0);
            rtfMainText.Focus();

            return true;
        }

        private void RemoveInsertedMarker(string marker, int caretPlain)
        {
            try
            {
                if (caretPlain >= 0 && caretPlain + marker.Length <= rtfMainText.TextLength)
                {
                    rtfMainText.Select(caretPlain, marker.Length);
                    if ((rtfMainText.SelectedText ?? "") == marker)
                        rtfMainText.SelectedText = "";
                }
            }
            catch { }
        }

        private static bool TryGetTableContextFromRtf(string rtf, string marker, int markerIndex, out TableContext ctx)
        {
            ctx = new TableContext { Marker = marker, MarkerIndex = markerIndex };

            int rowStart = rtf.LastIndexOf(@"\trowd", markerIndex, StringComparison.Ordinal);
            if (rowStart < 0) return false;

            int rowToken = rtf.IndexOf(@"\row", markerIndex, StringComparison.Ordinal);
            if (rowToken < 0) return false;

            int rowEndTokenEnd = FindRowEndTokenEnd(rtf, rowToken);

            int intblPos = rtf.IndexOf(@"\intbl", rowStart, StringComparison.Ordinal);
            if (intblPos < 0 || intblPos > rowToken) return false;

            int colCount = CountOccurrences(rtf, @"\cellx", rowStart, intblPos);
            if (colCount <= 0) return false;

            int colIndex = CountOccurrences(rtf, @"\cell ", rowStart, markerIndex);
            if (colIndex < 0) colIndex = 0;
            if (colIndex >= colCount) colIndex = colCount - 1;

            int tableStart = rowStart;
            while (true)
            {
                int prevRowStart = rtf.LastIndexOf(@"\trowd", tableStart - 1, StringComparison.Ordinal);
                if (prevRowStart < 0) break;

                int prevRowToken = rtf.IndexOf(@"\row", prevRowStart, StringComparison.Ordinal);
                if (prevRowToken < 0) break;

                int prevEnd = FindRowEndTokenEnd(rtf, prevRowToken);
                if (prevEnd != tableStart) break;

                tableStart = prevRowStart;
            }

            int tableEnd = rowEndTokenEnd;
            while (true)
            {
                if (tableEnd >= rtf.Length) break;
                if (!rtf.AsSpan(tableEnd).StartsWith(@"\trowd", StringComparison.Ordinal)) break;

                int nextRowStart = tableEnd;
                int nextRowToken = rtf.IndexOf(@"\row", nextRowStart, StringComparison.Ordinal);
                if (nextRowToken < 0) break;

                int nextEnd = FindRowEndTokenEnd(rtf, nextRowToken);
                tableEnd = nextEnd;
            }

            ctx.RowStart = rowStart;
            ctx.RowEndTokenEnd = rowEndTokenEnd;
            ctx.TableStart = tableStart;
            ctx.TableEnd = tableEnd;
            ctx.ColIndex = colIndex;
            ctx.ColCount = colCount;

            return true;
        }

        private static int FindRowEndTokenEnd(string rtf, int rowTokenIndex)
        {
            int i = rowTokenIndex + 4; // "\row"
            while (i < rtf.Length && char.IsWhiteSpace(rtf[i]))
                i++;
            return i;
        }

        private static int CountOccurrences(string s, string token, int start, int endExclusive)
        {
            if (start < 0) start = 0;
            if (endExclusive > s.Length) endExclusive = s.Length;
            if (endExclusive <= start) return 0;

            int count = 0;
            int idx = start;
            while (true)
            {
                idx = s.IndexOf(token, idx, endExclusive - idx, StringComparison.Ordinal);
                if (idx < 0) break;
                count++;
                idx += token.Length;
            }
            return count;
        }

        private static string AddRow(string rtf, TableContext ctx)
        {
            int rowLen = ctx.RowEndTokenEnd - ctx.RowStart;
            if (rowLen <= 0) return rtf;

            string rowSegment = rtf.Substring(ctx.RowStart, rowLen);

            int intblPos = rowSegment.IndexOf(@"\intbl", StringComparison.Ordinal);
            if (intblPos < 0) return rtf;

            string header = rowSegment.Substring(0, intblPos);

            int rowTokenLocal = rowSegment.LastIndexOf(@"\row", StringComparison.Ordinal);
            if (rowTokenLocal < 0) return rtf;

            string tail = rowSegment.Substring(rowTokenLocal);

            StringBuilder sb = new();
            sb.Append(header);
            for (int c = 0; c < ctx.ColCount; c++)
                sb.Append(@"\intbl \cell ");
            sb.Append(tail);

            return rtf.Insert(ctx.RowEndTokenEnd, sb.ToString());
        }

        private static string RemoveRow(string rtf, TableContext ctx)
        {
            int len = ctx.RowEndTokenEnd - ctx.RowStart;
            if (len <= 0) return rtf;
            return rtf.Remove(ctx.RowStart, len);
        }

        private static string AddOrRemoveColumn(string rtf, TableContext ctx, bool add)
        {
            int tableLen = ctx.TableEnd - ctx.TableStart;
            if (tableLen <= 0) return rtf;

            string table = rtf.Substring(ctx.TableStart, tableLen);

            StringBuilder sb = new();
            int pos = 0;

            while (pos < table.Length)
            {
                int rowStart = table.IndexOf(@"\trowd", pos, StringComparison.Ordinal);
                if (rowStart < 0) break;

                sb.Append(table, pos, rowStart - pos);

                int rowToken = table.IndexOf(@"\row", rowStart, StringComparison.Ordinal);
                if (rowToken < 0) break;

                int rowEnd = FindRowEndTokenEnd(table, rowToken);
                string rowSeg = table.Substring(rowStart, rowEnd - rowStart);

                if (!TryModifyRowColumns(rowSeg, ctx.ColIndex, add, out string modifiedRow, out int newColCount))
                {
                    modifiedRow = rowSeg;
                }

                sb.Append(modifiedRow);
                pos = rowEnd;
            }

            if (pos < table.Length)
                sb.Append(table, pos, table.Length - pos);

            string newTable = sb.ToString();
            return rtf.Substring(0, ctx.TableStart) + newTable + rtf.Substring(ctx.TableEnd);
        }

        private static bool TryModifyRowColumns(string rowSeg, int colIndex, bool add, out string modifiedRow, out int newColCount)
        {
            modifiedRow = rowSeg;
            newColCount = 0;

            int intblPos = rowSeg.IndexOf(@"\intbl", StringComparison.Ordinal);
            if (intblPos < 0) return false;

            string header = rowSeg.Substring(0, intblPos);

            int[] cellx = ParseCellx(header);
            int colCount = cellx.Length;
            if (colCount <= 0) return false;

            if (!TryParseCells(rowSeg, intblPos, out string[]? cells, out string tail))
                return false;

            if (cells.Length != colCount)
            {
                int min = Math.Min(cells.Length, colCount);
                cells = cells.Take(min).ToArray();
                cellx = cellx.Take(min).ToArray();
                colCount = min;
                if (colCount <= 0) return false;
            }

            colIndex = Math.Max(0, Math.Min(colIndex, colCount - 1));

            if (!add && colCount <= 1)
            {
                newColCount = colCount;
                modifiedRow = rowSeg;
                return true;
            }

            if (add)
            {
                int insertPos = colIndex + 1;

                int prevEdge = insertPos == 0 ? 0 : cellx[insertPos - 1];
                int widthRef = cellx[colIndex] - (colIndex == 0 ? 0 : cellx[colIndex - 1]);
                if (widthRef <= 0) widthRef = 720;

                int[] newCellx = new int[colCount + 1];
                for (int i = 0; i < insertPos; i++)
                    newCellx[i] = cellx[i];

                newCellx[insertPos] = prevEdge + widthRef;

                for (int i = insertPos; i < colCount; i++)
                    newCellx[i + 1] = cellx[i] + widthRef;

                string[] newCells = new string[colCount + 1];
                for (int i = 0; i < insertPos; i++)
                    newCells[i] = cells[i];

                newCells[insertPos] = @"\intbl \cell ";

                for (int i = insertPos; i < colCount; i++)
                    newCells[i + 1] = cells[i];

                string newHeader = RebuildHeaderWithCellx(header, newCellx);
                modifiedRow = newHeader + string.Concat(newCells) + tail;
                newColCount = colCount + 1;
                return true;
            }
            else
            {
                int removePos = colIndex;

                int removedWidth = cellx[removePos] - (removePos == 0 ? 0 : cellx[removePos - 1]);
                if (removedWidth < 0) removedWidth = 0;

                int[] newCellx = new int[colCount - 1];
                for (int i = 0; i < removePos; i++)
                    newCellx[i] = cellx[i];

                for (int i = removePos + 1; i < colCount; i++)
                    newCellx[i - 1] = cellx[i] - removedWidth;

                string[] newCells = new string[colCount - 1];
                for (int i = 0; i < removePos; i++)
                    newCells[i] = cells[i];

                for (int i = removePos + 1; i < colCount; i++)
                    newCells[i - 1] = cells[i];

                string newHeader = RebuildHeaderWithCellx(header, newCellx);
                modifiedRow = newHeader + string.Concat(newCells) + tail;
                newColCount = colCount - 1;
                return true;
            }
        }

        private static int[] ParseCellx(string header)
        {
            List<int> list = new();
            int i = 0;

            while (true)
            {
                int p = header.IndexOf(@"\cellx", i, StringComparison.Ordinal);
                if (p < 0) break;

                int nStart = p + 6;
                int nEnd = nStart;

                while (nEnd < header.Length && char.IsDigit(header[nEnd]))
                    nEnd++;

                if (nEnd > nStart &&
                    int.TryParse(header.Substring(nStart, nEnd - nStart), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
                {
                    list.Add(v);
                }

                i = nEnd;
            }

            return list.ToArray();
        }

        private static bool TryParseCells(string rowSeg, int intblPos, out string[] cells, out string tail)
        {
            List<string> list = new();
            int i = intblPos;

            while (true)
            {
                int start = rowSeg.IndexOf(@"\intbl", i, StringComparison.Ordinal);
                if (start < 0) break;

                int cellEnd = rowSeg.IndexOf(@"\cell ", start, StringComparison.Ordinal);
                if (cellEnd < 0) break;

                int end = cellEnd + 6;
                list.Add(rowSeg.Substring(start, end - start));
                i = end;
            }

            tail = rowSeg.Substring(Math.Min(i, rowSeg.Length));
            cells = list.ToArray();
            return cells.Length > 0;
        }

        private static string RebuildHeaderWithCellx(string header, int[] newCellx)
        {
            int first = header.IndexOf(@"\cellx", StringComparison.Ordinal);
            if (first < 0)
                return header;

            string prefix = header.Substring(0, first);

            StringBuilder sb = new(prefix.Length + newCellx.Length * 16);
            sb.Append(prefix);

            foreach (int v in newCellx)
            {
                sb.Append(@"\cellx");
                sb.Append(v.ToString(CultureInfo.InvariantCulture));
                sb.Append(' ');
            }

            return sb.ToString();
        }

        // ============================================================
        // Key management entrypoint
        // ============================================================
        private void keyManagementToolStripMenuItem_Click(object sender, EventArgs e) => UiKeyManagement();

        

        private void btnFontSizePls_Click(object sender, EventArgs e)
        {
            AdjustSelectionFontSize(+1f);
        }

        private void btnFontSizeMns_Click(object sender, EventArgs e)
        {
            AdjustSelectionFontSize(-1f);
        }

        private void AdjustSelectionFontSize(float delta)
        {
            RichTextBox rtb = rtfMainText;
            if (rtb == null) return;

            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            // No selection -> change caret font size (like Word)
            if (len == 0)
            {
                Font baseFont = rtb.SelectionFont ?? rtb.Font;
                float newSize = Math.Clamp(baseFont.Size + delta, 1f, 200f);
                rtb.SelectionFont = new Font(baseFont.FontFamily, newSize, baseFont.Style);
                UpdateRtfUiFromSelection();
                return;
            }

            // Mixed selection -> apply per character to preserve formatting
            rtb.SuspendLayout();
            try
            {
                for (int i = 0; i < len; i++)
                {
                    rtb.Select(start + i, 1);

                    Font f = rtb.SelectionFont ?? rtb.Font;
                    float newSize = Math.Clamp(f.Size + delta, 1f, 200f);

                    rtb.SelectionFont = new Font(f.FontFamily, newSize, f.Style);
                }

                rtb.Select(start, len);
                rtb.Focus();
            }
            finally
            {
                rtb.ResumeLayout();
                UpdateRtfUiFromSelection();
            }
        }

        // ============================================================
        // Paragraph formatting (Alignment + Bullets + Numbering)
        // ============================================================
        private void toolButtonAlignLeft_Click(object sender, EventArgs e)
        {
            ApplyParagraphAlignment(HorizontalAlignment.Left);
        }

        private void toolButtonAlignCenter_Click(object sender, EventArgs e)
        {
            ApplyParagraphAlignment(HorizontalAlignment.Center);
        }

        private void toolButtonAlignRight_Click(object sender, EventArgs e)
        {
            ApplyParagraphAlignment(HorizontalAlignment.Right);
        }

        private void ApplyParagraphAlignment(HorizontalAlignment alignment)
        {
            _updatingParagraphUi = true;
            try
            {
                rtfMainText.SelectionAlignment = alignment;
                rtfMainText.Focus();
            }
            finally
            {
                _updatingParagraphUi = false;
                UpdateRtfUiFromSelection();
            }
        }

        private void toolStripButtonBullets_Click(object sender, EventArgs e)
        {
            ToggleBullets();
        }

        private bool EnsurePrivateKeyConfiguredInteractive()
        {
            // 1) Fallback: stored path in AppState
            if (!string.IsNullOrWhiteSpace(_appState.PrivateKeyPath) && File.Exists(_appState.PrivateKeyPath))
            {
                _privateKeyPath = _appState.PrivateKeyPath;
                return true;
            }

            // 2) No fallback -> ask create/select
            var res = MessageBox.Show(
                this,
                "No private key is configured.\n\n" +
                "Yes: Create a new key pair\n" +
                "No:  Select an existing private key\n" +
                "Cancel: Exit",
                "Private key setup",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.Yes)
                return CreateNewKeyPairInteractiveAndStore();

            return SelectExistingPrivateKeyAndStore();
        }

        private bool CreateNewKeyPairInteractiveAndStore()
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                DefaultExt = "hstrypriv",
                AddExtension = true,
                FileName = "owner.hstrypriv",
                Title = "Save new private key"
            };

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return false;

            string privPath = sfd.FileName;
            if (string.IsNullOrWhiteSpace(privPath))
                return false;

            string pubPath = Path.ChangeExtension(privPath, ".hstrypub");

            bool overwrite = File.Exists(privPath) || File.Exists(pubPath);
            if (overwrite)
            {
                var res = MessageBox.Show(
                    this,
                    "Key files already exist. Overwrite them?",
                    "Create key pair",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (res != DialogResult.Yes)
                    return false;
            }

            try
            {
                using RSA rsa = HSTRYContainer.RsaKeyFiles.CreateNewKeyPair(3072);
                HSTRYContainer.RsaKeyFiles.SavePrivateKeyPkcs8(privPath, rsa);
                HSTRYContainer.RsaKeyFiles.SavePublicKeySpki(pubPath, rsa);

                _privateKeyPath = privPath;

                _appState.PrivateKeyPath = privPath;
                _appState.Save();

                MessageBox.Show(
                    this,
                    "A new key pair was created.\n\n" +
                    $"Private key:\n{privPath}\n\n" +
                    $"Public key:\n{pubPath}",
                    "Key pair created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Create key pair", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        private bool SelectExistingPrivateKeyAndStore()
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "HSTRY Private Key (*.hstrypriv)|*.hstrypriv|All files (*.*)|*.*",
                CheckFileExists = true,
                Title = "Select private key"
            };

            if (ofd.ShowDialog(this) != DialogResult.OK)
                return false;

            string chosen = ofd.FileName;
            if (string.IsNullOrWhiteSpace(chosen) || !File.Exists(chosen))
                return false;

            _privateKeyPath = chosen;

            _appState.PrivateKeyPath = chosen;
            _appState.Save();

            return true;
        }

        private void UpdateWindowTitle()
        {
            // Du kannst hier Global.AppName nutzen, falls das bereits "HstryDocu" ist:
            string app = Global.AppName; // oder: "HstryDocu";

            // Wenn ein Container offen ist, zeige den Pfad in [ ... ]
            string title = app;

            if (_container != null)
            {
                string path = string.IsNullOrWhiteSpace(_containerPath)
                    ? "<unsaved>"
                    : _containerPath!;

                title = $"{app} - [ {path} ]";
            }

            // Wenn irgendetwas geändert wurde => Stern hinten dran
            if (_containerDirty)
                title += " *";

            Text = title;
        }



        private void ToggleBullets()
        {
            _updatingParagraphUi = true;
            try
            {
                bool enable = !rtfMainText.SelectionBullet;

                // Prevent overlap with numeric lists
                if (enable)
                {
                    ApplyDecimalNumbering(enable: false, startAt: 1);
                    if (toolButtonBulletsNumeric != null) toolButtonBulletsNumeric.Checked = false;
                }

                rtfMainText.SelectionBullet = enable;

                // Apply a reasonable default layout when enabling bullets (do not destroy custom indents)
                if (enable)
                {
                    if (rtfMainText.BulletIndent == 0) rtfMainText.BulletIndent = 18;

                    if (rtfMainText.SelectionIndent == 0)
                        rtfMainText.SelectionIndent = 30;

                    if (rtfMainText.SelectionHangingIndent == 0)
                        rtfMainText.SelectionHangingIndent = 12;
                }
                else
                {
                    // Keep SelectionIndent as-is; just turn off bullet marker.
                    rtfMainText.BulletIndent = 0;
                }

                rtfMainText.Focus();
            }
            finally
            {
                _updatingParagraphUi = false;
                UpdateRtfUiFromSelection();
            }
        }

        private void toolButtonBulletsNumeric_Click(object sender, EventArgs e)
        {
            ToggleNumericBullets();
        }

        private void CancelHashWorkers()
        {
            // Cancel on-demand hash calculation (selection-based)
            _hashCts?.Cancel();
            _hashCts?.Dispose();
            _hashCts = null;

            // Cancel background list-hash filling
            _hashFillCts?.Cancel();
            _hashFillCts?.Dispose();
            _hashFillCts = null;
        }


        private void ToggleNumericBullets(ushort startAt = 1)
        {
            _updatingParagraphUi = true;
            try
            {
                // Determine current numbering state from paragraph format
                ushort current = GetCurrentParagraphNumbering(rtfMainText);
                bool enable = current == PFN_NONE;

                // Prevent overlap with normal bullets
                if (enable)
                {
                    rtfMainText.SelectionBullet = false;
                    if (toolStripButtonBullets != null) toolStripButtonBullets.Checked = false;
                }

                ApplyDecimalNumbering(enable, startAt);

                rtfMainText.Focus();
            }
            finally
            {
                _updatingParagraphUi = false;
                UpdateRtfUiFromSelection();
            }
        }

        private void ApplyDecimalNumbering(bool enable, ushort startAt = 1)
        {
            if (rtfMainText.IsDisposed) return;

            PARAFORMAT2 pf = new()
            {
                cbSize = (uint)Marshal.SizeOf<PARAFORMAT2>(),
                dwMask = PFM_NUMBERING | PFM_NUMBERINGSTART | PFM_OFFSET | PFM_STARTINDENT,
                wNumbering = enable ? PFN_ARABIC : PFN_NONE,
                wNumberingStart = startAt,

                // twips (1/1440 inch). 360 = 0.25"
                dxStartIndent = 360,
                dxOffset = 360,

                rgxTabs = new int[32]
            };

            SendMessage(rtfMainText.Handle, EM_SETPARAFORMAT, IntPtr.Zero, ref pf);
        }

        private static ushort GetCurrentParagraphNumbering(RichTextBox rtb)
        {
            if (rtb.IsDisposed) return PFN_NONE;

            PARAFORMAT2 pf = new()
            {
                cbSize = (uint)Marshal.SizeOf<PARAFORMAT2>(),
                rgxTabs = new int[32]
            };

            SendMessage(rtb.Handle, EM_GETPARAFORMAT, IntPtr.Zero, ref pf);

            // wNumbering == 0 => none; else some numbering type (arabic, roman, etc.)
            return pf.wNumbering;
        }

        private const int WM_USER = 0x0400;
        private const int EM_GETPARAFORMAT = WM_USER + 61;
        private const int EM_SETPARAFORMAT = WM_USER + 71;

        private const uint PFM_STARTINDENT = 0x00000001;
        private const uint PFM_OFFSET = 0x00000004;
        private const uint PFM_NUMBERING = 0x00000020;
        private const uint PFM_NUMBERINGSTART = 0x00008000;

        private const ushort PFN_NONE = 0;
        private const ushort PFN_ARABIC = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct PARAFORMAT2
        {
            public uint cbSize;
            public uint dwMask;
            public ushort wNumbering;
            public ushort wReserved;
            public int dxStartIndent;
            public int dxRightIndent;
            public int dxOffset;
            public ushort wAlignment;
            public short cTabCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] rgxTabs;

            public int dySpaceBefore, dySpaceAfter, dyLineSpacing;
            public short sStyle;
            public byte bLineSpacingRule, bOutlineLevel;
            public ushort wShadingWeight, wShadingStyle;
            public ushort wNumberingStart;
            public ushort wNumberingStyle;
            public ushort wNumberingTab;
            public ushort wBorderSpace, wBorderWidth, wBorders;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref PARAFORMAT2 lParam);

        private async void toolStripButton1_Click_1(object sender, EventArgs e)
        {
            await CreateTestBlocksAsync(count: 10000, minWordsPerBlock: 2500, maxWordsPerBlock: 5000);
        }
    }

    public sealed record ContainerSearchHit(int BlockIndex, string BlockTitle, int IndexInText, string Snippet);
}
