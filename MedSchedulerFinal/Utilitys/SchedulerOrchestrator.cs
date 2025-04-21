//using Models;
//using SchedulingEngine;
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace MedSchedulerFinal.Utilitys
//{
//    public class SchedulerOrchestrator
//    {
//        // Data fetched from source (e.g., DataSingelton, DataManager, Database)
//        private readonly List<Doctor> allDoctors;
//        private readonly List<Patient> allPatients;


//        private SharedState _sharedState;
//        public int GaPopulationSize { get; set; } = 300; // Default value
//        public int GaMaxGenerations { get; set; } = 500; // Default value
//        public double GaCrossoverRate { get; set; } = 0.85; // Default value
//        public double GaMutationRate { get; set; } = 0.1; // Default value
//        public int GaMaxStagnation { get; set; } = 100; // Default value
//        public double GaFitnessThreshold { get; set; } = 100000; // Default value

//        public double GaSpecializationMatchWeight { get; set; } = 6.0;
//        public double GaUrgencyWeight { get; set; } = 4.5;
//        public double GaWorkloadBalanceWeight { get; set; } = 5.5;
//        public double GaPatientAssignmentWeight { get; set; } = 1.5;
//        public double GaContinuityOfCareWeight { get; set; } = 2.0;
//        public double GaHierarchyWeight { get; set; } = 2.5;
//        public double GaExperienceLevelWeight { get; set; } = 3.0;
//        public double GaPreferenceMatchWeight { get; set; } = 4.0;
//        // Parameters for genetic algorithm
//        public readonly int populationSize = 300; // Example value, tune as needed
//        // public readonly int maxGenerations = 500; // Can be set on DoctorScheduler instance

//        // For tracking performance
//        public readonly Stopwatch stopwatch = new Stopwatch();

//        private int gaGenerationsRun = 0; // Track actual generations run by GA
//        private double gaFinalBestFitness = double.MinValue; // Track actual best fitness from GA
//        // Results storage
//        public Schedule finalDoctorSchedule; // Stores the result from DoctorScheduler
//        public List<Patient> unscheduledSurgeryPatients; // Stores patients needing surgery that couldn't be scheduled
//        public Statistics finalStatistics = new Statistics(); // Stores calculated stats
//        private ConcurrentDictionary<int, int> previousAssignments = new ConcurrentDictionary<int, int>(); // Populated in constructor
//        /// <summary>
//        /// Initializes the orchestrator, fetching data from a source (e.g., DataSingelton).
//        /// </summary>
//        /// 

//        public SchedulerOrchestrator()
//        {
//            // --- Fetch data ---
//            try
//            {


//                this.allDoctors = DashBoard._sharedState.Doctors.ToList();
//                this.allPatients = DashBoard._sharedState.Patients.ToList();


//                foreach (var patient in this.allPatients)
//                {
//                    if (patient.PreviousDoctors != null && patient.PreviousDoctors.Any())
//                    {
//                        previousAssignments[patient.Id] = patient.PreviousDoctors.Last();
//                    }
//                }

//                // Basic validation
//                if (!this.allDoctors.Any() || !this.allPatients.Any())
//                {
//                    Console.WriteLine("Warning: Doctor or Patient list is empty. Scheduling might not produce results.");
//                    // Consider throwing an exception if this is critical
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"FATAL: Error initializing data for orchestrator: {ex.Message}");
//                // Handle exception appropriately - maybe rethrow or ensure lists are empty
//                this.allDoctors = new List<Doctor>();
//                this.allPatients = new List<Patient>();
               
                
//            }

//            _sharedState = sharedState;
//        }

//        /// <summary>
//        /// Runs the surgery and doctor assignment scheduling processes.
//        /// </summary>
//        /// <returns>The final schedule object containing doctor assignments.</returns>
//        /// 
//        private async Task UpdateData(Schedule bestSchedule)
//        {

