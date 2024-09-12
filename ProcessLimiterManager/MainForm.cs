using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Text.Json;
using AppLimiterLibrary;
using Microsoft.Win32;
using System.IO;

namespace ProcessLimiterManager
{
    public partial class MainForm : Form
    {
        private HashSet<string> addedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<ProcessInfo> applications = new List<ProcessInfo>();
        private ListView listViewApplications;
        private Button btnRefresh;
        private Button btnSetLimits;
        private Button btnAddApplication;

        public MainForm()
        {
            SetupComponent();
            LoadApplications();
            MergeDuplicates();
            LoadExistingLimits();
        }

        private void SetupComponent()
        {
            this.Text = "Application Limiter Manager";
            this.Size = new System.Drawing.Size(800, 600);

            listViewApplications = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listViewApplications.Columns.Add("Application Name", 300);
            listViewApplications.Columns.Add("Executable", 200);
            listViewApplications.Columns.Add("Warning Time", 100);
            listViewApplications.Columns.Add("Kill Time", 100);

            btnRefresh = new Button
            {
                Text = "Refresh",
                Dock = DockStyle.Bottom
            };
            btnRefresh.Click += btnRefresh_Click;

            btnSetLimits = new Button
            {
                Text = "Set Limits",
                Dock = DockStyle.Bottom
            };
            btnSetLimits.Click += btnSetLimits_Click;

            btnAddApplication = new Button
            {
                Text = "Add Application",
                Dock = DockStyle.Bottom
            };
            btnAddApplication.Click += btnAddApplication_Click;

            this.Controls.Add(listViewApplications);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnSetLimits);
            this.Controls.Add(btnAddApplication);

        }

        private void LoadApplications()
        {
            listViewApplications.Items.Clear();
            applications.Clear();
            addedExecutables.Clear();

            // Scan registry (keep existing code)
            //ScanRegistry();

            // Scan common game directories
            ScanGameDirectories();

            // Check for specific launchers
            CheckForLaunchers();

            // Display applications in ListView
            foreach (var app in applications)
            {
                var item = new ListViewItem(app.Name);
                item.SubItems.Add(app.Executable);
                item.SubItems.Add(app.WarningTime);
                item.SubItems.Add(app.KillTime);
                listViewApplications.Items.Add(item);
            }
        }

        /*private void ScanRegistry()
        {
            string[] registryKeys = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" };

            foreach (string registryKey in registryKeys)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subkey_name in key.GetSubKeyNames())
                        {
                            using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                            {
                                string displayName = subkey.GetValue("DisplayName") as string;
                                string installLocation = subkey.GetValue("InstallLocation") as string;
                                if (!string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(installLocation))
                                {
                                    string executable = FindExecutableInDirectory(installLocation);
                                    if (!string.IsNullOrEmpty(executable) && addedExecutables.Add(executable))
                                    {
                                        var app = new ProcessInfo
                                        {
                                            Name = displayName,
                                            WarningTime = "00:00:00",
                                            KillTime = "00:00:00",
                                            Executable = executable
                                        };
                                        applications.Add(app);

                                        var item = new ListViewItem(displayName);
                                        item.SubItems.Add(executable);
                                        item.SubItems.Add("00:00:00");  // Warning Time
                                        item.SubItems.Add("00:00:00");  // Kill Time
                                        listViewApplications.Items.Add(item);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        */
        private void ScanGameDirectories()
        {
            string[] commonDirectories = {
                @"C:\Program Files (x86)\Steam\steamapps\common",
                @"C:\Program Files\Epic Games",
                @"C:\Riot Games",
                @"C:\Program Files (x86)\Origin Games"
                // Add more directories as needed
            };

            foreach (string directory in commonDirectories)
            {
                if (Directory.Exists(directory))
                {
                    foreach (string subDir in Directory.GetDirectories(directory))
                    {
                        string executable = FindExecutableInDirectory(subDir);
                        if (!string.IsNullOrEmpty(executable))
                        {
                            AddApplication(Path.GetFileNameWithoutExtension(subDir), executable);
                        }
                    }
                }
            }
        }

        private void CheckForLaunchers()
        {
            string[] launchers = {
                @"C:\Program Files (x86)\Steam\steam.exe",
                @"C:\Program Files (x86)\Epic Games\Launcher\Portal\Binaries\Win32\EpicGamesLauncher.exe",
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"C:\Program Files (x86)\Origin\Origin.exe"
                // Add more launchers as needed
            };

            foreach (string launcher in launchers)
            {
                if (File.Exists(launcher))
                {
                    AddApplication(Path.GetFileNameWithoutExtension(launcher), launcher);
                }
            }
        }

        private void AddApplication(string name, string executable)
        {
            if (addedExecutables.Add(executable))
            {
                applications.Add(new ProcessInfo
                {
                    Name = name,
                    Executable = executable,
                    WarningTime = "00:00:00",
                    KillTime = "00:00:00"
                });
            }
        }

