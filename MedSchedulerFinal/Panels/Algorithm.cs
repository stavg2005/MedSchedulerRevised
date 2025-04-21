using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SchedulingEngine;
using MedSchedulerFinal.Utilitys;
using Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Diagnostics;
using Serilog; // הוסף using ל-Serilog
using Serilog.Core; // הוסף using ל-ILogger
using Business_Logic_Layer;
namespace MedSchedulerFinal.Panels
{
    public partial class Algorithm: UserControl
    {
        
        GeneticAlgorithm Ga;
        public Statistics finalStatistics = new Statistics();

      
        public Algorithm()
        {
           
            InitializeComponent();
            
        }

        private void Algorithm_Load(object sender, EventArgs e)
        {
            
        }
        private void Algorithm_Disposed(object sender, EventArgs e)
        {
           
        }

 
        public void SetGa(List<Models.Doctor> docs,List<Patient> pets)
        {
           
             try
            {
                // קרא ערכים מה-TextBoxes, עם ערכי ברירת מחדל אם הקלט לא תקין
                int populationSize = int.TryParse(PopSize.Text, out int pop) ? pop : 300; // ברירת מחדל 300
                int maxGenerations = int.TryParse(MaxGen.Text, out int gen) ? gen : 100; // ברירת מחדל 100
                double crossoverRate = double.TryParse(CrosRate.Text, out double cross) ? cross : 0.8; // ברירת מחדל 0.8
                double mutationRate = double.TryParse(MutRate.Text, out double mut) ? mut : 0.1; // ברירת מחדל 0.1

                // קרא משקלים (הנח שהם גם כן double)
                double specWeight = double.TryParse(SpecWeight.Text, out double sw) ? sw : 1.0;
                double urgencyWeight = double.TryParse(UrgencyWeaight.Text, out double uw) ? uw : 1.0;
                double workWeight = double.TryParse(workloadWeight.Text, out double ww) ? ww : 1.0;
                double cocWeight = double.TryParse(CoCWeight.Text, out double ccw) ? ccw : 1.0;
                double expWeight = double.TryParse(ExpWeight.Text, out double ew) ? ew : 1.0;
                double prefWeight = double.TryParse(PrefWeight.Text, out double pw) ? pw : 1.0;


                // צור מופע חדש של האלגוריתם עם הפרמטרים שהתקבלו
                Ga = new GeneticAlgorithm(populationSize, docs, pets);

                // הגדר פרמטרים נוספים ב-Ga אם קיימים setters
                Ga.maxGenerations = maxGenerations; // אם קיים
                Ga.crossoverRate = crossoverRate; // אם קיים
                Ga.baseMutationRate = mutationRate; // אם קיים

                // הגדר משקלים ב-Ga (נניח שיש דרך להגדיר אותם)
                Ga.SetFitnessWeights(specWeight, urgencyWeight, workWeight, cocWeight, expWeight, prefWeight); // שנה לפי המבנה האמיתי של GA


                // עדכן את התוויות כדי להציג שוב "-" לפני ריצה חדשה
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting up algorithm parameters: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Ga = null; // מנע ריצה אם ההגדרה נכשלה
            }
        }

