using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
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
        public ICommand ExportContactsCommand { get; }
        public ICommand StartInviteCommand { get; }
        public ICommand StopInviteCommand { get; }

        public ObservableCollection<Contacts> ContactsList => ExtractedDataStore.Instance.ContactsList;
        public ObservableCollection<string> ActivePhoneNumbers { get; set; }
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

        private int _minDelay = 4;
        public int MinDelay
        {
            get => _minDelay;
            set
            {
                _minDelay = value;
                OnPropertyChanged(nameof(MinDelay));
            }
        }

        private int _maxDelay = 6;
        public int MaxDelay
        {
            get => _maxDelay;
            set
            {
                _maxDelay = value;
                OnPropertyChanged(nameof(MaxDelay));
            }
        }

        private int _membersPerNumber = 2;
        public int MembersPerNumber
        {
            get => _membersPerNumber;
            set
            {
                _membersPerNumber = value;
                OnPropertyChanged(nameof(MembersPerNumber));
            }
        }

        private bool _isInviting;
        public bool IsInviting
        {
            get => _isInviting;
            set
            {
                _isInviting = value;
                OnPropertyChanged(nameof(IsInviting));
            }
        }

        public string GroupLink { get; set; }
        public string CurrentInviteStatus { get; set; }

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

        public InviteGroupChannelViewModel()
        {
            ImportContactsCommand = new RelayCommand(_ => ImportContacts());
            ExportContactsCommand = new RelayCommand(_ => ExportContacts());
            StartInviteCommand = new RelayCommand(_ => StartInvite(), _ => !IsInviting);
            StopInviteCommand = new RelayCommand(_ => StopInvite(), _ => IsInviting);

            // Initialize ContactsList if not already done
            if (ExtractedDataStore.Instance.ContactsList == null)
            {
                ExtractedDataStore.Instance.ContactsList = new ObservableCollection<Contacts>();
            }

            // Initialize ActivePhoneNumbers
            var activeAccounts = Account.GetAccountsFromDatabase().Where(account => account.Status == "Active");
            ActivePhoneNumbers = new ObservableCollection<string>(activeAccounts.Select(account => account.Phone));
        }

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

        private async void StartInvite()
        {
            IsInviting = true;
            CurrentInviteStatus = "Starting invite process...";
            OnPropertyChanged(nameof(CurrentInviteStatus));
            Debug.WriteLine("Starting invite process...");

            var selectedContacts = ContactsList.Where(c => c.IsChecked).ToList();
            if (selectedContacts.Count == 0)
            {
                MessageBox.Show("No contacts selected for invitation.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                IsInviting = false;
                Debug.WriteLine("No contacts selected for invitation.");
                return;
            }

            var activeAccounts = Account.GetAccountsFromDatabase().Where(account => account.Status == "Active").ToList();
            if (activeAccounts.Count == 0)
            {
                MessageBox.Show("No active accounts available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsInviting = false;
                Debug.WriteLine("No active accounts available.");
                return;
            }

            int membersPerNumber = MembersPerNumber;
            int totalContacts = selectedContacts.Count;
            int currentIndex = 0;

            if (SwitchNumberAutomatically)
            {
                while (currentIndex < totalContacts)
                {
                    foreach (var account in activeAccounts)
                    {
                        if (currentIndex >= totalContacts)
                            break;

                        string sessionName = account.SessionName;
                        var members = selectedContacts.Skip(currentIndex).Take(membersPerNumber).ToList();

                        var result = await Task.Run(() => InviteMembers(sessionName, GroupLink, members));
                        if (!result)
                        {
                            CurrentInviteStatus = "Failed to invite some members";
                            OnPropertyChanged(nameof(CurrentInviteStatus));
                            Debug.WriteLine(CurrentInviteStatus);
                            IsInviting = false;
                            return;
                        }

                        currentIndex += membersPerNumber;
                    }
                }
            }
            else
            {
                var selectedAccount = activeAccounts.FirstOrDefault(a => a.Phone == SelectedPhoneNumber);
                if (selectedAccount == null)
                {
                    MessageBox.Show("Selected account is not active.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    IsInviting = false;
                    Debug.WriteLine("Selected account is not active.");
                    return;
                }

                string sessionName = selectedAccount.SessionName;
                while (currentIndex < totalContacts)
                {
                    var members = selectedContacts.Skip(currentIndex).Take(membersPerNumber).ToList();

                    var result = await Task.Run(() => InviteMembers(sessionName, GroupLink, members));
                    if (!result)
                    {
                        CurrentInviteStatus = "Failed to invite some members";
                        OnPropertyChanged(nameof(CurrentInviteStatus));
                        Debug.WriteLine(CurrentInviteStatus);
                        IsInviting = false;
                        return;
                    }

                    currentIndex += membersPerNumber;
                }
            }

            CurrentInviteStatus = "All members invited successfully";
            OnPropertyChanged(nameof(CurrentInviteStatus));
            Debug.WriteLine(CurrentInviteStatus);

            IsInviting = false;
            Debug.WriteLine("Invite process completed.");
        }

        private void StopInvite()
        {
            IsInviting = false;
            CurrentInviteStatus = "Invitation process stopped.";
            OnPropertyChanged(nameof(CurrentInviteStatus));
        }

        private bool InviteMembers(string sessionName, string groupLink, List<Contacts> members)
        {
            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("functions");
                    var memberIds = members.Select(m => m.ContactId).ToList();
                    dynamic result = py.invite_members_sync(sessionName, groupLink, memberIds, MinDelay, MaxDelay);
                    
                    bool success = result[0].As<bool>();
                    string message = result[1].As<string>();

                    foreach (var member in members)
                    {
                        member.Status = success ? "Success" : "Failed";
                    }

                    Debug.WriteLine($"Invite result: {success}, Message: {message}");
                    return success;
                }
            }
            catch (Exception ex)
            {
                foreach (var member in members)
                {
                    member.Status = "Failed";
                }
                MessageBox.Show($"Error during invitation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Error during invitation: {ex.Message}");
                return false;
            }
        }

        // Method to get session name from phone number
        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = Account.GetAccountsFromDatabase().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName; // Assuming Account has a SessionName property
        }
    }
}