using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Input;
using Python.Runtime;
using TELEBLASTER_PRO.Models;
using System.Diagnostics;
using Microsoft.Win32;
using System.Windows;
using System.Threading.Tasks;
using TELEBLASTER_PRO.Helpers;
using ClosedXML.Excel;
using TELEBLASTER_PRO.ViewModels;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class SendMessageViewModel : INotifyPropertyChanged
    {
        private readonly AccountViewModel _accountViewModel;

        public ObservableCollection<string> ActivePhoneNumbers { get; }
        public ObservableCollection<Contacts> ContactsList => ExtractedDataStore.Instance.SendMessageContactsList;

        public string CustomTextBoxText
        {
            get => ExtractedDataStore.Instance.MessageText;
            set
            {
                ExtractedDataStore.Instance.MessageText = value;
                OnPropertyChanged(nameof(CustomTextBoxText));
            }
        }

        public string SelectedPhoneNumber
        {
            get => ExtractedDataStore.Instance.SelectedPhoneNumber;
            set
            {
                ExtractedDataStore.Instance.SelectedPhoneNumber = value;
                OnPropertyChanged(nameof(SelectedPhoneNumber));
            }
        }

        public bool IsSwitchNumberChecked
        {
            get => ExtractedDataStore.Instance.IsSwitchNumberChecked;
            set
            {
                ExtractedDataStore.Instance.IsSwitchNumberChecked = value;
                OnPropertyChanged(nameof(IsSwitchNumberChecked));
            }
        }

        public int MessagesPerNumber
        {
            get => ExtractedDataStore.Instance.MessagesPerNumber > 0 ? ExtractedDataStore.Instance.MessagesPerNumber : 2; // Default to 2
            set
            {
                ExtractedDataStore.Instance.MessagesPerNumber = value;
                OnPropertyChanged(nameof(MessagesPerNumber));
            }
        }

        public int MinDelay
        {
            get => ExtractedDataStore.Instance.MinDelay > 0 ? ExtractedDataStore.Instance.MinDelay : 3; // Default to 3
            set
            {
                ExtractedDataStore.Instance.MinDelay = value;
                OnPropertyChanged(nameof(MinDelay));
            }
        }

        public int MaxDelay
        {
            get => ExtractedDataStore.Instance.MaxDelay > 0 ? ExtractedDataStore.Instance.MaxDelay : 5; // Default to 5
            set
            {
                ExtractedDataStore.Instance.MaxDelay = value;
                OnPropertyChanged(nameof(MaxDelay));
            }
        }

        public ICommand ExtractContactsCommand { get; }
        public ICommand SendMessageCommand { get; }
        public ICommand ExportContactsCommand { get; }
        public ICommand ImportContactsCommand { get; }

        private string _attachmentFilePath;
        public string AttachmentFilePath
        {
            get => _attachmentFilePath;
            set
            {
                _attachmentFilePath = value;
                OnPropertyChanged(nameof(AttachmentFilePath));
            }
        }

        public ICommand BrowseFileCommand { get; }

        private bool _isSending;
        public bool IsSending
        {
            get => _isSending;
            set
            {
                _isSending = value;
                OnPropertyChanged(nameof(IsSending));
            }
        }

        private string _currentRecipientName;
        public string CurrentRecipientName
        {
            get => _currentRecipientName;
            set
            {
                _currentRecipientName = value;
                OnPropertyChanged(nameof(CurrentRecipientName));
            }
        }

        public SendMessageViewModel(AccountViewModel accountViewModel)
        {
            _accountViewModel = accountViewModel;
            ActivePhoneNumbers = new ObservableCollection<string>(GetActiveAccounts().Select(a => a.Phone));
            ExtractContactsCommand = new RelayCommand(_ => ExtractContacts());
            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            ExportContactsCommand = new RelayCommand(_ => ExportContacts());
            ImportContactsCommand = new RelayCommand(_ => ImportContacts());
            SendMessageCommand = new RelayCommand(_ => SendMessage());
        }

        public IEnumerable<Account> GetActiveAccounts()
        {
            return _accountViewModel.GetActiveAccounts();
        }

        private void ExtractContacts()
        {
            if (string.IsNullOrEmpty(SelectedPhoneNumber))
            {
                MessageBox.Show("Please select a phone number.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);

            using (Py.GIL())
            {
                dynamic py = Py.Import("functions");
                py.extract_contacts(sessionName);
            }

            LoadContactsFromDatabase();
        }

        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = GetActiveAccounts().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName;
        }

        private void LoadContactsFromDatabase()
        {
            ContactsList.Clear();
            var connection = DatabaseConnection.Instance;
                var contacts = Contacts.LoadContacts();
                foreach (var contact in contacts)
                {
                    ContactsList.Add(contact);
                }
        }

        private void BrowseFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                AttachmentFilePath = openFileDialog.FileName;
                Debug.WriteLine($"Selected file: {AttachmentFilePath}");
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

                        ContactsList.Clear();
                        foreach (var contact in contacts)
                        {
                            ContactsList.Add(contact);
                        }
                    }

                    MessageBox.Show("Contacts imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing contacts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SendMessage()
        {
            Task.Run(() =>
            {
                bool allMessagesSent = false;
                while (!allMessagesSent)
                {
                    try
                    {
                        IsSending = true; // Mulai pengiriman
                        var recipientIds = ContactsList.Where(c => c.IsChecked).Select(c => c.ContactId).ToList();
                        int minDelay = MinDelay;
                        int maxDelay = MaxDelay;
                        int messagesPerNumber = MessagesPerNumber;

                        Debug.WriteLine($"Message Text: {CustomTextBoxText}");
                        Debug.WriteLine("Recipient IDs: " + string.Join(", ", recipientIds));
                        Debug.WriteLine($"Min Delay: {minDelay}, Max Delay: {maxDelay}");
                        Debug.WriteLine($"Messages Per Number: {messagesPerNumber}");
                        Debug.WriteLine($"Attachment File Path: {AttachmentFilePath}");
                        Debug.WriteLine($"Running on thread: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                        var activeAccounts = GetActiveAccounts().ToList();
                        if (IsSwitchNumberChecked && activeAccounts.Any())
                        {
                            int totalRecipients = recipientIds.Count;
                            int accountIndex = 0;

                            for (int i = 0; i < totalRecipients; i += messagesPerNumber)
                            {
                                var sessionName = activeAccounts[accountIndex].SessionName;
                                var batchRecipients = recipientIds.Skip(i).Take(messagesPerNumber).ToList();

                                foreach (var recipientId in batchRecipients)
                                {
                                    var contact = ContactsList.FirstOrDefault(c => c.ContactId == recipientId);
                                    CurrentRecipientName = contact?.FirstName ?? "Unknown";

                                    Debug.WriteLine($"Using session: {sessionName} for recipient: {CurrentRecipientName}");

                                    using (Py.GIL())
                                    {
                                        dynamic py = Py.Import("functions");
                                        var result = py.send_message(sessionName, CustomTextBoxText, new List<string> { recipientId }, minDelay, maxDelay, AttachmentFilePath);
                                        bool success = result[0];
                                        string message = result[1];

                                        contact.Status = success ? "Success" : "Failed";
                                    }
                                }

                                accountIndex = (accountIndex + 1) % activeAccounts.Count;
                            }
                        }
                        else
                        {
                            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);
                            Debug.WriteLine($"Using session: {sessionName} for all recipients.");

                            foreach (var recipientId in recipientIds)
                            {
                                var contact = ContactsList.FirstOrDefault(c => c.ContactId == recipientId);
                                CurrentRecipientName = contact?.FirstName ?? "Unknown";

                                using (Py.GIL())
                                {
                                    dynamic py = Py.Import("functions");
                                    var result = py.send_message(sessionName, CustomTextBoxText, new List<string> { recipientId }, minDelay, maxDelay, AttachmentFilePath);
                                    bool success = result[0];
                                    string message = result[1];

                                    contact.Status = success ? "Success" : "Failed";
                                }
                            }
                        }

                        allMessagesSent = true; // Semua pesan berhasil dikirim
                    }
                    catch (PythonException pe)
                    {
                        if (pe.Message.Contains("set_wakeup_fd only works in main thread of the main interpreter"))
                        {
                            Debug.WriteLine("Retrying due to Python error: " + pe.Message);
                            Task.Delay(1000).Wait(); // Wait before retrying
                            Task.Run(() => SendMessage()); // Start a new task to retry
                            return; // Exit current task
                        }
                        else
                        {
                            Debug.WriteLine($"Python error during message sending: {pe.Message}");
                            break; // Exit loop if it's a different Python error
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error during message sending: {ex.Message}");
                        break; // Exit loop on other exceptions
                    }
                    finally
                    {
                        IsSending = false; // Selesai pengiriman
                    }
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
