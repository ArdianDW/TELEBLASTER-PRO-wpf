﻿using System;
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

namespace TELEBLASTER_PRO.ViewModels
{
    internal class SendMessageViewModel : INotifyPropertyChanged
    {
        private readonly AccountViewModel _accountViewModel;
        private string _selectedPhoneNumber;
        public ObservableCollection<string> ActivePhoneNumbers { get; }
        public ObservableCollection<Contacts> ContactsList { get; private set; }

        private string _customTextBoxText;
        public string CustomTextBoxText
        {
            get => _customTextBoxText;
            set
            {
                _customTextBoxText = value;
                OnPropertyChanged(nameof(CustomTextBoxText));
            }
        }

        public string SelectedPhoneNumber
        {
            get => _selectedPhoneNumber;
            set
            {
                _selectedPhoneNumber = value;
                OnPropertyChanged(nameof(SelectedPhoneNumber));
                
                Debug.WriteLine($"Selected phone number: {_selectedPhoneNumber}");

                string sessionName = GetSessionNameFromPhoneNumber(_selectedPhoneNumber);
                Debug.WriteLine($"Session name for extraction: {sessionName}");
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

        public SendMessageViewModel(AccountViewModel accountViewModel)
        {
            _accountViewModel = accountViewModel;
            ActivePhoneNumbers = new ObservableCollection<string>(GetActiveAccounts().Select(a => a.Phone));
            ContactsList = new ObservableCollection<Contacts>();
            ExtractContactsCommand = new RelayCommand(_ => ExtractContacts());
            SendMessageCommand = new RelayCommand(_ => SendMessage());
            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            ExportContactsCommand = new RelayCommand(_ => ExportContacts());
            ImportContactsCommand = new RelayCommand(_ => ImportContacts());
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

            // Get session_name from SelectedPhoneNumber
            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);

            // Call the Python function to extract contacts
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

        private async void SendMessage()
        {
            if (string.IsNullOrEmpty(SelectedPhoneNumber))
            {
                MessageBox.Show("Please select a phone number.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedContacts = ContactsList.Where(c => c.IsChecked).ToList();
            if (!selectedContacts.Any())
            {
                MessageBox.Show("Please select at least one contact.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string message = CustomTextBoxText; // Assume you bind the TextBox text to this property

            // Get session_name from SelectedPhoneNumber
            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber);

            foreach (var contact in selectedContacts)
            {
                await SendMessageToContactAsync(sessionName, contact, message);
            }
        }

        private async Task SendMessageToContactAsync(string sessionName, Contacts contact, string message)
        {
            Debug.WriteLine($"Sending message with sessionName: {sessionName}, contact UserId: {contact.ContactId}, message: {message}, attachment: {AttachmentFilePath}");

            using (Py.GIL())
            {
                try
                {
                    dynamic py = Py.Import("functions");
                    Debug.WriteLine("Python module 'functions' imported successfully.");
                    var result = await Task.Run(() => py.send_message(sessionName, contact.ContactId, message, AttachmentFilePath));
                    Debug.WriteLine($"Python function 'send_message' executed successfully with result: {result}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error executing Python function: {ex.Message}");
                }
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

                        ContactsList = new ObservableCollection<Contacts>(contacts);
                    }

                    MessageBox.Show("Contacts imported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing contacts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
