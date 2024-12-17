using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TELEBLASTER_PRO.Models;
using Python.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace TELEBLASTER_PRO.ViewModels
{
    public class AccountViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Account> _accounts;
        private Account _selectedAccount;
        private bool _isRefreshing;
        private int _totalAccounts;
        private int _totalActiveAccounts;

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;
                OnPropertyChanged();
                UpdateAccountCounts();
            }
        }

        public Account SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public int TotalAccounts
        {
            get => _totalAccounts;
            set
            {
                _totalAccounts = value;
                OnPropertyChanged();
            }
        }

        public int TotalActiveAccounts
        {
            get => _totalActiveAccounts;
            set
            {
                _totalActiveAccounts = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand AddAccountCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand DeleteAccountCommand { get; }

        public AccountViewModel()
        {
            Accounts = new ObservableCollection<Account>(Account.GetAccountsFromDatabase());
            RefreshCommand = new RelayCommand(async _ => await RefreshAccountsAsync(null), CanExecuteRefresh);
            AddAccountCommand = new RelayCommand(AddAccount);
            LoginCommand = new RelayCommand(Login, CanExecuteLogin);
            LogoutCommand = new RelayCommand(Logout, CanExecuteLogout);
            DeleteAccountCommand = new RelayCommand(DeleteAccount, CanExecuteDelete);
        }

        public async Task RefreshAccountsAsync(object parameter)
        {
            System.Diagnostics.Debug.WriteLine("RefreshAccountsAsync method called");
            IsRefreshing = true;
            try
            {
                var accountsFromDb = await Task.Run(async () =>
                {
                    var accounts = new List<Account>();
                    var accountsFromDb = Account.GetAccountsFromDatabase();
                    foreach (var account in accountsFromDb)
                    {
                        account.Status = await CheckAccountLoginAsync(account.SessionName) ? "Active" : "Inactive";
                        account.UpdateStatusInDatabase(); 
                        accounts.Add(account);
                    }
                    return accounts;
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Accounts.Clear();
                    foreach (var account in accountsFromDb)
                    {
                        Accounts.Add(account);
                    }
                    UpdateAccountCounts();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("An error occurred during refresh: " + ex.Message);
            }
            finally
            {
                IsRefreshing = false;
                System.Diagnostics.Debug.WriteLine("Refresh completed");
            }
        }

        private async Task<bool> CheckAccountLoginAsync(string sessionName)
        {
            return await Task.Run(() =>
            {
                bool isLoggedIn = false;
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        using (Py.GIL())
                        {
                            dynamic py = Py.Import("functions");
                            isLoggedIn = py.check_account_login_sync(sessionName);
                        }
                    });
                    System.Diagnostics.Debug.WriteLine($"Status login akun {sessionName}: {(isLoggedIn ? "Aktif" : "Tidak Aktif")}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saat memeriksa status login untuk {sessionName}: {ex.Message}");
                }
                return isLoggedIn;
            });
        }

        private void AddAccount(object parameter)
        {
            try
            {
                string pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe");
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addAccount.py");

                Debug.WriteLine($"Python Path: {pythonPath}");
                Debug.WriteLine($"Script Path: {scriptPath}");

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"\"{pythonPath}\" \"{scriptPath}\"\"",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                using (Process process = Process.Start(start))
                {
                    process.WaitForExit();
                }

                Debug.WriteLine("Finished executing addAccount.py");
                RefreshAccountsAsync(null);
                UpdateAccountCounts();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("An error occurred while adding a new account: " + ex.Message);
            }
        }

        private void Login(object parameter)
        {
            if (SelectedAccount != null)
            {
                try
                {
                    string pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe");
                    string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "login.py");

                    Debug.WriteLine($"Python Path: {pythonPath}");
                    Debug.WriteLine($"Script Path: {scriptPath}");

                    ProcessStartInfo start = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"\"{pythonPath}\" \"{scriptPath}\" \"{SelectedAccount.SessionName}\"\"",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    using (Process process = Process.Start(start))
                    {
                        process.WaitForExit();
                    }

                    Debug.WriteLine("Finished executing login.py");
                    RefreshAccountsAsync(null);
                    UpdateAccountCounts();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("An error occurred while logging in: " + ex.Message);
                }
            }
        }

        private bool CanExecuteLogin(object parameter)
        {
            return SelectedAccount != null && SelectedAccount.Status != "Active";
        }

        private async Task LogoutAccount(Account account)
        {
            if (account != null)
            {
                bool logoutSuccessful = false;
                while (!logoutSuccessful)
                {
                    try
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            using (Py.GIL())
                            {
                                dynamic py = Py.Import("functions");
                                py.logout_and_delete_session_sync(account.SessionName);
                            }
                        });

                        account.Status = "Inactive";
                        account.SessionName = string.Empty;
                        account.UpdateStatusInDatabase();

                        OnPropertyChanged(nameof(Accounts));
                        UpdateAccountCounts();

                        logoutSuccessful = true;

                        await RefreshAccountsAsync(null);
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            Debug.WriteLine("Retrying logout due to Python error: " + pe.Message);
                            await Task.Delay(1000);
                        }
                        else
                        {
                            Debug.WriteLine($"Python error during logout for {account.Username}: {pe.Message}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during logout for {account.Username}: {ex.Message}");
                        break;
                    }
                }
            }
        }

        private void Logout(object parameter)
        {
            if (SelectedAccount != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to log out?",
                    "Logout Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    LogoutAccount(SelectedAccount);
                }
            }
        }

        private bool CanExecuteLogout(object parameter)
        {
            return SelectedAccount != null && SelectedAccount.Status == "Active";
        }

        private async void DeleteAccount(object parameter)
        {
            if (SelectedAccount != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this account?",
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (SelectedAccount.Status == "Active")
                    {
                        await LogoutAccount(SelectedAccount);
                    }

                    // Remove from database
                    Account.DeleteAccountFromDatabase(SelectedAccount);

                    // Remove from collection
                    Accounts.Remove(SelectedAccount);
                    UpdateAccountCounts();
                }
            }
        }

        private bool CanExecuteDelete(object parameter)
        {
            return SelectedAccount != null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool CanExecuteRefresh(object parameter)
        {
            return !IsRefreshing;
        }

        private void UpdateAccountCounts()
        {
            TotalAccounts = Accounts.Count;
            TotalActiveAccounts = Accounts.Count(account => account.Status == "Active");
        }

        public IEnumerable<Account> GetActiveAccounts()
        {
            return Accounts.Where(account => account.Status == "Active");
        }
    }
}
