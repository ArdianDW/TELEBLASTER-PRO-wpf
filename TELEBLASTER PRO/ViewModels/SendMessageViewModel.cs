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
            }
        }

        public ICommand ExtractContactsCommand { get; }
        public ICommand SendMessageCommand { get; }

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
                    await Task.Run(() => py.send_message(sessionName, contact.ContactId, message, AttachmentFilePath));
                    Debug.WriteLine("Python function 'send_message' executed successfully.");
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