//            foreach (var doctor in DataSingelton.Instance.Doctors)
//            {
//                if (bestSchedule.DoctorToPatients.TryGetValue(doctor.Id, out var assignedPatientIDs))
//                {

//                    doctor.patientsIDS = assignedPatientIDs;
//                }
//                else
//                {

//                    doctor.patientsIDS = new List<int>();
//                }

//                doctor.SetCurrentWorkLoad();
//            }


//            UpdatePatientsWithDoctorAssignments(bestSchedule);

//        }
//        public async Task<Schedule> GenerateOptimalSchedule()
//        {
//            stopwatch.Restart(); // Use Restart instead of Start

//            Console.WriteLine("\n=== Starting MedScheduler Optimization ===");
//            Console.WriteLine($"Total doctors: {allDoctors.Count} ({allDoctors.OfType<Surgeon>().Count()} Surgeons)");
//            Console.WriteLine($"Total patients: {allPatients.Count}");

//            // Step 1: Identify patients needing surgery vs. regular assignment
//            // Ensure patients needing surgery also have a RequiredProcedureId
//            var surgeryPatients = allPatients.Where(p => p.NeedsSurgery && p.RequiredProcedureId.HasValue && !p.ScheduledSurgeryDate.HasValue).ToList();
//            var regularPatients = allPatients.Where(p => !p.NeedsSurgery).ToList(); // Patients for GA



          

//            // --- Step 3: Assign Regular Patients (Genetic Algorithm) ---
//            finalDoctorSchedule = null; // Initialize
//            if (regularPatients.Any() && allDoctors.Any())
//            {
//                Console.WriteLine("\n=== Starting Doctor Assignment (Genetic Algorithm) ===");
//                Console.WriteLine($"GA Params: PopSize={GaPopulationSize}, MaxGen={GaMaxGenerations}, CrossRate={GaCrossoverRate}, MutRate={GaMutationRate}");
//                var geneticScheduler = new GeneticAlgorithm(
//                    populationSize,
//                    allDoctors,
//                    regularPatients // Pass only patients needing doctor assignment
//                );

//                // Set parameters on the GA instance
//                geneticScheduler.maxGenerations = this.GaMaxGenerations;
//                geneticScheduler.maxStagnation = this.GaMaxStagnation;
//                geneticScheduler.fitnessThreshold = this.GaFitnessThreshold;
//                // Set weights (using reflection or direct field access if made internal/public)
//                SetSchedulerParameters(geneticScheduler); // Use helper to set weights
//                // Execute the genetic algorithm
//                // Using Task.Run to avoid blocking UI thread if called from one, though this method is async Task
//                finalDoctorSchedule = await Task.Run(() => geneticScheduler.Solve());
//                this.gaGenerationsRun = geneticScheduler.currentGeneration; // Store actual generations
//                this.gaFinalBestFitness = geneticScheduler.bestFitness; // Store actual fitness
//                // Update the main patient list with doctor assignments from the final schedule
//                await UpdateSingeletonData(finalDoctorSchedule);

//                Console.WriteLine($"Doctor assignment completed in {stopwatch.ElapsedMilliseconds / 1000.0:F1} seconds");
//            }
//            else
//            {
//                Console.WriteLine("\nSkipping doctor assignment (no regular patients or doctors available).");
//                finalDoctorSchedule = new Schedule(); // Assign empty schedule
//            }


//            // --- Step 4: Finalization and Statistics ---
//            stopwatch.Stop();
//            Console.WriteLine($"\nTotal scheduling process completed in {stopwatch.ElapsedMilliseconds / 1000.0:F2} seconds");

