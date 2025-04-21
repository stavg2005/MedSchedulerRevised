using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Models;
using MySql.Data.MySqlClient;
using Serilog;
namespace DataAccessLayer
{
    public class DoctorRepository: BaseRepository<Doctor, int>
    {

            // Constructor passes connection string to the base class
            public DoctorRepository(string connectionString) : base(connectionString) { }

            public override async Task AddAsync(Doctor entity)
            {
                string sql = @"INSERT INTO Doctors (Name, Specialization, Workload, MaxWorkload, ExperienceLevel /*, IsSurgeon etc. */)
                       VALUES (@Name, @Specialization, @Workload, @MaxWorkload, @ExperienceLevel /*, @IsSurgeon etc. */);";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    await connection.ExecuteAsync(sql, entity); // Dapper maps properties to parameters
                }
            }

            public override async Task<bool> DeleteAsync(int id)
            {
                string sql = "DELETE FROM Doctors WHERE Id = @Id;";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                    return rowsAffected > 0;
                }
            }

            public override async Task<List<Doctor>> GetAllAsync()
            {
                // Simple GetAll - for complex scenarios like fetching related data,
                // you might override this or create a specific method like GetAllWithDetailsAsync
                string sql = "SELECT Id, Name, Specialization, Workload, MaxWorkload, ExperienceLevel FROM Doctors;";
                using (var connection = await CreateOpenConnectionAsync())
                {
                List<Doctor> d = (await connection.QueryAsync<Doctor>(sql)).ToList();
                    return  d ;
                }
            }

        // Example of fetching related data (similar to previous implementation but now in DoctorRepository)
        public async Task<List<Doctor>> GetAllWithDetailsAsync()
        {
            // SQL to get all core doctor details
            string doctorSql = @"
            SELECT Id, Name, Specialization, Workload, MaxWorkload, ExperienceLevel
            FROM Doctors;"; // Add other base Doctor fields if needed

            // SQL to get all relevant preferences
            string preferenceSql = @"
            SELECT DoctorId, PreferenceType, PreferenceDirection, LevelValue, ConditionValue
            FROM DoctorPreferences;"; // Maybe add WHERE DoctorId IN (SELECT Id FROM Doctors) if needed

            // SQL to get all relevant availability ranges
            string availabilitySql = @"
            SELECT DoctorID, DayOfWeek, StartTime, EndTime
            FROM DoctorAvailabilityRanges"; // Maybe add WHERE DoctorId IN (SELECT Id FROM Doctors) if needed

            try
            {
                using (var connection = await CreateOpenConnectionAsync()) // Assumes method from BaseRepository
                {
                    // 1. Fetch all core Doctor objects
                    var doctors = (await connection.QueryAsync<Doctor>(doctorSql)).ToList();
                    if (!doctors.Any())
                    {
                        Log.Information("GetAllDoctorsWithDetailsAsync: No doctors found in the database.");
                        return new List<Doctor>(); // Return empty list
                    }

                    // Create a lookup dictionary for easy access by ID
                    var doctorLookup = doctors.ToDictionary(d => d.Id);

                    // 2. Fetch all Preferences and populate Doctor objects
                    Log.Debug("Fetching doctor preferences...");
                    var preferencesData = await connection.QueryAsync<DoctorPreference>(preferenceSql);
                    foreach (var prefDto in preferencesData)
                    {
                        if (doctorLookup.TryGetValue(prefDto.DoctorId, out Doctor doc))
                        {
                            doc.Preferences = new List<DoctorPreference>(); // Initialize list if null
                                                                            // Map DTO to your actual DoctorPreference model
                            doc.Preferences.Add(MapPreferenceDtoToModel(prefDto));
                        }
                        else
                        {
                            Log.Warning("Found preference for unknown DoctorID {DoctorId}", prefDto.DoctorId);
                        }
                    }
                    Log.Debug("Populated preferences for {Count} doctors.", doctorLookup.Count);


                    // 3. Fetch all Availability Ranges and populate Doctor objects
                    Log.Debug("Fetching doctor availability ranges...");
                    var availabilityRangesData = await connection.QueryAsync<AvailabilityTimeRange>(availabilitySql);
                    foreach (var availDto in availabilityRangesData)
                    {
                        if (doctorLookup.TryGetValue(availDto.DoctorId, out Doctor associatedDoctor))
                        {
                            if(associatedDoctor.AvailabilitySchedule == null)
                                associatedDoctor.AvailabilitySchedule = new List<AvailabilityTimeRange>(); // Initialize list if null

                            // Create the actual AvailabilityTimeRange object
                            var timeRange = new AvailabilityTimeRange
                            {
                                DayOfWeek = availDto.DayOfWeek,
                                StartTime = availDto.StartTime,
                                EndTime = availDto.EndTime,
                                // Calculate Duration if it's a property in AvailabilityTimeRange
                                // Duration = availDto.EndTime - availDto.StartTime
                                // Map other properties if needed
                            };

                            // *** Add EACH range to the list ***
                            associatedDoctor.AvailabilitySchedule.Add(timeRange);
                        }
                        else
                        {
                            Log.Warning("Found availability range for unknown DoctorID {DoctorId}", availDto.DoctorId);
                        }
                    }
                    Log.Debug("Populated availability for {Count} doctors.", doctorLookup.Count);


                    // 4. Initialize internal lookups within Doctor objects (e.g., for preferences)
                    //    Do this AFTER related data (like Preferences) is loaded.
                    Log.Debug("Initializing internal doctor lookups (e.g., avoidance)...");
                    foreach (var doc in doctors)
                    {
                        doc.InitializeAvoidanceLookups(); // Assumes this method exists in Doctor class
                    }
                    Log.Debug("Internal doctor lookups initialized.");


                    Log.Information("GetAllDoctorsWithDetailsAsync: Successfully retrieved {DoctorCount} doctors with details.", doctors.Count);
                    return doctors;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetAllDoctorsWithDetailsAsync");
                throw; // Re-throw the exception to allow higher layers to handle it
            }
        }



        private DoctorPreference MapPreferenceDtoToModel(DoctorPreference dto)
        {
            // This is just an example, adjust based on your actual DoctorPreference class
            return new DoctorPreference
            {
                // Assuming DoctorPreference itself does NOT store DoctorId
                Type = dto.Type,
                Direction = dto.Direction,
                LevelValue = dto.LevelValue,
                ConditionValue = dto.ConditionValue
            };
        }

        public override async Task<Doctor> GetByIdAsync(int id)
            {
                // Fetching related data within GetById might require multi-mapping or separate queries
                string sql = "SELECT Id, Name, Specialization, Workload, MaxWorkload, ExperienceLevel FROM Doctors WHERE Id = @Id;";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // For simplicity, this only gets the base doctor.
                    // Getting related data would require more complex query/mapping here.
                    return await connection.QuerySingleOrDefaultAsync<Doctor>(sql, new { Id = id });
                }
            }

            public override async Task<bool> UpdateAsync(Doctor entity)
            {
                string sql = @"UPDATE Doctors
                       SET Name = @Name,
                           Specialization = @Specialization,
                           Workload = @Workload,
                           MaxWorkload = @MaxWorkload,
                           ExperienceLevel = @ExperienceLevel
                           /*, IsSurgeon = @IsSurgeon etc. */
                       WHERE Id = @Id;";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    int rowsAffected = await connection.ExecuteAsync(sql, entity);
                    return rowsAffected > 0;
                }
            }

            // --- Specific Methods for DoctorRepository ---
            public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
            {
                string sql = "SELECT Id, Name, Specialization, Workload, MaxWorkload, ExperienceLevel FROM Doctors WHERE Specialization = @Spec;";
                using (var connection = await CreateOpenConnectionAsync())
                {
                    return await connection.QueryAsync<Doctor>(sql, new { Spec = specialization });
                }
            }
        }
}
