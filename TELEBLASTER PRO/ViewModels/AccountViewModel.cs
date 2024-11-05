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

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;
                OnPropertyChanged();
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

        public ICommand RefreshCommand { get; }
        public ICommand AddAccountCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand LogoutCommand { get; }

        public AccountViewModel()
        {
            Accounts = new ObservableCollection<Account>(Account.GetAccountsFromDatabase());
            RefreshCommand = new RelayCommand(RefreshAccounts, CanExecuteRefresh);
            AddAccountCommand = new RelayCommand(AddAccount);
            LoginCommand = new RelayCommand(Login, CanExecuteLogin);
            LogoutCommand = new RelayCommand(Logout, CanExecuteLogout);
        }

        private async void RefreshAccounts(object parameter)
        {
            System.Diagnostics.Debug.WriteLine("RefreshAccounts method called");
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
                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c title TELEBLASTER PRO & python addAccount.py",
                    RedirectStandardOutput = false,
                    UseShellExecute = true
                };

                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Console.WriteLine(result);
                    }
                    using (StreamReader reader = process.StandardError)
                    {
                        string error = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error: " + error);
                        }
                    }
                }

                RefreshAccounts(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while adding a new account: " + ex.Message);
            }
        }

        private void Login(object parameter)
        {
            if (SelectedAccount != null)
            {
                SelectedAccount.Status = "Active";
                OnPropertyChanged(nameof(Accounts));
            }
        }

        private bool CanExecuteLogin(object parameter)
        {
            return SelectedAccount != null && SelectedAccount.Status != "Active";
        }

        private void Logout(object parameter)
        {
            if (SelectedAccount != null)
            {
                SelectedAccount.Status = "Inactive";
                OnPropertyChanged(nameof(Accounts));
            }
        }

        private bool CanExecuteLogout(object parameter)
        {
            return SelectedAccount != null && SelectedAccount.Status == "Active";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool CanExecuteRefresh(object parameter)
        {
            return !IsRefreshing; // Allow refresh only if not already refreshing
        }

            public IEnumerable<Account> GetActiveAccounts()
        {
            // Filter akun yang statusnya "Active"
            return Accounts.Where(account => account.Status == "Active");
        }
    }
}