//            // Calculate and print final statistics based on the state of 'allPatients' and 'finalDoctorSchedule'
//            finalStatistics = CalculateAndPrintFinalStatistics();
//            PrintStatistics(finalStatistics);
//            // Return the schedule containing doctor assignments
//            // Surgery assignments are reflected in the 'allPatients' list properties
//            return finalDoctorSchedule;
//        }
//        /// <summary>
//        /// Helper to set fitness weights on the DoctorScheduler instance.
//        /// Assumes fields exist in DoctorScheduler. Consider making them internal properties.
//        /// </summary>
//        private void SetSchedulerParameters(GeneticAlgorithm scheduler)
//        {
//            try
//            {
//                // Use reflection to set private readonly fields (adjust if they become properties)
//                var type = typeof(GeneticAlgorithm);
//                SetFieldValue(scheduler, type, "specializationMatchWeight", this.GaSpecializationMatchWeight);
//                SetFieldValue(scheduler, type, "urgencyWeight", this.GaUrgencyWeight);
//                SetFieldValue(scheduler, type, "workloadBalanceWeight", this.GaWorkloadBalanceWeight);
//                SetFieldValue(scheduler, type, "patientAssignmentWeight", this.GaPatientAssignmentWeight);
//                SetFieldValue(scheduler, type, "continuityOfCareWeight", this.GaContinuityOfCareWeight);
//                SetFieldValue(scheduler, type, "hierarchyWeight", this.GaHierarchyWeight);
//                SetFieldValue(scheduler, type, "experienceLevelWeight", this.GaExperienceLevelWeight);
//                SetFieldValue(scheduler, type, "preferenceMatchWeight", this.GaPreferenceMatchWeight);

//                // Also set rates if they are private fields
//                SetFieldValue(scheduler, type, "crossoverRate", this.GaCrossoverRate);
//                SetFieldValue(scheduler, type, "mutationRate", this.GaMutationRate);

//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Warning: Could not set GA parameters via reflection. Using defaults. Error: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Helper to set a field value using reflection.
//        /// </summary>
//        private void SetFieldValue(object target, Type type, string fieldName, object value)
//        {
//            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//            if (field != null)
//            {
//                field.SetValue(target, Convert.ChangeType(value, field.FieldType));
//            }
//            else
//            {
//                Console.WriteLine($"Warning: Field '{fieldName}' not found on {type.Name} for setting parameters.");
//            }
//        }

//        /// <summary>
//        /// Updates the main list of patients with the doctor assignments from the final GA schedule.
//        /// Avoids overwriting surgeon assignments made by the SurgeryScheduler.
//        /// </summary>
//        private void UpdatePatientsWithDoctorAssignments(Schedule schedule)
//        {
//            if (schedule == null || schedule.PatientToDoctor == null || allPatients == null) return;

//            var patientLookup = allPatients.ToDictionary(p => p.Id);

//            // Assign doctors based on the schedule
//            foreach (var kvp in schedule.PatientToDoctor)
//            {
//                if (patientLookup.TryGetValue(kvp.Key, out Patient patient))
//                {
//                    // Assign doctor from GA ONLY IF the patient doesn't need surgery OR
//                    // if they do need surgery but haven't been assigned a surgeon yet by the SurgeryScheduler.
//                    if (!patient.NeedsSurgery || !patient.AssignedSurgeonId.HasValue)
//                    {
//                        patient.AssignedDoctorId = kvp.Value;
//                    }
//                    // If patient.NeedsSurgery AND patient.AssignedSurgeonId.HasValue, we keep the surgeon assignment
//                    // and do NOT assign a general doctor from the GA (unless specific rules dictate otherwise).
//                    // You could potentially assign the surgeon as the main doctor too if needed:
//                    // else if (patient.NeedsSurgery && patient.AssignedSurgeonId.HasValue) { patient.AssignedDoctorId = patient.AssignedSurgeonId; }
//                }
//            }

//            // Clear assignments for regular patients *not* in the final schedule
//            foreach (var patient in allPatients)
//            {
//                // Clear only if it's a regular patient OR a surgery patient without a surgeon assigned
//                if (!patient.NeedsSurgery || !patient.AssignedSurgeonId.HasValue)
//                {
//                    if (!schedule.PatientToDoctor.ContainsKey(patient.Id))
//                    {
//                        patient.AssignedDoctorId = null;
//                    }
//                }
//            }
//        }

