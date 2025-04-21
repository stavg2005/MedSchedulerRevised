using Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Threading;
using System.Data;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Runtime.Remoting.Lifetime;
using System.Security.Policy;
namespace SchedulingEngine
{
    public class GeneticAlgorithm
    {
        #region Data Members
        // Core collections
        private List<Schedule> Population { get; set; }
        private readonly int populationSize;
        private readonly Random rnd = new Random();

        
        public Statistics finalStatistics = new Statistics();
        public Schedule finalDoctorSchedule;
        public readonly Stopwatch stopwatch = new Stopwatch();
        // Resource collections
        private Dictionary<int, Doctor> DoctorsById { get; set; }
        private Dictionary<int, Patient> PatientsById { get; set; }


        public static StreamWriter logFileWriter;
        // Sorted collections
        private Dictionary<string, List<Doctor>> DoctorsBySpecialization { get; set; }
        private SortedDictionary<UrgencyLevel, List<Patient>> PatientsByUrgency { get; set; }

        private ThreadLocal<Random> _localRandom = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode())); 

        #region GaParameters


        // Termination condition variables
        public int maxGenerations = 500;
        public int currentGeneration = 0; // Made public for logging context
        public double fitnessThreshold = 100000;
        public int stagnationCount = 0;
        public int maxStagnation = 300;
        public double bestFitness = double.MinValue;
        public double previousBestFitness = double.MinValue;

        // Genetic algorithm parameters
        public  double crossoverRate = 0.8;
        public double mutationRate = 0.1;
        public int tournamentSize = 7;
        public  int elitismCount;
        public  double baseMutationRate = 0.1;
        public double maxMutationRate = 0.5;
        public double mutationIncreaseFactor = 1.5; 
                                                             
        private readonly int stagnationThresholdForIncrease; 

        // Weights for fitness function
        private  double specializationMatchWeight = 6.0;
        private  double urgencyWeight = 4.5;
        private  double workloadBalanceWeight = 5.5;
        private double patientAssignmentWeight = 1.5;
        private double continuityOfCareWeight = 2.0;
        private double hierarchyWeight = 2.5;
        private double experienceLevelWeight = 3.0;
        private double preferenceMatchWeight = 4.0;
        internal double unassignedPatientPenaltyMultiplier = 5.0;
        #endregion
        // For tracking assignments for continuity of care
        private Dictionary<int, int> previousAssignments = new Dictionary<int, int>();

        public void SetFitnessWeights(double pecWeight, double urgencyWeight, double workloadWeight, double cocWeight, double expWeight, double prefWeight)
        {
            specializationMatchWeight = pecWeight;
            this.urgencyWeight = urgencyWeight;
            workloadBalanceWeight = workloadWeight;
            continuityOfCareWeight = cocWeight;
            this.experienceLevelWeight = expWeight;
            preferenceMatchWeight = prefWeight;

        }

        /// <summary>
        /// Stores diagnostic log messages generated during the Solve run.
        /// Use ConcurrentBag for thread safety as some loops are parallel.
        /// </summary>
        private ConcurrentBag<string> runLogs = new ConcurrentBag<string>();
        private readonly object logFileLock = new object();
        #endregion

        /// <summary>
        /// Initializes a new instance of the GeneticAlgorithm class with specified parameters and initial data.
        /// Validates input lists, sets GA parameters like population size and elitism count,
        /// and populates internal data structures for efficient access to doctors and patients.
        /// </summary>
        public GeneticAlgorithm(int populationSize, List<Doctor> doctors, List<Patient> patients)
        {
            if (doctors == null || !doctors.Any() || patients == null)
            {
                throw new ArgumentException("Doctors and patients lists cannot be null or empty.");
            }
            this.populationSize = populationSize;
            this.elitismCount = Math.Max(1, (int)(populationSize * 0.1));
            this.mutationRate = this.baseMutationRate;
            this.stagnationThresholdForIncrease = Math.Max(20, this.maxStagnation / 3);

            DoctorsById = new Dictionary<int, Doctor>();
            PatientsById = new Dictionary<int, Patient>();
            DoctorsBySpecialization = new Dictionary<string, List<Doctor>>();
            PatientsByUrgency = new SortedDictionary<UrgencyLevel, List<Patient>>(
                Comparer<UrgencyLevel>.Create((a, b) => ((int)b).CompareTo((int)a))
            );
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information() // קבע רמה מינימלית כללית
            .WriteTo.Console() // כתוב גם לקונסול                               אפשר להתחיל עם Debug ואז לסנן ב-UI.
            .CreateLogger();
            PopulateDataStructures(doctors, patients);
        }
        /// <summary>
        /// Populates the internal lookup data structures (DoctorsById, PatientsById, DoctorsBySpecialization, PatientsByUrgency, previousAssignments)
        /// based on the provided lists of doctors and patients. Also performs initial setup like resetting doctor workloads and initializing doctor preference lookups.
        /// </summary>
        private void PopulateDataStructures(List<Doctor> doctors, List<Patient> patients)
        {
            // Populate DoctorsById and DoctorsBySpecialization
            foreach (var doctor in doctors)
            {
                if (doctor == null) continue;
                DoctorsById[doctor.Id] = doctor;
                doctor.Workload = 0; // Reset workload
                if (!string.IsNullOrEmpty(doctor.Specialization))
                {
                    if (!DoctorsBySpecialization.ContainsKey(doctor.Specialization))
                    { DoctorsBySpecialization[doctor.Specialization] = new List<Doctor>(); }
                    DoctorsBySpecialization[doctor.Specialization].Add(doctor);
                }
            }
            Console.WriteLine("Initializing doctor preference lookups..."); // Log start
            foreach (var doctor in DoctorsById.Values)
            {
                doctor.InitializeAvoidanceLookups();
            }
            Console.WriteLine("Doctor preference lookups initialized."); // Log end
            // Populate PatientsById and PatientsByUrgency
            foreach (var patient in patients)
            {
                if (patient != null)
                {
                    PatientsById[patient.Id] = patient;

                    UrgencyLevel urgency = patient.Urgency;

                    if (!PatientsByUrgency.ContainsKey(urgency))
                    {
                        PatientsByUrgency[urgency] = new List<Patient>();
                    }

                    PatientsByUrgency[urgency].Add(patient);

                    if (patient.PreviousDoctors != null && patient.PreviousDoctors.Any())
                    {
                        previousAssignments[patient.Id] = patient.PreviousDoctors.Last();
                    }
                }

            }
        }

        #region Main Algorithm
        /// <summary>
        /// Executes the main genetic algorithm loop. Initializes the population, then iteratively applies selection,
        /// crossover, mutation, and elitism to evolve the population over generations until a termination condition is met.
        /// Calculates fitness, tracks progress and stagnation, and returns the best schedule solution found.
        /// Also updates the workload of the original Doctor objects based on the final schedule.
        /// </summary>
       
        public Schedule Solve()
        {
            stopwatch.Restart(); // Use Restart instead of Start
            double currentBestFitness;
            currentGeneration = 0; // Reset generation counter
            stagnationCount = 0;
            bestFitness = double.MinValue;
            previousBestFitness = double.MinValue;

            Log.Information("--- Genetic Algorithm Solve Started ---");
            Population = GeneratePopulation(populationSize);
            if (Population == null || !Population.Any())
            {
                // רמה Error לשגיאות שמונעות המשך ריצה
                Log.Error("Initial population generation failed or resulted in an empty population.");
                return null;
            }
            CalculateFitnessForAll(Population);

            Log.Debug("Initial population generated with {PopulationCount} individuals.", Population.Count);

            bestFitness = Population.OrderByDescending(s => s.FitnessScore).FirstOrDefault()?.FitnessScore ?? double.MinValue;
            currentBestFitness = bestFitness;
            previousBestFitness = bestFitness;
            Log.Information("Best initial fitness: {InitialBestFitness:F2}", bestFitness);

            while (!TerminationConditionMet())
            {
                Log.Debug("Current Gen :" + currentGeneration + " best fitness score: " + currentBestFitness);
                currentGeneration++; // Increment generation counter *before* using it in LogDebug




                var newPopulation = new List<Schedule>(populationSize);

                Log.Verbose("Applying elitism. Count: {ElitismCount}", elitismCount); // רמה Verbose לפרטים עדינים יותר
                var elite = Population.OrderByDescending(s => s.FitnessScore).Take(elitismCount);
                newPopulation.AddRange(elite.Select(CloneSchedule));

                while (newPopulation.Count < populationSize)
                {
                    Schedule parent1 = TournamentSelection();
                    Schedule parent2 = TournamentSelection();
                    if (parent1 == null || parent2 == null)
                    {
                        Log.Warning($"Null parent detected during selection. Pop Count: {Population?.Count ?? 0}. Skipping iteration."); // *** LOGGING ADDED/MODIFIED ***
                        continue;
                    }

                    List<Schedule> offspring;
                    if (rnd.NextDouble() < crossoverRate)
                    {
                        offspring = CrossoverListSinglePoint(parent1, parent2);
                    }
                    else
                    {
                        offspring = new List<Schedule> { CloneSchedule(parent1), CloneSchedule(parent2) };
                    }

                    foreach (var child in offspring)
                    {
                        if (rnd.NextDouble() < mutationRate)
                        {
                            Mutate(child);
                        }
                        if (newPopulation.Count < populationSize)
                        {
                            newPopulation.Add(child);
                        }
                    }
                }

                Population = newPopulation;
                CalculateFitnessForAll(Population);

                currentBestFitness = Population.Any() ? Population.Max(s => s.FitnessScore) : double.MinValue;
                UpdateStagnation(currentBestFitness); // UpdateStagnation now includes logging
                stopwatch.Stop();
            }

            Log.Information($"Genetic algorithm terminated after {currentGeneration} generations"); // *** LOGGING ADDED/MODIFIED ***
            var finalSchedule = GetLeadingSchedule();
            Log.Information($"Final best fitness: {finalSchedule?.FitnessScore ?? double.MinValue:F2}"); // *** LOGGING ADDED/MODIFIED ***

            Log.Debug("Updating final doctor workloads...");
            var finalWorkloads = RecalculateWorkloads(finalSchedule);
            foreach (var kvp in finalWorkloads)
            {
                if (DoctorsById.TryGetValue(kvp.Key, out Doctor doc))
                {
                    doc.Workload = kvp.Value; // Update the original doctor objects
                }
            }
            Log.Debug("Calculating final statistics...");
            // קוראים לפונקציה ומעבירים לה את הפתרון הסופי.
            // התוצאה נשמרת במשתנה החבר של הקלאס כדי שה-UI יוכל לגשת אליו.
            this.finalStatistics = CalculateAndPrintFinalStatistics(finalSchedule);

            return finalSchedule;
        }

        private Statistics CalculateAndPrintFinalStatistics(Schedule scheduleToAnalyze)
        {
            Console.WriteLine("\n=== Final Schedule Statistics ===");
            var stats = new Statistics(); // ניצור אובייקט סטטיסטיקות חדש

            // נשתמש בלוח הזמנים שהתקבל כפרמטר, או בלוח זמנים ריק אם הוא null
            var schedule = scheduleToAnalyze ?? new Schedule();

            // ודא שהמאגרים הראשיים של האלגוריתם (משתני חבר) קיימים לפני השימוש בהם
            if (this.PatientsById == null || this.DoctorsById == null)
            {
                Console.WriteLine("ERROR: PatientsById or DoctorsById is null. Cannot calculate statistics.");
                return stats; // החזר אובייקט סטטיסטיקות ריק
            }

            // --- חישובים בסיסיים וביצועים ---
            stats.TotalRegularPatients = this.PatientsById.Count(p => !p.Value.NeedsSurgery);
            stats.TotalSurgeryPatients = this.PatientsById.Count(p => p.Value.NeedsSurgery);
            // חישוב ניתוחים - תלוי איך ומתי השדה ScheduledSurgeryDate מתעדכן במערכת שלך
            stats.ScheduledSurgeriesCount = this.PatientsById.Count(p => p.Value.NeedsSurgery && p.Value.ScheduledSurgeryDate.HasValue);
            stats.SurgeryCompletionRatePercent = stats.TotalSurgeryPatients > 0
                ? (double)stats.ScheduledSurgeriesCount / stats.TotalSurgeryPatients * 100.0 : 0; // חישוב כאחוז

            // השתמש במשתני החבר של האלגוריתם
            stats.TotalElapsedTimeSeconds = this.stopwatch.ElapsedMilliseconds / 1000.0;
            stats.GaGenerations = this.currentGeneration;
            stats.GaFinalFitness = schedule.FitnessScore; // ציון הכשירות של לוח הזמנים שהתקבל

            // --- סטטיסטיקות שיבוץ (לא ניתוחים) ---
            var patientAssignments = schedule.PatientAssignmentLookup; // מתוך לוח הזמנים שהתקבל
                                                                       // ספירת המטופלים המשובצים שאינם צריכים ניתוח
            stats.AssignedRegularPatients = patientAssignments
                .Count(kvp => this.PatientsById.TryGetValue(kvp.Key, out var p) && !p.NeedsSurgery); // ודא שהמטופל קיים במאגר הראשי

            stats.RegularPatientAssignmentPercentage = stats.TotalRegularPatients > 0
                ? (double)stats.AssignedRegularPatients / stats.TotalRegularPatients * 100.0 : 0;

            // --- סטטיסטיקות עומס עבודה ---
            // השתמש בפונקציית העזר שכבר קיימת שמחשבת עומסים לפי לוח זמנים ספציפי
            var finalWorkloads = RecalculateWorkloads(schedule); // מנתח את ה-schedule שהתקבל
            var doctorUtilization = new List<double>();

            foreach (var doctor in this.DoctorsById.Values) // השתמש במאגר הרופאים הראשי
            {
                int load = finalWorkloads.TryGetValue(doctor.Id, out int count) ? count : 0;
                if (doctor.MaxWorkload > 0)
                {
                    doctorUtilization.Add((double)load / doctor.MaxWorkload * 100.0); // חישוב אחוז ניצול
                }
                else if (load > 0) // אם אין מקסימום אבל יש עומס
                {
                    doctorUtilization.Add(100.0); // נחשב כ-100% ניצול (או ערך אחר לפי ההיגיון שלך)
                }
                // אפשר להוסיף else { doctorUtilization.Add(0.0); } אם רוצים לכלול רופאים ללא עומס כלל
            }
            // חישוב ממוצע, מינימום, מקסימום וסטיית תקן של אחוזי הניצול (כמו קודם)
            stats.AverageDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Average() : 0;
            stats.MinDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Min() : 0;
            stats.MaxDoctorWorkloadPercent = doctorUtilization.Any() ? doctorUtilization.Max() : 0;
            stats.StdDevDoctorWorkloadPercent = 0;
            if (doctorUtilization.Count > 1)
            {
                double avg = stats.AverageDoctorWorkloadPercent / 100.0; // ממוצע כערך בין 0 ל-1
                double sumOfSquares = doctorUtilization.Sum(u => Math.Pow((u / 100.0) - avg, 2));
                stats.StdDevDoctorWorkloadPercent = Math.Sqrt(sumOfSquares / doctorUtilization.Count) * 100.0; // המר חזרה לאחוזים
            }

            // --- מדדי איכות (מעבר על התורים שנקבעו) ---
            int specMatchCount = 0;
            int continuityMatchCount = 0;
            int experienceComplexityMatchCount = 0; // התאמת ניסיון למורכבות
            double totalPreferenceScore = 0;
            int assignmentsAnalyzedForQuality = 0; // סופר רק תורים שאינם ניתוח לצורך מדדי האיכות
            int applicableContinuityPatients = 0; // סופר מטופלים משובצים שהיה להם רופא קודם

            var appointmentsList = schedule.Appointments; // מתוך לוח הזמנים שהתקבל

            foreach (var appt in appointmentsList)
            {
                // מצא את אובייקט המטופל והרופא המתאימים מהמאגרים הראשיים
                if (this.PatientsById.TryGetValue(appt.PatientId, out Patient patient) &&
                    this.DoctorsById.TryGetValue(appt.DoctorId, out Doctor doctor))
                {
                    // נתח רק שיבוצים שאינם ניתוח
                    if (!patient.NeedsSurgery)
                    {
                        assignmentsAnalyzedForQuality++; // ספור את השיבוץ הזה

                        // התאמת התמחות (תוך התעלמות מאותיות גדולות/קטנות)
                        if (string.Equals(doctor.Specialization, patient.RequiredSpecialization, StringComparison.OrdinalIgnoreCase))
                        {
                            specMatchCount++;
                        }

                        // התאמת ניסיון למורכבות
                        if (((int)doctor.ExperienceLevel >= (int)patient.ComplexityLevel))
                        {
                            experienceComplexityMatchCount++;
                        }

                        // בדיקת רצף טיפולי (השתמש במאגר previousAssignments של האלגוריתם)
                        if (this.previousAssignments != null && this.previousAssignments.TryGetValue(patient.Id, out int prevDocId))
                        {
                            applicableContinuityPatients++; // מטופל זה רלוונטי לבדיקת רצף
                            if (prevDocId == doctor.Id)
                            {
                                continuityMatchCount++; // נמצאה התאמה לרצף
                            }
                        }

                        // חישוב ציון העדפות (השתמש בפונקציית העזר שכבר קיימת)
                        totalPreferenceScore += CalculatePreferenceScoreForPair(doctor, patient);
                    }
                }
            }

            // --- חישוב אחוזים סופיים למדדי האיכות ---
            stats.SpecializationMatchRatePercent = assignmentsAnalyzedForQuality > 0
                ? (double)specMatchCount / assignmentsAnalyzedForQuality * 100.0 : (assignmentsAnalyzedForQuality == 0 && !appointmentsList.Any() ? 100.0 : 0); // 100% אם אין תורים רלוונטיים?

            stats.ExperienceComplexityMatchRatePercent = assignmentsAnalyzedForQuality > 0
                ? (double)experienceComplexityMatchCount / assignmentsAnalyzedForQuality * 100.0 : (assignmentsAnalyzedForQuality == 0 && !appointmentsList.Any() ? 100.0 : 0);

            // חשב אחוז רצף טיפולי רק מתוך המטופלים שהיה להם רופא קודם *ושובצו*
            stats.ContinuityOfCareRatePercent = applicableContinuityPatients > 0
                ? (double)continuityMatchCount / applicableContinuityPatients * 100.0
                : (assignmentsAnalyzedForQuality > 0 ? 100.0 : 0); // אם אין מטופלים עם היסטוריה, נחשיב 100%? או 0? ברירת המחדל הייתה 100.

            // ממוצע ציון העדפות (בין 0 ל-1)
            stats.AveragePreferenceScore = assignmentsAnalyzedForQuality > 0
                ? totalPreferenceScore / assignmentsAnalyzedForQuality : 0.5; // 0.5 כברירת מחדל אם אין שיבוצים לניתוח

            // --- הדפסת הסטטיסטיקות לקונסול (כמו קודם) ---
            Console.WriteLine($"Performance:");
            Console.WriteLine($"  Elapsed Time: {stats.TotalElapsedTimeSeconds:F2} s");
            Console.WriteLine($"  Generations Run: {stats.GaGenerations}");
            Console.WriteLine($"  Final Fitness: {stats.GaFinalFitness:F2}");
            Console.WriteLine($"Patient Assignments (Non-Surgery):");
            Console.WriteLine($"  Total Schedulable: {stats.TotalRegularPatients}");
            Console.WriteLine($"  Assigned: {stats.AssignedRegularPatients} ({stats.RegularPatientAssignmentPercentage:F1}%)");
            Console.WriteLine($"Doctor Workload (% of Max):");
            Console.WriteLine($"  Average: {stats.AverageDoctorWorkloadPercent:F1}%");
            Console.WriteLine($"  Min: {stats.MinDoctorWorkloadPercent:F1}%");
            Console.WriteLine($"  Max: {stats.MaxDoctorWorkloadPercent:F1}%");
            Console.WriteLine($"  StdDev: {stats.StdDevDoctorWorkloadPercent:F1}%");
            Console.WriteLine($"Quality Metrics (Non-Surgery Assignments):");
            Console.WriteLine($"  Specialization Match: {stats.SpecializationMatchRatePercent:F1}%");
            Console.WriteLine($"  Experience/Complexity Match: {stats.ExperienceComplexityMatchRatePercent:F1}%"); // שם מעודכן
            Console.WriteLine($"  Continuity of Care Match: {stats.ContinuityOfCareRatePercent:F1}% (of {applicableContinuityPatients} applicable)");
            Console.WriteLine($"  Avg. Preference Score: {stats.AveragePreferenceScore:F2} (0=Avoids..1=Prefers)");
            Console.WriteLine($"Surgery Completion: {stats.ScheduledSurgeriesCount}/{stats.TotalSurgeryPatients} ({stats.SurgeryCompletionRatePercent:F1}%)"); // שימוש באחוזים
            Console.WriteLine("===============================");

            return stats; // החזר את אובייקט הסטטיסטיקות המחושב

            return stats;
        }

        /// <summary>
        /// Recalculates the workload (number of assigned appointments) for each doctor based on the specific appointments
        /// present in the provided schedule object. It efficiently utilizes the schedule's internal DoctorAssignmentLookup.
        /// Initializes workload to zero for all known doctors and returns a dictionary mapping Doctor ID to their calculated appointment count for this schedule.
        /// Handles null input schedule gracefully by returning zero workloads for all doctors.
        /// </summary>
        private Dictionary<int, int> RecalculateWorkloads(Schedule schedule)
        {

            // Start with all doctors having 0 workload
            var workloads = DoctorsById.Keys.ToDictionary(id => id, id => 0);
            if (schedule == null) return workloads;

            // Count appointments using the schedule's lookup for efficiency
            foreach (var kvp in schedule.DoctorAssignmentLookup)
            {
                int doctorId = kvp.Key;
                int count = kvp.Value.Count; // Number of appointments for this doctor
                if (workloads.ContainsKey(doctorId)) // Should always be true if lookups are correct
                {
                    workloads[doctorId] = count;
                }
            }
            return workloads;
        }

        private Schedule GetLeadingSchedule()
        {
            if (!Population.Any()) return null;
            return Population.OrderByDescending(s => s.FitnessScore).First();
        }

        private void UpdateStagnation(double currentBestFitness)
        {
            const double tolerance = 1e-6; // סובלנות להשוואת נקודה צפה

            // בדיקה אם היה שיפור משמעותי
            if (currentBestFitness > bestFitness + tolerance)
            {
                // נמצא שיפור!
                Log.Debug("UpdateStagnation: Improvement detected. Current Best: {CurrentBest:F2}, Previous Best: {PreviousBest:F2}",
                    currentBestFitness, bestFitness);

                bestFitness = currentBestFitness; // עדכון ה-Best הכללי
                stagnationCount = 0; // איפוס מונה הקיפאון

                // --- מוטציה אדפטיבית: הקטנה / איפוס בשיפור ---
                // אפשרות א': איפוס לשיעור הבסיסי (הפשוטה ביותר)
                if (mutationRate > baseMutationRate)
                {
                    Log.Information("UpdateStagnation: Improvement found, resetting Mutation Rate from {OldRate:F3} to {NewRate:F3}",
                        mutationRate, baseMutationRate);
                    mutationRate = baseMutationRate;
                }
                // אפשרות ב': הקטנה הדרגתית (מסובך יותר)
                // double decreasedRate = Math.Max(baseMutationRate, mutationRate * mutationDecreaseFactor);
                // if (decreasedRate < mutationRate) {
                //     Log.Information("UpdateStagnation: Improvement found, decreasing Mutation Rate from {OldRate:F3} to {NewRate:F3}", mutationRate, decreasedRate);
                //     mutationRate = decreasedRate;
                // }
                // -------------------------------------------

                // לוג על השיפור (מדי פעם)
                if (currentGeneration % 20 == 0 || currentGeneration == 1)
                {
                    Log.Information("Improvement! Best fitness updated to: {BestFitness:F2} in Gen {Generation}", bestFitness, currentGeneration);
                }
            }
            else // אין שיפור משמעותי
            {
                stagnationCount++;

                // --- מוטציה אדפטיבית: הגברה בקיפאון ---
                // בדוק אם עברנו את הסף להגברת המוטציה
                if (stagnationCount >= stagnationThresholdForIncrease)
                {
                    // חשב את השיעור החדש, תוך הגבלה למקסימום
                    double increasedRate = Math.Min(maxMutationRate, mutationRate * mutationIncreaseFactor);

                    // הגבר את השיעור רק אם הוא באמת גדל ועדיין לא הגענו למקסימום
                    if (increasedRate > mutationRate && mutationRate < maxMutationRate)
                    {
                        Log.Information("UpdateStagnation: Stagnation threshold ({Threshold}) reached at Gen {Generation}. Increasing Mutation Rate from {OldRate:F3} to {NewRate:F3}",
                            stagnationThresholdForIncrease, currentGeneration, mutationRate, increasedRate);
                        mutationRate = increasedRate; // עדכן את שיעור המוטציה
                                                      // אופציונלי: לאפס חלקית את מונה הקיפאון כדי לתת למוטציה המוגברת זמן לעבוד?
                                                      // stagnationCount = 0; // איפוס מלא?
                                                      // stagnationCount = stagnationThresholdForIncrease / 2; // איפוס חלקי?
                                                      // או פשוט להמשיך לספור - הכי פשוט.
                    }
                    else if (mutationRate >= maxMutationRate)
                    {
                        // אם כבר הגענו למקסימום, נכתוב לוג רק מדי פעם
                        if (stagnationCount % 50 == 0 || stagnationCount == maxStagnation)
                        {
                            Log.Debug("UpdateStagnation: Stagnation continues ({StagnationCount}/{MaxStagnation}) but Mutation Rate already at MAX {MaxRate:F3}.",
                                stagnationCount, maxStagnation, maxMutationRate);
                        }
                    }
                }
                // -----------------------------------------

                // לוג על הקיפאון (מדי פעם)
                if (stagnationCount % 50 == 0 || stagnationCount == maxStagnation)
                {
                    Log.Debug("No significant improvement. Stagnation: {StagnationCount}/{MaxStagnation}. Best Fitness: {BestFitness:F2}",
                        stagnationCount, maxStagnation, bestFitness);
                }
            }

            // עדכון previousBestFitness (יכול להיות שימושי למעקב כללי, לא חובה ללוגיקת הקיפאון הזו)
            previousBestFitness = currentBestFitness;
        }

        
        private void Mutate(Schedule child)
        {

            var currentWorkloads = RecalculateWorkloads(child); // Get fresh workloads
            int mutationType = rnd.Next(100); // 0-99

            double wAdd = 20, wSwap = 20, wPref = 15, wCont = 15, wBal = 10, wSpec = 10, wRea = 10;
            double totalWeight = wAdd + wSwap + wPref + wCont + wBal + wSpec + wRea;
            double roll = rnd.NextDouble() * totalWeight; // rnd הוא מופע Random בטוח

            if ((roll -= wAdd) < 0) MutateAddUnassignedPatients(child, currentWorkloads);
            else if ((roll -= wSwap) < 0) MutateSwapPatients(child, currentWorkloads);
            else if ((roll -= wPref) < 0) MutateOptimizePreferences(child, currentWorkloads);
            else if ((roll -= wCont) < 0) MutateForContinuity(child);
            else if ((roll -= wBal) < 0) MutateForWorkloadBalance(child);
            else if ((roll -= wSpec) < 0) MutatePromoteToSpecialist(child, currentWorkloads);
            else MutateReassignPatient(child, currentWorkloads);
        } // האחרון

        #region Mutations
        private void MutateReassignPatient(Schedule schedule, Dictionary<int, int> currentWorkloads)
        {
            if (!schedule.PatientAssignmentLookup.Any()) return; // No patients to reassign

            // Select a random assigned patient
            var assignedPatientIds = schedule.PatientAssignmentLookup.Keys.ToList();
            int patientId = assignedPatientIds[rnd.Next(assignedPatientIds.Count)];

            if (!schedule.PatientAssignmentLookup.TryGetValue(patientId, out var currentAppointment) ||
                !PatientsById.TryGetValue(patientId, out var patient))
            {
                Log.Debug($"MutateReassign: Failed to find current appointment or patient object for P{patientId}.");
                return; // Should not happen if lookups are correct
            }

            // Find ALL possible alternative VALID assignments (Doctor + Slot)
            var alternativeAssignments = new List<(Doctor Doctor, AvailabilityTimeRange Slot)>();

            foreach (var doctor in DoctorsById.Values.Where(d => d.IsSuitableFor(patient))) // Check suitability first
            {
                // Check Doctor Capacity for THIS schedule
                int currentLoad = schedule.DoctorAssignmentLookup.TryGetValue(doctor.Id, out var appts) ? appts.Count : 0;
                // Allow moving TO a doctor at max capacity only IF we are moving FROM that same doctor (effectively changing time slot)
                bool hasCapacity = (currentLoad < doctor.MaxWorkload) || (doctor.Id == currentAppointment.DoctorId && currentLoad <= doctor.MaxWorkload); // Slightly relaxed check

                if (hasCapacity)
                {
                    foreach (var potentialSlot in doctor.AvailabilitySchedule)
                    {
                        // Is slot duration sufficient?
                        if (potentialSlot.Duration >= patient.EstimatedAppointmentDuration)
                        {
                            // Is it different from the current assignment's slot start time?
                            bool isOriginalSlot = (doctor.Id == currentAppointment.DoctorId &&
                                                   potentialSlot.DayOfWeek == currentAppointment.DayOfWeek &&
                                                   potentialSlot.StartTime == currentAppointment.StartTime);

                            // Is the slot free in the schedule?
                            if (!isOriginalSlot && schedule.IsSlotFree(doctor.Id, potentialSlot.DayOfWeek, potentialSlot.StartTime))
                            {
                                alternativeAssignments.Add((doctor, potentialSlot));
                            }
                        }
                    }
                }
            }


            // If alternatives were found...
            if (alternativeAssignments.Any())
            {
                // Pick one randomly
                var (newDoctor, newSlot) = alternativeAssignments[rnd.Next(alternativeAssignments.Count)];

                Log.Debug($"MutateReassign P{patientId}: Found alternative. Removing from Dr{currentAppointment.DoctorId}/{currentAppointment.DayOfWeek}/{currentAppointment.StartTime}.");

                // Remove the original assignment FIRST
                if (schedule.RemoveAppointment(patientId))
                {
                    // Create and add the new assignment
                    var newAppointment = new ScheduledAppointment
                    {
                        PatientId = patientId,
                        DoctorId = newDoctor.Id,
                        DayOfWeek = newSlot.DayOfWeek,
                        StartTime = newSlot.StartTime,
                        Duration = patient.EstimatedAppointmentDuration
                    };

                    if (schedule.TryAddAppointment(newAppointment))
                    {
                        
                        Log.Debug($"MutateReassign P{patientId}: Successfully moved to Dr{newDoctor.Id}/{newSlot.DayOfWeek}/{newSlot.StartTime}.");
                    }
                    else
                    {
                        // Should be rare: Failed to add back after removing? Maybe log error and try to add original back?
                        Log.Debug($"ERROR MutateReassign P{patientId}: Removed original but FAILED to add to Dr{newDoctor.Id}/{newSlot.DayOfWeek}/{newSlot.StartTime}! Patient unassigned.");
                        // Consider adding original back here if this happens
                        // schedule.TryAddAppointment(currentAppointment); // Be careful with object references if needed
                    }
                }
                else
                {
                    Log.Debug($"ERROR MutateReassign P{patientId}: Failed to remove original assignment?"); // Should not happen
                }
            }
            else
            {
                Log.Debug($"MutateReassign P{patientId}: No suitable alternative (Doctor + Slot) found.");
            }
        }

        private void MutatePromoteToSpecialist(Schedule schedule, Dictionary<int, int> currentWorkloads)
        {
            
            var candidates = schedule.PatientAssignmentLookup // Start with lookup: Dictionary<int, ScheduledAppointment>
            .Select(kvp => kvp.Value) // Select the ScheduledAppointment objects
            .Select(appt => {         // Perform lookups using TryGetValue for each appointment
                PatientsById.TryGetValue(appt.PatientId, out Patient p); // p will be null if PatientId not found
                DoctorsById.TryGetValue(appt.DoctorId, out Doctor d);    // d will be null if DoctorId not found
                return new { Appointment = appt, Patient = p, CurrentDoctor = d }; // Create object with results
            })
        .Where(x => x.Patient != null && // Ensure patient object was found
                x.CurrentDoctor != null && // Ensure doctor object was found
                x.CurrentDoctor.Specialization != x.Patient.RequiredSpecialization) // Actual filter logic
        .OrderBy(_ => rnd.Next()) // Shuffle potential candidates
    .ToList(); // Materialize the results

            if (!candidates.Any()) return; // No candidates needing promotion

            // Try to promote one candidate
            foreach (var candidate in candidates)
            {
                Patient patientToMove = candidate.Patient;
                ScheduledAppointment currentAppointment = candidate.Appointment;
                Doctor currentDoctor = candidate.CurrentDoctor;

                // Find potential target specialists
                if (DoctorsBySpecialization.TryGetValue(patientToMove.RequiredSpecialization, out var specialists))
                {
                    var potentialTargets = specialists
                        .Where(spec => spec.Id != currentDoctor.Id && // Different doctor
                                       spec.IsSuitableFor(patientToMove)) // Check suitability
                        .OrderBy(_ => rnd.Next())
                        .ToList();

                    foreach (var targetSpecialist in potentialTargets)
                    {
                        // Check capacity of the target specialist
                        int targetLoad = schedule.DoctorAssignmentLookup.TryGetValue(targetSpecialist.Id, out var appts) ? appts.Count : 0;
                        if (targetLoad < targetSpecialist.MaxWorkload)
                        {
                            // Find an available slot on the target specialist
                            AppointmentSlot targetSlot = FindFirstAvailableSlot(patientToMove, targetSpecialist, schedule);

                            if (targetSlot != null)
                            {
                                // Found suitable specialist with capacity and an available slot!
                                Log.Debug($"MutatePromoteToSpecialist: Moving P{patientToMove.Id} from non-spec Dr {currentDoctor.Id} ({currentDoctor.Specialization}) to spec Dr {targetSpecialist.Id} ({targetSpecialist.Specialization}) at Slot {targetSlot.DayOfWeek}/{targetSlot.StartTime}.");

                                if (schedule.RemoveAppointment(patientToMove.Id))
                                {
                                    var newAppointment = new ScheduledAppointment
                                    {
                                        PatientId = patientToMove.Id,
                                        DoctorId = targetSpecialist.Id,
                                        DayOfWeek = targetSlot.DayOfWeek,
                                        StartTime = targetSlot.StartTime,
                                        Duration = patientToMove.EstimatedAppointmentDuration
                                    };
                                    if (!schedule.TryAddAppointment(newAppointment))
                                    {
                                        Log.Debug($"ERROR MutatePromoteToSpec: Removed P{patientToMove.Id} but failed to add to Dr {targetSpecialist.Id}. Reverting (attempt).");
                                        schedule.TryAddAppointment(currentAppointment); // Try add original back
                                    }
                                    
                                    return; // Promoted one, exit mutation call
                                }
                                else
                                {
                                    Log.Debug($"ERROR MutatePromoteToSpec: Failed to remove P{patientToMove.Id} from Dr {currentDoctor.Id}.");
                                }
                            }
                            // else: No slot found on this target specialist
                        }
                        // else: Target specialist at capacity
                    } // End loop through potential target specialists
                }
                // else: Required specialization doesn't exist in dictionary? Should be rare.

            } // End foreach candidate

            // LogDebug($"MutatePromoteToSpecialist: Found {candidates.Count} candidates, but couldn't find a suitable specialist with capacity and an available slot.");
        }


        /// <summary>
        /// 
        ///Identifies the most and least loaded doctors based on their current appointment count relative to their maximum capacity.
        /// Only proceeds if there's a significant difference in load percentage between them (above a threshold) and if a transfer is feasible (most loaded has patients, least loaded has capacity).
        ///Tries to find a patient currently assigned to the most loaded doctor who is also suitable for the least loaded doctor.
        ///If such a patient is found, it searches for an available time slot for that patient with the least loaded doctor using FindFirstAvailableSlot.
        ///If a patient and a slot are found, it moves the patient by removing their original appointment and adding a new one with the least loaded doctor in the found slot, using the schedule's consistent methods.
        ///Invalidates the fitness score upon a successful mov
        /// </summary>
        /// <param name="schedule"></param>
        private void MutateForWorkloadBalance(Schedule schedule)
        {
            var currentWorkloads = RecalculateWorkloads(schedule); // Need counts
            var doctors = DoctorsById.Values.ToList();
            if (doctors.Count < 2) return;

            // Find most and least loaded doctors (based on count relative to MaxWorkload)
            var sortedByLoad = doctors
            .Where(d => currentWorkloads.ContainsKey(d.Id) || d.MaxWorkload > 0) // Consider doctors even if currently empty
                                                                                 // *** MODIFY THIS OrderBy LAMBDA vvv ***
            .OrderBy(d => {
                // Use TryGetValue instead of GetValueOrDefault
                currentWorkloads.TryGetValue(d.Id, out int load);
                // If TryGetValue returns false (key not found), 'load' will be 0 (default for int), which is correct.
                return d.MaxWorkload == 0 ? double.MaxValue : (double)load / d.MaxWorkload;
            })
            // *** END MODIFICATION ^^^ ***
            .ToList();

            if (sortedByLoad.Count() < 2) return;

            var leastLoadedDoc = sortedByLoad.First();
            var mostLoadedDoc = sortedByLoad.Last();

            // --- Get workload counts BEFORE the checks ---
            currentWorkloads.TryGetValue(mostLoadedDoc.Id, out int mostLoad); // Gets actual load or 0
            currentWorkloads.TryGetValue(leastLoadedDoc.Id, out int leastLoad); // Gets actual load or 0

            // --- Updated Checks ---
            // 1. Are they different doctors?
            // 2. Does the 'most loaded' doctor actually have patients to move (load > 0)?
            // 3. Is the 'least loaded' doctor already at or over capacity?
            if (mostLoadedDoc.Id == leastLoadedDoc.Id ||
                mostLoad == 0 || // Can't transfer if the 'most loaded' has 0 patients
                leastLoad >= leastLoadedDoc.MaxWorkload) // Use the 'leastLoad' variable here
            {
                return; // Exit if no valid transfer is possible based on these initial checks
            }

            // Now calculate percentages using the already retrieved loads
            double mostLoadPercent = mostLoadedDoc.MaxWorkload == 0 ? 1.0 : (double)mostLoad / mostLoadedDoc.MaxWorkload;
            double leastLoadPercent = leastLoadedDoc.MaxWorkload == 0 ? 1.0 : (double)leastLoad / leastLoadedDoc.MaxWorkload;

            if ((mostLoadPercent - leastLoadPercent) > 0.2) // Threshold
            {
                // Find a suitable patient assigned to the most loaded doctor
                var patientsOnMostLoaded = schedule.DoctorAssignmentLookup[mostLoadedDoc.Id];
                var transferablePatientAppointment = patientsOnMostLoaded
                    .Select(appt => { PatientsById.TryGetValue(appt.PatientId, out Patient p); return new { Appointment = appt, Patient = p }; })
                    .Where(x => x.Patient != null && leastLoadedDoc.IsSuitableFor(x.Patient)) // Check suitability for LEAST loaded doc
                    .OrderBy(_ => rnd.Next()) // Shuffle suitable patients
                    .FirstOrDefault();

                if (transferablePatientAppointment != null)
                {
                    Patient patientToMove = transferablePatientAppointment.Patient;
                    ScheduledAppointment originalAppointment = transferablePatientAppointment.Appointment;

                    // Now, find an available slot for this patient on the LEAST loaded doctor
                    AppointmentSlot targetSlot = FindFirstAvailableSlot(patientToMove, leastLoadedDoc, schedule);

                    if (targetSlot != null)
                    {
                        // Found a patient and a target slot! Perform the move.
                        Log.Debug($"MutateWorkloadBalance: Moving P{patientToMove.Id} from Dr {mostLoadedDoc.Id} ({(int)(mostLoadPercent * 100)}%) to Dr {leastLoadedDoc.Id} ({(int)(leastLoadPercent * 100)}%) at Slot {targetSlot.DayOfWeek}/{targetSlot.StartTime}.");

                        if (schedule.RemoveAppointment(patientToMove.Id))
                        {
                            var newAppointment = new ScheduledAppointment
                            {
                                PatientId = patientToMove.Id,
                                DoctorId = leastLoadedDoc.Id,
                                DayOfWeek = targetSlot.DayOfWeek,
                                StartTime = targetSlot.StartTime,
                                Duration = patientToMove.EstimatedAppointmentDuration
                            };
                            if (!schedule.TryAddAppointment(newAppointment))
                            {
                                // Failed to add after remove? Log error, maybe revert.
                                Log.Debug($"ERROR MutateWorkloadBalance: Removed P{patientToMove.Id} but failed to add to Dr {leastLoadedDoc.Id}. Reverting (attempt).");
                                schedule.TryAddAppointment(originalAppointment); // Try adding original back
                            }
                            
                        }
                        else
                        {
                            Log.Debug($"ERROR MutateWorkloadBalance: Failed to remove P{patientToMove.Id} from Dr {mostLoadedDoc.Id}.");
                        }
                    }
                    else
                    {
                        Log.Debug($"MutateWorkloadBalance: Found transferable P{patientToMove.Id} from Dr {mostLoadedDoc.Id}, but no available slot on Dr {leastLoadedDoc.Id}.");
                    }
                }
                else
                {
                    Log.Debug($"MutateWorkloadBalance: Found load imbalance but no suitable patient on Dr {mostLoadedDoc.Id} to transfer to Dr {leastLoadedDoc.Id}.");
                }
            }
        }

        /// <summary>
        /// Attempts to improve the schedule's continuity of care score by moving a patient back to their previously assigned doctor.
        /// Identifies patients currently assigned to a doctor different from their known previous one. For one such randomly selected candidate,
        /// it verifies if the previous doctor is suitable, has available capacity in the current schedule, and finds an available time slot using a helper method.
        /// If all conditions are met, the patient's appointment is moved to the previous doctor in the found slot, maintaining schedule consistency,
        /// and the schedule's fitness score is invalidated. Only one patient is moved per invocation.
        /// </summary>
        private void MutateForContinuity(Schedule schedule)
        {
            // Find patients not currently assigned to their previous doctor
            var candidates = schedule.PatientAssignmentLookup // Start with lookup: Dictionary<int, ScheduledAppointment>
                                                              // Filter first based on Patient ID (kvp.Key) and previous assignment info
             .Where(kvp => previousAssignments.ContainsKey(kvp.Key) &&
                           previousAssignments[kvp.Key] != kvp.Value.DoctorId)
             // Now safely get the Patient object for the filtered entries
             .Select(kvp => {
                 PatientsById.TryGetValue(kvp.Key, out Patient p); // p will be null if PatientId not found
                 return new { CurrentAppointment = kvp.Value, Patient = p }; // Create anon type with potentially null Patient
             })
             // Ensure the patient object was actually found
             .Where(x => x.Patient != null)
             .OrderBy(_ => rnd.Next()) // Shuffle candidates
             .ToList(); // Materialize the list

            if (!candidates.Any()) return; // No candidates for continuity move

            // Try to move one candidate
            foreach (var candidate in candidates)
            {
                Patient patientToMove = candidate.Patient;
                ScheduledAppointment currentAppointment = candidate.CurrentAppointment;
                int previousDoctorId = previousAssignments[patientToMove.Id];

                // Get the previous doctor object and check basic suitability & capacity
                if (DoctorsById.TryGetValue(previousDoctorId, out Doctor previousDoctor) &&
                    previousDoctor.IsSuitableFor(patientToMove)) // Check suitability first
                {
                    // Check capacity of the previous doctor in the current schedule
                    int prevDocLoad = schedule.DoctorAssignmentLookup.TryGetValue(previousDoctorId, out var appts) ? appts.Count : 0;
                    if (prevDocLoad < previousDoctor.MaxWorkload)
                    {
                        // Find an available slot on the previous doctor
                        AppointmentSlot targetSlot = FindFirstAvailableSlot(patientToMove, previousDoctor, schedule);

                        if (targetSlot != null)
                        {
                            // Found previous doctor suitable, with capacity, and an available slot!
                            Log.Debug($"MutateContinuity: Moving P{patientToMove.Id} from Dr {currentAppointment.DoctorId} back to previous Dr {previousDoctorId} at Slot {targetSlot.DayOfWeek}/{targetSlot.StartTime}.");

                            if (schedule.RemoveAppointment(patientToMove.Id))
                            {
                                var newAppointment = new ScheduledAppointment
                                {
                                    PatientId = patientToMove.Id,
                                    DoctorId = previousDoctorId,
                                    DayOfWeek = targetSlot.DayOfWeek,
                                    StartTime = targetSlot.StartTime,
                                    Duration = patientToMove.EstimatedAppointmentDuration
                                };
                                if (!schedule.TryAddAppointment(newAppointment))
                                {
                                    Log.Debug($"ERROR MutateContinuity: Removed P{patientToMove.Id} but failed to add back to Dr {previousDoctorId}. Reverting (attempt).");
                                    schedule.TryAddAppointment(currentAppointment); // Try add original back
                                }
                                
                                return; // Moved one patient, exit mutation call
                            }
                            else
                            {
                                Log.Debug($"ERROR MutateContinuity: Failed to remove P{patientToMove.Id} from Dr {currentAppointment.DoctorId}.");
                            }
                        }
                         else Log.Debug($"MutateContinuity: P{patientToMove.Id} could return to Dr {previousDoctorId} (suitable, capacity OK), but no free slot found.");
                    }
                    else Log.Debug($"MutateContinuity: P{patientToMove.Id} cannot return to Dr {previousDoctorId} due to capacity ({prevDocLoad}/{previousDoctor.MaxWorkload}).");
                }
                 else Log.Debug($"MutateContinuity: P{patientToMove.Id} cannot return to Dr {previousDoctorId} (not suitable or doesn't exist).");
            } // End foreach candidate
        }
        /// <summary>
        /// Attempts to improve the schedule by swapping the assigned doctors between two randomly selected appointments,
        /// specifically when the swap is predicted to increase the overall preference score for the involved pairs.
        /// Before attempting the swap, it performs comprehensive checks: doctor suitability for the new patient,
        /// patient duration fitting the target slot duration, and critically, ensures the target time slots are free
        /// for the swapped doctors using IsSlotFree. If the preference score improves AND all checks pass,
        /// the original appointments are replaced with the swapped ones, maintaining schedule consistency,
        /// and the schedule's fitness score is invalidated.
        /// </summary>
        private void MutateOptimizePreferences(Schedule schedule, Dictionary<int, int> currentWorkloads)
        {
            if (schedule.Appointments.Count < 2) return;

            // --- בחירת שני תורים שונים (כמו קודם) ---
            int index1 = rnd.Next(schedule.Appointments.Count);
            int index2 = rnd.Next(schedule.Appointments.Count);
            if (index1 == index2) index2 = (index1 + 1) % schedule.Appointments.Count;

            var appt1 = schedule.Appointments[index1];
            var appt2 = schedule.Appointments[index2];

            // --- שליפת אובייקטים (כמו קודם) ---
            if (!PatientsById.TryGetValue(appt1.PatientId, out var patient1) || !DoctorsById.TryGetValue(appt1.DoctorId, out var doctor1) ||
                !PatientsById.TryGetValue(appt2.PatientId, out var patient2) || !DoctorsById.TryGetValue(appt2.DoctorId, out var doctor2))
            {
                Log.Warning($"MutateOptimizePreferences: Could not find patient/doctor objects for Appt1/Appt2."); // עדיף Warning
                return;
            }

            // --- חישוב ציוני העדפות (כמו קודם) ---
            double currentScore1 = CalculatePreferenceScoreForPair(doctor1, patient1);
            double currentScore2 = CalculatePreferenceScoreForPair(doctor2, patient2);
            double potentialScore1 = CalculatePreferenceScoreForPair(doctor1, patient2); // Dr1 with P2
            double potentialScore2 = CalculatePreferenceScoreForPair(doctor2, patient1); // Dr2 with P1

            // --- בדיקות מקדימות (כמו קודם, עם חישוב משך זמן נכון) ---
            bool suitCheck1 = doctor1.IsSuitableFor(patient2); // האם Dr1 מתאים ל-P2?
            bool suitCheck2 = doctor2.IsSuitableFor(patient1); // האם Dr2 מתאים ל-P1?
            TimeSpan slot1Duration = appt1.EndTime - appt1.StartTime; // הנחה שקיים EndTime
            TimeSpan slot2Duration = appt2.EndTime - appt2.StartTime; // הנחה שקיים EndTime
            bool durationCheck1 = slot1Duration >= patient2.EstimatedAppointmentDuration; // האם P2 נכנס בזמן של S1?
            bool durationCheck2 = slot2Duration >= patient1.EstimatedAppointmentDuration; // האם P1 נכנס בזמן של S2?

            // --- בדיקת שיפור בהעדפות ---
            bool preferenceImproves = (potentialScore1 + potentialScore2 > currentScore1 + currentScore2);

            // --- התנאי המרכזי - כולל *כל* הבדיקות הנדרשות ---
            if (preferenceImproves && suitCheck1 && suitCheck2 && durationCheck1 && durationCheck2)
            {
                // *** בדיקת פניות משבצות הזמן - החלק הקריטי שהיה חסר ***
                bool slot1FreeForDr2 = schedule.IsSlotFree(doctor2.Id, appt1.DayOfWeek, appt1.StartTime);
                bool slot2FreeForDr1 = schedule.IsSlotFree(doctor1.Id, appt2.DayOfWeek, appt2.StartTime);

                // בצע את ההחלפה רק אם *גם* המשבצות פנויות
                if (slot1FreeForDr2 && slot2FreeForDr1)
                {
                    // --- כל הבדיקות עברו! בטוח לבצע את ההחלפה ---
                    Log.Debug($"MutateOptimizePreferences: Attempting swap for P{patient1.Id}(@{doctor1.Id}) <-> P{patient2.Id}(@{doctor2.Id}). Pref Score ({currentScore1 + currentScore2:F2} -> {potentialScore1 + potentialScore2:F2}) and all checks OK.");

                    // 1. צור את התורים החדשים (עם הרופאים המוחלפים)
                    //    חשוב להשתמש במשך הזמן הנדרש של *המטופל* עבור התור החדש
                    var newAppt1 = new ScheduledAppointment { PatientId = patient1.Id, DoctorId = doctor2.Id, DayOfWeek = appt1.DayOfWeek, StartTime = appt1.StartTime, Duration = patient1.EstimatedAppointmentDuration };
                    var newAppt2 = new ScheduledAppointment { PatientId = patient2.Id, DoctorId = doctor1.Id, DayOfWeek = appt2.DayOfWeek, StartTime = appt2.StartTime, Duration = patient2.EstimatedAppointmentDuration };

                    // 2. הסר את התורים המקוריים (בטוח יותר לעשות זאת עכשיו)
                    //    חשוב לבדוק הצלחה למקרה נדיר של בעיה
                    bool removed1 = schedule.RemoveAppointment(patient1.Id);
                    bool removed2 = schedule.RemoveAppointment(patient2.Id);

                    if (removed1 && removed2)
                    {
                        // 3. הוסף את התורים החדשים (אמורים להצליח כי בדקנו פניות)
                        bool added1 = schedule.TryAddAppointment(newAppt1); // P1 -> D2 @ S1 time
                        bool added2 = schedule.TryAddAppointment(newAppt2); // P2 -> D1 @ S2 time

                        if (added1 && added2)
                        {
                            Log.Debug("MutateOptimizePreferences: Swap successful.");
                            
                        }
                        else
                        {
                            // מקרה נדיר מאוד אם הבדיקות המקדימות היו נכונות. מצביע על בעיה עמוקה יותר?
                            Log.Error($"CRITICAL Error MutateOptimizePreferences: Failed to add swapped appointments even after checks and successful removal! Reverting...");
                            // נסה להחזיר את המקוריים (בלי אחריות מלאה)
                            if (removed1) schedule.TryAddAppointment(appt1); // הוסף את המקורי בחזרה אם נמחק
                            if (removed2) schedule.TryAddAppointment(appt2); // הוסף את המקורי בחזרה אם נמחק
                        }
                    }
                    else
                    {
                        // מקרה נדיר - המחיקה נכשלה אחרי שהבדיקות עברו?
                        Log.Error($"CRITICAL Error MutateOptimizePreferences: Failed to remove original appointments P{patient1.Id} or P{patient2.Id} after checks passed!");
                        // נסה להחזיר את המצב לקדמותו אם אפשר (מסובך)
                        if (!removed1 && removed2) schedule.TryAddAppointment(appt2); // נמחק רק 2, נחזיר אותו
                        if (!removed2 && removed1) schedule.TryAddAppointment(appt1); // נמחק רק 1, נחזיר אותו
                    }
                } // סוף if (slot1FreeForDr2 && slot2FreeForDr1)
                else
                {
                    // לוג במקרה שההחלפה נכשלה *בגלל* חוסר פניות במשבצת
                    Log.Debug($"MutateOptimizePreferences: Cannot swap P{patient1.Id}/P{patient2.Id}. Slot conflict detected (Slot1FreeDr2={slot1FreeForDr2}, Slot2FreeDr1={slot2FreeForDr1}).");
                }
            } // סוף if (preferenceImproves && suitCheck1 && ...)
              // אפשר להוסיף כאן else ולוג אם ההחלפה נכשלה בגלל סיבה אחרת (העדפה לא השתפרה / חוסר התאמה / משך זמן)
              // else if (preferenceImproves) { ... LogDebug("...Cannot swap. Failed suitability/duration checks.") ... }
        }
        /// <summary>
        /// Attempts to improve the schedule by swapping the assigned doctors between two randomly selected existing appointments.
        /// Performs several checks before attempting the swap: ensures both doctors are suitable for the swapped patients,
        /// checks if the patient durations fit the target slots, and critically, verifies that the target time slots
        /// are actually available (free) for the swapped doctors in the current schedule state using IsSlotFree.
        /// If all checks pass, the original appointments are removed and the new appointments with swapped doctors are added,
        /// maintaining the schedule's internal consistency via RemoveAppointment/TryAddAppointment. The fitness score is invalidated upon a successful swap.
        /// </summary>
        private void MutateSwapPatients(Schedule child, Dictionary<int, int> currentWorkloads)
        {

            if (child.Appointments.Count < 2) return; // Need at least two appointments

            // Select two distinct random appointments from the current schedule
            int index1 = rnd.Next(child.Appointments.Count);
            int index2 = rnd.Next(child.Appointments.Count);
            if (index1 == index2) index2 = (index1 + 1) % child.Appointments.Count;

            var appt1 = child.Appointments[index1];
            var appt2 = child.Appointments[index2];

            // Get related objects
            if (!PatientsById.TryGetValue(appt1.PatientId, out var patient1) || !DoctorsById.TryGetValue(appt1.DoctorId, out var doctor1) ||
                !PatientsById.TryGetValue(appt2.PatientId, out var patient2) || !DoctorsById.TryGetValue(appt2.DoctorId, out var doctor2))
            {
                Log.Debug($"MutateSwap: Could not find patient/doctor objects for Appt1({appt1.PatientId}/{appt1.DoctorId}) or Appt2({appt2.PatientId}/{appt2.DoctorId}).");
                return;
            }

            // Check suitability for swap
            bool suitCheck1 = doctor1.IsSuitableFor(patient2); // Can Dr1 handle P2?
            bool suitCheck2 = doctor2.IsSuitableFor(patient1); // Can Dr2 handle P1?

            bool slot1FreeForDr2 = child.IsSlotFree(doctor2.Id, appt1.DayOfWeek, appt1.StartTime);
            bool slot2FreeForDr1 = child.IsSlotFree(doctor1.Id, appt2.DayOfWeek, appt2.StartTime);

            // Check if durations fit the original slots
            bool durationCheck1 = appt1.Duration >= patient2.EstimatedAppointmentDuration; // Can P2 fit in S1?
            bool durationCheck2 = appt2.Duration >= patient1.EstimatedAppointmentDuration; // Can P1 fit in S2?

            if (suitCheck1 && suitCheck2 && durationCheck1 && durationCheck2 && slot1FreeForDr2 && slot2FreeForDr1)
            {
                Log.Debug($"MutateSwap: Swapping Doctors for P{patient1.Id}(Slot1@{doctor1.Id}) and P{patient2.Id}(Slot2@{doctor2.Id}). Suitability & Duration OK.");

                // Create the new swapped appointments
                var newAppt1 = new ScheduledAppointment { PatientId = patient1.Id, DoctorId = doctor2.Id, DayOfWeek = appt1.DayOfWeek, StartTime = appt1.StartTime, Duration = patient1.EstimatedAppointmentDuration };
                var newAppt2 = new ScheduledAppointment { PatientId = patient2.Id, DoctorId = doctor1.Id, DayOfWeek = appt2.DayOfWeek, StartTime = appt2.StartTime, Duration = patient2.EstimatedAppointmentDuration };

                // Remove originals first
                bool removed1 = child.RemoveAppointment(patient1.Id);
                bool removed2 = child.RemoveAppointment(patient2.Id);

                if (removed1 && removed2)
                {
                    // Try adding swapped appointments
                    bool added1 = child.TryAddAppointment(newAppt1); // P1 -> D2 @ S1 time
                    bool added2 = child.TryAddAppointment(newAppt2); // P2 -> D1 @ S2 time

                    if (added1 && added2)
                    {
                        Log.Debug("MutateSwap: Swap successful.");
                       
                    }
                    else
                    {
                        Log.Error($"CRITICAL Error MutateSwap: Failed to add swapped appointments even after checks passed! Reverting...");
                        // נסה להחזיר את המקוריים (best effort)
                        child.TryAddAppointment(appt1);
                        child.TryAddAppointment(appt2);
                    }
                }
                else
                {
                    Log.Debug($"ERROR MutateSwap: Failed to remove original appointments P{patient1.Id} or P{patient2.Id}. Aborting swap.");
                    // Re-add any that were removed if one failed? Requires careful state management.
                    if (!removed1 && removed2) child.TryAddAppointment(appt2); // Add P2 back if P1 remove failed
                    if (!removed2 && removed1) child.TryAddAppointment(appt1); // Add P1 back if P2 remove failed
                }
            }
            else
            {
                // Optional log: Which check failed?
                // LogDebug($"MutateSwap: Cannot swap. Suit1={suitCheck1}, Suit2={suitCheck2}, Dur1FitsS2={durationCheck2}, Dur2FitsS1={durationCheck1}");
            }
        }
        

        /// <summary>
        /// Attempts to assign a small number of currently unassigned (non-surgery) patients within the given schedule.
        /// Prioritizes patients by urgency, finds suitable doctors randomly who have capacity in the current schedule,
        /// searches for the first available valid time slot using a helper method, and adds the new appointment
        /// using the schedule's TryAddAppointment method to ensure consistency. This mutation aims to improve schedule completeness.
        /// </summary>
        private void MutateAddUnassignedPatients(Schedule child, Dictionary<int, int> currentWorkloads)
        {
            var assignedPatientIds = new HashSet<int>(child.PatientAssignmentLookup.Keys);
            var unassigned = PatientsById.Values
                .Where(p => !p.NeedsSurgery && !assignedPatientIds.Contains(p.Id))
                .OrderByDescending(p => (int)p.Urgency)
                .ThenBy(_ => rnd.Next())
                .Take(5) // Still attempt a few per mutation call
                .ToList();

            int assignedCount = 0;
            foreach (var patient in unassigned)
            {
                bool patientAssignedThisLoop = false;
                // Find suitable doctors (inherent compatibility)
                var suitableDoctors = DoctorsById.Values
                                        .Where(d => d.IsSuitableFor(patient))
                                        .OrderBy(_ => rnd.Next()) // Try doctors in random order
                                        .ToList();

                foreach (var doctor in suitableDoctors)
                {
                    // Check Doctor Capacity for THIS schedule
                    int currentLoad = child.DoctorAssignmentLookup.TryGetValue(doctor.Id, out var appts) ? appts.Count : 0;
                    if (currentLoad >= doctor.MaxWorkload)
                    {
                        // LogDebug($"MutateAddUnassigned P{patient.Id}: Skipping Dr {doctor.Id} due to capacity ({currentLoad}/{doctor.MaxWorkload}).");
                        continue; // Skip this doctor if full
                    }

                    // Find the first available slot for this suitable doctor with capacity
                    AppointmentSlot availableSlot = FindFirstAvailableSlot(patient, doctor, child);

                    if (availableSlot != null)
                    {
                        // Found a doctor and a slot! Create and add appointment.
                        var newAppointment = new ScheduledAppointment
                        {
                            PatientId = patient.Id,
                            DoctorId = doctor.Id,
                            DayOfWeek = availableSlot.DayOfWeek,
                            StartTime = availableSlot.StartTime,
                            Duration = patient.EstimatedAppointmentDuration // Use patient's required duration
                        };

                        if (child.TryAddAppointment(newAppointment))
                        {
                            assignedCount++;
                            patientAssignedThisLoop = true;
                            Log.Debug($"MutateAddUnassigned: Assigned P{patient.Id} to Dr {doctor.Id} at {availableSlot.DayOfWeek} {availableSlot.StartTime}");
                            break; // Patient assigned, move to next unassigned patient
                        }
                        // else: TryAdd failed unexpectedly (maybe race condition if parallelized incorrectly?)
                    }
                    // else: No available slot found for this doctor, try next doctor
                } // End loop through suitable doctors

                if (!patientAssignedThisLoop)
                {
                    Log.Debug($"MutateAddUnassigned: Failed to find suitable Doctor+Slot for P{patient.Id}");
                }

            } // End loop through unassigned patients

            if (assignedCount > 0)
            {
                Log.Debug($"MutateAddUnassigned: Attempted for {unassigned.Count} patients, successfully added {assignedCount}.");
            }
        }
        /// <summary>
        /// Finds the first available and suitable time slot for a given patient with a specific doctor
        /// in the context of the current schedule.
        /// </summary>
        /// <returns>The AvailabilitySlot if found, otherwise null.</returns>
        private AppointmentSlot FindFirstAvailableSlot(Patient patient, Doctor doctor, Schedule schedule)
        {
            if (doctor?.AvailabilitySchedule == null || patient == null || schedule == null)
                return null;

            // Iterate through the DOCTOR'S general availability RANGES for the week
            foreach (var timeRange in doctor.AvailabilitySchedule.OrderBy(_ => rnd.Next())) // Check ranges in random order
            {
                // Check if the range itself is long enough for the appointment
                if (timeRange.Duration >= patient.EstimatedAppointmentDuration)
                {
                    // Now, iterate through potential START times within this range
                    TimeSpan potentialStartTime = timeRange.StartTime;
                    TimeSpan slotIncrement = TimeSpan.FromMinutes(15); // Or use smallest possible slot increment

                    while (potentialStartTime.Add(patient.EstimatedAppointmentDuration) <= timeRange.EndTime)
                    {
                        // Check if this specific potential slot is free in the schedule object
                        if (schedule.IsSlotFree(doctor.Id, timeRange.DayOfWeek, potentialStartTime))
                        {
                            // Found a valid slot! Return information needed to create the appointment
                            // We return a temporary AvailabilitySlot object representing the FOUND slot
                            // (This is slightly different from returning the template range)
                            return new AppointmentSlot
                            {
                                DoctorId = doctor.Id,
                                DayOfWeek = timeRange.DayOfWeek,
                                StartTime = potentialStartTime,
                                // End time is based on patient duration for the actual booking
                                EndTime = potentialStartTime + patient.EstimatedAppointmentDuration
                            };
                        }
                        // Move to the next potential start time within the range
                        potentialStartTime = potentialStartTime.Add(slotIncrement);
                    }
                }
            }
            return null; // No suitable slot found across all ranges
        }

        #endregion

        /// <summary>
        /// Performs single-point crossover on the appointment lists of two parent schedules to create two offspring schedules.
        /// Selects a random crossover point and swaps segments of the appointment lists between parents.
        /// Since the initial combination likely results in invalid schedules (conflicts, duplicates),
        /// it relies on a 'RepairAndRebuildAppointments' helper method to process the combined lists and generate valid sets of appointments.
        /// Finally, populates the new offspring Schedule objects using the repaired appointments, ensuring their internal lookups are consistent.
        /// </summary>
        private List<Schedule> CrossoverListSinglePoint(Schedule parent1, Schedule parent2)
        {
            var offspring1 = new Schedule(); // Uses new Schedule structure
            var offspring2 = new Schedule();

            var list1 = parent1.Appointments;
            var list2 = parent2.Appointments;
            int n1 = list1.Count;
            int n2 = list2.Count;

            // Handle empty parents
            if (n1 == 0 && n2 == 0) return new List<Schedule> { offspring1, offspring2 };

            // Choose a crossover point (simplest: within the shorter list length)
            int crossoverPoint = Math.Min(n1, n2) > 0 ? rnd.Next(Math.Min(n1, n2)) : 0;
            // Alternative: Choose separate points relative to each list length if desired

            // Create combined lists (potentially invalid)
            var combinedAppts1 = list1.Take(crossoverPoint).Concat(list2.Skip(crossoverPoint)).ToList();
            var combinedAppts2 = list2.Take(crossoverPoint).Concat(list1.Skip(crossoverPoint)).ToList();

            // --- Repair the combined lists ---
            List<ScheduledAppointment> repairedAppts1 = RepairAndRebuildAppointments(combinedAppts1);
            List<ScheduledAppointment> repairedAppts2 = RepairAndRebuildAppointments(combinedAppts2);

            // --- Populate the offspring Schedules using the valid appointments ---
            // Using TryAddAppointment rebuilds the internal lookups correctly
            foreach (var appt in repairedAppts1)
            {
                offspring1.TryAddAppointment(appt);
            }
            foreach (var appt in repairedAppts2)
            {
                offspring2.TryAddAppointment(appt);
            }

            // Optional Logging
            Log.Debug($"Crossover: P1({n1}) + P2({n2}) -> Combined O1({combinedAppts1.Count})/O2({combinedAppts2.Count}) -> Repaired O1({repairedAppts1.Count})/O2({repairedAppts2.Count})");

            return new List<Schedule> { offspring1, offspring2 };
        }

        // Inside DoctorScheduler class
        /// <summary>
        /// Takes a list of potentially conflicting appointments and returns a new list
        /// containing only valid, non-conflicting appointments.
        /// It prioritizes keeping the first valid appointment encountered for any
        /// given patient or time slot after shuffling.
        /// </summary>
        /// <param name="potentiallyInvalidAppointments">List of appointments from crossover.</param>
        /// <returns>A list of valid ScheduledAppointment objects.</returns>
        private List<ScheduledAppointment> RepairAndRebuildAppointments(List<ScheduledAppointment> potentiallyInvalidAppointments)
        {
            var repairedAppointments = new List<ScheduledAppointment>();
            // Temporary lookups used ONLY during the repair process
            var assignedPatients = new HashSet<int>();
            var bookedSlots = new HashSet<(int DoctorId, DayOfWeek Day, TimeSpan StartTime)>();

            // Shuffle the input list randomly. This prevents bias towards keeping
            // appointments that happened to be earlier in one parent's list during crossover.
            // The first valid appointment encountered for a patient/slot will be kept.
            var shuffledAppointments = potentiallyInvalidAppointments.OrderBy(_ => rnd.Next()).ToList();

            foreach (var appointment in shuffledAppointments)
            {
                // --- Conflict Check 1: Patient already assigned? ---
                if (assignedPatients.Contains(appointment.PatientId))
                {
                    // LogDebug($"Repair: Discarding Appt for P{appointment.PatientId} (already assigned in repaired list).");
                    continue; // Skip this appointment - patient already handled
                }

                // --- Conflict Check 2: Doctor/Time slot already booked? ---
                // Use the specific start time as the key for the slot booking
                var timeSlotKey = (appointment.DoctorId, appointment.DayOfWeek, appointment.StartTime);
                if (bookedSlots.Contains(timeSlotKey))
                {
                    // LogDebug($"Repair: Discarding Appt for P{appointment.PatientId} at Dr{appointment.DoctorId}/{timeSlotKey.Day}/{timeSlotKey.StartTime} (slot conflict).");
                    continue; // Skip this appointment - slot conflict
                }

                // --- Check 3 (Optional but Recommended): Does Dr have capacity? ---
                // This requires tracking workload during repair, adding complexity.
                // If omitted, rely on fitness penalty or later mutation.
                // For now, let's assume operators should prevent exceeding MaxWorkload,
                // or fitness heavily penalizes it.

                // --- No Conflicts Found ---
                // Add this valid appointment to our repaired list
                repairedAppointments.Add(appointment);
                // Update lookups to prevent future conflicts for this patient/slot
                assignedPatients.Add(appointment.PatientId);
                bookedSlots.Add(timeSlotKey);
            }

            // Return the cleaned list of appointments
            return repairedAppointments;
        }
        private Schedule TournamentSelection()
        {
            if (!Population.Any()) return null;
            Schedule bestInTournament = null;
            double bestFitnessInTournament = double.MinValue;
            for (int i = 0; i < tournamentSize; i++)
            {
                Schedule candidate = Population[rnd.Next(Population.Count)];
                if (candidate.FitnessScore > bestFitnessInTournament)
                {
                    bestFitnessInTournament = candidate.FitnessScore;
                    bestInTournament = candidate;
                }
            }
            return bestInTournament ?? Population[rnd.Next(Population.Count)];
        }

        private Schedule CloneSchedule(Schedule original)
        {
            if (original == null) return new Schedule();

            var clone = new Schedule
            {
                FitnessScore = original.FitnessScore // Copy cached score
            };

            // Deep copy the core list of appointments
            foreach (var appointment in original.Appointments)
            {
                // Create a shallow copy of the appointment object itself
                // (assuming ScheduledAppointment is simple or cloning isn't needed deeper)
                var appointmentClone = new ScheduledAppointment
                {
                    PatientId = appointment.PatientId,
                    DoctorId = appointment.DoctorId,
                    DayOfWeek = appointment.DayOfWeek,
                    StartTime = appointment.StartTime,
                    Duration = appointment.Duration
                    // EndTime is calculated
                };
                // Use the Add method to rebuild lookups correctly in the clone
                clone.TryAddAppointment(appointmentClone); // This rebuilds all lookups
            }

            // Re-check counts (optional sanity check)
            // if (clone.Appointments.Count != original.Appointments.Count) {
            //     LogDebug("WARN: Clone appointment count mismatch!");
            // }

            return clone;
        }

        private bool TerminationConditionMet()
        {
            if (!Population.Any()) { Log.Debug("Termination: Population empty."); return true; } // *** LOGGING ADDED/MODIFIED ***
            if (currentGeneration >= maxGenerations) { Log.Debug("Termination: Max generations reached."); return true; } // *** LOGGING ADDED/MODIFIED ***
            if (bestFitness >= fitnessThreshold) { Log.Debug($"Termination: Fitness threshold {fitnessThreshold} reached."); return true; } // *** LOGGING ADDED/MODIFIED ***
            if (stagnationCount >= maxStagnation) { Log.Debug($"Termination: Stagnation ({stagnationCount}/{maxStagnation}) detected."); return true; } // *** LOGGING ADDED/MODIFIED ***
            return false;
        }

        private void CalculateFitnessForAll(List<Schedule> population)
        {
            // Clear cached scores before recalculating in parallel
            foreach (var schedule in population)
            {
                schedule.FitnessScore = -1.0; // Reset score to force recalculation
            }
            // Recalculate in parallel
            Parallel.ForEach(population, schedule => {
                FitnessFunction(schedule); // ScoreSchedule now caches the result
            });
        }

        /// <summary>
        /// Calculates the fitness score for a given schedule solution, evaluating its quality based on multiple weighted criteria.
        /// Checks for a previously calculated cached score first. If not found, it evaluates the schedule by:
        /// 1. Scoring each individual appointment based on specialization match, patient urgency, doctor experience, continuity of care, and preferences.
        /// 2. Calculating a score component that rewards balanced workloads across doctors.
        /// 3. Applying penalties for schedulable patients who were left unassigned.
        /// Includes detailed logging at various levels for diagnostics and caches the calculated score in the schedule object before returning it.
        /// </summary>
        private double FitnessFunction(Schedule schedule)
        {
            // שימוש בסובלנות להשוואת נקודה צפה
            const double tolerance = 1e-9;

            // 1. בדיקת מטמון
            // רמת Debug כי זה קורה הרבה פעמים
            if (Math.Abs(schedule.FitnessScore - (-1.0)) > tolerance) // בדיקה אם זה *לא* ערך האתחול
            {
                Log.Verbose("FitnessFunction: Returning cached score {FitnessScore:F2} for schedule.", schedule.FitnessScore); // רמה Verbose לפרטים מאוד עדינים
                return schedule.FitnessScore;
            }

            Log.Verbose("FitnessFunction: Calculating fitness for new schedule...");

            double score = 0;
            double assignmentScore = 0;
            double balanceScore = 0;
            double penaltyScore = 0;
            int assignedAppointmentsCount = 0;

            // 2. חישוב עומסים מחדש (בהתבסס על ה-schedule הנוכחי)
            Log.Verbose("FitnessFunction: Recalculating workloads for current schedule...");
            var currentWorkloads = RecalculateWorkloads(schedule); // הנחה שהמתודה קיימת
            Log.Verbose("FitnessFunction: Recalculated workloads for {DoctorCount} doctors.", currentWorkloads.Count);

            // --- 3. ניקוד שיבוצים שבוצעו ---
            Log.Verbose("FitnessFunction: Scoring individual assignments...");
            foreach (var appointment in schedule.Appointments)
            {
                assignedAppointmentsCount++;
                int patientId = appointment.PatientId;
                int doctorId = appointment.DoctorId;
                double currentAppointmentScore = 0; // ניקוד לתור הספציפי הזה

                // שליפת אובייקטים, דילוג אם חסר (לא אמור לקרות)
                if (!PatientsById.TryGetValue(patientId, out Patient patient) || !DoctorsById.TryGetValue(doctorId, out Doctor doctor))
                {
                    Log.Warning("FitnessFunction: Skipping appointment scoring. Could not find Patient {PatientId} or Doctor {DoctorId} in lookups.", patientId, doctorId); // רמת Warning לבעיה אפשרית
                    continue;
                }

                // חישוב הניקוד עבור התור הזה (כמו קודם)
                currentAppointmentScore += patientAssignmentWeight; // ניקוד בסיסי
                if (string.Equals(doctor.Specialization, patient.RequiredSpecialization, StringComparison.OrdinalIgnoreCase))
                { // השוואה בטוחה
                    currentAppointmentScore += specializationMatchWeight;
                }
                else
                {
                    currentAppointmentScore -= specializationMatchWeight; // עונש
                }
                currentAppointmentScore += (int)patient.Urgency * urgencyWeight; // דחיפות
                ExperienceLevel requiredLevel = GetRequiredExperienceLevel(patient.Urgency); // הנחה שקיים
                if (doctor.ExperienceLevel >= requiredLevel)
                {
                    currentAppointmentScore += experienceLevelWeight;
                    if (doctor.ExperienceLevel == requiredLevel)
                    {
                        currentAppointmentScore += hierarchyWeight;
                    }
                }
                else
                {
                    currentAppointmentScore -= experienceLevelWeight * 2; // עונש כבד
                }
                if (previousAssignments.TryGetValue(patientId, out int prevDoctorId) && prevDoctorId == doctorId)
                {
                    currentAppointmentScore += continuityOfCareWeight; // רציפות
                }
                double preferenceScoreContribution = CalculatePreferenceScoreForPair(doctor, patient) * preferenceMatchWeight; // הנחה שקיים
                currentAppointmentScore += preferenceScoreContribution; // העדפות

                // הוסף ניקוד מבוסס-זמן כאן...

                // לוג מפורט עבור כל תור (אופציונלי, רמת Verbose כדי לא להציף)
                Log.Verbose("FitnessFunction Detail: P{PatientId}-Dr{DoctorId} Appt Score: {AppointmentScore:F2} (Prefs: {PreferenceContribution:F2})",
                    patientId, doctorId, currentAppointmentScore, preferenceScoreContribution);

                assignmentScore += currentAppointmentScore; // צבירת ניקוד השיבוצים
            }
            Log.Debug("FitnessFunction: Total score from {AppointmentCount} assignments: {AssignmentScore:F2}", assignedAppointmentsCount, assignmentScore); // רמת Debug לסיכום השלב

            // --- 4. ניקוד איזון עומסים ---
            Log.Verbose("FitnessFunction: Scoring workload balance...");
            double totalWorkloadFactorSquaredSum = 0;
            int doctorsConsideredForWorkload = 0;
            // ודא שהמילון לא null לפני גישה ל-Keys
            if (DoctorsById != null)
            {
                foreach (var docId in DoctorsById.Keys) // עדיף אולי לעבור על currentWorkloads.Keys אם הוא מכיל את כל הרופאים הרלוונטיים? או להישאר עם DoctorsById כדי לכלול גם רופאים עם 0 עומס.
                {
                    if (DoctorsById.TryGetValue(docId, out Doctor doctor) && doctor.MaxWorkload > 0)
                    {
                        doctorsConsideredForWorkload++;
                        currentWorkloads.TryGetValue(docId, out int load); // מקבל 0 אם אין שיבוצים
                        double workloadFactor = (double)load / doctor.MaxWorkload;
                        totalWorkloadFactorSquaredSum += Math.Pow(workloadFactor, 2);
                        Log.Verbose("FitnessFunction Detail: Dr {DoctorId} WorkloadFactor: {WorkloadFactor:F2} (Load: {Load}, Max: {MaxLoad})",
                            docId, workloadFactor, load, doctor.MaxWorkload);
                    }
                }
            }
            else
            {
                Log.Warning("FitnessFunction: DoctorsById dictionary is null, cannot calculate workload balance.");
            }
            double avgWorkloadFactorSquared = doctorsConsideredForWorkload > 0 ? totalWorkloadFactorSquaredSum / doctorsConsideredForWorkload : 0;
            balanceScore = (1.0 - avgWorkloadFactorSquared) * workloadBalanceWeight * doctorsConsideredForWorkload;
            Log.Debug("FitnessFunction: Workload balance score contribution: {BalanceScore:F2} (AvgSqrFactor: {AvgSqrFactor:F3})", balanceScore, avgWorkloadFactorSquared);

            // --- 5. עונש על אי-שיבוץ מטופלים ---
            Log.Verbose("FitnessFunction: Calculating penalty for unassigned patients...");
            double unassignedPenalty = 0;
            int unassignedCount = 0;
            if (PatientsById != null) // בדיקת null
            {
                foreach (var patient in PatientsById.Values)
                {
                    // בדוק מטופלים ללא ניתוח שאינם מופיעים ב-Lookup של השיבוצים של ה-Schedule הנוכחי
                    if (!patient.NeedsSurgery && (schedule.PatientAssignmentLookup == null || !schedule.PatientAssignmentLookup.ContainsKey(patient.Id)))
                    {
                        unassignedCount++;
                        double penaltyForPatient = ((int)patient.Urgency * urgencyWeight * unassignedPatientPenaltyMultiplier);
                        unassignedPenalty += penaltyForPatient;
                        Log.Verbose("FitnessFunction Detail: P{PatientId} is unassigned. Penalty added: {Penalty:F2}", patient.Id, penaltyForPatient);
                    }
                }
            }
            else
            {
                Log.Warning("FitnessFunction: PatientsById dictionary is null, cannot calculate unassigned penalty.");
            }
            penaltyScore = -unassignedPenalty; // עונש הוא ניקוד שלילי

            if (unassignedCount > 0)
            {
                Log.Debug("FitnessFunction: Penalty for {UnassignedCount} unassigned patients: {PenaltyScore:F2}", unassignedCount, penaltyScore);
                // הלוג המותנה שהיה לך קודם יכול להישאר אם תרצה פחות רעש בלוגים:
                // if (currentGeneration % 50 == 0 || TerminationConditionMet()) { ... }
            }

            // --- 6. חישוב סופי והחזרה ---
            score = assignmentScore + balanceScore + penaltyScore;

            Log.Debug("FitnessFunction: Final Calculated Score: {FinalScore:F2} (Assignments: {AssignScore:F2}, Balance: {BalanceScore:F2}, Penalty: {PenaltyScore:F2})",
                score, assignmentScore, balanceScore, penaltyScore);

            schedule.FitnessScore = score; // שמירה במטמון
            return score;
        }

        private ExperienceLevel GetRequiredExperienceLevel(UrgencyLevel urgency)
        {
            switch (urgency)
            {
                case UrgencyLevel.High: return ExperienceLevel.Senior;
                case UrgencyLevel.Medium: return ExperienceLevel.Regular;
                default: return ExperienceLevel.Junior;
            }
        }
        /// <summary>
        /// Calculates a preference score for assigning a patient to a doctor, optimized to first check critical avoidance preferences.
        /// Returns 0.0 if an avoidance preference is violated. Otherwise, calculates an average score based on relevant 'Prefers' preferences.
        /// Returns a neutral score (0.5 or 0.8) if no relevant preferences apply or only non-violated 'Avoids' exist.
        /// </summary>
        /// <param name="doctor">The doctor.</param>
        /// <param name="patient">The patient.</param>
        /// <returns>A score typically between 0.0 (violation) and 1.0 (perfect preference match).</returns>
        private double CalculatePreferenceScoreForPair(Doctor doctor, Patient patient)
        {
            // 1. בדיקה מהירה של הפרות הימנעות (Avoids)
            //    הנחה שקיימת מתודה ב-Doctor שבודקת זאת במהירות בעזרת ה-HashSets
            if (doctor.ViolatesAvoidancePreference(patient)) // <<--- קריאה למתודה המהירה
            {
                Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId} avoids P{PatientId}. Score: 0.0", doctor.Id, patient.Id);
                return 0.0; // הפרה קריטית -> הציון הנמוך ביותר
            }

            // אם הגענו לכאן, אין הפרת הימנעות ידועה.

            // 2. טיפול במקרה שאין העדפות כלל
            if (doctor.Preferences == null || !doctor.Preferences.Any())
            {
                Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId} has no preferences for P{PatientId}. Score: 0.5 (Neutral)", doctor.Id, patient.Id);
                return 0.5; // ציון ניטרלי אם אין העדפות
            }

            // 3. חישוב ציון המבוסס *רק* על העדפות "מעדיף" (Prefers)
            double totalPrefersScore = 0;
            int relevantPrefersCount = 0;
            bool doctorHasAnyPrefersPreferences = false; // לבדוק אם בכלל קיימות העדפות 'Prefers'
            bool isRelevantPreference=false;
            foreach (var preference in doctor.Preferences)
            {
                // דלג על העדפות שאינן מסוג 'Prefers'
                if (preference.Direction != PreferenceDirection.Prefers) continue;

                doctorHasAnyPrefersPreferences = true; // מצאנו לפחות העדפת 'Prefers' אחת
                double currentPrefScore = -1; // ערך התחלתי לבדיקה אם ההעדפה רלוונטית
                

                try
                {
                    bool match = false;
                    isRelevantPreference = false; // נאתחל כאן כדי להיות ברורים

                    // השתמש ב-switch על סוג ההעדפה
                    switch (preference.Type)
                    {
                        case PreferenceType.PatientComplexity:
                            // בדוק אם יש ערך מספרי להעדפה הזו
                            if (preference.LevelValue.HasValue)
                            {
                                isRelevantPreference = true;
                                // בצע את ההשוואה
                                match = (patient.ComplexityLevel == (ComplexityLevel)preference.LevelValue.Value);
                            }
                            break; // חשוב לסיים כל case ב-break

                        case PreferenceType.PatientUrgency:
                            // בדוק אם יש ערך מספרי להעדפה הזו
                            if (preference.LevelValue.HasValue)
                            {
                                isRelevantPreference = true;
                                // בצע את ההשוואה
                                match = (patient.Urgency == (UrgencyLevel)preference.LevelValue.Value);
                            }
                            break;

                        case PreferenceType.PatientCondition:
                            // בדוק אם יש ערך טקסטואלי (לא ריק) להעדפה הזו
                            if (!string.IsNullOrEmpty(preference.ConditionValue))
                            {
                                isRelevantPreference = true;
                                // בצע את ההשוואה (לא תלוית רישיות)
                                match = (patient.Condition != null && patient.Condition.Equals(preference.ConditionValue, StringComparison.OrdinalIgnoreCase));
                            }
                            break;

                            // (אופציונלי) אפשר להוסיף default אם יש עוד סוגי העדפות שלא מטופלים
                            // default:
                            //     Log.Warning("CalculatePreferenceScoreForPair Detail: Unsupported PreferenceType encountered: {PreferenceType}", preference.Type);
                            //     break;
                    }

                    // החלק הזה נשאר זהה: אם ההעדפה נמצאה רלוונטית, עדכן את הסופר ואת הציון
                    if (isRelevantPreference)
                    {
                        relevantPrefersCount++;
                        // ניקוד עבור 'Prefers': 1.0 להתאמה, 0.2 לאי-התאמה
                        currentPrefScore = match ? 1.0 : 0.2;
                        Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId}/P{PatientId} - Prefers {PrefType}={PrefValue}, Match={Match}, Score={Score}",
                            doctor.Id, patient.Id, preference.Type, preference.LevelValue ?? (object)preference.ConditionValue ?? "N/A", match, currentPrefScore);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"ERROR evaluating 'Prefers' preference Dr{doctor.Id}/P{patient.Id}: {ex.Message}");
                    // במקרה של שגיאה, לא נכלול את ההעדפה בחישוב הממוצע
                    if (isRelevantPreference && relevantPrefersCount > 0) relevantPrefersCount--; // תקן את הספירה אם השגיאה קרתה אחרי העלאת הסופר
                    currentPrefScore = -1; // ודא שלא יתווסף לסיכום
                }

                if (currentPrefScore >= 0) // הוסף רק אם ההעדפה עובדה בהצלחה והייתה רלוונטית
                {
                    totalPrefersScore += currentPrefScore;
                }
            } // סוף לולאת ההעדפות

            // --- 4. חישוב הציון הסופי ---

            if (!doctorHasAnyPrefersPreferences)
            {
                // אם לרופא היו רק העדפות 'Avoids' (שלא הופרו), זה מצב טוב
                Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId}/P{PatientId} - No 'Prefers' criteria found (only non-violated Avoids?). Score: 0.8", doctor.Id, patient.Id);
                return 0.8; // ציון טוב, זהה להצלחה בהימנעות
            }
            else if (relevantPrefersCount == 0)
            {
                // אם היו העדפות 'Prefers', אבל אף אחת לא הייתה רלוונטית למטופל הספציפי הזה
                Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId}/P{PatientId} - 'Prefers' criteria exist but none were relevant. Score: 0.5 (Neutral)", doctor.Id, patient.Id);
                return 0.5; // ציון ניטרלי
            }
            else
            {
                // החזר את הציון הממוצע של העדפות ה-'Prefers' הרלוונטיות
                double finalScore = totalPrefersScore / relevantPrefersCount;
                Log.Verbose("CalculatePreferenceScoreForPair Detail: Dr {DoctorId}/P{PatientId} - Final Avg 'Prefers' Score: {FinalScore:F2}", doctor.Id, patient.Id, finalScore);
                return finalScore;
            }
        }

        /// <summary>
        /// Generates the initial population of candidate schedule solutions.
        /// Creates the specified number of 'Schedule' instances, typically in parallel,
        /// by calling GenerateInitialSchedule for each one.
        /// </summary>
        private List<Schedule> GeneratePopulation(int populationSize)
        {
            ConcurrentBag<Schedule> populationBag = new ConcurrentBag<Schedule>();
            Parallel.For(0, populationSize, i =>
            {
                var schedule = GenerateInitialSchedule();
                populationBag.Add(schedule);
            });
            return populationBag.ToList();
        }

        /// <summary>
        /// Creates a single, randomized initial schedule solution. Attempts to assign non-surgery patients
        /// to suitable doctors in available time slots using a randomized greedy approach,
        /// ensuring the generated schedule is internally consistent (no double-booking for a doctor).
        /// </summary>
        private Schedule GenerateInitialSchedule()
        {
            var schedule = new Schedule(); // יצירת לוח זמנים ריק
            Random localRnd = _localRandom.Value ?? new Random(); // שימוש ב-Random מקומי (thread-safe)

            // קבל את כל המטופלים שניתן לשבץ (לא ניתוח) וערבב את הסדר שלהם
            var allSchedulablePatients = PatientsById.Values
                                            .Where(p => !p.NeedsSurgery)
                                            .OrderBy(_ => localRnd.Next()) // ערבוב סדר המטופלים
                                            .ToList();

            int assignedCount = 0;
            Log.Debug("GenerateInitialSchedule: Starting initial assignment for {PatientCount} patients.", allSchedulablePatients.Count);

            // עבור על כל מטופל לפי הסדר המעורבב
            foreach (var patient in allSchedulablePatients)
            {
                bool patientAssigned = false;

                // 1. מצא את כל הרופאים שמתאימים *באופן כללי* למטופל (לפי IsSuitableFor)
                var suitableDoctors = DoctorsById.Values
                                        .Where(d => d.IsSuitableFor(patient))
                                        .ToList();

                // אם אין רופאים מתאימים בכלל, דלג למטופל הבא
                if (!suitableDoctors.Any())
                {
                    Log.Verbose("InitialAssign Skip: P{PatientId} - No suitable doctors found at all.", patient.Id);
                    continue;
                }

                // 2. חלק את הרופאים המתאימים לשתי קבוצות וערבב כל קבוצה
                var matchingSpecialists = suitableDoctors
                    .Where(d => string.Equals(d.Specialization, patient.RequiredSpecialization, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => localRnd.Next()) // ערבב את המומחים התואמים
                    .ToList();

                var otherSuitableDoctors = suitableDoctors
                    .Where(d => !string.Equals(d.Specialization, patient.RequiredSpecialization, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(_ => localRnd.Next()) // ערבב את שאר המתאימים
                    .ToList();

                Log.Verbose("InitialAssign P{PatientId}: Trying {MatchCount} matching specialists first, then {OtherCount} others.",
                            patient.Id, matchingSpecialists.Count, otherSuitableDoctors.Count);

                // 3. נסה לשבץ קודם אצל מומחה תואם
                foreach (var doctor in matchingSpecialists)
                {
                    // ערבב את סדר המשבצות הפנויות של הרופא הזה
                    var shuffledSlots = doctor.AvailabilitySchedule.OrderBy(_ => localRnd.Next());

                    foreach (var potentialSlot in shuffledSlots)
                    {
                        // בדוק אם המשבצת מספיק ארוכה והאם היא פנויה בלוח הזמנים הנוכחי (שנבנה בהדרגה)
                        if (potentialSlot.Duration >= patient.EstimatedAppointmentDuration &&
                            schedule.IsSlotFree(doctor.Id, potentialSlot.DayOfWeek, potentialSlot.StartTime))
                        {
                            // מצאנו שיבוץ! צור את התור והוסף אותו ללוח הזמנים
                            var appointment = new ScheduledAppointment
                            {
                                PatientId = patient.Id,
                                DoctorId = doctor.Id,
                                DayOfWeek = potentialSlot.DayOfWeek,
                                StartTime = potentialSlot.StartTime,
                                Duration = patient.EstimatedAppointmentDuration
                            };

                            if (schedule.TryAddAppointment(appointment)) // המתודה שמעדכנת גם את ה-Lookups הפנימיים
                            {
                                patientAssigned = true;
                                assignedCount++;
                                Log.Verbose("InitialAssign P{PatientId}: Assigned to matching specialist Dr {DoctorId} at {Day}/{Time}",
                                           patient.Id, doctor.Id, potentialSlot.DayOfWeek, potentialSlot.StartTime);
                                break; // מצאנו שיבוץ למטופל זה, עבור למטופל הבא
                            }
                            // אם TryAddAppointment נכשל (נדיר), נמשיך לנסות משבצות/רופאים אחרים
                        }
                    } // סוף לולאת המשבצות
                    if (patientAssigned) break; // אם שובץ, צא מלולאת הרופאים
                } // סוף לולאת המומחים התואמים

                // 4. אם לא הצלחנו לשבץ אצל מומחה תואם, נסה אצל שאר הרופאים המתאימים
                if (!patientAssigned)
                {
                    Log.Verbose("InitialAssign P{PatientId}: No slot found with matching specialists. Trying other suitable doctors...", patient.Id);
                    foreach (var doctor in otherSuitableDoctors)
                    {
                        var shuffledSlots = doctor.AvailabilitySchedule.OrderBy(_ => localRnd.Next());
                        foreach (var potentialSlot in shuffledSlots)
                        {
                            if (potentialSlot.Duration >= patient.EstimatedAppointmentDuration &&
                                schedule.IsSlotFree(doctor.Id, potentialSlot.DayOfWeek, potentialSlot.StartTime))
                            {
                                var appointment = new ScheduledAppointment
                                {
                                    PatientId = patient.Id,
                                    DoctorId = doctor.Id,
                                    DayOfWeek = potentialSlot.DayOfWeek,
                                    StartTime = potentialSlot.StartTime,
                                    Duration = patient.EstimatedAppointmentDuration
                                };

                                if (schedule.TryAddAppointment(appointment))
                                {
                                    patientAssigned = true;
                                    assignedCount++;
                                    Log.Verbose("InitialAssign P{PatientId}: Assigned to non-matching specialist Dr {DoctorId} at {Day}/{Time}",
                                               patient.Id, doctor.Id, potentialSlot.DayOfWeek, potentialSlot.StartTime);
                                    break; // מצאנו שיבוץ
                                }
                            }
                        } // סוף לולאת המשבצות
                        if (patientAssigned) break; // אם שובץ, צא מלולאת הרופאים
                    } // סוף לולאת שאר המתאימים
                }

                // אם גם אחרי כל הניסיונות לא מצאנו שיבוץ למטופל
                if (!patientAssigned)
                {
                    Log.Debug("InitialAssign Fail: P{PatientId} ({Spec}, Complex:{Comp}) - No suitable Doctor+Slot found.",
                              patient.Id, patient.RequiredSpecialization, patient.ComplexityLevel);
                }
            } // סוף לולאת המטופלים

            // דווח כמה הצלחנו לשבץ בשלב ההתחלתי
            Log.Information("GenerateInitialSchedule: Assignment attempt complete. Assigned {AssignedCount}/{TotalCount} patients initially.",
                        assignedCount, allSchedulablePatients.Count);
            return schedule; // החזר את לוח הזמנים שנוצר
        }
        #endregion

    }
}
