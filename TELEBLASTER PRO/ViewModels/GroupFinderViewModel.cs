using System;
using System.ComponentModel;
using System.Windows.Input;
using Python.Runtime;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using TELEBLASTER_PRO.Models;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class GroupFinderViewModel : INotifyPropertyChanged
    {
        private string _keyword;
        private int _pages = 1;
        private bool _isHeadless;
        private ObservableCollection<GroupLinks> _groupLinks;
        private bool _isCheckedAll;
        private ObservableCollection<string> _activePhoneNumbers;
        private int _minDelay = 4;
        private int _maxDelay = 6;
        private string _selectedPhoneNumber;
        private string _groupName;
        private int _totalMember;
        private string _status;
        private bool _isJoinAllNumbers;

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

        public bool IsHeadless
        {
            get => _isHeadless;
            set
            {
                if (_isHeadless != value)
                {
                    _isHeadless = value;
                    Debug.WriteLine($"IsHeadless changed to: {_isHeadless}");
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

        public bool IsJoinAllNumbers
        {
            get => _isJoinAllNumbers;
            set
            {
                if (_isJoinAllNumbers != value)
                {
                    _isJoinAllNumbers = value;
                    Debug.WriteLine($"IsJoinAllNumbers changed to: {_isJoinAllNumbers}");
                    OnPropertyChanged();
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand FilterLinksCommand { get; }
        public ICommand JoinGroupsCommand { get; }

        public GroupFinderViewModel()
        {
            Debug.WriteLine("GroupFinderViewModel initialized.");
            StartCommand = new RelayCommand(StartAutomation);
            FilterLinksCommand = new RelayCommand(async _ => await FilterLinksAsync());
            JoinGroupsCommand = new RelayCommand(async _ => await JoinSelectedGroupsAsync());
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
                Debug.WriteLine($"Starting automation with IsHeadless: {IsHeadless}");
                Debug.WriteLine($"Keyword: {Keyword}, Pages: {Pages}");

                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append("path_to_your_python_script_directory");

                    dynamic groupFinder = Py.Import("groupFinder");
                    Debug.WriteLine("Calling automate_group_finding function...");
                    driver = groupFinder.automate_group_finding(Keyword, Pages, !IsHeadless);
                    Debug.WriteLine("Function call completed.");
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
                        driver.quit();
                        Debug.WriteLine("Browser closed successfully.");
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
            using (var connection = new SQLiteConnection("Data Source=teleblaster.db"))
            {
                connection.Open();
                var links = TELEBLASTER_PRO.Models.GroupLinks.LoadGroupLinks(connection);
                GroupLinks.Clear();
                foreach (var link in links)
                {
                    GroupLinks.Add(link);
                }
            }
        }

        private void UpdateCheckAllItems(bool isChecked)
        {
            foreach (var item in GroupLinks)
            {
                item.Check = isChecked ? 1 : 0;
            }
        }

        private async Task FilterLinksAsync()
        {
            Debug.WriteLine("FilterLinksAsync started.");

            // Ambil sesi aktif dari database
            var activeSessions = Account.GetAccountsFromDatabase()
                                        .Where(account => account.Status == "Active")
                                        .Select(account => account.SessionName)
                                        .ToList();

            if (activeSessions.Count == 0)
            {
                Debug.WriteLine("No active sessions found.");
                return;
            }

            int sessionIndex = 0;
            int linksPerUser = 5;

            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("functions");

                    for (int i = 0; i < GroupLinks.Count; i++)
                    {
                        var link = GroupLinks[i];
                        if (link.Check == 1)
                        {
                            string session = activeSessions[sessionIndex];
                            Debug.WriteLine($"Processing link: {link.Link} with session: {session}");
                            
                            // Panggil fungsi Python untuk mendapatkan tipe grup
                            string groupType = py.get_group_type(session, link.Link);
                            Debug.WriteLine($"Link: {link.Link}, Group Type: {groupType}");

                            // Perbarui tipe grup di model dan database
                            if (groupType == "supergroup" || groupType == "channel" || groupType == "group" || groupType == "invalid link or not a group/channel" || groupType == "invite link expired")
                            {
                                link.Type = groupType == "invalid link or not a group/channel" ? "invalid" : groupType;
                                if (groupType == "invite link expired")
                                {
                                    link.Type = "expired";
                                }

                                // Update database with new type
                                using (var connection = new SQLiteConnection("Data Source=teleblaster.db"))
                                {
                                    connection.Open();
                                    link.UpdateCheckStatusInDatabase(connection);
                                }

                                // Panggil OnPropertyChanged untuk memperbarui UI
                                link.OnPropertyChanged(nameof(link.Type));
                            }

                            // Update UI
                            OnPropertyChanged(nameof(GroupLinks));

                            // Switch session after processing a set number of links
                            if ((i + 1) % linksPerUser == 0)
                            {
                                sessionIndex = (sessionIndex + 1) % activeSessions.Count;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An error occurred during filtering: {ex.Message}");
            }
            Debug.WriteLine("FilterLinksAsync completed.");
        }

        private void SaveCheckedLinksToDatabase()
        {
            using (var connection = new SQLiteConnection("Data Source=teleblaster.db"))
            {
                connection.Open();
                foreach (var link in GroupLinks)
                {
                    if (link.Check == 1) // Hanya simpan yang dicentang
                    {
                        link.UpdateCheckStatusInDatabase(connection);
                    }
                }
            }
        }

        private async Task JoinSelectedGroupsAsync()
        {
            var random = new Random();
            int minDelay = 5; // Default minimum delay
            int maxDelay = 8; // Default maximum delay

            if (MinDelay > 0) minDelay = MinDelay;
            if (MaxDelay > 0) maxDelay = MaxDelay;

            Debug.WriteLine("JoinSelectedGroupsAsync started.");

            // Log status checkbox "Join all numbers"
            Debug.WriteLine($"IsJoinAllNumbers is checked: {IsJoinAllNumbers}");

            // Tentukan nomor yang akan digunakan berdasarkan status checkbox
            var phoneNumbersToUse = IsJoinAllNumbers ? ActivePhoneNumbers : new ObservableCollection<string> { SelectedPhoneNumber };

            // Debugging: Log daftar nomor yang akan digunakan
            Debug.WriteLine("Phone numbers to use:");
            foreach (var phoneNumber in phoneNumbersToUse)
            {
                Debug.WriteLine(phoneNumber);
            }

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

                        if (success)
                        {
                            link.GroupName = groupName;
                            link.TotalMember = totalMember ?? 0;
                            link.Status = "Success";
                            Debug.WriteLine($"Successfully joined group: {groupName} with {totalMember} members.");

                            // Panggil OnPropertyChanged untuk setiap properti yang diubah
                            link.OnPropertyChanged(nameof(link.GroupName));
                            link.OnPropertyChanged(nameof(link.TotalMember));
                            link.OnPropertyChanged(nameof(link.Status));
                        }
                        else
                        {
                            link.Status = "Failed";
                            Debug.WriteLine($"Failed to join group: {groupLink}");
                            link.OnPropertyChanged(nameof(link.Status));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception occurred while joining group: {ex.Message}");
                        link.Status = "Failed";
                        link.OnPropertyChanged(nameof(link.Status));
                    }

                    // Delay sebelum bergabung dengan grup berikutnya
                    int delay = random.Next(minDelay, maxDelay) * 1000; // Convert to milliseconds
                    Debug.WriteLine($"Waiting for {delay / 1000} seconds before next join.");
                    await Task.Delay(delay);
                }
            }

            Debug.WriteLine("JoinSelectedGroupsAsync completed.");

            // Simpan perubahan ke database
            SaveCheckedLinksToDatabase();
        }

        private async Task<(bool, string, int?)> JoinGroupAsync(string sessionName, string groupLink)
        {
            using (Py.GIL())
            {
                dynamic py = Py.Import("functions");
                Debug.WriteLine($"Calling Python function join_group with session: {sessionName} and link: {groupLink}");
                dynamic result = py.join_group(sessionName, groupLink);

                // Ekstrak nilai dari PyObject
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
