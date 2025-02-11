using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using HourglassLibrary.Data;
using HourglassLibrary.Dtos;
using Microsoft.Win32;
using NAudio.Wave;
using HourglassManager.WPF.Views;
using HourglassManager.WPF.Commands;

namespace HourglassManager.WPF.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsRepository _settingsRepo;
        private readonly string _computerId;

        public ICommand SaveCommand { get; }

        public SettingsViewModel(string computerId)
        {
            _computerId = computerId;
            _settingsRepo = new SettingsRepository(computerId);
            SaveCommand = new AsyncRelayCommand(_ => Save());

            LoadSettings();
        }

        private async void LoadSettings()
        {

        }

        private async Task Save()
        {
            // Save any general settings here
            await Task.CompletedTask;
        }

        public void Cleanup()
        {

        }
    }
}
