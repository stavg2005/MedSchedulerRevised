using System;
using System.Collections.Generic;
using System.Diagnostics; // לשימוש ב-Stopwatch למדידת זמנים אם רוצים
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml; // EPPlus - לספריית האקסל
using Serilog; // אם אתה משתמש בלוגים גם פה
// שנה את שמות ה-namespaces בהתאם למבנה הפרויקט שלך:
using Models;
using DataAccessLayer;
using SchedulingEngine;
using System.Drawing;
// using YourProject.Core; // אם יש ספרייה משותפת ללוגים

namespace Testing
{
    class statrun
    {
        // --- הגדרות עיקריות ---
        const int NUMBER_OF_RUNS = 50; // מספר הריצות הרצוי
        const string CONNECTION_STRING = "server=localhost;port=3306;database=medscheduler;user=root;password=LuffyDono2005;"; // שנה ל-Connection String שלך
        const string OUTPUT_FOLDER = "BatchResults"; // תיקייה לשמירת קובץ האקסל

        static async Task Main(string[] args)
        {

            ExcelPackage.License.SetNonCommercialPersonal("Stav");
            // --- 1. הגדרת Serilog (אופציונלי, אם רוצים לוגים גם מהריצה הזו) ---
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(OUTPUT_FOLDER, "batch_runner_log-.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("--- Starting Genetic Algorithm Batch Runner ---");
            Log.Information($"Number of runs configured: {NUMBER_OF_RUNS}");

            // --- 2. הגדרת רישיון EPPlus ---
           // שנה ל-Commercial אם רלוונטי

            // --- 3. טעינת נתונים ראשוניים ---
            Log.Information("Loading initial Doctor and Patient data...");
            List<Doctor> initialDoctors;
            List<Patient> initialPatients;
            try
            {
                // השתמש ב-Repositories שלך לטעינת הנתונים
                var doctorRepo = new DoctorRepository(CONNECTION_STRING);
                var patientRepo = new PatientRepository(CONNECTION_STRING);
                initialDoctors = (await doctorRepo.GetAllWithDetailsAsync()).ToList();
                initialPatients = (await patientRepo.GetAllAsync()).ToList();
                Log.Information($"Loaded {initialDoctors.Count} doctors and {initialPatients.Count} patients.");

                if (!initialDoctors.Any() || !initialPatients.Any())
                {
                    Log.Error("No doctors or patients loaded. Exiting.");
                    return;
                }
                // חשוב: אתחול מוקדם של חישובי ההימנעות ברופאים
                Log.Debug("Initializing doctor avoidance lookups...");
                foreach (var doctor in initialDoctors)
                {
                    doctor.InitializeAvoidanceLookups();
                }
                Log.Debug("Avoidance lookups initialized.");

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to load initial data. Exiting.");
                Console.WriteLine($"Error loading data: {ex.Message}");
                return;
            }

            Log.Information($"--- Starting Genetic Algorithm Batch Runner ---");
            Log.Information($"EPPlus License Context set to: {ExcelPackage.LicenseContext}");
            Log.Information($"Number of runs configured: {NUMBER_OF_RUNS}");
            // --- 4. הגדרת פרמטרים קבועים ל-GA (אפשר לקרוא מקובץ קונפיגורציה) ---
            // (אותם פרמטרים ישמשו לכל 50 הריצות בדוגמה זו)
            int populationSize = 300;
            int maxGenerations = 500; // אולי כדאי להגדיל לריצות ארוכות יותר
            double crossoverRate = 0.8;
            double mutationRate = 0.1; // שיעור מוטציה בסיסי
            // משקלים לפונקציית הכשירות
            double specWeight = 5.0, urgencyWeight = 4.5, workWeight = 6.5, cocWeight = 2.0, expWeight = 3.0, prefWeight = 4.0;
            Log.Information("GA Parameters set (Pop: {PopSize}, Gen: {MaxGen}, Cross: {CrossRate}, Mut: {MutRate})",
                            populationSize, maxGenerations, crossoverRate, mutationRate);

            // --- 5. ביצוע הריצות בלולאה ---
            List<Statistics> allRunStatistics = new List<Statistics>(); // רשימה לאיסוף כל התוצאות
            Log.Information($"Starting {NUMBER_OF_RUNS} GA runs...");
            Stopwatch totalBatchStopwatch = Stopwatch.StartNew(); // מדידת זמן כולל

            for (int i = 0; i < NUMBER_OF_RUNS; i++)
            {
                int runNumber = i + 1;
                Log.Information("--- Starting Run {RunNumber}/{TotalRuns} ---", runNumber, NUMBER_OF_RUNS);
                Stopwatch singleRunStopwatch = Stopwatch.StartNew(); // מדידת זמן לריצה בודדת

                // ** חשוב: איפוס מצב הנתונים לפני כל ריצה **
                // כדי שכל ריצה תתחיל מאותה נקודה, נאפס שדות שהאלגוריתם משנה.
                // אם האלגוריתם משנה שדות רבים, ייתכן שעדיף לבצע Deep Copy של הרשימות,
                // אך איפוס פשוט יותר אם השינויים מוגבלים (כמו Workload ו-AssignedDoctorId).
                ResetDoctorAndPatientState(initialDoctors, initialPatients);

                // ** יצירת מופע חדש של GA והרצתו **
                // חשוב ליצור מופע חדש בכל איטרציה כדי לאפס מצב פנימי של האלגוריתם.
                var ga = new GeneticAlgorithm(populationSize, initialDoctors, initialPatients);
                // הגדרת פרמטרים לריצה זו
                ga.maxGenerations = maxGenerations;
                ga.crossoverRate = crossoverRate;
                ga.baseMutationRate = mutationRate; // שימוש בשיעור הבסיסי
                ga.SetFitnessWeights(specWeight, urgencyWeight, workWeight, cocWeight, expWeight, prefWeight);

                try
                {
                    // הרצת האלגוריתם
                    // המתודה Solve אמורה לחשב ולשמור את הסטטיסטיקה ב-ga.finalStatistics
                    Schedule finalSchedule = ga.Solve(); // הפתרון עצמו אולי לא קריטי פה, רק הסטטיסטיקה

                    if (ga.finalStatistics != null)
                    {
                        // הוספת מספר הריצה לסטטיסטיקה ושמירה ברשימה
                        ga.finalStatistics.RunNumber = runNumber; // הנחה שהוספנו את השדה הזה לקלאס
                        allRunStatistics.Add(ga.finalStatistics);
                        Log.Information("Run {RunNumber} completed in {ElapsedMs} ms. Final Fitness: {Fitness:F2}, Generations: {Generations}",
                                        runNumber, singleRunStopwatch.ElapsedMilliseconds, ga.finalStatistics.GaFinalFitness, ga.finalStatistics.GaGenerations);
                    }
                    else
                    {
                        Log.Warning("Run {RunNumber} completed but no statistics were generated by the GA instance.", runNumber);
                        // אפשר להוסיף רשומה ריקה כדי לציין שהריצה התבצעה אך ללא תוצאות
                        allRunStatistics.Add(new Statistics { RunNumber = runNumber, GaFinalFitness = double.NaN });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error occurred during GA run {RunNumber}", runNumber);
                    // הוסף רשומה שמציינת כשלון
                    allRunStatistics.Add(new Statistics { RunNumber = runNumber, GaFinalFitness = double.NaN /* ציון כשלון */ });
                }
                finally
                {
                    singleRunStopwatch.Stop(); // עצירת שעון הריצה הבודדת
                    Log.Information("--- Finished Run {RunNumber} ---", runNumber);
                }
            } // סוף הלולאה

            totalBatchStopwatch.Stop();
            Log.Information("--- Completed all {TotalRuns} runs in {TotalElapsed} ---", NUMBER_OF_RUNS, totalBatchStopwatch.Elapsed);

            // --- 6. ייצוא התוצאות לאקסל ---
            ExportResultsToExcel(allRunStatistics, OUTPUT_FOLDER);

            // --- סיום ---
            Log.Information("--- Batch Runner Finished ---");
            Console.WriteLine("\nBatch run complete. Press Enter to exit.");
            Console.ReadLine();
        }

        // פונקציית עזר לאיפוס מצב הרופאים והמטופלים בין ריצות
        static void ResetDoctorAndPatientState(List<Doctor> doctors, List<Patient> patients)
        {
            // Log.Debug("Resetting doctor/patient state for new run...");
            foreach (var doctor in doctors)
            {
                doctor.Workload = 0;
                doctor.CurrentPatientIds?.Clear(); // נקה רשימה קיימת
                if (doctor.CurrentPatientIds == null) doctor.CurrentPatientIds = new List<int>(); // ודא שהיא קיימת
            }
            foreach (var patient in patients)
            {
                patient.AssignedDoctorId = null;
                // אפס שדות נוספים אם האלגוריתם או פונקציית העדכון משנים אותם
                // patient.ScheduledSurgeryDate = null;
                // patient.AssignedOperatingRoomId = null;
            }
            // בדרך כלל אין צורך לאתחל מחדש את InitializeAvoidanceLookups
            // אלא אם כן העדפות הרופאים יכולות להשתנות בין ריצות (לא סביר כאן).
        }


        // פונקציית עזר לייצוא הנתונים לאקסל באמצעות EPPlus
        static void ExportResultsToExcel(List<Statistics> statsList, string outputFolder)
        {
            if (statsList == null || !statsList.Any())
            {
                Log.Warning("No statistics data available to export.");
                return;
            }

            // ודא שהתיקייה קיימת
            Directory.CreateDirectory(outputFolder);
            string fileName = $"GARunResults_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(outputFolder, fileName);
            Log.Information("Exporting {Count} results to Excel file: {FilePath}", statsList.Count, filePath);

            try
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("GA Run Statistics");

                    // --- כתיבת שורת הכותרת ---
                    int col = 1;
                    // רשימה ידנית של הכותרות לפי הסדר הרצוי
                    worksheet.Cells[1, col++].Value = "RunNumber";
                    worksheet.Cells[1, col++].Value = "GaFinalFitness";
                    worksheet.Cells[1, col++].Value = "GaGenerations";
                    worksheet.Cells[1, col++].Value = "TotalElapsedTimeSeconds";
                    worksheet.Cells[1, col++].Value = "AssignedRegularPatients";
                    worksheet.Cells[1, col++].Value = "TotalRegularPatients";
                    worksheet.Cells[1, col++].Value = "RegularPatientAssignmentPercentage";
                    worksheet.Cells[1, col++].Value = "AverageDoctorWorkloadPercent";
                    worksheet.Cells[1, col++].Value = "MinDoctorWorkloadPercent";
                    worksheet.Cells[1, col++].Value = "MaxDoctorWorkloadPercent";
                    worksheet.Cells[1, col++].Value = "StdDevDoctorWorkloadPercent";
                    worksheet.Cells[1, col++].Value = "SpecializationMatchRatePercent";
                    worksheet.Cells[1, col++].Value = "ExperienceComplexityMatchRatePercent"; // שם מעודכן מהפונקציה
                    worksheet.Cells[1, col++].Value = "ContinuityOfCareRatePercent";
                    worksheet.Cells[1, col++].Value = "AveragePreferenceScore";
                    worksheet.Cells[1, col++].Value = "TotalSurgeryPatients";
                    worksheet.Cells[1, col++].Value = "ScheduledSurgeriesCount";
                    worksheet.Cells[1, col++].Value = "SurgeryCompletionRatePercent"; // שם מעודכן
                    // הוסף כותרות נוספות לפי הצורך

                    // עיצוב הכותרת (אופציונלי)
                    using (var range = worksheet.Cells[1, 1, 1, col - 1])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // --- כתיבת שורות הנתונים ---
                    int row = 2;
                    foreach (var stats in statsList)
                    {
                        col = 1;
                        worksheet.Cells[row, col++].Value = stats.RunNumber;
                        worksheet.Cells[row, col++].Value = stats.GaFinalFitness;
                        worksheet.Cells[row, col++].Value = stats.GaGenerations;
                        worksheet.Cells[row, col++].Value = stats.TotalElapsedTimeSeconds;
                        worksheet.Cells[row, col++].Value = stats.AssignedRegularPatients;
                        worksheet.Cells[row, col++].Value = stats.TotalRegularPatients;
                        worksheet.Cells[row, col++].Value = stats.RegularPatientAssignmentPercentage;
                        worksheet.Cells[row, col++].Value = stats.AverageDoctorWorkloadPercent;
                        worksheet.Cells[row, col++].Value = stats.MinDoctorWorkloadPercent;
                        worksheet.Cells[row, col++].Value = stats.MaxDoctorWorkloadPercent;
                        worksheet.Cells[row, col++].Value = stats.StdDevDoctorWorkloadPercent;
                        worksheet.Cells[row, col++].Value = stats.SpecializationMatchRatePercent;
                        worksheet.Cells[row, col++].Value = stats.ExperienceComplexityMatchRatePercent;
                        worksheet.Cells[row, col++].Value = stats.ContinuityOfCareRatePercent;
                        worksheet.Cells[row, col++].Value = stats.AveragePreferenceScore;
                        worksheet.Cells[row, col++].Value = stats.TotalSurgeryPatients;
                        worksheet.Cells[row, col++].Value = stats.ScheduledSurgeriesCount;
                        worksheet.Cells[row, col++].Value = stats.SurgeryCompletionRatePercent;
                        // הוסף עמודות נוספות לפי הצורך
                        row++;
                    }

                    // --- התאמת רוחב עמודות אוטומטית ---
                    if (worksheet.Dimension != null) // Check if worksheet has data
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // --- קביעת פורמט מספרים (אופציונלי, לשיפור הקריאות) ---
                    // שים לב שהאינדקסים של העמודות מתחילים מ-1
                    worksheet.Column(2).Style.Numberformat.Format = "0.00";  // Fitness
                    worksheet.Column(4).Style.Numberformat.Format = "0.00";  // Time
                    worksheet.Column(7).Style.Numberformat.Format = "0.00\\%"; // Percentages
                    worksheet.Column(8).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(9).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(10).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(11).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(12).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(13).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(14).Style.Numberformat.Format = "0.00\\%";
                    worksheet.Column(15).Style.Numberformat.Format = "0.00";  // Preference Score
                    worksheet.Column(18).Style.Numberformat.Format = "0.00\\%"; // Surgery Percentage

                    // --- שמירת הקובץ ---
                    FileInfo excelFile = new FileInfo(filePath);
                    package.SaveAs(excelFile);
                    Log.Information("Successfully exported results to {FilePath}", filePath);
                } // סגירה אוטומטית של ה-package
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to export results to Excel file: {FilePath}", filePath);
                Console.WriteLine($"Error saving Excel file: {ex.Message}");
            }
        }
    }

   
}
