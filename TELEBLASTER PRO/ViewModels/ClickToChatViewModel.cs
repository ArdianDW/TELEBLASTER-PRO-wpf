using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Python.Runtime;
using TELEBLASTER_PRO.Helpers;
using TELEBLASTER_PRO.Models;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;

namespace TELEBLASTER_PRO.ViewModels
{
    internal class ClickToChatViewModel : INotifyPropertyChanged
    {
        public ICommand SendCommand { get; }
        public ICommand BrowseFileCommand { get; }
        public ICommand EmojiPickerCommand { get; }

        public ObservableCollection<string> ActivePhoneNumbers { get; set; }
        
        public string SelectedPhoneNumber
        {
            get => ExtractedDataStore.Instance.ClickToChatSelectedPhoneNumber;
            set
            {
                ExtractedDataStore.Instance.ClickToChatSelectedPhoneNumber = value;
                OnPropertyChanged(nameof(SelectedPhoneNumber)); 
            }
        }

        public string MessageText
        {
            get => ExtractedDataStore.Instance.ClickToChatMessageText;
            set
            {
                ExtractedDataStore.Instance.ClickToChatMessageText = value;
                OnPropertyChanged(nameof(MessageText));
            }
        }

        public string Target
        {
            get => ExtractedDataStore.Instance.ClickToChatTarget;
            set
            {
                ExtractedDataStore.Instance.ClickToChatTarget = value;
                OnPropertyChanged(nameof(Target));
            }
        }

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

        public event Action RequestFocusOnTextBox;

        public ClickToChatViewModel()
        {
            SendCommand = new RelayCommand(_ => SendMessageToUser());
            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            EmojiPickerCommand = new RelayCommand(_ => OpenEmojiPicker());

            var activeAccounts = Account.GetAccountsFromDatabase().Where(account => account.Status == "Active");
            ActivePhoneNumbers = new ObservableCollection<string>(activeAccounts.Select(account => account.Phone));
        }

        public void SendMessageToUser()
        {
            if (string.IsNullOrEmpty(SelectedPhoneNumber)) 
            {
                MessageBox.Show("Please select a phone number.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(Target))
            {
                MessageBox.Show("Please enter a target phone number or username.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string sessionName = GetSessionNameFromPhoneNumber(SelectedPhoneNumber); 

            Debug.WriteLine($"Sending message with session: {sessionName}");
            Debug.WriteLine($"Target: {Target}");
            Debug.WriteLine($"Message Text: {MessageText}");
            Debug.WriteLine($"Attachment File Path: {AttachmentFilePath}");

            try
            {
                using (Py.GIL())
                {
                    dynamic py = Py.Import("Backend.functions");
                    var result = py.send_to_user(sessionName, Target, MessageText, AttachmentFilePath);
                    bool success = result[0];
                    string message = result[1];

                    Debug.WriteLine($"Send message result: Success = {success}, Message = {message}");

                    if (success)
                    {
                        MessageBox.Show("Message sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to send message: {message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex) 
            { 
                Debug.WriteLine($"Error sending message: {ex.Message}");
                MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSessionNameFromPhoneNumber(string phoneNumber)
        {
            var account = Account.GetAccountsFromDatabase().FirstOrDefault(a => a.Phone == phoneNumber);
            return account?.SessionName;
        }

        private void BrowseFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                AttachmentFilePath = openFileDialog.FileName;
            }
        }

        private void OpenEmojiPicker()
        {
            SimulateKeyPress(Key.LWin, Key.OemPeriod);
            RequestFocusOnTextBox?.Invoke();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const uint KEYEVENTF_KEYUP = 0x0002;

        private void SimulateKeyPress(Key modifierKey, Key key)
        {
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(modifierKey), 0, 0, 0);
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, 0, 0);
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(key), 0, KEYEVENTF_KEYUP, 0);
            keybd_event((byte)KeyInterop.VirtualKeyFromKey(modifierKey), 0, KEYEVENTF_KEYUP, 0);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
