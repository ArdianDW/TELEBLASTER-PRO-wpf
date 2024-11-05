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

        public ICommand StartCommand { get; }

        public GroupFinderViewModel()
        {
            StartCommand = new RelayCommand(StartAutomation);
            GroupLinks = ExtractedDataStore.Instance.GroupLinks;
            GroupLinks.CollectionChanged += GroupLinks_CollectionChanged;

            Keyword = ExtractedDataStore.Instance.Keyword;
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
            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("functions");

                    foreach (var link in GroupLinks.Where(gl => gl.Check == 1))
                    {
                        Debug.WriteLine($"Processing link: {link.Link}");
                        string groupType = py.get_group_type("user1.session", link.Link);
                        Debug.WriteLine($"Link: {link.Link}, Group Type: {groupType}");

                        if (groupType == "supergroup" || groupType == "channel" || groupType == "group")
                        {
                            link.Type = groupType;
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
