using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class ScheduledAppointment
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        // Store the specific time directly - simpler than linking back to AvailabilitySlot template
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; } // Duration assigned (from Patient)
        public TimeSpan EndTime => StartTime + Duration;
    }
}