//        // Assumes RecalculateWorkloads exists and uses the new Schedule structure
//        private Dictionary<int, int> RecalculateWorkloads(Schedule schedule)
//        {
//            var workloads = allDoctors.ToDictionary(d => d.Id, d => 0); // Use allDoctors for base
//            if (schedule?.DoctorAssignmentLookup != null)
//            {
//                foreach (var kvp in schedule.DoctorAssignmentLookup)
//                {
//                    if (workloads.ContainsKey(kvp.Key)) workloads[kvp.Key] = kvp.Value.Count;
//                }
//            }
//            return workloads;
//        }

//        /// <summary>
//        /// Calculates and prints final statistics based on the generated schedules.
//        /// </summary>
//        private Statistics CalculateAndPrintFinalStatistics()
//        {
//            Console.WriteLine("\n=== Final Schedule Statistics ===");
//            var stats = new Statistics();
//            // Use the final schedule result, or an empty schedule if null
//            var schedule = finalDoctorSchedule ?? new Schedule();

//            // Use the new lookups and appointment list from the Schedule object
//            var patientAssignments = schedule.PatientAssignmentLookup; // Dictionary<int, ScheduledAppointment>
//            var appointmentsList = schedule.Appointments;            // List<ScheduledAppointment>

//            // --- Basic Counts & Performance ---
//            stats.TotalRegularPatients = allPatients.Count(p => !p.NeedsSurgery);
//            stats.TotalSurgeryPatients = allPatients.Count(p => p.NeedsSurgery);
//            // Surgery scheduling stats depend on how that logic updates Patient objects
//            stats.ScheduledSurgeriesCount = allPatients.Count(p => p.NeedsSurgery && p.ScheduledSurgeryDate.HasValue);
//            stats.SurgeryCompletionRatePercent = stats.TotalSurgeryPatients > 0
//                ? (double)stats.ScheduledSurgeriesCount / stats.TotalSurgeryPatients * 100 : 0;

//            stats.TotalElapsedTimeSeconds = stopwatch.ElapsedMilliseconds / 1000.0; // Assumes stopwatch ran
//            stats.GaGenerations = this.gaGenerationsRun;                            // Assumes member var exists
//            stats.GaFinalFitness = schedule.FitnessScore; // Get final score from the schedule object

//            // --- Doctor Assignment Stats (Non-Surgery) ---
//            // Count entries in the lookup where the key corresponds to a non-surgery patient
//            stats.AssignedRegularPatients = patientAssignments
//                // Ensure patient exists in main list before checking NeedsSurgery
//                .Count(kvp => allPatients.FirstOrDefault(p => p.Id == kvp.Key)?.NeedsSurgery == false);

//            stats.RegularPatientAssignmentPercentage = stats.TotalRegularPatients > 0
//                ? (double)stats.AssignedRegularPatients / stats.TotalRegularPatients * 100 : 0;

//            // --- Workload Stats ---
//            // Use the helper method which now calculates based on the schedule's lookups/appointments
//            var finalWorkloads = RecalculateWorkloads(schedule); // Assumes this helper is accessible and updated
//            var doctorUtilization = new List<double>();

//            foreach (var doctor in allDoctors)
//            {
//                int load = finalWorkloads.TryGetValue(doctor.Id, out int count) ? count : 0; // Get count from helper result
//                if (doctor.MaxWorkload > 0)
//                {
//                    doctorUtilization.Add((double)load / doctor.MaxWorkload * 100.0);
//                }
//                else if (load > 0)
//                {
//                    doctorUtilization.Add(100.0); // Or skip/handle as needed
//                }
//            }
//            stats.AverageDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Average() : 0;
//            stats.MinDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Min() : 0;
//            stats.MaxDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Max() : 0;
//            stats.StdDevDoctorWorkloadPercent = 0;
//            if (doctorUtilization.Count > 1)
//            {
//                double avg = stats.AverageDoctorWorkloadPercent / 100.0;
//                double sumOfSquares = doctorUtilization.Sum(u => Math.Pow((u / 100.0) - avg, 2));
//                stats.StdDevDoctorWorkloadPercent = Math.Sqrt(sumOfSquares / doctorUtilization.Count) * 100.0;
//            }

