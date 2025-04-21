using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class MedicalProcedure
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RequiredSpecialization { get; set; } // Still needed for Surgeon matching
        public double EstimatedDuration { get; set; } // In hours
        public bool IsOperation { get; set; }
        public int ComplexityLevel { get; set; }
        // public List<string> RequiredEquipment { get; set; } = new List<string>(); // REMOVED
        public ExperienceLevel MinimumDoctorExperienceLevel { get; set; }

        // Check if a doctor is qualified (no equipment check needed here)
        public bool IsQualified(Doctor doctor)
        {
            if (doctor == null) return false;
            return doctor.Specialization == RequiredSpecialization &&
                   doctor.ExperienceLevel >= MinimumDoctorExperienceLevel;
        }
    }
}
