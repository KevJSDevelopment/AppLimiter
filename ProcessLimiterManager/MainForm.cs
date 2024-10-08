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
        private Button btnRemoveApplication;
        private AppRepository _appRepository;

        public MainForm()
        {
            _appRepository = new AppRepository();
            SetupComponent();
            LoadApplications();
            MergeDuplicates();
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

            btnRemoveApplication = new Button
            {
                Text = "Remove Application",
                Dock = DockStyle.Bottom
            };
            btnRemoveApplication.Click += btnRemoveApplication_Click;

            this.Controls.Add(listViewApplications);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnSetLimits);
            this.Controls.Add(btnAddApplication);
            this.Controls.Add(btnRemoveApplication);

        }

        private async Task LoadApplications()
        {
            listViewApplications.Items.Clear();
            applications.Clear();
            addedExecutables.Clear();

            applications = await _appRepository.LoadAllLimits();

            foreach (var app in applications)
            {
                addedExecutables.Add(app.Executable);
                var item = new ListViewItem(app.Name);
                item.SubItems.Add(app.Executable);
                item.SubItems.Add(app.WarningTime);
                item.SubItems.Add(app.KillTime);
                listViewApplications.Items.Add(item);
            }
        }

        private void CheckForPersistedApplications()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppLimiter", "AddedApplications.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var persistedApps = JsonSerializer.Deserialize<List<ProcessInfo>>(json);

                    if (persistedApps != null)
                    {
                        foreach (var app in persistedApps)
                        {
                            if (!addedExecutables.Contains(app.Executable))
                            {
                                applications.Add(app);
                                addedExecutables.Add(app.Executable);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading persisted applications: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task AddApplication(string name, string executable)
        {
            if (addedExecutables.Add(executable))
            {
                var newApp = new ProcessInfo
                {
                    Name = name,
                    Executable = executable,
                    WarningTime = "00:00:00",
                    KillTime = "00:00:00"
                };
                await _appRepository.SaveLimits(newApp);
                applications.Add(newApp);
            }
        }

        private async Task RemoveApplication(string executable)
        {
            await _appRepository.DeleteApp(executable);
            applications.RemoveAll(a => a.Executable == executable);
            addedExecutables.Remove(executable);
        }

        private async Task<string> PersistApplicationsAdded()
        {
            string json = JsonSerializer.Serialize(applications, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppLimiter", "AddedApplications.json");

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json);

            return filePath;
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

        private async void btnAddApplication_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe";
                openFileDialog.Title = "Select an Application";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string name = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    await AddApplication(name, openFileDialog.FileName);
                    await LoadApplications();
                }
            }
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await LoadApplications();
        }

        private async void btnSetLimits_Click(object sender, EventArgs e)
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

                        await _appRepository.SaveLimits(selectedApp);

                        listViewApplications.SelectedItems[0].SubItems[2].Text = selectedApp.WarningTime;
                        listViewApplications.SelectedItems[0].SubItems[3].Text = selectedApp.KillTime;

                        MessageBox.Show("Limits saved successfully.");
                    }
                }
            }
        }

        private async void btnRemoveApplication_Click(object sender, EventArgs e)
        {
            if (listViewApplications.SelectedItems.Count > 0)
            {
                var selectedApp = applications[listViewApplications.SelectedIndices[0]];
                await RemoveApplication(selectedApp.Executable);
                await LoadApplications();
            }
        }
    }
}