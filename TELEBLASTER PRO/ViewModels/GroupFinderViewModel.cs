using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using Python.Runtime;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using TELEBLASTER_PRO.Models;
using TELEBLASTER_PRO.Helpers;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class GroupFinderViewModel : INotifyPropertyChanged
    {
        private string _keyword;
        private int _pages = 1;
        private ObservableCollection<GroupLinks> _groupLinks;
        private bool _isCheckedAll;
        private ObservableCollection<string> _activePhoneNumbers;
        private int _minDelay = 4;
        private int _maxDelay = 6;
        private string _selectedPhoneNumber;
        private string _groupName;
        private int _totalMember;
        private string _status;
        private bool _isFiltering;
        private bool _isJoining;
        private string _statusText;

        public string Keyword
        {
            get => ExtractedDataStore.Instance.Keyword;
            set
            {
                if (ExtractedDataStore.Instance.Keyword != value)
                {
                    ExtractedDataStore.Instance.Keyword = value;
                    OnPropertyChanged(nameof(Keyword));
                }
            }
        }

        public int Pages
        {
            get => _pages;
            set
            {
                if (_pages != value)
                {
                    _pages = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<GroupLinks> GroupLinks
        {
            get => _groupLinks;
            set
            {
                if (_groupLinks != value)
                {
                    _groupLinks = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCheckedAll
        {
            get => _isCheckedAll;
            set
            {
                if (_isCheckedAll != value)
                {
                    _isCheckedAll = value;
                    OnPropertyChanged();
                    UpdateCheckAllItems(_isCheckedAll);
                }
            }
        }

        public ObservableCollection<string> ActivePhoneNumbers
        {
            get => _activePhoneNumbers;
            private set
            {
                if (_activePhoneNumbers != value)
                {
                    _activePhoneNumbers = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MinDelay
        {
            get => _minDelay;
            set
            {
                if (_minDelay != value)
                {
                    _minDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxDelay
        {
            get => _maxDelay;
            set
            {
                if (_maxDelay != value)
                {
                    _maxDelay = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedPhoneNumber
        {
            get => _selectedPhoneNumber;
            set
            {
                if (_selectedPhoneNumber != value)
                {
                    _selectedPhoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalMember
        {
            get => _totalMember;
            set
            {
                if (_totalMember != value)
                {
                    _totalMember = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalLinks => GroupLinks?.Count ?? 0;
        public int TotalTarget { get; set; } = 0;
        public int Success { get; set; } = 0;
        public int Fail { get; set; } = 0;

        public bool IsFiltering
        {
            get => _isFiltering;
            set
            {
                if (_isFiltering != value)
                {
                    _isFiltering = value;
                    OnPropertyChanged();
                    UpdateStatusText();
                }
            }
        }

        public bool IsJoining
        {
            get => _isJoining;
            set
            {
                if (_isJoining != value)
                {
                    _isJoining = value;
                    OnPropertyChanged();
                    UpdateStatusText();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand JoinGroupsCommand { get; }
        public ICommand DeleteLinksCommand { get; }
        public ICommand FilterLinksCommand { get; }

        public GroupFinderViewModel()
        {
            Debug.WriteLine("GroupFinderViewModel initialized.");
            StartCommand = new RelayCommand(StartAutomation);
            JoinGroupsCommand = new RelayCommand(async _ => await JoinSelectedGroupsAsync());
            DeleteLinksCommand = new RelayCommand(DeleteLinks);
            FilterLinksCommand = new RelayCommand(async _ => await FilterLinksAsync());
            GroupLinks = ExtractedDataStore.Instance.GroupLinks;
            GroupLinks.CollectionChanged += GroupLinks_CollectionChanged;

            Keyword = ExtractedDataStore.Instance.Keyword;

            // Initialize ActivePhoneNumbers
            ActivePhoneNumbers = new ObservableCollection<string>(GetActiveAccounts().Select(a => a.Phone));
        }

        private IEnumerable<Account> GetActiveAccounts()
        {
            return Account.GetAccountsFromDatabase().Where(account => account.Status == "Active");
        }

        private void GroupLinks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (GroupLinks link in GroupLinks)
            {
                link.PropertyChanged += GroupLink_PropertyChanged;
            }
            OnPropertyChanged(nameof(TotalLinks));
            OnPropertyChanged(nameof(TotalTarget));
            OnPropertyChanged(nameof(Success));
            OnPropertyChanged(nameof(Fail));
        }

        private void GroupLink_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TELEBLASTER_PRO.Models.GroupLinks.Check))
            {
                IsCheckedAll = GroupLinks.All(link => link.Check == 1);
            }
        }

        private void StartAutomation(object parameter)
        {
            dynamic driver = null;
            try
            {
                Debug.WriteLine("Starting automation");
                Debug.WriteLine($"Keyword: {Keyword}, Pages: {Pages}");

                using (Py.GIL())
                {
                    dynamic groupFinder = Py.Import("Backend.groupFinder");
                    Debug.WriteLine("Calling automate_group_finding function...");
                    string result = groupFinder.automate_group_finding(Keyword, Pages);
                    Debug.WriteLine($"Function call completed with result: {result}");
                }

                Debug.WriteLine("Automation completed successfully.");

                // Load data from database after automation
                LoadGroupLinksFromDatabase();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during automation: {ex.Message}");
            }
            finally
            {
                // Ensure the browser is closed
                if (driver != null)
                {
                    try
                    {
                        string closeResult = driver.quit();
                        Debug.WriteLine($"Browser close result: {closeResult}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to close browser: {ex.Message}");
                    }
                }
            }
        }

        private void LoadGroupLinksFromDatabase()
        {
            var connection = DatabaseConnection.Instance;
                var links = TELEBLASTER_PRO.Models.GroupLinks.LoadGroupLinks();
                GroupLinks.Clear();
                foreach (var link in links)
                {
                    GroupLinks.Add(link);
                }
        }

        private void UpdateCheckAllItems(bool isChecked)
        {
            foreach (var item in GroupLinks)
            {
                item.IsChecked = isChecked;
            }
        }

        private void SaveCheckedLinksToDatabase()
        {
            var connection = DatabaseConnection.Instance;
                foreach (var link in GroupLinks)
                {
                    if (link.Check == 1) // Hanya simpan yang dicentang
                    {
                        link.UpdateCheckStatusInDatabase();
                    }
                }
        }

        private async Task JoinSelectedGroupsAsync()
        {
            IsJoining = true;
            UpdateStatusText();

            // Set TotalTarget to the number of selected links
            TotalTarget = GroupLinks.Count(l => l.Check == 1);
            OnPropertyChanged(nameof(TotalTarget));

            var random = new Random();
            int minDelay = 5; // Default minimum delay
            int maxDelay = 8; // Default maximum delay

            if (MinDelay > 0) minDelay = MinDelay;
            if (MaxDelay > 0) maxDelay = MaxDelay;

            Debug.WriteLine("JoinSelectedGroupsAsync started.");
            Debug.WriteLine($"Total checked links: {TotalTarget}");

            var phoneNumbersToUse = new ObservableCollection<string> { SelectedPhoneNumber };
            Debug.WriteLine($"Using phone number: {SelectedPhoneNumber}");

            foreach (var link in GroupLinks.Where(l => l.Check == 1))
            {
                foreach (var phoneNumber in phoneNumbersToUse)
                {
                    string sessionName = GetSessionNameFromPhoneNumber(phoneNumber);
                    string groupLink = link.Link;

                    Debug.WriteLine($"Attempting to join group: {groupLink} with session: {sessionName}");

                    try
                    {
                        var (success, groupName, totalMember) = await JoinGroupAsync(sessionName, groupLink);
                        Debug.WriteLine($"Join result: success={success}, groupName={groupName}, totalMember={totalMember}");

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (success)
                            {
                                link.GroupName = groupName;
                                link.TotalMember = totalMember ?? 0;
                                link.Status = "Success";
                                Success++; // Increment success count
                                Debug.WriteLine($"Successfully joined group: {groupName} with {totalMember} members.");
                            }
                            else
                            {
                                link.Status = "Failed";
                                Fail++; // Increment fail count
                                Debug.WriteLine($"Failed to join group: {groupLink}");
                            }

                            link.OnPropertyChanged(nameof(link.GroupName));
                            link.OnPropertyChanged(nameof(link.TotalMember));
                            link.OnPropertyChanged(nameof(link.Status));
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception occurred while joining group: {ex.Message}");
                        link.Status = "Failed";
                        Fail++; // Increment fail count
                        link.OnPropertyChanged(nameof(link.Status));
                    }

                    // Update UI for Success and Fail counts
                    OnPropertyChanged(nameof(Success));
                    OnPropertyChanged(nameof(Fail));

                    int delay = random.Next(minDelay, maxDelay) * 1000; // Convert to milliseconds
                    Debug.WriteLine($"Waiting for {delay / 1000} seconds before next join.");
                    await Task.Delay(delay);
                }
            }

            Debug.WriteLine("JoinSelectedGroupsAsync completed.");
            IsJoining = false;
            UpdateStatusText();

            SaveCheckedLinksToDatabase();
        }

        private async Task<(bool, string, int?)> JoinGroupAsync(string sessionName, string groupLink)
        {
            using (Py.GIL())
            {
                dynamic py = Py.Import("Backend.functions");
                Debug.WriteLine($"Calling Python function join_group with session: {sessionName} and link: {groupLink}");
                dynamic result = py.join_group(sessionName, groupLink);

                bool success = result[0].As<bool>();
                string groupName = result[1].As<string>();
                int? totalMember = result[2].IsNone() ? (int?)null : result[2].As<int>();

                Debug.WriteLine($"Python function returned: success={success}, groupName={groupName}, totalMember={totalMember}");

                return (success, groupName, totalMember);
            }
        }

        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = Account.GetAccountsFromDatabase().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName;
        }

        private void DeleteLinks(object parameter)
        {
            try
            {
                // Show confirmation dialog
                var result = MessageBox.Show("Are you sure you want to delete all links?", "Delete Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                // If the user selects "Yes", proceed with deletion
                if (result == MessageBoxResult.Yes)
                {
                    // Delete all links from the database
                    var connection = DatabaseConnection.Instance;
                    TELEBLASTER_PRO.Models.GroupLinks.DeleteAllLinksFromDatabase();

                    // Clear all links from the collection
                    GroupLinks.Clear();

                    // Reset total data
                    TotalTarget = 0;
                    Success = 0;
                    Fail = 0;

                    // Call OnPropertyChanged to update the UI
                    OnPropertyChanged(nameof(TotalLinks));
                    OnPropertyChanged(nameof(TotalTarget));
                    OnPropertyChanged(nameof(Success));
                    OnPropertyChanged(nameof(Fail));

                    Debug.WriteLine("All links deleted successfully.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred while deleting links: {ex.Message}");
            }
        }

        private async Task FilterLinksAsync()
        {
            IsFiltering = true;
            UpdateStatusText();
            var activeAccounts = GetActiveAccounts().ToList();
            if (!activeAccounts.Any())
            {
                Debug.WriteLine("No active accounts available for filtering.");
                IsFiltering = false;
                UpdateStatusText();
                return;
            }

            int accountIndex = 0;
            int linksPerAccount = 5; // Set jumlah link per akun
            var random = new Random();

            bool allLinksFiltered = false;
            while (!allLinksFiltered)
            {
                try
                {
                    for (int i = 0; i < GroupLinks.Count; i++)
                    {
                        if (i >= GroupLinks.Count)
                        {
                            allLinksFiltered = true;
                            break;
                        }

                        var link = GroupLinks[i];
                        string currentSession = activeAccounts[accountIndex].SessionName;

                        Debug.WriteLine($"Filtering link: {link.Link} using session: {currentSession}");

                        using (Py.GIL())
                        {
                            dynamic functions = Py.Import("Backend.functions");
                            var result = functions.get_group_type(currentSession, link.Link);
                            string groupType = result.As<string>();

                            // Logging hasil dari Python
                            Debug.WriteLine($"Filter result for {link.Link}: groupType={groupType}");

                            link.Type = groupType;
                            link.OnPropertyChanged(nameof(link.Type));

                            // Update type in database
                            link.UpdateTypeInDatabase();
                        }

                        int delay = random.Next(3, 5) * 1000; // Tambahkan jeda
                        await Task.Delay(delay);

                        if ((i + 1) % linksPerAccount == 0 && activeAccounts.Count > 1)
                        {
                            accountIndex = (accountIndex + 1) % activeAccounts.Count;
                            await Task.Delay(5000); // Tambahkan jeda setelah setiap batch
                        }
                    }

                    allLinksFiltered = true; // Semua link berhasil difilter
                }
                catch (PythonException pe)
                {
                    Debug.WriteLine($"Python error during link filtering: {pe.Message}");
                    break; // Exit loop on Python exceptions
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error during link filtering: {ex.Message}");
                    break; // Exit loop on other exceptions
                }
                finally
                {
                    IsFiltering = false;
                    UpdateStatusText();
                }
            }
        }

        private void UpdateStatusText()
        {
            if (IsJoining)
            {
                StatusText = "Joining...";
            }
            else if (IsFiltering)
            {
                StatusText = "Filtering Links...";
            }
            else
            {
                StatusText = string.Empty;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Method to check if all items are checked
        public void UpdateIsCheckedAll()
        {
            IsCheckedAll = GroupLinks.All(link => link.IsChecked);
        }
    }
}