        private string FindExecutableInDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return string.Empty;

            var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.AllDirectories);
            return exeFiles.FirstOrDefault() ?? string.Empty;
        }

        private void LoadExistingLimits()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppLimiter", "ProcessLimits.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var existingLimits = JsonSerializer.Deserialize<List<ProcessInfo>>(json);

                foreach (var limit in existingLimits)
                {
                    var listItem = listViewApplications.Items.Cast<ListViewItem>().FirstOrDefault(item => item.SubItems[1].Text.Equals(limit.Executable, StringComparison.OrdinalIgnoreCase));
                    if (listItem != null)
                    {
                        listItem.SubItems[2].Text = limit.WarningTime;
                        listItem.SubItems[3].Text = limit.KillTime;

                        var app = applications.FirstOrDefault(a => a.Executable.Equals(limit.Executable, StringComparison.OrdinalIgnoreCase));
                        if (app != null)
                        {
                            app.WarningTime = limit.WarningTime;
                            app.KillTime = limit.KillTime;
                        }
                    }
                }
            }
        }
        private void MergeDuplicates()
        {
            var groups = applications.GroupBy(app => app.Executable.ToLower());
            applications = groups.Select(group =>
            {
                var merged = group.First();
                if (group.Count() > 1)
                {
                    merged.Name = string.Join(" / ", group.Select(app => app.Name).Distinct());
                    merged.WarningTime = group.Max(app => app.WarningTime);
                    merged.KillTime = group.Max(app => app.KillTime);
                }
                return merged;
            }).ToList();
            Console.WriteLine(applications);
        }

        private void btnAddApplication_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe";
                openFileDialog.Title = "Select an Application";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string name = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    AddApplication(name, openFileDialog.FileName);

                    // Refresh the ListView
                    LoadApplications();
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadApplications();
            LoadExistingLimits();
        }

        private void btnSetLimits_Click(object sender, EventArgs e)
        {
            if (listViewApplications.SelectedItems.Count > 0)
            {
                var selectedApp = applications[listViewApplications.SelectedIndices[0]];
                using (var limitForm = new SetLimitForm(selectedApp.Name, selectedApp.WarningTime, selectedApp.KillTime))
                {
                    if (limitForm.ShowDialog() == DialogResult.OK)
                    {
                        selectedApp.WarningTime = limitForm.WarningTime;
                        selectedApp.KillTime = limitForm.KillTime;

                        listViewApplications.SelectedItems[0].SubItems[2].Text = selectedApp.WarningTime;
                        listViewApplications.SelectedItems[0].SubItems[3].Text = selectedApp.KillTime;

                        SaveLimits();
                    }
                }
            }
        }

        private void SaveLimits()
        {
            var limits = applications.Where(a => TimeSpan.Parse(a.WarningTime) > TimeSpan.Zero || TimeSpan.Parse(a.KillTime) > TimeSpan.Zero)
                                     .Select(a => new ProcessInfo
                                     {
                                         Name = a.Name,
                                         Executable = a.Executable,
                                         WarningTime = a.WarningTime,
                                         KillTime = a.KillTime
                                     });

            string json = JsonSerializer.Serialize(limits, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppLimiter", "ProcessLimits.json");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);

            MessageBox.Show("Limits saved successfully.");
        }

        public class SetLimitForm : Form
        {
            private TextBox txtWarningTime;
            private TextBox txtKillTime;

            public string WarningTime { get; private set; }
            public string KillTime { get; private set; }

            public SetLimitForm(string processName, string currentWarningTime, string currentKillTime)
            {
                Text = $"Set Limits for {processName}";

                var lblWarning = new Label { Text = "Warning Time (hh:mm:ss):", Left = 10, Top = 10 };
                txtWarningTime = new TextBox { Text = currentWarningTime, Left = 10, Top = 30, Width = 100 };

                var lblKill = new Label { Text = "Kill Time (hh:mm:ss):", Left = 10, Top = 60 };
                txtKillTime = new TextBox { Text = currentKillTime, Left = 10, Top = 80, Width = 100 };

                var btnOk = new Button { Text = "OK", Left = 10, Top = 110, DialogResult = DialogResult.OK };
                btnOk.Click += BtnOk_Click;

                var btnCancel = new Button { Text = "Cancel", Left = 100, Top = 110, DialogResult = DialogResult.Cancel };

                Controls.AddRange(new Control[] { lblWarning, txtWarningTime, lblKill, txtKillTime, btnOk, btnCancel });

                AcceptButton = btnOk;
                CancelButton = btnCancel;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                Size = new System.Drawing.Size(250, 200);
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                if (txtWarningTime.Text != null && txtKillTime.Text != null)
                {
                    WarningTime = txtWarningTime.Text;
                    KillTime = txtKillTime.Text;
                }
                else
                {
                    MessageBox.Show("Invalid time format. Please use hh:mm:ss.");
                    DialogResult = DialogResult.None;
                }
            }
        }
    }
}