//            // --- Quality Metrics (Iterate through actual appointments) ---
//            int specMatchCount = 0;
//            int continuityMatchCount = 0;
//            // int experienceMatchCount = 0; // Old name - let's be specific
//            int experienceComplexityMatchCount = 0; // Experience vs Complexity
//            double totalPreferenceScore = 0;
//            int assignmentsAnalyzedForQuality = 0;
//            int applicableContinuityPatients = 0; // Count patients *in the schedule* who had a previous doctor

//            // Use dictionaries for faster lookups if allPatients/allDoctors are large lists
//            var patientDict = allPatients.ToDictionary(p => p.Id);
//            var doctorDict = allDoctors.ToDictionary(d => d.Id);

//            foreach (var appt in appointmentsList) // Iterate the list of scheduled appointments
//            {
//                if (patientDict.TryGetValue(appt.PatientId, out Patient patient) &&
//                    doctorDict.TryGetValue(appt.DoctorId, out Doctor doctor))
//                {
//                    // Analyze non-surgery doctor assignments
//                    if (!patient.NeedsSurgery)
//                    {
//                        assignmentsAnalyzedForQuality++; // Count this assignment

//                        // Specialization
//                        if (doctor.Specialization == patient.RequiredSpecialization) specMatchCount++;

//                        // Experience vs Complexity Check (using direct comparison)
//                        if (((int)doctor.ExperienceLevel >= (int)patient.ComplexityLevel)) experienceComplexityMatchCount++;

//                        // Continuity Check
//                        if (previousAssignments.TryGetValue(patient.Id, out int prevDocId))
//                        {
//                            applicableContinuityPatients++; // This assigned patient is eligible for continuity check
//                            if (prevDocId == doctor.Id) continuityMatchCount++;
//                        }

//                        // Preference Score
//                        totalPreferenceScore += CalculatePreferenceScoreForPair(doctor, patient); // Assumes accessible helper
//                    }
//                }
//            }

//            // Calculate final percentages
//            stats.SpecializationMatchRatePercent = assignmentsAnalyzedForQuality > 0
//                ? (double)specMatchCount / assignmentsAnalyzedForQuality * 100 : 0;

//            stats.ExperienceComplexityMatchRatePercent = assignmentsAnalyzedForQuality > 0
//                ? (double)experienceComplexityMatchCount / assignmentsAnalyzedForQuality * 100 : 0;

//            // Base continuity % only on patients who had a previous doctor AND were assigned in this schedule
//            stats.ContinuityOfCareRatePercent = applicableContinuityPatients > 0
//                ? (double)continuityMatchCount / applicableContinuityPatients * 100 : (assignmentsAnalyzedForQuality > 0 ? 100.0 : 0); // If no applicable patients, score is 100%? Or 0? Or N/A? Let's default to 100 if schedule exists but no history applies.

//            stats.AveragePreferenceScore = assignmentsAnalyzedForQuality > 0
//                ? totalPreferenceScore / assignmentsAnalyzedForQuality : 0.5;


