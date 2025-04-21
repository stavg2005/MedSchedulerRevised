using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class Doctor
    {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Specialization { get; set; } // Keep as string for now, but consider alternatives (see comment above)
            public int Workload { get; set; } // Current number of patients assigned. Consider how this is updated/loaded.
            public int MaxWorkload { get; set; } // Maximum number of patients the doctor can handle
            public ExperienceLevel ExperienceLevel { get; set; } // Using Enum

            // Renamed and initialized
            public List<int> CurrentPatientIds { get; set; } = new List<int>();
            public List<DoctorPreference> Preferences { get; set; } = new List<DoctorPreference>();
            public List<int> PreviousPatients { get; set; } = new List<int>(); // IDs of patients previously treated

            public List<AvailabilityTimeRange> AvailabilitySchedule { get; set; } = new List<AvailabilityTimeRange>();

            // Calculated property for workload percentage
            public double WorkloadPercentage => MaxWorkload > 0 ? (double)Workload / MaxWorkload * 100 : 0;

            // Check if doctor can take a new patient based on workload
            public virtual bool CanAcceptPatient() => Workload < MaxWorkload;

            // Pre-processed lookups for avoidance preferences
            private HashSet<ComplexityLevel> _avoidsComplexityLevels = null;
            private HashSet<UrgencyLevel> _avoidsUrgencyLevels = null;
            private HashSet<string> _avoidsConditions = null; // Uses case-insensitive comparer
            private bool _avoidanceLookupsInitialized = false;

            /// <summary>
            /// IMPORTANT: Call this method AFTER Preferences have been loaded for the Doctor,
            /// and BEFORE calling IsSuitableFor for the first time.
            /// Typically called in the Repository after fetching Doctor data.
            /// </summary>
            public void InitializeAvoidanceLookups()
            {
                if (_avoidanceLookupsInitialized) return;

                if (Preferences != null && Preferences.Any())
                {
                    // Initialize with potentially slightly larger capacity if many preferences are expected
                    _avoidsComplexityLevels = new HashSet<ComplexityLevel>();
                    _avoidsUrgencyLevels = new HashSet<UrgencyLevel>();
                    _avoidsConditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var preference in Preferences)
                    {
                        if (preference.Direction == PreferenceDirection.Avoids)
                        {
                            try // Keep try-catch here as enum casting or accessing values might fail with bad data
                            {
                                if (preference.Type == PreferenceType.PatientComplexity && preference.LevelValue.HasValue)
                                    _avoidsComplexityLevels.Add((ComplexityLevel)preference.LevelValue.Value);
                                else if (preference.Type == PreferenceType.PatientUrgency && preference.LevelValue.HasValue)
                                    _avoidsUrgencyLevels.Add((UrgencyLevel)preference.LevelValue.Value);
                                else if (preference.Type == PreferenceType.PatientCondition && !string.IsNullOrEmpty(preference.ConditionValue))
                                    _avoidsConditions.Add(preference.ConditionValue);
                            }
                            catch (Exception ex)
                            {
                                // TODO: Replace Console.WriteLine with proper logging (e.g., inject ILogger)
                                //Console.WriteLine($"ERROR initializing avoidance lookup for Dr {this.Id}, PrefType:{preference.Type}, Value:'{preference.LevelValue ?? (object)preference.ConditionValue ?? "N/A"}'. {ex.Message}");
                                // Depending on severity, you might want to re-throw or handle differently
                            }
                        }
                    }

                    // Optional: Null out empty sets
                    if (_avoidsComplexityLevels.Count == 0) _avoidsComplexityLevels = null;
                    if (_avoidsUrgencyLevels.Count == 0) _avoidsUrgencyLevels = null;
                    if (_avoidsConditions.Count == 0) _avoidsConditions = null;
                }

                _avoidanceLookupsInitialized = true;
            }

            /// <summary>
            /// Checks if the doctor is suitable for the given patient based on various criteria.
            /// Assumes InitializeAvoidanceLookups() has been called previously.
            /// </summary>
            public virtual bool IsSuitableFor(Patient patient)
            {
                if (patient == null) return false; // Basic null check

                // 1. Specialization Match (Case-insensitive and null-safe)
                bool specializationOk = string.Equals(this.Specialization, patient.RequiredSpecialization, StringComparison.OrdinalIgnoreCase);

                // 2. Capability vs. Requirement Check (Experience vs. Complexity)
                // Removed try-catch assuming enum values are valid; handle potential issues during data loading/validation instead.
                bool complexityCapabilityOk = ((int)this.ExperienceLevel >= (int)patient.ComplexityLevel);

                // 3. Experience vs. Urgency Check
                // Assuming this rule always applies. If it's configurable, the configuration should ideally be passed in.
                ExperienceLevel requiredExpForUrgency = GetRequiredExperienceLevel(patient.Urgency);
                bool urgencyExperienceOk = (this.ExperienceLevel >= requiredExpForUrgency);

                // 4. Preferences Check (Avoidance)
                bool preferenceOk = CheckAvoidancePreferences(patient);

                // --- Calculate Overall Suitability ---
                bool overallSuitability = specializationOk
                                          && complexityCapabilityOk
                                          && urgencyExperienceOk // Included the urgency check directly
                                          && preferenceOk;

                // --- Optional Debug Logging ---
                if (!overallSuitability)
                {
                    // Construct a detailed message showing the results of each check
                    string logMessage =
                        $"DEBUG IsSuitableFor: Dr {this.Id} ({this.Specialization}, ExpLevel:{this.ExperienceLevel})" +
                        $" checking P{patient.Id} (ReqSpec:{patient.RequiredSpecialization}, ComplexLevel:{patient.ComplexityLevel}, Urg:{patient.Urgency}, ReqExpForUrg:{requiredExpForUrgency})" +
                        $" -> FAILED. " +
                        $"Reasons: [SpecOK:{specializationOk}, ComplexCapOK:{complexityCapabilityOk}, UrgExpOK:{urgencyExperienceOk}, PrefOK:{preferenceOk}]";

                    // TODO: Replace Console.WriteLine with proper logging (e.g., logger.LogDebug(logMessage))
                    //Console.WriteLine(logMessage);
                }
                // --- End Debug Logging ---

                return overallSuitability;
            }

            /// <summary>
            /// Helper to check ONLY avoidance preferences using pre-calculated lookups.
            /// </summary>
            private bool CheckAvoidancePreferences(Patient patient)
            {
                if (!_avoidanceLookupsInitialized)
                {
                    // TODO: Replace Console.WriteLine with proper logging (logger.LogWarning(...))
                    Console.WriteLine($"WARN: Avoidance lookups not initialized for Dr {this.Id} during suitability check for P{patient.Id}. Assuming preference OK.");
                    // Decide on fallback behavior: true (assume OK), false (assume NOT OK), or call InitializeAvoidanceLookups() again (less ideal).
                    return true; // Returning true assumes no avoidance if not initialized.
                }

                // Perform fast O(1) average time lookups using the HashSets
                if (_avoidsComplexityLevels != null && _avoidsComplexityLevels.Contains(patient.ComplexityLevel))
                {
                    return false; // Avoids this complexity
                }

                if (_avoidsUrgencyLevels != null && _avoidsUrgencyLevels.Contains(patient.Urgency))
                {
                    return false; // Avoids this urgency
                }

                // Check condition only if patient condition is not null/empty and the avoidance set exists
                if (_avoidsConditions != null && !string.IsNullOrEmpty(patient.Condition) && _avoidsConditions.Contains(patient.Condition))
                {
                    return false; // Avoids this condition
                }

                return true; // No avoidance criteria matched
            }

            /// <summary>
            /// Updates the Workload based on the count of CurrentPatientIds.
            /// Call this after the CurrentPatientIds list is populated or modified.
            /// Alternatively, consider managing Workload directly or calculating it differently.
            /// </summary>
            public void SetCurrentWorkLoad()
            {
                // Ensure the list isn't null, though initialization should prevent this.
                Workload = CurrentPatientIds?.Count ?? 0;
            }

            /// <summary>
            /// Helper to determine required experience based on patient urgency.
            /// </summary>
            private ExperienceLevel GetRequiredExperienceLevel(UrgencyLevel urgency)
            {
                switch (urgency)
                {
                    case UrgencyLevel.High: return ExperienceLevel.Senior;
                    case UrgencyLevel.Medium: return ExperienceLevel.Regular;
                    // Default includes Low and potentially any other unforeseen values
                    default: return ExperienceLevel.Junior;
                }
            }

            public bool ViolatesAvoidancePreference(Patient patient)
            {
                if (!_avoidanceLookupsInitialized)
            {
                // Log Warning: Lookups not initialized! Decide on fallback (e.g., assume no violation)
                return false; // Assuming no violation if not initialized (safer might be true?)
            }
                    if (_avoidsComplexityLevels != null && _avoidsComplexityLevels.Contains(patient.ComplexityLevel)) return true;
                    if (_avoidsUrgencyLevels != null && _avoidsUrgencyLevels.Contains(patient.Urgency)) return true;
                    if (_avoidsConditions != null && !string.IsNullOrEmpty(patient.Condition) && _avoidsConditions.Contains(patient.Condition)) return true;
                    return false; // No avoidance violation found
        }
    }
}
