using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingEngine
{
    public class Statistics
    {
        public int RunNumber { get; set; } // הוסף שדה זה
        public int totalAssigned { get; set; } // Regular patients assigned by GA
        public double assignmentPercentage { get; set; } // Percentage of regular patients assigned
        public double AvarageDoctorWorkLoad { get; set; } // Average % workload for doctors based on GA assignments
        public double specializationMatchRate { get; set; } // % match for GA assignments
                                                            // Add surgery stats if needed (e.g., total scheduled, completion rate)
        public int TotalSurgeriesScheduled { get; set; }
        public double SurgeryCompletionRate { get; set; }

        public int TotalRegularPatients { get; set; }
        public int AssignedRegularPatients { get; set; }
        public double RegularPatientAssignmentPercentage { get; set; }
        public double AverageDoctorWorkloadPercent { get; set; }
        public double MinDoctorWorkloadPercent { get; set; }
        public double MaxDoctorWorkloadPercent { get; set; }
        public double StdDevDoctorWorkloadPercent { get; set; }
        public double SpecializationMatchRatePercent { get; set; }
        public double ContinuityOfCareRatePercent { get; set; } // New
        public double ExperienceMatchRatePercent { get; set; } // New
        public double AveragePreferenceScore { get; set; } // New (0.0 to 1.0)

        public double ExperienceComplexityMatchRatePercent { get; set; }
        // Surgery Assignments
        public int TotalSurgeryPatients { get; set; }
        public int ScheduledSurgeriesCount { get; set; }
        public double SurgeryCompletionRatePercent { get; set; }

        // Performance
        public double TotalElapsedTimeSeconds { get; set; }
        public int GaGenerations { get; set; } // Get from orchestrator.currentGeneration
        public double GaFinalFitness { get; set; } //
    }
}
