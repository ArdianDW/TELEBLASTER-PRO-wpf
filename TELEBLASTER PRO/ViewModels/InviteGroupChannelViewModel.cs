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
        public ICommand ClearContactsCommand { get; }

        public ObservableCollection<Contacts> ContactsList => ExtractedDataStore.Instance.InviteGroupContactsList;
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

        private int _totalContacts;
        public int TotalContacts
        {
            get => _totalContacts;
            set
            {
                _totalContacts = value;
                OnPropertyChanged(nameof(TotalContacts));
            }
        }

        private int _totalTarget;
        public int TotalTarget
        {
            get => _totalTarget;
            set
            {
                _totalTarget = value;
                OnPropertyChanged(nameof(TotalTarget));
            }
        }

        private int _successCount;
        public int SuccessCount
        {
            get => _successCount;
            set
            {
                _successCount = value;
                OnPropertyChanged(nameof(SuccessCount));
            }
        }

        private int _failCount;
        public int FailCount
        {
            get => _failCount;
            set
            {
                _failCount = value;
                OnPropertyChanged(nameof(FailCount));
            }
        }

        private bool _isStopInviteRequested;
        public bool IsStopInviteRequested
        {
            get => _isStopInviteRequested;
            set
            {
                _isStopInviteRequested = value;
                OnPropertyChanged(nameof(IsStopInviteRequested));
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
            ClearContactsCommand = new RelayCommand(_ => ClearContacts());

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
                        var worksheet = workbook.Worksheet(1); 
                        var rows = worksheet.RowsUsed().Skip(1); 

                        ContactsList.Clear();
                        int index = 1;
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
                                No = index++,
                                ContactId = contactId,
                                AccessHash = row.Cell(3).GetValue<string>(),
                                FirstName = row.Cell(4).GetValue<string>(),
                                LastName = row.Cell(5).GetValue<string>(),
                                UserName = row.Cell(6).GetValue<string>(),
                                IsChecked = false
                            };
                            ContactsList.Add(contact);
                        }
                    }

                    TotalContacts = ContactsList.Count; 
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
            await Task.Run(() =>
            {
                bool allInvitesSent = false;
                TotalTarget = ContactsList.Count(c => c.IsChecked); // Update total target

                while (!allInvitesSent && !IsStopInviteRequested)
                {
                    try
                    {
                        IsInviting = true; // Mulai proses undangan
                        var selectedContacts = ContactsList.Where(c => c.IsChecked).ToList();
                        int minDelay = MinDelay;
                        int maxDelay = MaxDelay;
                        int membersPerNumber = MembersPerNumber;

                        Debug.WriteLine($"Group Link: {GroupLink}");
                        Debug.WriteLine("Selected Contacts: " + string.Join(", ", selectedContacts.Select(c => c.ContactId)));
                        Debug.WriteLine($"Min Delay: {minDelay}, Max Delay: {maxDelay}");
                        Debug.WriteLine($"Members Per Number: {membersPerNumber}");
                        Debug.WriteLine($"Running on thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                        Debug.WriteLine($"SwitchNumberAutomatically: {SwitchNumberAutomatically}");

                        var activeAccounts = Account.GetAccountsFromDatabase().Where(account => account.Status == "Active").ToList();
                        if (SwitchNumberAutomatically && activeAccounts.Any())
                        {
                            Debug.WriteLine("Switch is ON: Automatically changing accounts.");
                            int totalContacts = selectedContacts.Count;
                            int accountIndex = 0;

                            for (int i = 0; i < totalContacts; i += membersPerNumber)
                            {
                                var sessionName = activeAccounts[accountIndex].SessionName;
                                var batchMembers = selectedContacts.Skip(i).Take(membersPerNumber).ToList();

                                foreach (var member in batchMembers)
                                {
                                    if (IsStopInviteRequested) break; // Check if stop is requested

                                    CurrentInviteStatus = $"Inviting {member.FirstName ?? "Unknown"}";
                                    OnPropertyChanged(nameof(CurrentInviteStatus));

                                    Debug.WriteLine($"Using session: {sessionName} for member: {member.FirstName ?? "Unknown"}");

                                    var result = InviteMembers(sessionName, GroupLink, new List<Contacts> { member });
                                    if (result)
                                    {
                                        SuccessCount++;
                                    }
                                    else
                                    {
                                        FailCount++;
                                    }
                                }

                                accountIndex = (accountIndex + 1) % activeAccounts.Count;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Switch is OFF: Using a single account.");
                            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);
                            Debug.WriteLine($"Using session: {sessionName} for all members.");

                            foreach (var member in selectedContacts)
                            {
                                if (IsStopInviteRequested) break; // Check if stop is requested

                                CurrentInviteStatus = $"Inviting {member.FirstName ?? "Unknown"}";
                                OnPropertyChanged(nameof(CurrentInviteStatus));

                                var result = InviteMembers(sessionName, GroupLink, new List<Contacts> { member });
                                if (result)
                                {
                                    SuccessCount++;
                                }
                                else
                                {
                                    FailCount++;
                                }
                            }
                        }

                        allInvitesSent = true; // Semua undangan berhasil dikirim
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            Debug.WriteLine("Retrying due to Python error: " + pe.Message);
                            Task.Delay(1000).Wait(); // Wait before retrying
                            Task.Run(() => StartInvite()); // Start a new task to retry
                            return;
                        }
                        else
                        {
                            Debug.WriteLine($"Python error during invitation: {pe.Message}");
                            break; // Exit loop if it's a different Python error
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during invitation: {ex.Message}");
                        break; // Exit loop on other exceptions
                    }
                    finally
                    {
                        IsInviting = false; // Selesai proses undangan
                        if (IsStopInviteRequested)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Invitation process stopped.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        IsStopInviteRequested = false; // Reset IsStopInviteRequested
                    }
                }
            });
        }

        private void StopInvite()
        {
            IsStopInviteRequested = true;
            CurrentInviteStatus = "Invitation process stopped.";
            OnPropertyChanged(nameof(CurrentInviteStatus));
        }

        private bool InviteMembers(string sessionName, string groupLink, List<Contacts> members)
        {
            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("Backend.functions");
                    var memberIds = members.Select(m => m.ContactId).ToList();
                    var memberUsernames = members.Select(m => m.UserName).ToList();
                    dynamic result = py.invite_members_sync(sessionName, groupLink, memberIds, memberUsernames, MinDelay, MaxDelay);
                    
                    bool success = result[0].As<bool>();
                    string message = result[1].As<string>();

                    foreach (var member in members)
                    {
                        member.Status = success ? "Success" : "Failed";
                        if (success)
                        {
                            SuccessCount++;
                        }
                        else
                        {
                            FailCount++;
                        }
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
                    FailCount++;
                }
                
                if (ex.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                {
                    Debug.WriteLine("Retrying due to error: " + ex.Message);
                    Task.Delay(1000).Wait(); 
                    return false;
                }
                
                CurrentInviteStatus = $"Error during invitation: {ex.Message}";
                OnPropertyChanged(nameof(CurrentInviteStatus));
                Debug.WriteLine(CurrentInviteStatus);
                return false;
            }
        }

        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = Account.GetAccountsFromDatabase().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName;
        }

        private void ClearContacts()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all contacts?",
                "Clear Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ContactsList.Clear();
            }
        }
    }
}