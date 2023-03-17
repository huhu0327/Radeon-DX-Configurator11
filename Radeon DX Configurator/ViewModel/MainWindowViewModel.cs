using Microsoft.Win32;
using Radeon_DX_Configurator.Model;
using Radeon_DX_Configurator.Tools.Ini;
using Radeon_DX_Configurator.ViewModel.Command;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Radeon_DX_Configurator.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private MainWindowModel model = new();
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

            Model.CurrentValue = new ObservableCollection<string>(vender);
        }

        private void ReadWowRegist()
        {
            using var powerKey = Parentkey.OpenSubKey(SubkeyName, true);
            if (powerKey is null || powerKey.GetValue(Vender32Name) is not string[] vender)
            {
                MessageBox.Show("There is no Vender32Name type. Exit", "null");
                return;
            }

            Model.CurrentWOWValue = new ObservableCollection<string>(vender);
        }

        private void LoadBackup()
        {
            IniFile ini = new();
            MessageBoxResult result;

            if (!ini.Load(BackupFile))
            {
                result = MessageBox.Show("There is no backup file. Create a backup file?", "backup", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                for (int i = 0; i < 4; i++)
                {
                    string num = i.ToString();
                    ini[Vender64Name][num] = Model.CurrentValue[i];
                    ini[Vender32Name][num] = Model.CurrentWOWValue[i];
                }
                ini.Save(BackupFile);

                return;
            }

            string backupPath = Path.GetDirectoryName(ini[Vender64Name]["0"].GetString());
            string curPath = Path.GetDirectoryName(Model.CurrentValue[0]);
            if (backupPath == curPath) return;

            result = MessageBox.Show("The backup recovery function does not work because the current path of the .dll does not match the backup file. Would you like to create a new backup file?", "backup", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            for (int i = 0; i < 4; i++)
            {
                string num = i.ToString();
                ini[Vender64Name][num] = Model.CurrentValue[i];
                ini[Vender32Name][num] = Model.CurrentWOWValue[i];
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

            string backupPath = Path.GetDirectoryName(ini[Vender64Name]["0"].GetString());
            string curPath = Path.GetDirectoryName(Model.CurrentValue[0]);
            if (backupPath != curPath)
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

                powerKey.SetValue(Vender64Name, new string[] { ini[Vender64Name]["0"].GetString(), ini[Vender64Name]["1"].GetString(), ini[Vender64Name]["2"].GetString(), ini[Vender64Name]["3"].GetString() });
                powerKey.SetValue(Vender32Name, new string[] { ini[Vender32Name]["0"].GetString(), ini[Vender32Name]["1"].GetString(), ini[Vender32Name]["2"].GetString(), ini[Vender32Name]["3"].GetString() });
                MessageBox.Show("Recovery success.");
            }

            ReadRegist();
            ReadWowRegist();
        }

        private void SetupRegist()
        {
            using var powerKey = Parentkey.OpenSubKey(SubkeyName, true);
            if (powerKey is null) return;

            var veder32 = new string[] { Model.CurrentWOWValue[0], Model.CurrentWOWValue[1], Model.CurrentWOWValue[2], Model.CurrentWOWValue[3] };
            var veder64 = new string[] { Model.CurrentValue[0], Model.CurrentValue[1], Model.CurrentValue[2], Model.CurrentValue[3] };
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
