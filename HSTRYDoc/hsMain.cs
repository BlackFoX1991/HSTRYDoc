using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace HSTRYDoc
{
    public partial class hsMain : Form
    {

        private bool _closingAfterSave = false;

        private string? _sessionKeyPassword;

        private HSTRYDoc.colorPicker? _colorPopup;
        private bool _suppressBlockSelectionChanged = false;

        // ---- Hash compute (on-demand for large containers) ----
        private CancellationTokenSource? _hashCts;


        // Background hash filling for the block list
        private CancellationTokenSource? _hashFillCts;


        private bool _updatingFontSizeUi = false;
        private bool _updatingParagraphUi = false;
        private bool _updatingRtfUi = false;

        private readonly AppState _appState = AppState.Load();

        // V5: ECDH private key used to open the container (membership + BEK unwrap)
        private string? _ecdhPrivateKeyPath;

        private static string DeriveSigningPrivateKeyPath(string ecdhPrivPath)
    => KeyStorage.GetSigningPrivateKeyPath(ecdhPrivPath);



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

        private const int EditorPageBaseWidth = 860;
        private const int EditorPageBaseHeight = 1180;
        private const int EditorPageBaseMarginX = 58;
        private const int EditorPageBaseMarginY = 64;
        private const int EditorWorkspacePadding = 22;

        private ToolStripStatusLabel? _statusZoomValueLabel;
        private ToolStripControlHost? _statusZoomTrackHost;
        private ToolStripStatusLabel? _statusSpringLabel;


        public hsMain()
        {
            InitializeComponent();
            ConfigureEditorWorkspace();
            ConfigureStatusStripZoomUi();

            if (Global.Testmode)
            {
                this.toolStripSeparator5.Visible = true;
                this.btnTestblocks.Visible = true;
            }

            var tmr = new System.Windows.Forms.Timer();
            tmr.Interval = 2000;

            var splsh = new SplashScreen();
            splsh.Show(this);
            tmr.Tick += (_, __) =>
            {
                splsh.Close();
                tmr.Stop();
            };

            tmr.Start();


        }

        private void ConfigureEditorWorkspace()
        {
            panel1.SuspendLayout();
            mainshadowPanel.SuspendLayout();
            try
            {
                panel1.AutoScroll = true;
                panel1.BackColor = Color.FromArgb(243, 245, 248);
                panel1.BorderStyle = BorderStyle.None;
                panel1.Padding = Padding.Empty;

                splitContainer1.Panel2.BackColor = panel1.BackColor;

                mainshadowPanel.Dock = DockStyle.None;
                mainshadowPanel.Anchor = AnchorStyles.Top;
                mainshadowPanel.BackColor = Color.Transparent;
                mainshadowPanel.ShadowSize = 12;
                mainshadowPanel.ShadowOffsetX = 5;
                mainshadowPanel.ShadowOffsetY = 5;
                mainshadowPanel.MaxAlpha = 44;
                mainshadowPanel.CornerRadius = 0;
                mainshadowPanel.PageBackColor = Color.White;
                mainshadowPanel.PageBorderColor = Color.FromArgb(226, 229, 234);

                pnlScales.Visible = false;

                rtfMainText.Dock = DockStyle.None;
                rtfMainText.BackColor = Color.White;
                rtfMainText.BorderStyle = BorderStyle.None;
                rtfMainText.ScrollBars = RichTextBoxScrollBars.Vertical;

                panel1.Resize += (_, __) => UpdateEditorPageView();
            }
            finally
            {
                mainshadowPanel.ResumeLayout();
                panel1.ResumeLayout();
            }

            UpdateEditorPageView();
        }

        private void ConfigureStatusStripZoomUi()
        {
            _statusSpringLabel = new ToolStripStatusLabel
            {
                Spring = true
            };

            _statusZoomValueLabel = new ToolStripStatusLabel
            {
                Text = "100%",
                IsLink = true,
                LinkBehavior = LinkBehavior.NeverUnderline,
                ActiveLinkColor = Color.FromArgb(48, 87, 173),
                LinkColor = Color.FromArgb(48, 87, 173),
                Margin = new Padding(0, 0, 8, 0)
            };
            _statusZoomValueLabel.Click += btnResetScale_Click;

            if (rtfScaleBar.Parent != null)
                rtfScaleBar.Parent.Controls.Remove(rtfScaleBar);

            rtfScaleBar.AutoSize = false;
            rtfScaleBar.BackColor = statusStrip1.BackColor;
            rtfScaleBar.TickStyle = TickStyle.None;
            rtfScaleBar.Size = new Size(170, 18);
            rtfScaleBar.Margin = Padding.Empty;

            _statusZoomTrackHost = new ToolStripControlHost(rtfScaleBar)
            {
                AutoSize = false,
                Margin = new Padding(0, 0, 4, 0),
                Padding = Padding.Empty,
                Size = new Size(170, 20)
            };

            statusStrip1.SizingGrip = false;
            statusStrip1.Padding = new Padding(8, 0, 10, 0);
            statusStrip1.MinimumSize = new Size(0, 30);
            statusStrip1.Items.Clear();
            statusStrip1.Items.Add(ContainerSizeLabel);
            statusStrip1.Items.Add(_statusSpringLabel);
            statusStrip1.Items.Add(_statusZoomValueLabel);
            statusStrip1.Items.Add(_statusZoomTrackHost);

            ApplyEditorZoomFromTrackBar();
        }

        private void UpdateEditorPageView()
        {
            if (panel1.IsDisposed || mainshadowPanel.IsDisposed || rtfMainText.IsDisposed)
                return;

            float zoom = Math.Clamp(rtfScaleBar.Value / 100f, 0.1f, 64f);

            int pageWidth = Math.Max(220, (int)Math.Round(EditorPageBaseWidth * zoom));
            int pageHeight = Math.Max(300, (int)Math.Round(EditorPageBaseHeight * zoom));

            mainshadowPanel.Size = new Size(
                pageWidth + mainshadowPanel.Padding.Horizontal,
                pageHeight + mainshadowPanel.Padding.Vertical);

            int marginX = Math.Max(16, (int)Math.Round(EditorPageBaseMarginX * zoom));
            int marginY = Math.Max(18, (int)Math.Round(EditorPageBaseMarginY * zoom));

            rtfMainText.Bounds = new Rectangle(
                mainshadowPanel.Padding.Left + marginX,
                mainshadowPanel.Padding.Top + marginY,
                Math.Max(120, pageWidth - (marginX * 2)),
                Math.Max(160, pageHeight - (marginY * 2)));

            panel1.AutoScrollMinSize = new Size(
                mainshadowPanel.Width + (EditorWorkspacePadding * 2),
                mainshadowPanel.Height + (EditorWorkspacePadding * 2));

            int scrollbarAllowance = panel1.AutoScrollMinSize.Height > panel1.ClientSize.Height
                ? SystemInformation.VerticalScrollBarWidth
                : 0;
            int availableWidth = Math.Max(0, panel1.ClientSize.Width - scrollbarAllowance);
            int centeredX = (availableWidth - mainshadowPanel.Width) / 2;

            mainshadowPanel.Location = new Point(
                Math.Max(EditorWorkspacePadding, centeredX),
                EditorWorkspacePadding);

            mainshadowPanel.Invalidate();
        }

        private void ApplyEditorZoomFromTrackBar()
        {
            float zoom = rtfScaleBar.Value / 100f;
            zoom = Math.Clamp(zoom, 0.1f, 64f);

            rtfMainText.ZoomFactor = zoom;

            string zoomText = $"{rtfScaleBar.Value}%";
            lblScaleLabel.Text = zoomText;
            if (_statusZoomValueLabel != null)
                _statusZoomValueLabel.Text = zoomText;

            UpdateEditorPageView();
        }

        private async void UiKeyManagement()
        {
            if (_container == null || string.IsNullOrWhiteSpace(_containerPath))
            {
                MessageBox.Show(this, "No container is currently open.", "Key Management",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Remember current key path to detect changes
            string? oldKeyPath = _ecdhPrivateKeyPath;

            using var dlg = new KeyManagerDialog(
                container: _container,
                containerPath: _containerPath!,
                currentEcdhPrivateKeyPath: _ecdhPrivateKeyPath);

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _ecdhPrivateKeyPath = dlg.SelectedEcdhPrivateKeyPath;
            if (!string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath) &&
                File.Exists(_ecdhPrivateKeyPath) &&
                KeyStorage.IsKeyPathInManagedFolder(_ecdhPrivateKeyPath))
            {
                StoreSelectedPrivateKey(_ecdhPrivateKeyPath);
            }

            // If container changed in dialog -> save now
            if (dlg.ContainerChanged)
            {
                try
                {
                    if (!MaybeCommitCurrentBlock())
                        return;

                    _container.Save(_containerPath!);
                    _containerDirty = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Key Management", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // If user selected different ECDH key -> reload container to apply rights
            bool keyChanged =
                !string.Equals(oldKeyPath ?? string.Empty, _ecdhPrivateKeyPath ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath) && File.Exists(_ecdhPrivateKeyPath);

            if (keyChanged)
            {
                int keepIndex = _currentBlockIndex;
                bool ok = await ReloadContainerWithPrivateKeyAsync(_containerPath!, _ecdhPrivateKeyPath!, keepIndex);
                if (!ok)
                    return;
            }

            UpdateUiState();
        }

        private async Task<bool> ReloadContainerWithPrivateKeyAsync(string containerPath, string ecdhPrivateKeyPath, int keepSelectedIndex)
        {
            if (!EnsureSessionPasswordPrompt(out string pw))
                return false;

            try
            {
                HSTRYContainer loaded = await reporterDiag.RunAsync(
                    owner: this,
                    title: "Reloading container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Reloading container ", Indeterminate = true });

                        return await Task.Run(() =>
                        {
                            using var ecdh = LoadEcdhPrivateKeyWithPassword(ecdhPrivateKeyPath, pw);

                            using var fs = new FileStream(containerPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 20);
                            using var bs = new BufferedStream(fs, 1 << 20);

                            return HSTRYContainer.LoadWithPrivateKey(bs, ecdh);
                        }, token);
                    });

                CancelHashWorkers();

                _container?.CloseKeyMaterial();
                _container = loaded;
                _containerPath = containerPath;
                _ecdhPrivateKeyPath = ecdhPrivateKeyPath;

                _blockDirty = false;
                _containerDirty = false;

                RefreshBlockList(selectIndex: null);

                int newIndex = -1;
                if (_container.Blocks.Count > 0)
                    newIndex = Math.Clamp(keepSelectedIndex, 0, _container.Blocks.Count - 1);

                if (newIndex >= 0)
                {
                    SelectListIndex(newIndex);
                    LoadBlockIntoEditor(newIndex);
                }
                else
                {
                    ClearEditor();
                    splitContainer1.Panel2.Enabled = false;
                }

                RebuildAutoCompleteWordsFromEditor();
                return true;
            }
            catch (CryptographicException)
            {
                _sessionKeyPassword = null;
                MessageBox.Show(this, "Wrong password or corrupted key file.", "Reload container",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Reload container", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }


        #region test_blocks

        private bool IsOwnerKeyLoadedForCurrentContainer()
        {
            if (_container == null) return false;
            if (string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath) || !File.Exists(_ecdhPrivateKeyPath)) return false;

            if (!EnsureSessionPasswordPrompt(out string pw))
                return false;

            try
            {
                using var ecdh = LoadEcdhPrivateKeyWithPassword(_ecdhPrivateKeyPath!, pw);
                byte[] spki = ecdh.ExportSubjectPublicKeyInfo();
                return spki.SequenceEqual(_container.OwnerEcdhPublicKeySpki);
            }
            catch
            {
                // wrong password / corrupt key -> force re-prompt next time
                _sessionKeyPassword = null;
                return false;
            }
        }

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

            // Only owner can create blocks (Variant 1)
            if (_container.Version == HSTRYContainer.CurrentVersion && !IsOwnerKeyLoadedForCurrentContainer())
            {
                MessageBox.Show(this,
                    "Access denied.\n\nOnly the owner can create new blocks.\nLoad the owner ECDH private key first.",
                    "Test blocks",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!MaybeCommitCurrentBlock())
                return;

            if (minWordsPerBlock < 1) minWordsPerBlock = 1;
            if (maxWordsPerBlock < minWordsPerBlock) maxWordsPerBlock = minWordsPerBlock;

            try
            {
                CancelHashWorkers();
                using var ownerSig = LoadSigningKeyForCurrentSession();

                await reporterDiag.RunAsync(
                    owner: this,
                    title: "Creating test blocks",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress
                        {
                            Message = "Preparing ",
                            Indeterminate = false,
                            Maximum = count,
                            Value = 0
                        });

                        await Task.Run(() =>
                        {
                            string[] words = BuildWordPool();

                            for (int i = 0; i < count; i++)
                            {
                                token.ThrowIfCancellationRequested();

                                string title = _container!.GenerateUniqueTitle();

                                int wCount = RandomNumberGenerator.GetInt32(minWordsPerBlock, maxWordsPerBlock + 1);
                                string plain = BuildRandomParagraphs(words, wCount);
                                string rtf = BuildSimpleRtf(title, plain);

                                _container!.AddRtfDocument(ownerSig, title, rtf);

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
            // Create 2 6 paragraphs depending on size
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

        // ============================================================
        // Interactive key selection dialog (drive root + HSTRY_KEY)
        // ============================================================
        private bool ResolvePrivateKeyInteractive(string containerPath, out string ecdhPrivateKeyPath)
        {
            ecdhPrivateKeyPath = string.Empty;

            if (TryGetConfiguredPrivateKeyPath(out ecdhPrivateKeyPath))
            {
                if (!string.Equals(_ecdhPrivateKeyPath ?? string.Empty, ecdhPrivateKeyPath, StringComparison.OrdinalIgnoreCase))
                    _sessionKeyPassword = null;

                _ecdhPrivateKeyPath = ecdhPrivateKeyPath;
                return true;
            }

            return SelectPrivateKeyFromDrive(out ecdhPrivateKeyPath);
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
                   c == ' ' || c == ' ' || c == ' ' ||
                   c == ' ' || c == ' ' || c == ' ' || c == ' ';
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

                if (dr == DialogResult.Retry)
                {
                    ConfigureKeyOptionsFromChooser();
                    continue;
                }

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

        private void ConfigureKeyOptionsFromChooser()
        {
            SelectExistingEcdhPrivateKeyAndStore();
        }

        private bool TryGetConfiguredPrivateKeyPath(out string ecdhPrivateKeyPath)
        {
            ecdhPrivateKeyPath = string.Empty;

            string driveRoot = KeyStorage.NormalizeDriveRoot(_appState.PrivateKeyDriveRoot ?? string.Empty);
            string fileName = Path.GetFileName(_appState.PrivateKeyFileName ?? string.Empty);
            if (string.IsNullOrWhiteSpace(driveRoot) || string.IsNullOrWhiteSpace(fileName))
                return false;
            if (!KeyStorage.IsAvailableDriveRoot(driveRoot))
                return false;

            string path = KeyStorage.GetPrivateKeyPath(driveRoot, fileName);
            if (!File.Exists(path))
                return false;

            ecdhPrivateKeyPath = path;
            return true;
        }

        private void StoreSelectedPrivateKey(string ecdhPrivateKeyPath)
        {
            if (!KeyStorage.TryGetDriveRootAndFileNameFromKeyPath(ecdhPrivateKeyPath, out string driveRoot, out string fileName))
                throw new InvalidOperationException("Private keys must be stored in the HSTRY_KEY folder at the root of a drive.");
            if (!KeyStorage.IsAvailableDriveRoot(driveRoot))
                throw new InvalidOperationException("Private keys must be stored on a ready drive.");

            if (!string.Equals(_ecdhPrivateKeyPath ?? string.Empty, ecdhPrivateKeyPath, StringComparison.OrdinalIgnoreCase))
                _sessionKeyPassword = null;

            _ecdhPrivateKeyPath = ecdhPrivateKeyPath;
            _appState.PrivateKeyDriveRoot = driveRoot;
            _appState.PrivateKeyFileName = fileName;
            _appState.Save();
        }

        private bool SelectPrivateKeyFromDrive(out string ecdhPrivateKeyPath)
        {
            ecdhPrivateKeyPath = string.Empty;

            using KeySourceDialog dlg = new(
                _appState.PrivateKeyDriveRoot ?? string.Empty,
                _appState.PrivateKeyFileName,
                requirePrivateKey: true)
            {
                StartPosition = FormStartPosition.CenterParent
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return false;

            if (string.IsNullOrWhiteSpace(dlg.SelectedPrivateKeyPath) || !File.Exists(dlg.SelectedPrivateKeyPath))
                return false;

            StoreSelectedPrivateKey(dlg.SelectedPrivateKeyPath);
            ecdhPrivateKeyPath = dlg.SelectedPrivateKeyPath;
            return true;
        }

        private bool SelectKeyDrive(out string driveRoot)
        {
            driveRoot = string.Empty;

            using KeySourceDialog dlg = new(
                _appState.PrivateKeyDriveRoot ?? string.Empty,
                _appState.PrivateKeyFileName,
                requirePrivateKey: false)
            {
                StartPosition = FormStartPosition.CenterParent
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return false;

            string selected = KeyStorage.NormalizeDriveRoot(dlg.SelectedDriveRoot ?? string.Empty);
            if (string.IsNullOrWhiteSpace(selected))
                return false;

            driveRoot = selected;
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
            if (!ResolvePrivateKeyInteractive(path, out string ecdhPrivPath))
                return false;

            if (!EnsureSessionPasswordPrompt(out string pw))
                return false;

            try
            {
                HSTRYContainer loaded = await reporterDiag.RunAsync(
                    owner: this,
                    title: "Opening container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Opening container ", Indeterminate = true });

                        return await Task.Run(() =>
                        {
                            using var ecdh = LoadEcdhPrivateKeyWithPassword(ecdhPrivPath, pw);

                            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1 << 20);
                            using var bs = new BufferedStream(fs, 1 << 20);

                            return HSTRYContainer.LoadWithPrivateKey(bs, ecdh);
                        }, token);
                    });

                CancelHashWorkers();

                _container?.CloseKeyMaterial();
                _container = loaded;
                _containerPath = path;
                _ecdhPrivateKeyPath = ecdhPrivPath;

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
            catch (CryptographicException)
            {
                _sessionKeyPassword = null;
                MessageBox.Show(this, "Wrong password or corrupted key file.", "Open container",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (InvalidDataException ex) when (ex.Message.Contains("Unsupported container version", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this,
                    $"This file was created with a different container version.\n\nThis build supports V{HSTRYContainer.CurrentVersion} only.",
                    "Open container",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
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
            rtfMainText.Enter += (_, __) => UpdateRtfUiFromSelection();
            ctxRtf.Opening += (_, __) => UpdateRtfUiFromSelection();

            // Shortcuts in editor
            rtfMainText.KeyDown += RtfMainText_KeyDown;

            // Closing
            FormClosing += hsMain_FormClosing;

            keyLookupExternalDriveToolStripMenuItem.Click += (_, __) => SelectExistingEcdhPrivateKeyAndStore();

        }

        // ============================================================
        // UI state
        // ============================================================
        private void UpdateUiState()
        {
            bool hasContainer = _container != null;
            bool canCreateBlocks = hasContainer && _container!.IsOpenedAsOwner;

            saveContainerToolStripButton.Enabled = hasContainer;
            saveContainerToolStripMenuItem.Enabled = hasContainer;
            saveContainerAsToolStripMenuItem.Enabled = hasContainer;

            newBlockToolStripButton.Enabled = canCreateBlocks;
            newToolStripMenuItem.Enabled = canCreateBlocks;
            newBlockToolStripMenuItem.Enabled = canCreateBlocks;

            renameBlockToolStripMenuItem.Enabled = hasContainer && _currentBlockIndex >= 0;
            removeBlockToolStripMenuItem.Enabled = hasContainer && _currentBlockIndex >= 0;

            ContainerSizeLabel.Visible = hasContainer;
            ContainerSizeLabel.Text = hasContainer
                ? $"Container: {ByteFormat.ToHumanSize(_container!.GetStoredSizeBytes())}"
                : "<Container_Size>";

            // NEW: Title logic centralized
            UpdateWindowTitle();
            UpdateRtfCommandAvailability();
        }




        private async void hsMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_closingAfterSave)
                return;

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
                    e.Cancel = true; // cancel close; we will close after save

                    bool saved = await UiSaveContainerAsync();
                    if (!saved)
                        return;

                    _closingAfterSave = true;
                    try { Close(); }
                    finally { _closingAfterSave = false; }

                    return;
                }
            }

            SaveWindowPlacement();
            _appState.Save();

            _acRebuildTimer.Stop();
            _rtfAutoComplete?.Dispose();

            CancelHashWorkers();

            _sessionKeyPassword = null;

            _container?.CloseKeyMaterial();
        }

        // ============================================================
        // Container operations (V2-only create/open/save) (ASYNC)
        // ============================================================
        private async Task<bool> UiCreateNewContainerAsync(bool initialStartup = false)
        {
            if (!EnsurePrivateKeyConfiguredInteractive())
                return false;

            if (string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath) || !File.Exists(_ecdhPrivateKeyPath))
            {
                MessageBox.Show(this, "No ECDH private key is configured.", "Create container",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // signing key must exist (encrypted too)
            string signPath = DeriveSigningPrivateKeyPath(_ecdhPrivateKeyPath);
            if (!File.Exists(signPath))
            {
                MessageBox.Show(this,
                    "Owner signing key is missing.\n\nExpected file:\n" + signPath,
                    "Create container",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }

            // Need password
            if (!EnsureSessionPasswordPrompt(out string pw))
                return false;

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
                        progress.Report(new UiProgress { Message = "Creating container ", Indeterminate = true });

                        return await Task.Run(() =>
                        {
                            using var ownerEcdh = LoadEcdhPrivateKeyWithPassword(_ecdhPrivateKeyPath!, pw);
                            using var ownerSig = LoadEcdsaSigningKeyWithPassword(_ecdhPrivateKeyPath!, pw);

                            HSTRYContainer c = HSTRYContainer.CreateNewForRecipients(
                                ownerSigningPrivateKey: ownerSig,
                                ownerEcdhPrivateKey: ownerEcdh,
                                recipientEcdhPublicKeys: new[] { ownerEcdh },
                                encoding: Global.CurrentEditorEncoding);

                            c.Save(containerPath);
                            return c;
                        }, token);
                    });

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
            catch (CryptographicException)
            {
                _sessionKeyPassword = null;
                MessageBox.Show(this, "Wrong password or corrupted key file.", "Create container",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
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

            bool needsMainSave = true;

            // When no save path exists yet, run Save As first (it already saves).
            if (string.IsNullOrWhiteSpace(_containerPath))
            {
                bool savedAs = await UiSaveContainerAsAsync();
                if (!savedAs) return false;
                needsMainSave = false;
            }

            string mainPath = _containerPath!;

            // Spiegel-Kopie in Private-Key-Ordner vorbereiten
            string? mirrorPath = null;
            if (!string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath) && File.Exists(_ecdhPrivateKeyPath))
            {
                string? keyFolder = Path.GetDirectoryName(_ecdhPrivateKeyPath);
                if (!string.IsNullOrWhiteSpace(keyFolder))
                    mirrorPath = Path.Combine(keyFolder, Path.GetFileName(mainPath));
            }

            try
            {
                await reporterDiag.RunAsync<object>(
                    owner: this,
                    title: "Save container",
                    work: async (progress, token) =>
                    {
                        progress.Report(new UiProgress { Message = "Saving container ", Indeterminate = true });

                        await Task.Run(() =>
                        {
                            // 1) Original speichern (nur wenn nicht bereits via Save As erledigt)
                            if (needsMainSave)
                                _container.Save(mainPath);

                            // 2) Always mirror a copy into the key folder when possible.
                            if (!string.IsNullOrWhiteSpace(mirrorPath))
                            {
                                string src = Path.GetFullPath(mainPath);
                                string dst = Path.GetFullPath(mirrorPath);

                                // Nicht auf sich selbst kopieren
                                if (!string.Equals(src, dst, StringComparison.OrdinalIgnoreCase))
                                    File.Copy(src, dst, overwrite: true);
                            }
                        }, token);

                        return new object();
                    });

                _containerDirty = false;
                UpdateUiState();

                // If no mirror copy was possible, only show an informational hint.
                if (string.IsNullOrWhiteSpace(mirrorPath))
                {
                    MessageBox.Show(this,
                        "Container saved.\n\nMirror copy was skipped because no valid private-key path is available.",
                        "Save container",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

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
                        progress.Report(new UiProgress { Message = "Saving container ", Indeterminate = true });

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

            if (!_container.IsOpenedAsOwner)
            {
                MessageBox.Show(this,
                    "Only the container owner can create new blocks.\n\nLoad the owner ECDH private key first.",
                    "New block",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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
                using var ownerSig = LoadSigningKeyForCurrentSession();

                Block b = _container.AddRtfDocument(ownerSig, title, emptyRtf);
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
                using var ownerSig = LoadSigningKeyForCurrentSession();

                _container.RenameBlock(ownerSig, idx, dlg.InputText);
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
                using var ownerSig = LoadSigningKeyForCurrentSession();

                _container.RemoveBlock(ownerSig, idx);
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

                // Try decrypt/load
                string rtf = _container.GetRtfDocument(index);

                rtfMainText.Rtf = rtf ?? string.Empty;

                _currentBlockIndex = index;
                _blockDirty = false;

                splitContainer1.Panel2.Enabled = true;

                ResetEditorScaleTo100();
                UpdateRtfUiFromSelection();
            }
            catch (UnauthorizedAccessException)
            {
                // V4: No read access to this block
                _currentBlockIndex = index;
                _blockDirty = false;

                _loadingBlockIntoEditor = true;
                try
                {
                    rtfMainText.Clear();
                    rtfMainText.Text = "You do not have permission to open this block.";
                }
                finally
                {
                    _loadingBlockIntoEditor = false;
                }

                splitContainer1.Panel2.Enabled = false;

                MessageBox.Show(this,
                    "You do not have read access to this block.\n\nAsk the owner to grant access in Key Management.",
                    "Access denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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
                using var ownerSig = LoadSigningKeyForCurrentSession();

                _container.UpdateRtfDocument(ownerSig, _currentBlockIndex, rtfMainText.Rtf ?? string.Empty);
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
                using var ownerSig = LoadSigningKeyForCurrentSession();

                _container.UpdateRtfDocument(ownerSig, _currentBlockIndex, rtfMainText.Rtf ?? string.Empty);
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
                        : ""; // IMPORTANT: empty means "not computed yet" for on-demand

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

            var container = _container;
            Block[] blocks = container.Blocks.ToArray();

            _hashFillCts?.Cancel();
            _hashFillCts?.Dispose();
            _hashFillCts = new CancellationTokenSource();

            CancellationToken token = _hashFillCts.Token;
            int count = blocks.Length;

            _ = Task.Run(() =>
            {
                try
                {
                    const int batchSize = 25;
                    var batch = new List<(int index, string hex)>(batchSize);

                    for (int i = 0; i < count; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        string hex = Convert.ToHexString(container.ComputeBlockHash(blocks[i]));
                        batch.Add((i, hex));

                        if (batch.Count >= batchSize)
                        {
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
                }
                catch (OperationCanceledException)
                {
                    // Expected when the list is rebuilt or the container changes.
                }
                catch
                {
                    // Best-effort UI hashing: ignore stale background failures.
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
                        if (!ReferenceEquals(container, _container)) return;
                        if (lvwBlocks.IsDisposed) return;

                        int itemCount = lvwBlocks.Items.Count;

                        for (int k = 0; k < updates.Length; k++)
                        {
                            int idx = updates[k].index;
                            string hex = updates[k].hex;

                            if (idx < 0 || idx >= itemCount) continue;

                            var it = lvwBlocks.Items[idx];
                            if (it.SubItems.Count < 2) continue;

                            if (it.SubItems[1].Text == "Computing " || string.IsNullOrWhiteSpace(it.SubItems[1].Text))
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

            var container = _container;

            // Hash column index 1 (Name is 0)
            if (item.SubItems.Count < 2) return;

            string cur = item.SubItems[1].Text ?? string.Empty;

            // Already computed? (Accept only a real hex string as computed)
            // If it's empty or "Computing " we compute.
            bool looksComputed = cur.Length >= 64 && cur.All(c => Uri.IsHexDigit(c));
            if (looksComputed) return;

            if (item.Tag is not int idx) return;
            if (idx < 0 || idx >= container.Blocks.Count) return;

            Block block = container.Blocks[idx];

            // Cancel previous ongoing hash
            _hashCts?.Cancel();
            _hashCts?.Dispose();
            _hashCts = new CancellationTokenSource();
            var token = _hashCts.Token;

            item.SubItems[1].Text = "Computing ";

            try
            {
                string hex = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    byte[] h = container.ComputeBlockHash(block);
                    return Convert.ToHexString(h);
                }, token);

                if (!token.IsCancellationRequested &&
                    ReferenceEquals(container, _container) &&
                    item.ListView == lvwBlocks &&
                    item.SubItems.Count >= 2)
                    item.SubItems[1].Text = hex;
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                if (!token.IsCancellationRequested &&
                    ReferenceEquals(container, _container) &&
                    item.ListView == lvwBlocks &&
                    item.SubItems.Count >= 2)
                    item.SubItems[1].Text = "";
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

                ResetEditorScaleTo100();
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
                            Message = "Searching ",
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
                                    Message = $"Searching block {i + 1}/{_container.Blocks.Count} ",
                                    Value = i + 1
                                });

                                string rtf;
                                try
                                {
                                    rtf = _container.GetRtfDocument(i);
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    continue;
                                }

                                tmp.Rtf = rtf ?? string.Empty;
                                string text = tmp.Text ?? string.Empty;

                                IReadOnlyList<TextSearchMatch> matches = TextSearch.FindAll(text, query, dlg.MatchCase, dlg.WholeWord);
                                foreach (TextSearchMatch match in matches)
                                {
                                    string snippet = TextSearch.BuildSnippet(text, match.Index, match.Length, 40);
                                    hits.Add(new ContainerSearchHit(i, _container.Blocks[i].Title, match.Index, match.Length, snippet));
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
                SelectSearchHitInEditor(hit);
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

        private void SelectSearchHitInEditor(ContainerSearchHit hit)
        {
            int start = Math.Max(0, Math.Min(hit.IndexInText, rtfMainText.TextLength));
            int length = Math.Max(0, Math.Min(hit.MatchLength, rtfMainText.TextLength - start));

            if (length == 0)
                return;

            rtfMainText.Select(start, length);
            rtfMainText.ScrollToCaret();
            rtfMainText.Focus();
        }

        private static void IndentSelectionByTab(RichTextBox rtb)
        {
            int selStart = rtb.SelectionStart;
            int selLen = rtb.SelectionLength;

            // No selection -> normal tab insert with current typing format
            if (selLen == 0)
            {
                rtb.SelectedText = "\t";
                return;
            }

            int firstLine = rtb.GetLineFromCharIndex(selStart);

            int lastChar = Math.Max(selStart, selStart + selLen - 1);
            int lastLine = rtb.GetLineFromCharIndex(lastChar);

            var lineStarts = new List<int>();
            for (int line = firstLine; line <= lastLine; line++)
                lineStarts.Add(rtb.GetFirstCharIndexFromLine(line));

            rtb.SuspendLayout();
            try
            {
                // Insert bottom->top so indices stay stable
                for (int i = lineStarts.Count - 1; i >= 0; i--)
                {
                    int lineStart = lineStarts[i];
                    if (lineStart < 0 || lineStart > rtb.TextLength) continue;

                    // Find a reference character in that line to copy formatting from
                    int refPos = FindFirstNonNewlineCharInLine(rtb, lineStart);

                    // Copy char format (font/colors) from refPos
                    var fmt = GetCharFormat(rtb, refPos >= 0 ? refPos : Math.Max(0, Math.Min(lineStart, rtb.TextLength - 1)));

                    // Insert tab at line start using that format
                    rtb.Select(lineStart, 0);
                    ApplyFormatForInsertion(rtb, fmt);
                    rtb.SelectedText = "\t";
                }

                int insertedTotal = lineStarts.Count;
                int insertedBeforeStart = lineStarts.Count(x => x <= selStart);

                int newStart = selStart + insertedBeforeStart;
                int newLen = selLen + insertedTotal;

                rtb.Select(newStart, newLen);
            }
            finally
            {
                rtb.ResumeLayout();
            }
        }

        private static void UnindentSelectionByTab(RichTextBox rtb)
        {
            int selStart = rtb.SelectionStart;
            int selLen = rtb.SelectionLength;

            // No selection -> remove one tab before caret if present
            if (selLen == 0)
            {
                if (selStart <= 0) return;

                if (rtb.Text[selStart - 1] == '\t')
                {
                    rtb.Select(selStart - 1, 1);
                    rtb.SelectedText = "";
                    rtb.Select(selStart - 1, 0);
                }
                return;
            }

            int firstLine = rtb.GetLineFromCharIndex(selStart);
            int lastChar = Math.Max(selStart, selStart + selLen - 1);
            int lastLine = rtb.GetLineFromCharIndex(lastChar);

            var lineStarts = new List<int>();
            for (int line = firstLine; line <= lastLine; line++)
                lineStarts.Add(rtb.GetFirstCharIndexFromLine(line));

            int removedTotal = 0;
            int removedBeforeStart = 0;

            rtb.SuspendLayout();
            try
            {
                // Remove bottom->top
                for (int i = lineStarts.Count - 1; i >= 0; i--)
                {
                    int idx = lineStarts[i];
                    if (idx < 0 || idx >= rtb.TextLength) continue;

                    if (rtb.Text[idx] == '\t')
                    {
                        rtb.Select(idx, 1);
                        rtb.SelectedText = "";
                        removedTotal += 1;
                        if (idx < selStart) removedBeforeStart += 1;
                    }
                    else
                    {
                        // Optional: treat leading spaces as indent (up to 4)
                        int max = Math.Min(4, rtb.TextLength - idx);
                        int spaces = 0;
                        while (spaces < max && rtb.Text[idx + spaces] == ' ') spaces++;

                        if (spaces > 0)
                        {
                            rtb.Select(idx, spaces);
                            rtb.SelectedText = "";
                            removedTotal += spaces;
                            if (idx < selStart) removedBeforeStart += spaces;
                        }
                    }
                }

                int newStart = Math.Max(0, selStart - removedBeforeStart);
                int newLen = Math.Max(0, selLen - removedTotal);
                rtb.Select(newStart, newLen);
            }
            finally
            {
                rtb.ResumeLayout();
            }
        }

        private static int FindFirstNonNewlineCharInLine(RichTextBox rtb, int lineStart)
        {
            if (rtb.TextLength == 0) return -1;
            if (lineStart < 0 || lineStart >= rtb.TextLength) return -1;

            // Scan forward until end-of-line or end-of-text
            int i = lineStart;
            while (i < rtb.TextLength)
            {
                char c = rtb.Text[i];
                if (c == '\r' || c == '\n') return -1; // empty line
                return i; // first char in line
            }
            return -1;
        }

        private readonly struct CharFormat
        {
            public CharFormat(Font font, Color fore, Color back)
            {
                Font = font;
                Fore = fore;
                Back = back;
            }
            public Font Font { get; }
            public Color Fore { get; }
            public Color Back { get; }
        }

        private static CharFormat GetCharFormat(RichTextBox rtb, int pos)
        {
            if (rtb.TextLength == 0)
                return new CharFormat(rtb.Font, rtb.ForeColor, rtb.BackColor);

            pos = Math.Max(0, Math.Min(pos, rtb.TextLength - 1));

            int s = rtb.SelectionStart;
            int l = rtb.SelectionLength;

            rtb.Select(pos, 1);
            Font f = rtb.SelectionFont ?? rtb.Font;
            Color fore = rtb.SelectionColor;
            Color back = rtb.SelectionBackColor;

            rtb.Select(s, l);

            return new CharFormat(f, fore, back);
        }

        private static void ApplyFormatForInsertion(RichTextBox rtb, CharFormat fmt)
        {
            // Important: apply before inserting so RichEdit doesn't "invent" a new default run
            rtb.SelectionFont = fmt.Font;
            rtb.SelectionColor = fmt.Fore;
            rtb.SelectionBackColor = fmt.Back;
        }



        // ============================================================
        // RTF Formatting + Clipboard + Shortcuts
        // ============================================================
        private void RtfMainText_KeyDown(object? sender, KeyEventArgs e)
        {
            // TAB / Shift+TAB: indent / unindent selected lines (keep text)
            if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
            {
                // Keep default behavior inside tables (cell navigation)
                if (!IsCaretInTable(rtfMainText))
                {
                    if (e.Shift) UnindentSelectionByTab(rtfMainText);
                    else IndentSelectionByTab(rtfMainText);

                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }

            // Ctrl+F: search in block
            if (e.Control && !e.Shift && e.KeyCode == Keys.F)
            {
                UiSearchInBlock();
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+F: search in container
            if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                _ = UiSearchInContainerAsync();
                e.Handled = true;
                return;
            }

            // F3: find next
            if (!e.Control && !e.Shift && e.KeyCode == Keys.F3)
            {
                if (!string.IsNullOrWhiteSpace(_lastFindText))
                    FindNextInEditor(_lastFindText, _lastFindMatchCase, _lastFindWholeWord, _lastFindWrap);

                e.Handled = true;
                return;
            }
        }


        private void UpdateRtfUiFromSelection()
        {
            if (_updatingRtfUi) return;

            _updatingRtfUi = true;
            try
            {
                bool hasActiveEditor = HasActiveEditor();
                UpdateRtfCommandAvailability();

                if (!hasActiveEditor)
                {
                    ResetRtfUiDisplayState();
                    return;
                }

                if (_updatingParagraphUi)
                    return;

                CharFormat displayFormat = GetDisplayCharFormatForUi();
                Font f = displayFormat.Font;

                boldToolButton.Checked = f.Bold;
                ItalicToolButton.Checked = f.Italic;
                UnderlineToolButton.Checked = f.Underline;
                StrikeTroughToolButton.Checked = f.Strikeout;

                boldToolStripMenuItem.Checked = f.Bold;
                italicToolStripMenuItem.Checked = f.Italic;
                underlineToolStripMenuItem.Checked = f.Underline;
                strikeToolStripMenuItem.Checked = f.Strikeout;

                foreColorToolButton.Tag = displayFormat.Fore;
                backgroundColorToolButton.Tag = displayFormat.Back;

                toolButtonFontstyle.ToolTipText = $"Choose Font... ({f.FontFamily.Name}, {FormatFontSizeForUi(f.Size)} pt)";

                _updatingFontSizeUi = true;
                try
                {
                    FontSizeComboBox.Text = FormatFontSizeForUi(f.Size);
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

                    ushort numbering = GetCurrentParagraphNumbering(rtfMainText);
                    bool numericOn = numbering != PFN_NONE;

                    if (toolButtonBulletsNumeric != null) toolButtonBulletsNumeric.Checked = numericOn;
                }
                finally
                {
                    _updatingParagraphUi = false;
                }
            }
            finally
            {
                _updatingRtfUi = false;
            }
        }

        private void UpdateRtfCommandAvailability()
        {
            bool hasActiveEditor = HasActiveEditor();
            bool hasText = hasActiveEditor && rtfMainText.TextLength > 0;
            bool hasSelection = hasActiveEditor && rtfMainText.SelectionLength > 0;
            bool canPaste = hasActiveEditor && ClipboardHasPasteData();

            copyToolStripMenuItem1.Enabled = hasSelection;
            toolButtonCopy.Enabled = hasSelection;

            cutToolStripMenuItem1.Enabled = hasSelection;
            toolButtonCut.Enabled = hasSelection;

            pasteToolStripMenuItem1.Enabled = canPaste;
            toolButtonPaste.Enabled = canPaste;

            selectAllToolStripMenuItem1.Enabled = hasText;
            toolButtonSelectAll.Enabled = hasText;

            boldToolStripMenuItem.Enabled = hasActiveEditor;
            italicToolStripMenuItem.Enabled = hasActiveEditor;
            underlineToolStripMenuItem.Enabled = hasActiveEditor;
            strikeToolStripMenuItem.Enabled = hasActiveEditor;
            forecolorToolStripMenuItem.Enabled = hasActiveEditor;
            textBackgroundcolorToolStripMenuItem.Enabled = hasActiveEditor;

            boldToolButton.Enabled = hasActiveEditor;
            ItalicToolButton.Enabled = hasActiveEditor;
            UnderlineToolButton.Enabled = hasActiveEditor;
            StrikeTroughToolButton.Enabled = hasActiveEditor;
            toolButtonFontstyle.Enabled = hasActiveEditor;
            FontSizeComboBox.Enabled = hasActiveEditor;
            btnFontSizeMns.Enabled = hasActiveEditor;
            btnFontSizePls.Enabled = hasActiveEditor;
            toolButtonAlignLeft.Enabled = hasActiveEditor;
            toolButtonAlignCenter.Enabled = hasActiveEditor;
            toolButtonAlignRight.Enabled = hasActiveEditor;
            dropDownHeader.Enabled = hasActiveEditor;
            toolStripButtonBullets.Enabled = hasActiveEditor;
            toolButtonBulletsNumeric.Enabled = hasActiveEditor;
            foreColorToolButton.Enabled = hasActiveEditor;
            backgroundColorToolButton.Enabled = hasActiveEditor;
            toolTableInsert.Enabled = hasActiveEditor;
        }

        private void ResetRtfUiDisplayState()
        {
            boldToolButton.Checked = false;
            ItalicToolButton.Checked = false;
            UnderlineToolButton.Checked = false;
            StrikeTroughToolButton.Checked = false;

            boldToolStripMenuItem.Checked = false;
            italicToolStripMenuItem.Checked = false;
            underlineToolStripMenuItem.Checked = false;
            strikeToolStripMenuItem.Checked = false;

            toolButtonAlignLeft.Checked = false;
            toolButtonAlignCenter.Checked = false;
            toolButtonAlignRight.Checked = false;
            toolStripButtonBullets.Checked = false;
            toolButtonBulletsNumeric.Checked = false;

            toolButtonFontstyle.ToolTipText = "Choose Font...";

            foreColorToolButton.Tag = Color.Red;
            backgroundColorToolButton.Tag = Color.Yellow;

            _updatingFontSizeUi = true;
            try
            {
                FontSizeComboBox.Text = FormatFontSizeForUi(rtfMainText.Font.Size);
            }
            finally
            {
                _updatingFontSizeUi = false;
            }
        }

        private bool HasActiveEditor()
        {
            return !IsDisposed &&
                   _container != null &&
                   _currentBlockIndex >= 0 &&
                   splitContainer1.Panel2.Enabled &&
                   !rtfMainText.IsDisposed;
        }

        private CharFormat GetDisplayCharFormatForUi()
        {
            if (rtfMainText.IsDisposed)
                return new CharFormat(Font, ForeColor, BackColor);

            if (rtfMainText.TextLength == 0)
            {
                Font baseFont = rtfMainText.SelectionFont ?? rtfMainText.Font;
                return new CharFormat(baseFont, rtfMainText.SelectionColor, rtfMainText.SelectionBackColor);
            }

            if (rtfMainText.SelectionFont != null)
                return new CharFormat(rtfMainText.SelectionFont, rtfMainText.SelectionColor, rtfMainText.SelectionBackColor);

            int probePos = rtfMainText.SelectionLength > 0
                ? rtfMainText.SelectionStart
                : Math.Max(0, Math.Min(rtfMainText.SelectionStart, rtfMainText.TextLength - 1));

            if (rtfMainText.SelectionLength == 0 && probePos == rtfMainText.TextLength && probePos > 0)
                probePos--;

            return GetCharFormat(rtfMainText, probePos);
        }

        private static string FormatFontSizeForUi(float size)
        {
            float rounded = (float)Math.Round(size, 2);
            bool isWholeNumber = Math.Abs(rounded - MathF.Round(rounded)) < 0.01f;
            return isWholeNumber
                ? ((int)MathF.Round(rounded)).ToString(CultureInfo.InvariantCulture)
                : rounded.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static bool ClipboardHasPasteData()
        {
            try
            {
                return Clipboard.ContainsText(TextDataFormat.Rtf) ||
                       Clipboard.ContainsText(TextDataFormat.UnicodeText) ||
                       Clipboard.ContainsText();
            }
            catch
            {
                return false;
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
            if (TryGetConfiguredPrivateKeyPath(out string configuredPath))
            {
                _ecdhPrivateKeyPath = configuredPath;
                return true;
            }

            var res = MessageBox.Show(
                this,
                "No private key is configured.\n\n" +
                "Yes: Create a new key set on a drive\n" +
                "No:  Select an existing key from a drive\n" +
                "Cancel: Exit",
                "Private key setup",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
                return false;

            if (res == DialogResult.Yes)
                return CreateNewKeySetInteractiveAndStore();

            return SelectExistingEcdhPrivateKeyAndStore();
        }

        private bool CreateNewKeySetInteractiveAndStore()
        {
            if (!SelectKeyDrive(out string driveRoot))
                return false;

            string keyFolder = KeyStorage.GetKeyFolderForDrive(driveRoot);
            string ecdhPrivPath = KeyStorage.GetPrivateKeyPath(driveRoot, KeyStorage.DefaultOwnerPrivateKeyFileName);

            string ecdhPubPath = Path.ChangeExtension(ecdhPrivPath, ".hstrypub");
            string signPrivPath = DeriveSigningPrivateKeyPath(ecdhPrivPath);
            string signPubPath = Path.ChangeExtension(signPrivPath, ".hstrysigpub");

            bool overwrite = File.Exists(ecdhPrivPath) || File.Exists(ecdhPubPath) || File.Exists(signPrivPath) || File.Exists(signPubPath);
            if (overwrite)
            {
                var res = MessageBox.Show(
                    this,
                    "Key files already exist. Overwrite them?",
                    "Create key set",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (res != DialogResult.Yes)
                    return false;
            }

            if (!EnsureNewPasswordSet(out string pw))
                return false;

            try
            {
                Directory.CreateDirectory(keyFolder);

                using var ecdh = HSTRYContainer.EcdhKeyFiles.CreateNewKeyPair();
                using var ecdsa = HSTRYContainer.EcdsaKeyFiles.CreateNewKeyPair();

                // PRIVATE: encrypted (binary)
                HSTRYContainer.EcdhKeyFiles.SavePrivateKeyPkcs8Encrypted(ecdhPrivPath, ecdh, pw);
                HSTRYContainer.EcdsaKeyFiles.SavePrivateKeyPkcs8Encrypted(signPrivPath, ecdsa, pw);

                // PUBLIC: unchanged (Base64 SPKI)
                HSTRYContainer.EcdhKeyFiles.SavePublicKeySpki(ecdhPubPath, ecdh);
                HSTRYContainer.EcdsaKeyFiles.SavePublicKeySpki(signPubPath, ecdsa);

                _ecdhPrivateKeyPath = ecdhPrivPath;

                StoreSelectedPrivateKey(ecdhPrivPath);

                MessageBox.Show(
                    this,
                    "A new key set was created.\n\n" +
                    $"ECDH private:\n{ecdhPrivPath}\n" +
                    $"ECDH public:\n{ecdhPubPath}\n\n" +
                    $"ECDSA signing private:\n{signPrivPath}\n" +
                    $"ECDSA signing public:\n{signPubPath}",
                    "Key set created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                _sessionKeyPassword = null;
                MessageBox.Show(this, ex.Message, "Create key set", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool SelectExistingEcdhPrivateKeyAndStore()
        {
            return SelectPrivateKeyFromDrive(out _);
        }

        private void UpdateWindowTitle()
        {
            // Du kannst hier Global.AppName nutzen, falls das bereits "HstryDocu" ist:
            string app = $"{Global.AppName} v{Global.AppVersion}"; // oder: "HstryDocu";

            // Wenn ein Container offen ist, zeige den Pfad in [ ... ]
            string title = app;

            if (_container != null)
            {
                string path = string.IsNullOrWhiteSpace(_containerPath)
                    ? "<unsaved>"
                    : _containerPath!;

                title = $"{app} - [ {path} ]";
            }

            // Wenn irgendetwas ge ndert wurde => Stern hinten dran
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
            await CreateTestBlocksAsync(count: 100, minWordsPerBlock: 2500, maxWordsPerBlock: 5000);
        }


        private void toolButtonFontstyle_Click(object sender, EventArgs e)
        {
            Font current = GetDisplayCharFormatForUi().Font;

            using var fd = new FontDialog
            {
                Font = current,
                ShowColor = false,
                ShowEffects = false,
                FontMustExist = true,
                MinSize = 1,
                MaxSize = 200
            };

            if (fd.ShowDialog(this) != DialogResult.OK)
                return;

            ApplySelectionFont(rtfMainText, fd.Font);
            UpdateRtfUiFromSelection();
        }

        private static void ApplySelectionFont(RichTextBox rtb, Font selectedFont)
        {
            int start = rtb.SelectionStart;
            int len = rtb.SelectionLength;

            if (len == 0)
            {
                rtb.SelectionFont = selectedFont;
                return;
            }

            rtb.SelectionFont = selectedFont;
            rtb.Select(start, len);
            rtb.Focus();
        }

        private void rtfScaleBar_Scroll(object sender, EventArgs e)
        {
            ApplyEditorZoomFromTrackBar();
        }

        private void btnResetScale_Click(object? sender, EventArgs e)
        {
            int reset = 100;
            if (reset < rtfScaleBar.Minimum) reset = rtfScaleBar.Minimum;
            if (reset > rtfScaleBar.Maximum) reset = rtfScaleBar.Maximum;

            rtfScaleBar.Value = reset;
            ApplyEditorZoomFromTrackBar();
        }
        private bool EnsureSessionPasswordPrompt(out string password)
        {
            password = _sessionKeyPassword ?? string.Empty;

            if (!string.IsNullOrEmpty(_sessionKeyPassword))
                return true;

            string? pw = PasswordDialog.ShowPassword(
                this,
                "Unlock key",
                "Enter password:",
                PasswordDialog.PasswordDialogMode.Prompt);

            if (pw == null)
                return false;

            if (string.IsNullOrWhiteSpace(pw))
                return false;

            _sessionKeyPassword = pw;
            password = pw;
            return true;
        }

        private string GetContainerSavePathInKeyFolder()
        {
            if (string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath))
                throw new InvalidOperationException("No ECDH private key configured.");

            string? keyDir = Path.GetDirectoryName(_ecdhPrivateKeyPath);
            if (string.IsNullOrWhiteSpace(keyDir))
                throw new InvalidOperationException("Private key folder could not be resolved.");

            // Dateiname beibehalten, sonst Default
            string fileName = string.IsNullOrWhiteSpace(_containerPath)
                ? "container.hstry"
                : Path.GetFileName(_containerPath);

            if (!fileName.EndsWith(".hstry", StringComparison.OrdinalIgnoreCase))
                fileName = Path.ChangeExtension(fileName, ".hstry");

            return Path.Combine(keyDir, fileName);
        }


        private bool EnsureNewPasswordSet(out string password)
        {
            password = string.Empty;

            string? pw = PasswordDialog.ShowPassword(
                this,
                "Set password",
                "Set a password for your private keys:",
                PasswordDialog.PasswordDialogMode.SetNew);

            if (pw == null)
                return false;

            if (string.IsNullOrWhiteSpace(pw))
                return false;

            _sessionKeyPassword = pw; // cache in session
            password = pw;
            return true;
        }

        private ECDiffieHellman LoadEcdhPrivateKeyWithPassword(string ecdhPrivPath, string password)
        {
            return HSTRYContainer.EcdhKeyFiles.LoadPrivateKeyPkcs8Encrypted(ecdhPrivPath, password);
        }

        private ECDsa LoadEcdsaSigningKeyWithPassword(string ecdhPrivPath, string password)
        {
            string signPath = DeriveSigningPrivateKeyPath(ecdhPrivPath);
            if (!File.Exists(signPath))
                throw new FileNotFoundException("Owner signing key is missing.", signPath);

            return HSTRYContainer.EcdsaKeyFiles.LoadPrivateKeyPkcs8Encrypted(signPath, password);
        }

        private ECDsa LoadSigningKeyForCurrentSession()
        {
            if (string.IsNullOrWhiteSpace(_ecdhPrivateKeyPath))
                throw new InvalidOperationException("No ECDH private key is configured.");

            if (!EnsureSessionPasswordPrompt(out string password))
                throw new OperationCanceledException();

            return LoadEcdsaSigningKeyWithPassword(_ecdhPrivateKeyPath!, password);
        }


        private void ResetEditorScaleTo100()
        {
            int reset = 100;
            if (reset < rtfScaleBar.Minimum) reset = rtfScaleBar.Minimum;
            if (reset > rtfScaleBar.Maximum) reset = rtfScaleBar.Maximum;

            rtfScaleBar.Value = reset;
            ApplyEditorZoomFromTrackBar();
        }



    }

    public sealed record ContainerSearchHit(int BlockIndex, string BlockTitle, int IndexInText, int MatchLength, string Snippet);
}
