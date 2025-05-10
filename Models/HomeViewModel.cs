using System;
using System.Collections.Generic;

namespace Health_Care_MIS.Models
{
    public class HomeViewModel
    {
        public HomeViewModel()
        {
            UpcomingAppointments = new List<AppointMent>();
            RecentPrescriptions = new List<prescription>();
            RecentLabResults = new List<laboratoryResult>();
            RecentBills = new List<bill>();
            AvailableRooms = new List<Room>();
        }

        public bool HasPersonalInfo { get; set; }
        public string UserEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public IEnumerable<AppointMent> UpcomingAppointments { get; set; }
        public IEnumerable<prescription> RecentPrescriptions { get; set; }
        public IEnumerable<laboratoryResult> RecentLabResults { get; set; }
        public IEnumerable<bill> RecentBills { get; set; }
        public IEnumerable<Room> AvailableRooms { get; set; }
    }
}
