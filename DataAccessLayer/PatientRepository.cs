using Dapper;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace DataAccessLayer
{
    public class PatientRepository : BaseRepository<Patient,int>
    {


        public PatientRepository(string connectionString) : base(connectionString)
        {
            
        }
        /// <summary>
        /// מוסיף מטופל חדש למסד הנתונים.
        /// הערה: כרגע לא מטפל בשמירת רשימת PreviousDoctors.
        /// </summary>
        public override async Task AddAsync(Patient entity)
        {
            // ודא שהמודל תקין לפני הוספה
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // הנחה שעמודת משך הזמן ב-DB היא INT של דקות
            string sql = @"
            INSERT INTO Patients (Name, `Condition`, Urgency, RequiredSpecialization, NeedsSurgery,
                                  AdmissionDate, ScheduledSurgeryDate, AssignedDoctorId, AssignedSurgeonId,
                                  ComplexityLevel, EstimatedTreatmentTime, RequiredProcedureId,
                                  AssignedOperatingRoomId, EstimatedAppointmentDurationMinutes)
            VALUES (@Name, @Condition, @Urgency, @RequiredSpecialization, @NeedsSurgery,
                    @AdmissionDate, @ScheduledSurgeryDate, @AssignedDoctorId, @AssignedSurgeonId,
                    @ComplexityLevel, @EstimatedTreatmentTime, @RequiredProcedureId,
                    @AssignedOperatingRoomId, @EstimatedAppointmentDurationMinutes);";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // הכנת פרמטרים, כולל המרת TimeSpan לדקות
                    var parameters = new
                    {
                        entity.Name,
                        entity.Condition,
                        Urgency = (int)entity.Urgency, // המרה ל-int אם מאוחסן כ-int ב-DB
                        entity.RequiredSpecialization,
                        entity.NeedsSurgery,
                        entity.AdmissionDate,
                        entity.ScheduledSurgeryDate,
                        entity.AssignedDoctorId,
                        entity.AssignedSurgeonId,
                        ComplexityLevel = (int)entity.ComplexityLevel, // המרה ל-int
                        entity.EstimatedTreatmentTime,
                        entity.RequiredProcedureId,
                        entity.AssignedOperatingRoomId,
                        // המרת TimeSpan לדקות
                        EstimatedAppointmentDurationMinutes = (int)entity.EstimatedAppointmentDuration.TotalMinutes
                    };
                    await connection.ExecuteAsync(sql, parameters);
                    // אופציונלי: אם תרצה לקבל את ה-ID החדש שנוצר (AUTO_INCREMENT):
                    // entity.Id = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();");
                    Log.Debug("Patient added successfully: {PatientName} (ID might be generated)", entity.Name);

                    // TODO: הוספת לוגיקה לשמירת PreviousDoctors בטבלת PatientDoctorHistory אם נדרש,
                    // ייתכן שיצריך שימוש ב-entity.Id שנוצר וטרנזקציה.
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error adding Patient {PatientName}", entity?.Name ?? "NULL");
                throw; // זרוק את החריגה הלאה כדי שהשכבה העליונה תדע על הכשל
            }
        }

        /// <summary>
        /// מוחק מטופל ממסד הנתונים לפי ה-ID שלו.
        /// הערה: כרגע לא מטפל במחיקת רשומות קשורות (כמו היסטוריה).
        /// </summary>
        public override async Task<bool> DeleteAsync(int id)
        {
            string sql = "DELETE FROM Patients WHERE Id = @Id;";
            // TODO: הוסף מחיקה מטבלות קשורות כמו PatientDoctorHistory, רצוי בטרנזקציה
            // string historySql = "DELETE FROM PatientDoctorHistory WHERE PatientId = @Id;";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // מומלץ לבצע מחיקות בטרנזקציה
                    // using (var transaction = await ((MySqlConnection)connection).BeginTransactionAsync()) { ... }
                    int rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
                    // await connection.ExecuteAsync(historySql, new { Id = id }); // בתוך טרנזקציה
                    // await transaction.CommitAsync();

                    if (rowsAffected > 0) Log.Debug("Patient deleted successfully: ID {PatientId}", id);
                    else Log.Warning("Attempted to delete Patient with ID {PatientId}, but no rows were affected.", id);

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting Patient with ID {PatientId}", id);
                throw;
            }
        }

        /// <summary>
        /// מאחזר את כל המטופלים ממסד הנתונים, כולל היסטוריית הרופאים שלהם.
        /// </summary>
        public override async Task<List<Patient>> GetAllAsync()
        {
            // שאילתה למשיכת כל המטופלים
            string patientsSql = @"
            SELECT Id, Name, `Condition`, Urgency, RequiredSpecialization, NeedsSurgery,
                   AdmissionDate, ScheduledSurgeryDate, AssignedDoctorId, AssignedSurgeonId,
                   ComplexityLevel, EstimatedTreatmentTime, RequiredProcedureId,
                   AssignedOperatingRoomId, EstimatedAppointmentDurationMinutes
            FROM Patients;";

            // שאילתה למשיכת *כל* רשומות ההיסטוריה
            string historySql = "SELECT PatientId, DoctorId FROM PatientDoctorHistory;";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // 1. משוך את כל המטופלים
                    // שימוש ב-dynamic או DTO אם יש חוסר התאמה ישיר למודל Patient (כמו עם הדקות)
                    var patientData = await connection.QueryAsync(patientsSql);

                    // 2. משוך את כל ההיסטוריה למבנה נתונים יעיל (Lookup)
                    var historyRecords = await connection.QueryAsync<PatientDoctorHistoryDto>(historySql);
                    // יצירת Lookup: מפתח = PatientId, ערך = List<DoctorId>
                    var historyLookup = historyRecords
                        .GroupBy(h => h.PatientId)
                        .ToDictionary(g => g.Key, g => g.Select(h => h.DoctorId).ToList());

                    // 3. הרכבת אובייקטי Patient עם ההיסטוריה
                    var patients = patientData.Select(p =>
                    {
                        var patient = new Patient
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Condition = p.Condition,
                            Urgency = (UrgencyLevel)p.Urgency, // המרה מ-int
                            RequiredSpecialization = p.RequiredSpecialization,
                            NeedsSurgery = p.NeedsSurgery,
                            AdmissionDate = p.AdmissionDate,
                            ScheduledSurgeryDate = p.ScheduledSurgeryDate,
                            AssignedDoctorId = p.AssignedDoctorId,
                            AssignedSurgeonId = p.AssignedSurgeonId,
                            ComplexityLevel = (ComplexityLevel)p.ComplexityLevel, // המרה מ-int
                            EstimatedTreatmentTime =  (p.EstimatedTreatmentTime == null || p.EstimatedTreatmentTime is DBNull)
                             ? (double?)null // אם הערך הוא NULL ב-DB, השם null
                             : (double)((decimal)p.EstimatedTreatmentTime), // אחרת, בצע המרה מפורשת: dynamic -> decimal -> double,
                            RequiredProcedureId = p.RequiredProcedureId,
                            AssignedOperatingRoomId = p.AssignedOperatingRoomId,
                            // המרת דקות ל-TimeSpan
                            EstimatedAppointmentDuration = TimeSpan.FromMinutes((int?)p.EstimatedAppointmentDurationMinutes ?? 0)
                        };

                        // אכלוס רשימת הרופאים הקודמים מה-Lookup
                        if (historyLookup.TryGetValue(patient.Id, out var previousDoctors))
                        {
                            patient.PreviousDoctors = previousDoctors;
                        }
                        else
                        {
                            patient.PreviousDoctors = new List<int>(); // אתחול לרשימה ריקה אם אין היסטוריה
                        }
                        return patient;
                    }).ToList(); // המרה לרשימה בסוף

                    Log.Debug("Retrieved {PatientCount} patients with their history.", patients.Count);
                    return patients;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting all Patients.");
                throw;
            }
        }


        /// <summary>
        /// מאחזר מטופל ספציפי לפי ה-ID שלו, כולל היסטוריית הרופאים שלו.
        /// </summary>
        public override async Task<Patient> GetByIdAsync(int id) // Changed to Patient? for consistency
        {
            string patientSql = @"
            SELECT Id, Name, `Condition`, Urgency, RequiredSpecialization, NeedsSurgery,
                   AdmissionDate, ScheduledSurgeryDate, AssignedDoctorId, AssignedSurgeonId,
                   ComplexityLevel, EstimatedTreatmentTime, RequiredProcedureId,
                   AssignedOperatingRoomId, EstimatedAppointmentDurationMinutes
            FROM Patients WHERE Id = @Id;";
            string historySql = "SELECT DoctorId FROM PatientDoctorHistory WHERE PatientId = @Id;";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // שימוש ב-QueryFirstOrDefaultAsync כדי לקבל מטופל בודד או null
                    // שוב, שימוש ב-dynamic או DTO להתאמה
                    var patientData = await connection.QueryFirstOrDefaultAsync(patientSql, new { Id = id });

                    if (patientData == null)
                    {
                        Log.Warning("Patient with ID {PatientId} not found.", id);
                        return null; // החזר null אם המטופל לא נמצא
                    }

                    // הרכבת אובייקט המטופל (דומה ל-GetAllAsync)
                    var patient = new Patient
                    {
                        Id = patientData.Id,
                        Name = patientData.Name,
                        Condition = patientData.Condition,
                        Urgency = (UrgencyLevel)patientData.Urgency,
                        RequiredSpecialization = patientData.RequiredSpecialization,
                        NeedsSurgery = patientData.NeedsSurgery,
                        AdmissionDate = patientData.AdmissionDate,
                        ScheduledSurgeryDate = patientData.ScheduledSurgeryDate,
                        AssignedDoctorId = patientData.AssignedDoctorId,
                        AssignedSurgeonId = patientData.AssignedSurgeonId,
                        ComplexityLevel = (ComplexityLevel)patientData.ComplexityLevel,
                        EstimatedTreatmentTime = patientData.EstimatedTreatmentTime,
                        RequiredProcedureId = patientData.RequiredProcedureId,
                        AssignedOperatingRoomId = patientData.AssignedOperatingRoomId,
                        EstimatedAppointmentDuration = TimeSpan.FromMinutes((int?)patientData.EstimatedAppointmentDurationMinutes ?? 0)
                    };


                    // משיכת רשימת הרופאים הקודמים עבור המטופל הספציפי הזה
                    var previousDoctorIds = await connection.QueryAsync<int>(historySql, new { Id = id });
                    patient.PreviousDoctors = previousDoctorIds.ToList();

                    Log.Debug("Retrieved Patient with ID {PatientId}. Found {HistoryCount} previous doctors.", id, patient.PreviousDoctors.Count);
                    return patient;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting Patient with ID {PatientId}", id);
                throw;
            }
        }

        /// <summary>
        /// מעדכן את פרטי המטופל במסד הנתונים.
        /// הערה: כרגע לא מטפל בעדכון רשימת PreviousDoctors.
        /// </summary>
        public override async Task<bool> UpdateAsync(Patient entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            string sql = @"
            UPDATE Patients SET
                Name = @Name,
                `Condition` = @Condition,
                Urgency = @Urgency,
                RequiredSpecialization = @RequiredSpecialization,
                NeedsSurgery = @NeedsSurgery,
                AdmissionDate = @AdmissionDate,
                ScheduledSurgeryDate = @ScheduledSurgeryDate,
                AssignedDoctorId = @AssignedDoctorId,
                AssignedSurgeonId = @AssignedSurgeonId,
                ComplexityLevel = @ComplexityLevel,
                EstimatedTreatmentTime = @EstimatedTreatmentTime,
                RequiredProcedureId = @RequiredProcedureId,
                AssignedOperatingRoomId = @AssignedOperatingRoomId,
                EstimatedAppointmentDurationMinutes = @EstimatedAppointmentDurationMinutes
            WHERE Id = @Id;";

            try
            {
                using (var connection = await CreateOpenConnectionAsync())
                {
                    // הכנת פרמטרים כמו ב-AddAsync
                    var parameters = new
                    {
                        entity.Name,
                        entity.Condition,
                        Urgency = (int)entity.Urgency,
                        entity.RequiredSpecialization,
                        entity.NeedsSurgery,
                        entity.AdmissionDate,
                        entity.ScheduledSurgeryDate,
                        entity.AssignedDoctorId,
                        entity.AssignedSurgeonId,
                        ComplexityLevel = (int)entity.ComplexityLevel,
                        entity.EstimatedTreatmentTime,
                        entity.RequiredProcedureId,
                        entity.AssignedOperatingRoomId,
                        EstimatedAppointmentDurationMinutes = (int)entity.EstimatedAppointmentDuration.TotalMinutes,
                        entity.Id // חשוב לכלול את ה-ID עבור תנאי ה-WHERE
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);

                    // TODO: עדכון היסטוריית רופאים אם נדרש (לרוב מצריך מחיקה והוספה מחדש, בטרנזקציה)

                    if (rowsAffected > 0) Log.Debug("Patient updated successfully: ID {PatientId}", entity.Id);
                    else Log.Warning("Attempted to update Patient with ID {PatientId}, but no rows were affected (maybe ID not found?).", entity.Id);

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating Patient with ID {PatientId}", entity?.Id ?? 0);
                throw;
            }
        }

        // מחלקה פנימית פשוטה (DTO) לקריאת נתוני היסטוריה
        private class PatientDoctorHistoryDto
        {
            public int PatientId { get; set; }
            public int DoctorId { get; set; }
        }
    }
}
