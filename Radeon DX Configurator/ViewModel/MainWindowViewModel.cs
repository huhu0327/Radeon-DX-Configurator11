using Microsoft.Win32;
using Radeon_DX_Configurator.Model;
using Radeon_DX_Configurator.Tools.Ini;
using Radeon_DX_Configurator.ViewModel.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Radeon_DX_Configurator.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private MainWindowModel model;
        public MainWindowModel Model
        {
            get { return model; }
            set { model = value; OnPropertyChanged("Model"); }
        }

        private readonly RegistryKey Parentkey = Registry.LocalMachine;

        private readonly string[] RegularDX_LIST =
        {
            "atiumd64.dll",
            "atiumd64.dll",
            "atidxx64.dll",
            "atidxx64.dll"
        };

        private readonly string[] DXNAVI_LIST =
        {
            "amdxn64.dll",
            "amdxn64.dll",
            "amdxx64.dll",
            "amdxx64.dll"
        };

        private readonly string[] RegularDXWOW_LIST =
        {
            "atiumdag.dll",
            "atiumdag.dll",
            "atidxx32.dll",
            "atidxx32.dll"
        };

        private readonly string[] DXNAVIWOW_LIST =
        {
            "amdxn32.dll",
            "amdxn32.dll",
            "amdxx32.dll",
            "amdxx32.dll"
        };

        private readonly int[] DX9_Idxs = { 0, 1 };
        private readonly int[] DX11_Idxs = { 2, 3 };

        public ICommand RegularDX9Button { get; }
        public ICommand RegularDX11Button { get; }
        public ICommand DXNAVI_DX9Button { get; }
        public ICommand DXNAVI_DX11Button { get; }
        public ICommand ApplyButton { get; }
        public ICommand RestoreBackupButton { get; }

        private readonly string BackupFile = "backup.ini";

        private readonly string Vender32Name = "D3DVendorNameWoW";
        private readonly string Vender64Name = "D3DVendorName";

        private readonly string SubkeyName;

        public MainWindowViewModel()
        {
            var osVersion = Environment.OSVersion.Version.Build < 2200 ? 0 : 1;
            SubkeyName = $@"SYSTEM\CurrentControlSet\Control\Class\{{4d36e968-e325-11ce-bfc1-08002be10318}}\{string.Format("{0:X4}", osVersion)}";
            Model = new()
            {
                CurrentValue = new ObservableCollection<string>(),
                CurrentWOWValue = new ObservableCollection<string>()
            };

            ReadRegist();
            ReadWowRegist();

            LoadBackup();

            RegularDX9Button = new DelegateCommand<object>(p => RegularDX9ButtonClick(p));
            RegularDX11Button = new DelegateCommand<object>(p => RegularDX11ButtonClick(p));
            DXNAVI_DX9Button = new DelegateCommand<object>(p => DXNAVI_DX9ButtonClick(p));
            DXNAVI_DX11Button = new DelegateCommand<object>(p => DXNAVI_DX11ButtonClick(p));
            ApplyButton = new DelegateCommand<object>(_ => ApplyButtonClick());
            RestoreBackupButton = new DelegateCommand<object>(p => RestoreBackupButtonClick(p));
        }

        private void ReadRegist()
        {
            using var powerKey = Parentkey.OpenSubKey(SubkeyName, true);
            if (powerKey is null || powerKey.GetValue(Vender64Name) is not string[] vender)
            {
                MessageBox.Show("There is no Vender64Name type. Exit", "null");
                return;
            }

            if (Model.CurrentValue.Count > 0)
            {
                Model.CurrentValue.Clear();
            }

            foreach (var value in vender)
            {
                Model.CurrentValue.Add(value);
            }
        }

        private void ReadWowRegist()
        {
            using var powerKey = Parentkey.OpenSubKey(SubkeyName, true);
            if (powerKey is null || powerKey.GetValue(Vender32Name) is not string[] vender)
            {
                MessageBox.Show("There is no Vender32Name type. Exit", "null");
                return;
            }

            if (Model.CurrentWOWValue.Count > 0)
            {
                Model.CurrentWOWValue.Clear();
            }

            foreach (var value in vender)
            {
                Model.CurrentWOWValue.Add(value);
            }
        }

        private void LoadBackup()
        {
            IniFile ini = new();
            MessageBoxResult result;

            if (!ini.Load(BackupFile))
            {
                result = MessageBox.Show("There is no backup file. Create a backup file?", "backup", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                SaveIniFile(ini);
                return;
            }

            if (EqualIniAndCurrentValue(ini)) return;

            result = MessageBox.Show("The backup recovery function does not work because the current path of the .dll does not match the backup file. Would you like to create a new backup file?", "backup", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            SaveIniFile(ini);
        }

        private void SaveIniFile(IniFile ini)
        {
            for (int i = 0; i < 4; i++)
            {
                ini[Vender64Name][i.ToString()] = Model.CurrentValue[i];
                ini[Vender32Name][i.ToString()] = Model.CurrentWOWValue[i];
            }
            ini.Save(BackupFile);
        }

        private void ApplyButtonClick()
        {
            var result = MessageBox.Show($"Apply?", "Apply?", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            SetupRegist();
        }

        private void RestoreBackupButtonClick(object _)
        {
            var ini = new IniFile();
            if (!ini.Load(BackupFile)) return;

            if (!EqualIniAndCurrentValue(ini))
            {
                MessageBox.Show("Recovery failed because the current path of the .dll does not match the backup file");
                return;
            }

            using (var powerKey = Parentkey.OpenSubKey(SubkeyName, true))
            {
                if (powerKey is null)
                {
                    MessageBox.Show("Not found registry key directory");
                    return;
                }

                var vender32 = ini[Vender32Name].Values
                    .Select(x => x.Value.ToString())
                    .ToArray();
                var vender64 = ini[Vender64Name].Values
                    .Select(x => x.Value.ToString())
                    .ToArray();

                powerKey.SetValue(Vender32Name, vender32);
                powerKey.SetValue(Vender64Name, vender64);

                MessageBox.Show("Recovery success.");
            }

            ReadRegist();
            ReadWowRegist();
        }

        private bool EqualIniAndCurrentValue(IniFile ini)
        {
            var backup32Path = ini[Vender32Name].Values
                .Select(x => Path.GetDirectoryName(x.Value.ToString()))
                .ToArray();
            var cur32Path = Model.CurrentWOWValue
                .Select(x => Path.GetDirectoryName(x))
                .ToArray();

            var backup64Path = ini[Vender64Name].Values
                .Select(x => Path.GetDirectoryName(x.Value.ToString()))
                .ToArray();
            var cur64Path = Model.CurrentValue
                .Select(x => Path.GetDirectoryName(x))
                .ToArray();

            return backup32Path.SequenceEqual(cur32Path) && backup64Path.SequenceEqual(cur64Path);
        }

        private void SetupRegist()
        {
            using var powerKey = Parentkey.OpenSubKey(SubkeyName, true);
            if (powerKey is null) return;

            var veder32 = Model.CurrentWOWValue.ToArray();
            var veder64 = Model.CurrentValue.ToArray();

            powerKey.SetValue(Vender32Name, veder32);
            powerKey.SetValue(Vender64Name, veder64);
        }

        private void SetVender(int[] idxs, string[] list32, string[] list64)
        {
            foreach (var item in idxs)
            {
                var path_32 = Path.GetDirectoryName(Model.CurrentWOWValue[item]);
                var path_64 = Path.GetDirectoryName(Model.CurrentValue[item]);

                Model.CurrentWOWValue[item] = Path.Combine(path_32, list32[item]);
                Model.CurrentValue[item] = Path.Combine(path_64, list64[item]);
            }
        }

        private void RegularDX9ButtonClick(object _)
        {
            SetVender(DX9_Idxs, RegularDXWOW_LIST, RegularDX_LIST);
        }
        private void RegularDX11ButtonClick(object _)
        {
            SetVender(DX11_Idxs, RegularDXWOW_LIST, RegularDX_LIST);
        }
        private void DXNAVI_DX9ButtonClick(object _)
        {
            SetVender(DX9_Idxs, DXNAVIWOW_LIST, DXNAVI_LIST);
        }
        private void DXNAVI_DX11ButtonClick(object _)
        {
            SetVender(DX11_Idxs, DXNAVIWOW_LIST, DXNAVI_LIST);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
