using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ClosedXML.Excel;
using TELEBLASTER_PRO.Models;
using Python.Runtime;

namespace TELEBLASTER_PRO.ViewModels
{
    class InviteGroupChannelViewModel : INotifyPropertyChanged
    {
        public ICommand ImportContactsCommand { get; }
        public ICommand StartInviteCommand { get; }
        public ICommand StopInviteCommand { get; }
        public ICommand ExportContactsCommand { get; }

        public ObservableCollection<Contacts> ContactsList => ExtractedDataStore.Instance.ContactsList;
        public ObservableCollection<string> ActivePhoneNumbers { get; private set; }

        private Dictionary<string, string> _inviteStatuses = new Dictionary<string, string>();
        public Dictionary<string, string> InviteStatuses
        {
            get => _inviteStatuses;
            set
            {
                _inviteStatuses = value;
                OnPropertyChanged(nameof(InviteStatuses));
            }
        }

        private int _minDelay;
        public int MinDelay
        {
            get => _minDelay;
            set
            {
                _minDelay = value;
                OnPropertyChanged(nameof(MinDelay));
            }
        }

        private int _maxDelay;
        public int MaxDelay
        {
            get => _maxDelay;
            set
            {
                _maxDelay = value;
                OnPropertyChanged(nameof(MaxDelay));
            }
        }

        private string _groupLink;
        public string GroupLink
        {
            get => _groupLink;
            set
            {
                _groupLink = value;
                OnPropertyChanged(nameof(GroupLink));
            }
        }

        private string _selectedPhoneNumber;
        public string SelectedPhoneNumber
        {
            get => _selectedPhoneNumber;
            set
            {
                _selectedPhoneNumber = value;
                OnPropertyChanged(nameof(SelectedPhoneNumber));
            }
        }

        private int _membersPerNumber;
        public int MembersPerNumber
        {
            get => _membersPerNumber;
            set
            {
                _membersPerNumber = value;
                OnPropertyChanged(nameof(MembersPerNumber));
            }
        }

