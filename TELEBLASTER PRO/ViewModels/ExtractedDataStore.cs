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

        private ExtractedDataStore()
        {
            GroupLinks = new ObservableCollection<GroupLinks>();
            ExtractedMembers = new ObservableCollection<GroupMember>();
            LoadedGroups = new ObservableCollection<GroupInfo>();
            GeneratedNumbers = new ObservableCollection<NumberGenerated>();
        }
    }
}
