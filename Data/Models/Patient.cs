using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Patient
    {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Condition { get; set; } // Primary condition/diagnosis
            public UrgencyLevel Urgency { get; set; } // Using Enum
            public string RequiredSpecialization { get; set; } // Matches Doctor.Specialization
            public bool NeedsSurgery { get; set; }
            public DateTime AdmissionDate { get; set; }
            public DateTime? ScheduledSurgeryDate { get; set; } // Nullable
            public int? AssignedDoctorId { get; set; } // Nullable FK ID
            public int? AssignedSurgeonId { get; set; } // Nullable FK ID (Should be a Doctor where IsSurgeon=true)
            public ComplexityLevel ComplexityLevel { get; set; } // Using Enum
            public double? EstimatedTreatmentTime { get; set; } // In hours, Nullable

            public TimeSpan EstimatedAppointmentDuration { get; set; }
            public List<int> PreviousDoctors { get; set; } = new List<int>(); // History of doctor IDs

            public int? RequiredProcedureId { get; set; } // FK to the specific procedure needed
            public int? AssignedOperatingRoomId { get; set; } // FK to the OR assigned
                                                              // Method to check continuity of care with a specific doctor ID
            public bool HasContinuityOfCare(int doctorId)
            {
                return PreviousDoctors.Contains(doctorId);
            }
        }
}
