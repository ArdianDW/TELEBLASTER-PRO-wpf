using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TELEBLASTER_PRO.Models
{
    public class License
    {
        public string Email { get; set; }
        public string LicenseKey { get; set; }
        public string LicenseExpires { get; set; }
        public int Status { get; set; }

        public License(string email, string licenseKey, string licenseExpires, int status)
        {
            Email = email;
            LicenseKey = licenseKey;
            LicenseExpires = licenseExpires;
            Status = status;
        }

        public override string ToString()
        {
            return $"Email: {Email}, LicenseKey: {LicenseKey}, LicenseExpires: {LicenseExpires}, Status: {Status}";
        }
    }
}
