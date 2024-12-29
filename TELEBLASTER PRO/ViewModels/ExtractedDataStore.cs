using System.Collections.ObjectModel;
using TELEBLASTER_PRO.Models;

namespace TELEBLASTER_PRO.ViewModels
{
    public class ExtractedDataStore
    {
        private static ExtractedDataStore _instance;
        public static ExtractedDataStore Instance => _instance ??= new ExtractedDataStore();

        public ObservableCollection<GroupLinks> GroupLinks { get; private set; }
        public ObservableCollection<GroupMember> ExtractedMembers { get; private set; }
        public string SelectedPhoneNumber { get; set; }
        public GroupInfo SelectedGroup { get; set; }
        public ObservableCollection<GroupInfo> LoadedGroups { get; private set; }
        public string Keyword { get; set; }

        public string PrefixName { get; set; }
        public string NumberPrefix { get; set; }
        public ObservableCollection<NumberGenerated> GeneratedNumbers { get; private set; }

        public ObservableCollection<Contacts> ContactsList { get; set; }
        public ObservableCollection<Contacts> SendMessageContactsList { get; private set; }
        public ObservableCollection<Contacts> InviteGroupContactsList { get; private set; }

        public string MessageText { get; set; }
        public bool IsSwitchNumberChecked { get; set; }
        public int MessagesPerNumber { get; set; }
        public int MinDelay { get; set; }
        public int MaxDelay { get; set; }

        public string Target { get; set; }

        public string ClickToChatSelectedPhoneNumber { get; set; }
        public string ClickToChatMessageText { get; set; }
        public string ClickToChatTarget { get; set; }

        private ExtractedDataStore()
        {
            GroupLinks = new ObservableCollection<GroupLinks>();
            ExtractedMembers = new ObservableCollection<GroupMember>();
            LoadedGroups = new ObservableCollection<GroupInfo>();
            GeneratedNumbers = new ObservableCollection<NumberGenerated>();
            ContactsList = new ObservableCollection<Contacts>();
            SendMessageContactsList = new ObservableCollection<Contacts>();
            InviteGroupContactsList = new ObservableCollection<Contacts>();
        }
    }
}