//            // --- Print Stats (Example) ---
//            Console.WriteLine($"Performance:");
//            Console.WriteLine($"  Elapsed Time: {stats.TotalElapsedTimeSeconds:F2} s");
//            Console.WriteLine($"  Generations Run: {stats.GaGenerations}");
//            Console.WriteLine($"  Final Fitness: {stats.GaFinalFitness:F2}");
//            Console.WriteLine($"Patient Assignments (Non-Surgery):");
//            Console.WriteLine($"  Total Schedulable: {stats.TotalRegularPatients}");
//            Console.WriteLine($"  Assigned: {stats.AssignedRegularPatients} ({stats.RegularPatientAssignmentPercentage:F1}%)");
//            Console.WriteLine($"Doctor Workload (% of Max):");
//            Console.WriteLine($"  Average: {stats.AverageDoctorWorkloadPercent:F1}%");
//            Console.WriteLine($"  Min: {stats.MinDoctorWorkloadPercent:F1}%");
//            Console.WriteLine($"  Max: {stats.MaxDoctorWorkloadPercent:F1}%");
//            Console.WriteLine($"  StdDev: {stats.StdDevDoctorWorkloadPercent:F1}%");
//            Console.WriteLine($"Quality Metrics (Non-Surgery Assignments):");
//            Console.WriteLine($"  Specialization Match: {stats.SpecializationMatchRatePercent:F1}%");
//            Console.WriteLine($"  Experience/Complexity Match: {stats.ExperienceComplexityMatchRatePercent:F1}%");
//            Console.WriteLine($"  Continuity of Care Match: {stats.ContinuityOfCareRatePercent:F1}% (of {applicableContinuityPatients} applicable)");
//            Console.WriteLine($"  Avg. Preference Score: {stats.AveragePreferenceScore:F2} (0=Avoids..1=Prefers)");
//            // Console.WriteLine($"Surgery Completion: {stats.ScheduledSurgeriesCount}/{stats.TotalSurgeryPatients} ({stats.SurgeryCompletionRatePercent:F1}%)"); // Uncomment if relevant
//            Console.WriteLine("===============================");

//            return stats;
//        }

//        private void PrintStatistics(Statistics stats)
//        {
//            if (stats == null) return;

//            Console.WriteLine("\n--- Final Schedule Statistics ---");
//            Console.WriteLine("[Performance]");
//            Console.WriteLine($"  Total Time: {stats.TotalElapsedTimeSeconds:F2} seconds");
//            Console.WriteLine($"  GA Generations: {stats.GaGenerations}");
//            Console.WriteLine($"  GA Final Fitness: {stats.GaFinalFitness:F2}");

//            Console.WriteLine("\n[Doctor Assignments (Regular Patients)]");
//            Console.WriteLine($"  Total Regular Patients: {stats.TotalRegularPatients}");
//            Console.WriteLine($"  Assigned: {stats.AssignedRegularPatients} ({stats.RegularPatientAssignmentPercentage:F1}%)");
//            Console.WriteLine($"  Workload (% Max): Avg={stats.AverageDoctorWorkloadPercent:F1}%, Min={stats.MinDoctorWorkloadPercent:F1}%, Max={stats.MaxDoctorWorkloadPercent:F1}%, StdDev={stats.StdDevDoctorWorkloadPercent:F1}%");
//            Console.WriteLine($"  Specialization Match: {stats.SpecializationMatchRatePercent:F1}%");
//            Console.WriteLine($"  Experience Level Match: {stats.ExperienceMatchRatePercent:F1}%");
//            Console.WriteLine($"  Continuity of Care Match: {stats.ContinuityOfCareRatePercent:F1}%");
//            Console.WriteLine($"  Average Preference Score: {stats.AveragePreferenceScore:F2} (0.0-1.0)");


//            Console.WriteLine("\n[Surgery Assignments]");
//            Console.WriteLine($"  Total Surgery Patients: {stats.TotalSurgeryPatients}");
//            Console.WriteLine($"  Scheduled: {stats.ScheduledSurgeriesCount} ({stats.SurgeryCompletionRatePercent:F1}%)");
//            Console.WriteLine("------------------------------------");
//        }


//        /// <summary>
//        /// Calculates the preference match score (0 to 1) for a single doctor-patient pair.
//        /// (Copied/adapted from DoctorScheduler - consider placing in a shared utility class)
//        /// </summary>
//        private double CalculatePreferenceScoreForPair(Doctor doctor, Patient patient)
//        {
//            // Return neutral score immediately if doctor has no preferences defined
//            if (doctor.Preferences == null || !doctor.Preferences.Any()) return 0.5;

