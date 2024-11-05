using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TELEBLASTER_PRO.Models;
using Python.Runtime;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Data.SQLite;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class GroupChannelViewModel : INotifyPropertyChanged
    {
        private readonly AccountViewModel _accountViewModel;
        private string _selectedPhoneNumber;
        private ObservableCollection<GroupInfo> _loadedGroups;
        private bool _isLoading;
        private bool _isExtracting;
        private int _extractedMembersCount;
        private GroupInfo _selectedGroup;
        private ObservableCollection<GroupMember> _extractedMembers;

        public ICommand LoadGroupsCommand { get; }
        public ICommand ExtractMembersCommand { get; }
        public ICommand StopExtractionCommand { get; }
        public ObservableCollection<string> ActivePhoneNumbers { get; }
        public ObservableCollection<GroupInfo> LoadedGroups
        {
            get => _loadedGroups;
            set
            {
                _loadedGroups = value;
                OnPropertyChanged(nameof(LoadedGroups));
            }
        }
        public string SelectedPhoneNumber
        {
            get => _selectedPhoneNumber;
            set
            {
                _selectedPhoneNumber = value;
                ExtractedDataStore.Instance.SelectedPhoneNumber = value;
                OnPropertyChanged(nameof(SelectedPhoneNumber));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }
        public bool IsExtracting
        {
            get => _isExtracting;
            set
            {
                if (_isExtracting != value)
                {
                    _isExtracting = value;
                    OnPropertyChanged(nameof(IsExtracting));
                }
            }
        }
        public int ExtractedMembersCount
        {
            get => _extractedMembersCount;
            set
            {
                _extractedMembersCount = value;
                OnPropertyChanged(nameof(ExtractedMembersCount));
            }
        }
        public GroupInfo SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                ExtractedDataStore.Instance.SelectedGroup = value;
                OnPropertyChanged(nameof(SelectedGroup));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public ObservableCollection<GroupMember> ExtractedMembers
        {
            get => _extractedMembers;
            set
            {
                _extractedMembers = value;
                OnPropertyChanged(nameof(ExtractedMembers));
            }
        }

        public GroupChannelViewModel(AccountViewModel accountViewModel)
        {
            _accountViewModel = accountViewModel;
            LoadGroupsCommand = new RelayCommand(async _ => await StartLoadingGroupsAsync(), CanExecuteLoadGroups);
            ExtractMembersCommand = new RelayCommand(async _ => await StartExtractingMembersAsync(), CanExecuteExtractMembers);
            StopExtractionCommand = new RelayCommand(_ => StopExtraction(), CanExecuteStopExtraction);
            ActivePhoneNumbers = new ObservableCollection<string>(GetActiveAccounts().Select(a => a.Phone));
            LoadedGroups = ExtractedDataStore.Instance.LoadedGroups;
            ExtractedMembers = ExtractedDataStore.Instance.ExtractedMembers;

            SelectedPhoneNumber = ExtractedDataStore.Instance.SelectedPhoneNumber;
            SelectedGroup = ExtractedDataStore.Instance.SelectedGroup;

            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing Python: {ex.Message}");
            }
        }

        private void Initialize()
        {
            if (!PythonEngine.IsInitialized)
            {
                PythonEngine.Initialize();
                PythonEngine.BeginAllowThreads();
            }
        }

        private async Task StartLoadingGroupsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                System.Diagnostics.Debug.WriteLine($"StartLoadingGroupsAsync running on thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                List<GroupInfo> loadedGroups = null;

                while (true) // Loop until successful
                {
                    try
                    {
                        loadedGroups = await Task.Run(() =>
                        {
                            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
                            System.Diagnostics.Debug.WriteLine($"LoadGroups running on thread: {threadId}");

                            if (threadId == 5)
                            {
                                System.Diagnostics.Debug.WriteLine("Skipping execution on thread 5");
                                return null; // Skip execution on thread 5
                            }

                            return LoadGroups();
                        });

                        if (loadedGroups != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Groups loaded successfully.");
                            break; // Exit loop if loading is successful
                        }
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            System.Diagnostics.Debug.WriteLine("Retrying due to Python error: " + pe.Message);
                            await Task.Delay(1000); // Wait for 1 second before retrying
                            continue; // Retry immediately
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Python error loading groups: {pe.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading groups: {ex.Message}");
                    }

                    // Optional: Add a delay to prevent rapid retry in case of other exceptions
                    await Task.Delay(1000);
                }

                if (loadedGroups != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Updating UI on thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                        LoadedGroups.Clear();
                        foreach (var groupInfo in loadedGroups)
                        {
                            LoadedGroups.Add(groupInfo);
                        }
                    });
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<GroupInfo> LoadGroups()
        {
            var loadedGroups = new List<GroupInfo>();
            var activeAccounts = GetActiveAccounts().Where(a => a.Phone == SelectedPhoneNumber);
            foreach (var account in activeAccounts)
            {
                try
                {
                    using (Py.GIL())
                    {
                        dynamic py = Py.Import("functions");
                        var groups = py.extract_groups_and_channels_sync(account.SessionName);

                        foreach (var group in groups)
                        {
                            var groupInfo = new GroupInfo
                            {
                                GroupId = group["group_id"].ToString(),
                                TotalMembers = group["total_members"],
                                GroupName = group["group_name"]
                            };
                            loadedGroups.Add(groupInfo);
                        }
                    }
                }
                catch (PythonException pe)
                {
                    System.Diagnostics.Debug.WriteLine($"Python error loading groups for {account.SessionName}: {pe.Message}");
                    return null; // Return null if there's a Python exception
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading groups for {account.SessionName}: {ex.Message}");
                    return null; // Return null if there's any other exception
                }
            }
            return loadedGroups;
        }

        public IEnumerable<Account> GetActiveAccounts()
        {
            return _accountViewModel.GetActiveAccounts();
        }

        private bool CanExecuteLoadGroups(object parameter)
        {
            return !string.IsNullOrEmpty(SelectedPhoneNumber);
        }

        private bool CanExecuteExtractMembers(object parameter)
        {
            bool canExecute = !string.IsNullOrEmpty(SelectedPhoneNumber) && SelectedGroup != null;
            System.Diagnostics.Debug.WriteLine($"CanExecuteExtractMembers: {canExecute}");
            return canExecute;
        }

        private bool CanExecuteStopExtraction(object parameter)
        {
            return IsExtracting;
        }

        private async Task StartExtractingMembersAsync()
        {
            if (IsExtracting) return;

            IsExtracting = true;
            ExtractedMembersCount = 0;

            try
            {
                System.Diagnostics.Debug.WriteLine("Starting member extraction...");

                if (SelectedGroup == null)
                {
                    System.Diagnostics.Debug.WriteLine("No group selected for extraction.");
                    return;
                }

                string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);
                if (string.IsNullOrEmpty(sessionName))
                {
                    System.Diagnostics.Debug.WriteLine("Session name not found for the selected phone number.");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Extracting members from group: {SelectedGroup.GroupName} (ID: {SelectedGroup.GroupId}) using session: {sessionName}");

                if (!long.TryParse(SelectedGroup.GroupId, out long groupId))
                {
                    System.Diagnostics.Debug.WriteLine("Error: GroupId is not a valid long.");
                    return;
                }

                List<GroupMembers> extractedMembers = null;

                while (true) // Loop until successful
                {
                    try
                    {
                        extractedMembers = await Task.Run(() =>
                        {
                            using (Py.GIL())
                            {
                                dynamic py = Py.Import("functions");
                                py.extract_members_sync(sessionName, groupId, SelectedGroup.GroupName, new Action<string>(UpdateExtractStatus));
                            }
                            return LoadExtractedMembers(groupId);
                        });

                        if (extractedMembers != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Member extraction completed.");
                            break;
                        }
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            System.Diagnostics.Debug.WriteLine("Retrying due to Python error: " + pe.Message);
                            await Task.Delay(1000); // Wait for 1 second before retrying
                            continue; // Retry immediately
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Python error during member extraction: {pe.Message}");
                            break; // Exit loop if it's a different Python error
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error during member extraction: {ex.Message}");
                        break; // Exit loop on other exceptions
                    }
                }

                if (extractedMembers != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ExtractedDataStore.Instance.ExtractedMembers.Clear();
                        int no = 1; // Mulai dari 1
                        foreach (var member in extractedMembers)
                        {
                            long memberId;
                            if (!long.TryParse(member.MemberId, out memberId))
                            {
                                System.Diagnostics.Debug.WriteLine($"Error: MemberId '{member.MemberId}' is not a valid long.");
                                continue; // Skip this member if conversion fails
                            }

                            ExtractedDataStore.Instance.ExtractedMembers.Add(new GroupMember
                            {
                                No = no++, // Set nomor urut
                                MemberId = memberId, // Use the converted long value
                                AccessHash = member.AccessHash,
                                FirstName = member.FirstName,
                                LastName = member.LastName,
                                Username = member.UserName
                            });
                        }
                    });
                }
            }
            finally
            {
                IsExtracting = false;
            }
        }

        private List<GroupMembers> LoadExtractedMembers(long groupId)
        {
            using (var connection = new SQLiteConnection("Data Source=teleblaster.db"))
            {
                connection.Open();
                return GroupMembers.LoadGroupMembers(connection, groupId);
            }
        }

        private void UpdateExtractStatus(string status)
        {
            System.Diagnostics.Debug.WriteLine($"Status update: {status}");

            if (status.StartsWith("Extracting"))
            {
                var parts = status.Split(' ');
                if (int.TryParse(parts[1], out int count))
                {
                    ExtractedMembersCount = count;
                }
            }
            else if (status.Contains("stopped"))
            {
                IsExtracting = false;
            }
        }

        private void StopExtraction()
        {
            using (Py.GIL())
            {
                dynamic py = Py.Import("functions");
                py.stop_extraction_sync();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = GetActiveAccounts().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName;
        }
    }

    public class GroupInfo
    {
        public string GroupId { get; set; }
        public int TotalMembers { get; set; }
        public string GroupName { get; set; }
    }

    public class GroupMember
    {
        public int No { get; set; }
        public long MemberId { get; set; }
        public string AccessHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
    }
}
