using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{

        // --- Supporting Enums ---

        /// <summary>
        /// Represents the experience level of a doctor.
        /// </summary>
        public enum ExperienceLevel
        {
            Junior = 1,
            Regular = 2,
            Senior = 3
        }

        /// <summary>
        /// Represents the urgency level of a patient's condition.
        /// </summary>
        public enum UrgencyLevel
        {
            Low = 1,
            Medium = 2,
            High = 3
        }

        /// <summary>
        /// Represents the complexity level of a patient's case.
        /// </summary>
        public enum ComplexityLevel
        {
            Simple = 1,
            Moderate = 2,
            Complex = 3
        }

        /// <summary>
        /// Defines the category of a doctor's preference.
        /// </summary>
        public enum PreferenceType
        {
            PatientComplexity,
            PatientUrgency,
            PatientCondition
            // Add other types like specific age groups, etc., if needed
        }

        /// <summary>
        /// Defines the direction of a doctor's preference (prefer or avoid).
        /// </summary>
        public enum PreferenceDirection
        {
            Prefers,
            Avoids
        }
    
}