//            double totalScore = 0;
//            int relevantPreferenceCount = 0; // Only count preferences that actually apply to the comparison

//            foreach (var preference in doctor.Preferences)
//            {
//                // Use -1 as a flag to indicate the score wasn't calculated for this specific preference rule
//                double currentPrefScore = -1.0;
//                bool evaluated = false; // Was this preference type applicable and checked?
//                bool match = false;     // Did the patient match the preference criteria?

//                try
//                {
//                    // Use switch for cleaner handling of different preference types
//                    switch (preference.Type)
//                    {
//                        case PreferenceType.PatientComplexity:
//                            if (preference.LevelValue.HasValue) // Check if LevelValue is set for this type
//                            {
//                                // Safely cast and compare
//                                ComplexityLevel prefLevel = (ComplexityLevel)preference.LevelValue.Value;
//                                match = (patient.ComplexityLevel == prefLevel);
//                                evaluated = true; // This preference rule was applicable
//                            }
//                            break; // Don't forget break in each case

//                        case PreferenceType.PatientUrgency:
//                            if (preference.LevelValue.HasValue)
//                            {
//                                UrgencyLevel prefLevel = (UrgencyLevel)preference.LevelValue.Value;
//                                match = (patient.Urgency == prefLevel);
//                                evaluated = true;
//                            }
//                            break;

//                        case PreferenceType.PatientCondition:
//                            if (!string.IsNullOrEmpty(preference.ConditionValue))
//                            {
//                                // Perform case-insensitive comparison
//                                match = (patient.Condition != null &&
//                                         patient.Condition.Equals(preference.ConditionValue, StringComparison.OrdinalIgnoreCase));
//                                evaluated = true;
//                            }
//                            break;

//                        default:
//                            // Optional: Log unhandled preference types?
//                            // LogDebug($"WARN: Unhandled PreferenceType '{preference.Type}' for Dr {doctor.Id}");
//                            evaluated = false; // Not applicable/handled
//                            break;
//                    }

//                    // If this preference rule was relevant for this patient...
//                    if (evaluated)
//                    {
//                        relevantPreferenceCount++; // Increment count for averaging

//                        // Calculate score based on Direction (Prefers/Avoids) and Match result
//                        if (preference.Direction == PreferenceDirection.Prefers)
//                        {
//                            // Prefers: 1.0 if match, 0.2 if no match
//                            currentPrefScore = match ? 1.0 : 0.2;
//                        }
//                        else // Avoids
//                        {
//                            // Avoids: 0.0 if match (bad), 0.8 if no match (good)
//                            currentPrefScore = match ? 0.0 : 0.8;
//                        }
//                    }
//                    // If !evaluated, currentPrefScore remains -1.0

//                }
//                catch (Exception ex) // Catch errors during comparison/casting
//                {
//                    // Log error (Use your LogDebug or Console)
//                    Console.WriteLine($"ERROR calculating preference score for Dr {doctor.Id}/P{patient.Id}, PrefType {preference.Type}: {ex.Message}");
//                    // Assign a neutral score for this preference on error
//                    currentPrefScore = 0.5;
//                    // Ensure count increments if this was the first relevant pref, to avoid div by zero
//                    if (relevantPreferenceCount == 0) relevantPreferenceCount = 1;
//                    evaluated = true; // Mark as evaluated (with neutral score) for averaging
//                }

//                // Add the calculated score if the preference was applicable/evaluated
//                if (evaluated && currentPrefScore >= 0) // Check >= 0 as valid scores are 0.0 to 1.0
//                {
//                    totalScore += currentPrefScore;
//                }
//                // If preference wasn't applicable (evaluated=false), relevantPreferenceCount isn't incremented,
//                // and no score is added, which correctly excludes it from the average.

//            } // End foreach

//            // Return the average score, defaulting to neutral if no relevant preferences applied
//            return (relevantPreferenceCount == 0) ? 0.5 : totalScore / relevantPreferenceCount;
//        }

