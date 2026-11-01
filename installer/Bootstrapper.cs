using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AstraHorizonSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupWindow());
        }
    }

    internal sealed class SetupWindow : Form
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("ASTRASETUPV2");
        private const int MagicLength = 16;
        private const int TrailerLength = 24;
        private readonly TextBox destination;
        private readonly CheckBox desktopShortcut;
        private readonly Button installButton;
        private readonly Button cancelButton;
        private readonly ProgressBar progressBar;
        private readonly Label status;
        private CancellationTokenSource cancellation;
        private bool installing;

        public SetupWindow()
        {
            Text = "ASTRA OS Horizon Setup";
            Width = 680;
            Height = 430;
            MinimumSize = new Size(580, 400);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(8, 17, 31);
            ForeColor = Color.FromArgb(231, 243, 255);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            var header = new Panel { Dock = DockStyle.Top, Height = 118, BackColor = Color.FromArgb(13, 31, 53), Padding = new Padding(28, 20, 28, 14) };
            var title = new Label { Dock = DockStyle.Top, Height = 38, Text = "ASTRA OS  ·  HORIZON", Font = new Font("Segoe UI Semibold", 20f), ForeColor = Color.FromArgb(224, 244, 255) };
            var subtitle = new Label { Dock = DockStyle.Fill, Text = "Install the Windows workspace for your account. No administrator access is required.", ForeColor = Color.FromArgb(160, 194, 224), Font = new Font("Segoe UI", 10f) };
            header.Controls.Add(subtitle);
            header.Controls.Add(title);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 18) };
            var locationLabel = new Label { Dock = DockStyle.Top, Height = 26, Text = "Installation folder", ForeColor = Color.FromArgb(193, 221, 246) };
            var browseRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 2, Padding = new Padding(0), Margin = new Padding(0) };
            browseRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            browseRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102f));
            destination = new TextBox { Dock = DockStyle.Fill, Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "ASTRA OS Horizon"), BackColor = Color.FromArgb(17, 38, 63), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10f) };
            var browse = MakeButton("Browse…", false);
            browse.Click += Browse_Click;
            browseRow.Controls.Add(destination, 0, 0);
            browseRow.Controls.Add(browse, 1, 0);

            desktopShortcut = new CheckBox { Dock = DockStyle.Top, Height = 37, Text = "Create a desktop shortcut", Checked = true, ForeColor = Color.FromArgb(193, 221, 246), Padding = new Padding(0, 7, 0, 0) };
            var info = new Label { Dock = DockStyle.Top, Height = 76, Text = "OpenRouter AI is available after setup with your own free API key. The included offline model is optional. Your Second Brain notes, PDFs, and preferences are kept separately from the app folder.", ForeColor = Color.FromArgb(145, 180, 211), Font = new Font("Segoe UI", 9.5f), Padding = new Padding(0, 8, 0, 0) };
            status = new Label { Dock = DockStyle.Bottom, Height = 28, Text = "Ready to install · Windows account only", ForeColor = Color.FromArgb(143, 197, 226), TextAlign = ContentAlignment.MiddleLeft };
            progressBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 14, Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous, Value = 0 };
            body.Controls.Add(info);
            body.Controls.Add(desktopShortcut);
            body.Controls.Add(browseRow);
            body.Controls.Add(locationLabel);
            body.Controls.Add(progressBar);
            body.Controls.Add(status);

            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 70, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(20, 12, 28, 12), BackColor = Color.FromArgb(10, 22, 39), WrapContents = false };
            installButton = MakeButton("Install ASTRA", true);
            installButton.Width = 158;
            installButton.Click += Install_Click;
            cancelButton = MakeButton("Cancel", false);
            cancelButton.Width = 110;
            cancelButton.Click += Cancel_Click;
            footer.Controls.Add(installButton);
            footer.Controls.Add(cancelButton);

            Controls.Add(body);
            Controls.Add(footer);
            Controls.Add(header);
            AcceptButton = installButton;
            CancelButton = cancelButton;
        }

        private Button MakeButton(string text, bool primary)
        {
            return new Button
            {
                Text = text,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = primary ? Color.FromArgb(68, 158, 206) : Color.FromArgb(24, 48, 76),
                ForeColor = Color.White,
                FlatAppearance = { BorderColor = primary ? Color.FromArgb(95, 190, 232) : Color.FromArgb(51, 82, 114), BorderSize = 1 },
                Font = new Font("Segoe UI Semibold", 9.5f),
                Margin = new Padding(7, 0, 0, 0),
                Cursor = Cursors.Hand
            };
        }

        private void Browse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose where ASTRA OS Horizon will be installed.";
                dialog.ShowNewFolderButton = true;
                string current = destination.Text.Trim().Trim('"');
                string parent = Path.GetDirectoryName(current);
                if (!String.IsNullOrWhiteSpace(parent) && Directory.Exists(parent)) dialog.SelectedPath = parent;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string selected = dialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    destination.Text = String.Equals(Path.GetFileName(selected), "ASTRA OS Horizon", StringComparison.OrdinalIgnoreCase)
                        ? selected : Path.Combine(selected, "ASTRA OS Horizon");
                }
            }
        }

        private async void Install_Click(object sender, EventArgs e)
        {
            if (installing) return;
            string target;
            try
            {
                target = Path.GetFullPath(destination.Text.Trim().Trim('"'));
                if (target.Length < 8 || target.IndexOfAny(Path.GetInvalidPathChars()) >= 0) throw new InvalidDataException("Choose a valid installation folder.");
                if (File.Exists(target)) throw new InvalidDataException("The installation path points to a file. Choose a folder instead.");
                if (Directory.Exists(target) && Directory.GetFileSystemEntries(target).Length > 0)
                {
                    var answer = MessageBox.Show(this, "This folder already contains files:\r\n" + target + "\r\n\r\nInstall ASTRA here and replace matching application files? Other files will be kept.", "ASTRA OS Horizon Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (answer != DialogResult.Yes) return;
                }
                Process[] running = Process.GetProcessesByName("Astra.Desktop");
                bool appIsRunning = running.Length > 0;
                foreach (Process process in running) process.Dispose();
                if (appIsRunning)
                {
                    MessageBox.Show(this, "Close ASTRA OS Horizon and try the installer again.", "ASTRA OS Horizon Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "ASTRA OS Horizon Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            installing = true;
            cancellation = new CancellationTokenSource();
            installButton.Enabled = false;
            destination.Enabled = false;
            cancelButton.Text = "Cancel";
            status.Text = "Preparing the verified application package…";
            var progress = new Progress<InstallProgress>(value =>
            {
                progressBar.Value = Math.Max(0, Math.Min(100, value.Percent));
                status.Text = value.Status;
            });
            try
            {
                await Task.Run(() => ExtractPackage(target, desktopShortcut.Checked, progress, cancellation.Token), cancellation.Token);
                status.Text = "ASTRA OS Horizon installed successfully.";
                progressBar.Value = 100;
                var start = MessageBox.Show(this, "ASTRA OS Horizon is installed for your Windows account. Start it now?", "ASTRA OS Horizon Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (start == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo(Path.Combine(target, "Astra.Desktop.exe")) { WorkingDirectory = target, UseShellExecute = true });
                Close();
            }
            catch (OperationCanceledException)
            {
                status.Text = "Installation cancelled. You can run Setup again to continue.";
                progressBar.Value = 0;
                installButton.Enabled = true;
                destination.Enabled = true;
            }
            catch (Exception ex)
            {
                status.Text = "Installation needs attention.";
                MessageBox.Show(this, "ASTRA OS Horizon could not be installed. Close ASTRA and check available disk space, then run Setup again.\r\n\r\n" + ex.Message, "ASTRA OS Horizon Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                installButton.Enabled = true;
                destination.Enabled = true;
            }
            finally
            {
                installing = false;
                cancellation.Dispose();
                cancellation = null;
                cancelButton.Text = "Close";
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            if (installing)
            {
                cancelButton.Enabled = false;
                status.Text = "Cancelling after the current file…";
                cancellation.Cancel();
                return;
            }
            Close();
        }

        private void ExtractPackage(string installRoot, bool addDesktopShortcut, IProgress<InstallProgress> progress, CancellationToken token)
        {
            string executable = Assembly.GetExecutingAssembly().Location;
            var info = new FileInfo(executable);
            if (info.Length < TrailerLength) throw new InvalidDataException("The setup file is incomplete.");
            using (var file = new FileStream(executable, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                file.Position = file.Length - TrailerLength;
                byte[] trailer = ReadExactly(file, TrailerLength);
                for (int i = 0; i < Magic.Length; i++) if (trailer[i] != Magic[i]) throw new InvalidDataException("The embedded ASTRA package marker is invalid.");
                long packageLength = BitConverter.ToInt64(trailer, MagicLength);
                long packageStart = file.Length - TrailerLength - packageLength;
                if (packageLength <= 0 || packageStart <= 0 || packageStart > file.Length - TrailerLength) throw new InvalidDataException("The embedded ASTRA package length is invalid.");
                var slice = new SeekableSliceStream(file, packageStart, packageLength);
                using (var archive = new ZipArchive(slice, ZipArchiveMode.Read, true))
                {
                    long total = 0;
                    foreach (var entry in archive.Entries) total += entry.Length;
                    long done = 0;
                    string root = Path.GetFullPath(installRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string rootPrefix = root + Path.DirectorySeparatorChar;
                    Directory.CreateDirectory(root);
                    foreach (var entry in archive.Entries)
                    {
                        token.ThrowIfCancellationRequested();
                        string name = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                        if (name.Length == 0 || Path.IsPathRooted(name) || name.IndexOf(':') >= 0) throw new InvalidDataException("The package contains an unsafe path.");
                        var segments = name.Split(Path.DirectorySeparatorChar);
                        foreach (string segment in segments) if (segment == ".." || segment == ".") throw new InvalidDataException("The package contains a path traversal entry.");
                        if (segments.Length < 2 || !String.Equals(segments[0], "ASTRA-OS-V4-HORIZON", StringComparison.Ordinal)) throw new InvalidDataException("The package folder layout is not recognized.");
                        string relative = String.Join(Path.DirectorySeparatorChar.ToString(), segments, 1, segments.Length - 1);
                        if (relative.Length == 0) continue;
                        string destinationPath = Path.GetFullPath(Path.Combine(root, relative));
                        if (!destinationPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("The package contains a path outside the installation folder.");
                        if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) { Directory.CreateDirectory(destinationPath); continue; }
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                        using (Stream source = entry.Open())
                        using (var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
                        {
                            byte[] buffer = new byte[1024 * 1024];
                            int read;
                            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                token.ThrowIfCancellationRequested();
                                output.Write(buffer, 0, read);
                                done += read;
                                if (total > 0) progress.Report(new InstallProgress((int)(done * 100 / total), "Installing application files… " + (done * 100 / total) + "%"));
                            }
                        }
                    }
                }
            }
            token.ThrowIfCancellationRequested();
            if (!File.Exists(Path.Combine(installRoot, "Astra.Desktop.exe")) || !File.Exists(Path.Combine(installRoot, "FILES.sha256"))) throw new InvalidDataException("The extracted application did not pass its required-file check.");
            WriteUninstaller(installRoot);
            CreateShortcuts(installRoot, addDesktopShortcut);
            RegisterUninstall(installRoot);
            progress.Report(new InstallProgress(100, "Creating shortcuts and finishing setup…"));
        }

        private static void WriteUninstaller(string installRoot)
        {
            const string resourceName = "AstraInstaller.Uninstall.ps1";
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resource == null) throw new InvalidDataException("The uninstall helper is missing.");
                using (var target = new FileStream(Path.Combine(installRoot, "Uninstall-ASTRA.ps1"), FileMode.Create, FileAccess.Write, FileShare.None)) resource.CopyTo(target);
            }
        }

        private static void CreateShortcuts(string installRoot, bool addDesktop)
        {
            string menu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "ASTRA OS Horizon");
            Directory.CreateDirectory(menu);
            CreateShortcut(Path.Combine(menu, "ASTRA OS Horizon.lnk"), Path.Combine(installRoot, "Astra.Desktop.exe"), installRoot, Path.Combine(installRoot, "Assets", "astra.ico"));
            string powershell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            CreateShortcut(Path.Combine(menu, "Uninstall ASTRA OS Horizon.lnk"), powershell, installRoot, null, "-NoProfile -ExecutionPolicy Bypass -File \"" + Path.Combine(installRoot, "Uninstall-ASTRA.ps1") + "\"");
            if (addDesktop) CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ASTRA OS Horizon.lnk"), Path.Combine(installRoot, "Astra.Desktop.exe"), installRoot, Path.Combine(installRoot, "Assets", "astra.ico"));
        }

        private static void CreateShortcut(string path, string target, string workingDirectory, string icon, string arguments = "")
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) throw new InvalidOperationException("Windows could not create the ASTRA shortcuts.");
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
            Type type = shortcut.GetType();
            type.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { target });
            type.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            type.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
            if (!String.IsNullOrEmpty(icon)) type.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { icon + ",0" });
            type.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }

        private static void RegisterUninstall(string installRoot)
        {
            string script = Path.Combine(installRoot, "Uninstall-ASTRA.ps1");
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ASTRAOSHorizon"))
            {
                key.SetValue("DisplayName", "ASTRA OS V4 Horizon");
                key.SetValue("DisplayVersion", "4.0.0.5");
                key.SetValue("Publisher", "Emanuele Magno and ASTRA contributors");
                key.SetValue("InstallLocation", installRoot);
                key.SetValue("UninstallString", "\"" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe") + "\" -NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                key.SetValue("EstimatedSize", (int)Math.Min(Int32.MaxValue, DirectorySize(installRoot) / 1024), RegistryValueKind.DWord);
            }
        }

        private static long DirectorySize(string root)
        {
            long size = 0;
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                try { size += new FileInfo(file).Length; } catch (IOException) { }
            }
            return size;
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            byte[] bytes = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(bytes, offset, count - offset);
                if (read <= 0) throw new EndOfStreamException("The setup trailer is truncated.");
                offset += read;
            }
            return bytes;
        }
    }

    internal sealed class InstallProgress
    {
        public readonly int Percent;
        public readonly string Status;
        public InstallProgress(int percent, string status) { Percent = percent; Status = status; }
    }

    internal sealed class SeekableSliceStream : Stream
    {
        private readonly Stream source;
        private readonly long start;
        private readonly long length;
        private long position;
        public SeekableSliceStream(Stream source, long start, long length) { this.source = source; this.start = start; this.length = length; }
        public override bool CanRead { get { return source.CanRead; } }
        public override bool CanSeek { get { return source.CanSeek; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return length; } }
        public override long Position { get { return position; } set { Seek(value, SeekOrigin.Begin); } }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= length) return 0;
            int allowed = (int)Math.Min(count, length - position);
            source.Position = start + position;
            int read = source.Read(buffer, offset, allowed);
            position += read;
            return read;
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            long next = origin == SeekOrigin.Begin ? offset : origin == SeekOrigin.Current ? position + offset : length + offset;
            if (next < 0 || next > length) throw new IOException("Seek is outside the embedded package.");
            position = next;
            return position;
        }
        public override void Flush() { }
        public override void SetLength(long value) { throw new NotSupportedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
    }
}
