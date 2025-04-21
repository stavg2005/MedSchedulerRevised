using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class AppointmentSlot
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; } // e.g., 09:00:00
        public TimeSpan EndTime { get; set; }   // e.g., 09:30:00

        public int? DoctorId { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsBooked { get; set; } = false;
        public int? BookedPatientId { get; set; } = null;
    }
}