//        /// <summary>
//        /// Helper to determine required experience based on patient urgency enum.
//        /// (Copied/adapted from DoctorScheduler - consider placing in a shared utility class)
//        /// </summary>
//        private ExperienceLevel GetRequiredExperienceLevel(UrgencyLevel urgency)
//        {
//            switch (urgency)
//            {
//                case UrgencyLevel.High: return ExperienceLevel.Senior;
//                case UrgencyLevel.Medium: return ExperienceLevel.Regular;
//                default: return ExperienceLevel.Junior;
//            }
//        }


//        /// <summary>
//        /// Analyzes the specialization match rate based on the doctor assignment schedule from GA.
//        /// </summary>
//        private void AnalyzeSpecializationMatch(Schedule schedule)
//        {
//            if (schedule == null || schedule.PatientToDoctor == null || !schedule.PatientToDoctor.Any())
//            {
//                Console.WriteLine("\nSpecialization Match: No GA assignments to analyze.");
//                return;
//            }

//            int correctSpecializationCount = 0;
//            int totalAssignmentsAnalyzed = 0;

//            // Count by urgency level
//            int[] assignedByUrgency = new int[4]; // 0=none, 1=low, 2=medium, 3=high
//            int[] correctByUrgency = new int[4];

//            foreach (var pair in schedule.PatientToDoctor)
//            {
//                int patientId = pair.Key;
//                int doctorId = pair.Value;

//                // Find corresponding objects (handle potential missing data)
//                var patient = allPatients.FirstOrDefault(p => p.Id == patientId);
//                var doctor = allDoctors.FirstOrDefault(d => d.Id == doctorId);

//                // Only analyze if both found and patient didn't need surgery
//                // (as GA primarily handles non-surgery patients)
//                if (patient != null && doctor != null && !patient.NeedsSurgery)
//                {
//                    totalAssignmentsAnalyzed++;
//                    int urgencyLevel = (int)patient.Urgency; // Use Enum value
//                    if (urgencyLevel >= 1 && urgencyLevel <= 3) // Basic bounds check for array index
//                    {
//                        assignedByUrgency[urgencyLevel]++;
//                    }


//                    if (doctor.Specialization == patient.RequiredSpecialization)
//                    {
//                        correctSpecializationCount++;
//                        if (urgencyLevel >= 1 && urgencyLevel <= 3)
//                        {
//                            correctByUrgency[urgencyLevel]++;
//                        }
//                    }
//                }
//            }

//            double specializationMatchRate = totalAssignmentsAnalyzed > 0 ?
//            (double)correctSpecializationCount / totalAssignmentsAnalyzed * 100 : 0;

//            Console.WriteLine($"\nSpecialization Match (GA Assignments for Regular Patients):");
//            Console.WriteLine($"  Overall: {correctSpecializationCount}/{totalAssignmentsAnalyzed} ({specializationMatchRate:F1}%)");
//            finalStatistics.specializationMatchRate = specializationMatchRate; // Store overall rate

//            // Print by urgency
//            for (int i = 1; i <= 3; i++)
//            {
//                string urgencyName = ((UrgencyLevel)i).ToString(); // Get name from Enum
//                double urgencyMatchRate = assignedByUrgency[i] > 0 ?
//                    (double)correctByUrgency[i] / assignedByUrgency[i] * 100 : 0;

//                Console.WriteLine($"  {urgencyName} urgency: {correctByUrgency[i]}/{assignedByUrgency[i]} ({urgencyMatchRate:F1}%)");
//            }
//        }

//        /// <summary>
//        /// Helper method to get the date of the next Monday from the given date.
//        /// If the given date is Monday, it returns the following Monday.
//        /// </summary>
//        private DateTime GetNextMonday(DateTime date)
//        {
//            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
//            // If today is Monday (daysUntilMonday is 0), add 7 days to get next Monday.
//            // Otherwise, add the calculated daysUntilMonday.
//            return date.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
//        }

//    }
//}