        private void InitializeResultLabels()
        {
            // החלף בשמות התוויות האמיתיים
            labelRunStatus.Text = "Idle"; // נניח שיש תווית סטטוס כללית
            PatientsAssinged.Text = "-";
            SpecMacth.Text = "-";
            AvgWorkLoad.Text = "-";
            COC.Text = "-";
            ExperienceLabel.Text = "-";
            AvgPrefrenceScore.Text = "-";
            SurgoryComp.Text = "-";
            c.Text = "-";
            GaGenerations.Text = "-";
            v.Text = "-";
            // textBoxLogOutput.Clear(); // נקה את תיבת הלוג אם קיימת
        }
        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private async void RunButton_Click(object sender, EventArgs e)
        {
            if (Ga == null)
            {
                MessageBox.Show("Algorithm is not initialized. Please ensure valid parameters are set.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 0. נקה תוויות קודמות והצג סטטוס "Running"
            InitializeResultLabels();
            labelRunStatus.Text = "Running...";
            // textBoxLogOutput.AppendText("Starting genetic algorithm...\n"); // הוסף לוג
            RunButton.Enabled = false; // נטרל את הכפתור בזמן ריצה
            this.Cursor = Cursors.WaitCursor; // שנה את סמן העכבר

            Stopwatch stopwatch = new Stopwatch(); // מדוד זמן ריצה
            Schedule finalSchedule = null; // אחסן את התוצאה
            Exception runException = null; // אחסן שגיאה אם תתרחש

            try
            {
                stopwatch.Start();
                // 1. הרץ את האלגוריתם באופן אסינכרוני כדי לא לחסום את הממשק
                finalSchedule = await Task.Run(() => Ga.Solve());
                if (finalSchedule != null)
                {
                    if(DashBoard._sharedState != null)
                    {

                        finalStatistics = Ga.finalStatistics;
                    }
                }
                    
                stopwatch.Stop();
                // textBoxLogOutput.AppendText($"Algorithm finished in {stopwatch.ElapsedMilliseconds} ms.\n");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                runException = ex; // שמור את השגיאה
                                   // textBoxLogOutput.AppendText($"ERROR: Algorithm failed after {stopwatch.ElapsedMilliseconds} ms. Details: {ex.Message}\n");
                MessageBox.Show($"An error occurred during the algorithm execution: {ex.Message}", "Algorithm Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // 3. עדכן את הממשק בחזרה ב-UI Thread (זה קורה אוטומטית אחרי await)
                this.Cursor = Cursors.Default; // החזר את סמן העכבר
                RunButton.Enabled = true; // אפשר את הכפתור מחדש
                labelRunStatus.Text = (runException == null && finalSchedule != null) ? "Completed" : "Error";
            }


            // 4. אם הריצה הצליחה והתקבלה תוצאה, עדכן את התוויות
            if (finalStatistics != null)
            {
                try
                {
                    // --- עדכון התוויות ---
                    // החלף את שמות התוויות ואת שמות המאפיינים של finalSchedule לפי הצורך

                    // דוגמאות (תצטרך להתאים למבנה המדויק של Schedule):
                    labelRunStatus.Text = "running"; // נניח שקיים מאפיין כזה
                    SpecMacth.Text = finalStatistics.SpecializationMatchRatePercent.ToString("F2"); // "F2" פורמט לשתי ספרות אחרי הנקודה
                    AvgWorkLoad.Text = finalStatistics.AverageDoctorWorkloadPercent.ToString("F2");
                    COC.Text = finalStatistics.ContinuityOfCareRatePercent.ToString("F2");
                    ExperienceLabel.Text = finalStatistics.ExperienceMatchRatePercent.ToString("F2");
                    AvgPrefrenceScore.Text = finalStatistics.AveragePreferenceScore.ToString("F2");
                    SurgoryComp.Text = finalStatistics.SurgeryCompletionRate.ToString("P1"); // "P1" פורמט לאחוז עם ספרה אחת
                    GaGenerations.Text = finalStatistics.GaGenerations.ToString(); ;
                    v.Text = finalStatistics.GaFinalFitness.ToString("F5"); // חמש ספרות אחרי הנקודה
                    PatientsAssinged.Text = finalStatistics.AssignedRegularPatients.ToString();
                    // עדכון זמן ריצה
                    c.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss\.fff"); // פורמט זמן

                    // textBoxLogOutput.AppendText("UI updated with final schedule results.\n");
                }
                catch (Exception uiEx)
                {
                    MessageBox.Show($"An error occurred while updating the UI with results: {uiEx.Message}", "UI Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // textBoxLogOutput.AppendText($"ERROR updating UI: {uiEx.Message}\n");
                }
            }
        }

        /// <summary>
    /// מעדכן את האובייקטים של הרופאים והמטופלים ברשימות הראשיות
    /// בהתבסס על לוח הזמנים הסופי שהתקבל מהאלגוריתם הגנטי.
    /// </summary>
    /// <param name="finalSchedule">לוח הזמנים הסופי שהאלגוריתם מצא.</param>
    /// <param name="doctors">רשימת כל הרופאים (למשל, מ-SharedState.Doctors).</param>
    /// <param name="patients">רשימת כל המטופלים (למשל, מ-SharedState.Patients).</param>
        private void UpdateStateFromSchedule(Schedule finalSchedule, BindingList<Doctor> doctors, BindingList<Patient> patients)
        {
            // בדיקה אם יש בכלל לוח זמנים לעדכן לפיו
            if (finalSchedule == null)
            {
                Console.WriteLine("UpdateStateFromSchedule: finalSchedule is null, cannot update state.");
                // אפשר להוסיף כאן לוג או הודעה למשתמש אם רוצים
                return;
            }

            // כדי לייעל את תהליך העדכון, ניצור מילונים זמניים לגישה מהירה לפי ID.
            // אם הרשימות קטנות, אפשר גם לעבור עליהן ישירות בלולאות, אך זה פחות יעיל.
            Dictionary<int, Doctor> doctorsDict;
            Dictionary<int, Patient> patientsDict;
            try
            {
                // ודא שאין מזהים כפולים לפני יצירת המילון
                doctorsDict = doctors.GroupBy(d => d.Id).ToDictionary(g => g.Key, g => g.First());
                patientsDict = patients.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
            }
            catch (ArgumentException ex)
            {
                // טיפול במקרה של מזהים כפולים ברשימות המקוריות
                Console.WriteLine($"ERROR creating lookup dictionaries: {ex.Message}. Cannot update state.");
                MessageBox.Show($"Data consistency error: Duplicate IDs found in Doctors or Patients list.\n{ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            Console.WriteLine("UpdateStateFromSchedule: Resetting current assignments...");
            // --- 1. ניקוי המצב הקיים ---
            // עבור כל הרופאים: נקה את רשימת המטופלים הנוכחיים ואפס את עומס העבודה.
            foreach (var doctor in doctorsDict.Values)
            {
                // ודא שהרשימה אינה null לפני ניקוי (למקרה שלא אותחלה כראוי)
                doctor.CurrentPatientIds?.Clear();
                // אם הרשימה הייתה null, אתחל אותה מחדש
                if (doctor.CurrentPatientIds == null)
                {
                    doctor.CurrentPatientIds = new List<int>();
                }
                doctor.Workload = 0; // מאפסים את עומס העבודה
            }
            // עבור כל המטופלים: הסר את שיבוץ הרופא הקיים.
            foreach (var patient in patientsDict.Values)
            {
                patient.AssignedDoctorId = null;
                // אפשר לאפס כאן שדות נוספים אם צריך, למשל תאריך תור/ניתוח קודם
                // patient.ScheduledAppointmentTime = null;
            }

            Console.WriteLine($"UpdateStateFromSchedule: Applying {finalSchedule.Appointments.Count} new assignments from the final schedule...");
            // --- 2. עדכון לפי השיבוץ החדש ---
            // עבור כל תור בלוח הזמנים הסופי
            foreach (var appointment in finalSchedule.Appointments)
            {
                // עדכן את המטופל
                if (patientsDict.TryGetValue(appointment.PatientId, out Patient patient))
                {
                    patient.AssignedDoctorId = appointment.DoctorId;
                    // כאן אפשר לעדכן פרטים נוספים על המטופל אם יש בתור, למשל:
                    // patient.ScheduledAppointmentTime = appointment.StartTime;
                    // patient.ScheduledAppointmentDay = appointment.DayOfWeek;
                }
                else
                {
                    // שגיאה: מטופל מהשיבוץ לא נמצא ברשימה הראשית - כדאי לדווח
                    Console.WriteLine($"WARN: Patient ID {appointment.PatientId} from schedule not found in main patient list.");
                }

                // עדכן את הרופא
                if (doctorsDict.TryGetValue(appointment.DoctorId, out Doctor doctor))
                {
                    // ודא שהרשימה קיימת לפני הוספה
                    if (doctor.CurrentPatientIds == null)
                    {
                        doctor.CurrentPatientIds = new List<int>();
                    }
                    // הוסף את ה-ID של המטופל לרשימת המטופלים הנוכחיים של הרופא
                    // (מומלץ למנוע כפילויות אם יש חשש שהשיבוץ מכיל אותן, למרות שתיקון ההצלבה אמור למנוע זאת)
                    if (!doctor.CurrentPatientIds.Contains(appointment.PatientId))
                    {
                        doctor.CurrentPatientIds.Add(appointment.PatientId);
                    }
                }
                else
                {
                    // שגיאה: רופא מהשיבוץ לא נמצא ברשימה הראשית - כדאי לדווח
                    Console.WriteLine($"WARN: Doctor ID {appointment.DoctorId} from schedule not found in main doctor list.");
                }
            }

            Console.WriteLine("UpdateStateFromSchedule: Finalizing doctor workloads...");
            // --- 3. סנכרון סופי של עומס העבודה ---
            // עבור כל הרופאים: קרא לפונקציה שמחשבת מחדש את העומס לפי רשימת המטופלים המעודכנת.
            foreach (var doctor in doctorsDict.Values)
            {
                // ודא ש-CurrentPatientIds אינו null לפני הקריאה ל-SetCurrentWorkLoad
                if (doctor.CurrentPatientIds != null)
                {
                    doctor.SetCurrentWorkLoad(); // מחשב Workload = CurrentPatientIds.Count
                }
                else
                {
                    doctor.Workload = 0; // אם הרשימה null, העומס הוא 0
                    Console.WriteLine($"WARN: Doctor ID {doctor.Id} CurrentPatientIds list was null during final workload set.");
                }
            }

            Console.WriteLine("UpdateStateFromSchedule: State update process complete.");

            // הערה חשובה לממשק המשתמש:
            // אם אתה משתמש ב-BindingList וקושר אותו ישירות לפקדים כמו DataGridView,
            // ייתכן שתצטרך "להודיע" לממשק שהנתונים השתנו בצורה משמעותית כדי שהוא יתרענן ויציג את השינויים.
            // לעיתים קרובות עושים זאת על ידי קריאה ל-ResetBindings() על ה-BindingList עצמו או על ה-BindingSource שקשור אליו.
            // לדוגמה:
            // doctors.ResetBindings();
            // patients.ResetBindings();
            // הצורך בכך תלוי איך בדיוק קשרת את הנתונים לממשק.
        }
        private void PopSize_TextChanged(object sender, EventArgs e)
        {

        }

        private void MaxGen_TextChanged(object sender, EventArgs e)
        {

        }

        private void CrosRate_TextChanged(object sender, EventArgs e)
        {

        }

        private void MutRate_TextChanged(object sender, EventArgs e)
        {

        }

        private void SpecWeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void UrgencyWeaight_TextChanged(object sender, EventArgs e)
        {

        }

        private void workloadWeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void CoCWeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void ExpWeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void PrefWeight_TextChanged(object sender, EventArgs e)
        {

        }

        private void DefGen_Click(object sender, EventArgs e)
        {

        }

        private void DefCross_Click(object sender, EventArgs e)
        {

        }

        private void DefRate_Click(object sender, EventArgs e)
        {

        }

        private void SpecDef_Click(object sender, EventArgs e)
        {

        }

        private void UrgencyDef_Click(object sender, EventArgs e)
        {

        }

        private void WorkDef_Click(object sender, EventArgs e)
        {

        }

        private void CoCDef_Click(object sender, EventArgs e)
        {

        }

        private void ExpDef_Click(object sender, EventArgs e)
        {

        }

        private void PrefDef_Click(object sender, EventArgs e)
        {

        }

        private void c_Click(object sender, EventArgs e)
        {

        }

        //
        private void LogBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