        private bool _switchNumberAutomatically;
        public bool SwitchNumberAutomatically
        {
            get => _switchNumberAutomatically;
            set
            {
                _switchNumberAutomatically = value;
                OnPropertyChanged(nameof(SwitchNumberAutomatically));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isInviting;
        private CancellationTokenSource _cancellationTokenSource;

        public InviteGroupChannelViewModel()
        {
            ImportContactsCommand = new RelayCommand(_ => ImportContacts());
            StartInviteCommand = new RelayCommand(async _ => await StartInviteAsync(), _ => CanStartInvite());
            StopInviteCommand = new RelayCommand(_ => StopInvite(), _ => CanStopInvite());
            ExportContactsCommand = new RelayCommand(_ => ExportContacts());

            // Initialize ContactsList if not already done
            if (ExtractedDataStore.Instance.ContactsList == null)
            {
                ExtractedDataStore.Instance.ContactsList = new ObservableCollection<Contacts>();
            }

            // Initialize ActivePhoneNumbers
            ActivePhoneNumbers = new ObservableCollection<string>(GetActivePhoneNumbers());

            // Set default delay values
            MinDelay = 4;
            MaxDelay = 6;

            // Set default values for new properties
            MembersPerNumber = 2; // Default value, adjust as needed
            SwitchNumberAutomatically = false; // Default value
        }

        private IEnumerable<string> GetActivePhoneNumbers()
        {
            return Account.GetAccountsFromDatabase()
                          .Where(account => account.Status == "Active")
                          .Select(account => account.Phone);
        }

        private async Task StartInviteAsync()
        {
            _isInviting = true;
            _cancellationTokenSource = new CancellationTokenSource();

            var groupLink = GroupLink;
            var membersPerNumber = MembersPerNumber;
            var selectedContacts = ContactsList.Where(c => c.IsChecked).ToList();
            var activePhoneNumbers = ActivePhoneNumbers.ToList(); // Use all active phone numbers

            int phoneNumberIndex = 0; // Start with the first phone number
            int invitesSent = 0; // Track the number of invites sent with the current phone number

            foreach (var contact in selectedContacts)
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }

                var phoneNumber = activePhoneNumbers[phoneNumberIndex];

                try
                {
                    var success = await InviteMembersAsync(phoneNumber, groupLink, new List<string> { contact.ContactId });
                    InviteStatuses[contact.ContactId] = success ? "Success" : "Failed";
                }
                catch (Exception ex)
                {
                    InviteStatuses[contact.ContactId] = "Failed";
                    MessageBox.Show($"Error inviting member {contact.ContactId}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                invitesSent++;

                // Switch to the next phone number after a certain number of invites
                if (invitesSent >= membersPerNumber)
                {
                    phoneNumberIndex = (phoneNumberIndex + 1) % activePhoneNumbers.Count;
                    invitesSent = 0; // Reset the invite count for the new phone number
                }

                int delay = new Random().Next(MinDelay, MaxDelay);
                await Task.Delay(TimeSpan.FromSeconds(delay), _cancellationTokenSource.Token);
            }

            _isInviting = false;
        }

        private async Task<bool> InviteMembersAsync(string phoneNumber, string groupLink, IEnumerable<string> memberIds)
        {
            using (Py.GIL())
            {
                dynamic py = Py.Import("functions");
                var result = py.invite_members_sync(phoneNumber, groupLink, memberIds.ToList(), memberIds.Count());
                return result[0];
            }
        }

        private void StopInvite()
        {
            if (_isInviting)
            {
                _cancellationTokenSource.Cancel();
                _isInviting = false;
            }
        }

        private bool CanStartInvite() => !_isInviting;
        private bool CanStopInvite() => _isInviting;

        private void ImportContacts()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Select an Excel File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        var worksheet = workbook.Worksheet(1); // Assuming the data is in the first worksheet
                        var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                        var contacts = new List<Contacts>();

                        foreach (var row in rows)
                        {
                            var contactId = row.Cell(2).GetValue<string>();
                            if (string.IsNullOrEmpty(contactId))
                            {
                                MessageBox.Show("ContactId is empty or invalid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            var contact = new Contacts
                            {
                                Id = row.Cell(1).GetValue<int>(),
                                ContactId = contactId,
                                AccessHash = row.Cell(3).GetValue<string>(),
                                FirstName = row.Cell(4).GetValue<string>(),
                                LastName = row.Cell(5).GetValue<string>(),
                                UserName = row.Cell(6).GetValue<string>(),
                                IsChecked = false
                            };
                            contacts.Add(contact);
                        }

                        // Update the global ContactsList
                        ExtractedDataStore.Instance.ContactsList = new ObservableCollection<Contacts>(contacts);
                    }

                    MessageBox.Show("Contacts imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing contacts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportContacts()
        {
            if (!ContactsList.Any())
            {
                MessageBox.Show("No contacts available to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files|*.xlsx",
                Title = "Save Contacts as Excel File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Contacts");
                        worksheet.Cell(1, 1).Value = "No";
                        worksheet.Cell(1, 2).Value = "User ID";
                        worksheet.Cell(1, 3).Value = "Access Hash";
                        worksheet.Cell(1, 4).Value = "First Name";
                        worksheet.Cell(1, 5).Value = "Last Name";
                        worksheet.Cell(1, 6).Value = "Username";

                        int row = 2;
                        foreach (var contact in ContactsList)
                        {
                            worksheet.Cell(row, 1).Value = contact.Id;
                            worksheet.Cell(row, 2).Value = contact.ContactId;
                            worksheet.Cell(row, 3).Value = contact.AccessHash;
                            worksheet.Cell(row, 4).Value = contact.FirstName;
                            worksheet.Cell(row, 5).Value = contact.LastName;
                            worksheet.Cell(row, 6).Value = contact.UserName;
                            row++;
                        }

                        workbook.SaveAs(filePath);
                    }

                    MessageBox.Show("Contacts exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting contacts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}