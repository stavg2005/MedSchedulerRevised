using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MedSchedulerFinal.Utilitys;
using Models;

namespace MedSchedulerFinal.Panels
{
    public partial class Doctors: UserControl
    {
        //SharedState s = DashBoard._sharedState;
        private BindingList<Doctor> allDoctors;
        private int currentPageIndex = 0;
        private const int ItemsPerPage = 10;

        public Doctors()
        {
            InitializeComponent();
            

        }
        private void Doctors_Load(object sender, EventArgs e)
        {
            
        }

        /// <summary>
        /// מתודה ציבורית לקבלת רשימת הרופאים מהטופס המארח (DashBoard).
        /// </summary>
        /// <param name="doctorsData">רשימת הרופאים המעודכנת.</param>
        public void SetDoctorData(BindingList<Doctor> doctorsData)
        {
            allDoctors = doctorsData; // שמור את ההתייחסות לרשימה המשותפת
            currentPageIndex = 0;     // אפס לעמוד הראשון בכל פעם שהנתונים מתעדכנים
            DisplayCurrentPage();     // הצג את העמוד הראשון עם הנתונים החדשים

            // אופציונלי: אם רוצים שה-UserControl יגיב לשינויים עתידיים ברשימה
            // שנעשים מחוץ לו, אפשר להירשם לאירוע ListChanged כאן.
            // if (allDoctors != null)
            // {
            //     allDoctors.ListChanged -= AllDoctors_ListChanged; // הסר רישום קודם
            //     allDoctors.ListChanged += AllDoctors_ListChanged;
            // }
        }




        private void DisplayCurrentPage()
        {
            // נקה את הפקדים הקודמים מה-FlowLayoutPanel
            // וודא שהפקד doctorsFlowPanel קיים (הוספת אותו בעיצוב בתוך panel2)
            if (this.Controls.Find("doctorsFlowPanel", true).FirstOrDefault() is FlowLayoutPanel flowPanel)
            {
                flowPanel.SuspendLayout(); // השהיית ציור לביצועים טובים יותר
                flowPanel.Controls.Clear(); // הסרת כל השורות הקודמות

                if (allDoctors == null || !allDoctors.Any())
                {
                    // אפשר להציג כאן הודעה שאין נתונים
                    Label noDataLabel = new Label();
                    noDataLabel.Text = "No doctors found.";
                    noDataLabel.AutoSize = true;
                    flowPanel.Controls.Add(noDataLabel);
                }
                else
                {
                    // חישוב אינדקס התחלה וקבלת תת-רשימה לעמוד הנוכחי
                    int startIndex = currentPageIndex * ItemsPerPage;
                    var doctorsToShow = allDoctors.Skip(startIndex).Take(ItemsPerPage).ToList();

                    // יצירה והוספה של פקדים עבור כל רופא בעמוד הנוכחי
                    foreach (var doctor in doctorsToShow)
                    {
                        // יצירת פאנל עבור כל שורה כדי לקבץ את הפקדים
                        Panel rowPanel = new Panel
                        {
                            Width = flowPanel.ClientSize.Width - 25, // התאם רוחב לפי הצורך (פחות קצת למרווח/פס גלילה)
                            Height = 30, // גובה שורה קבוע
                            Margin = new Padding(0, 0, 0, 5), // מרווח תחתון בין שורות
                            BorderStyle = BorderStyle.FixedSingle // אופציונלי: להוספת גבול
                        };

                        // יצירת תווית לשם הרופא
                        Label nameLabel = new Label
                        {
                            Text = doctor.Name,
                            // Location = new Point(10, 5), // מיקום בתוך rowPanel
                            AutoSize = true, // גודל אוטומטי
                            MaximumSize = new Size(150, 0), // הגבלת רוחב מקסימלי
                            Dock = DockStyle.Left // עגינה לשמאל בתוך הפאנל
                        };

                        // יצירת תווית ל-Condition
                        Label conditionLabel = new Label
                        {
                            Text = doctor.Specialization,
                            Location = new Point(170, 5), // מיקום בתוך rowPanel
                            AutoSize = true,
                            MaximumSize = new Size(120, 0),
                            
                        };
                        Padding currentPadding = conditionLabel.Padding;
                        conditionLabel.Padding = new Padding(20 + currentPadding.Left, currentPadding.Top, currentPadding.Right, currentPadding.Bottom); // הוספת ריווח משמאל

                        string is_surgeon = "no";
                        if (doctor is Surgeon)
                            is_surgeon = "yes";

                        // יצירת תווית ל-Surgery
                        Label surgeryLabel = new Label
                        {
                            Text = is_surgeon,
                            Location = new Point(350, 5), // מיקום בתוך rowPanel
                            AutoSize = true,
                            MaximumSize = new Size(120, 0),
                            
                        };
                        currentPadding = surgeryLabel.Padding;
                        surgeryLabel.Padding = new Padding(20 + currentPadding.Left, currentPadding.Top, currentPadding.Right, currentPadding.Bottom); // הוספת ריווח משמאל


                        // יצירת כפתור Delete
                        Button deleteButton = new Button
                        {
                            Text = "D", // או "Delete"
                            Location = new Point(rowPanel.Width - 50, 3), // מיקום בתוך rowPanel
                            Size = new Size(30, 23),
                            Tag = doctor.Id, // שמירת ה-ID של הרופא בכפתור לשימוש עתידי
                            Dock = DockStyle.Right // עגינה לימין
                        };
                        deleteButton.Click += DeleteButton_Click; // קישור לאירוע לחיצה

                        // יצירת כפתור Edit
                        Button editButton = new Button
                        {
                            Text = "E", // או "Edit"
                            Location = new Point(rowPanel.Width - 90, 3), // מיקום בתוך rowPanel
                            Size = new Size(30, 23),
                            Tag = doctor.Id, // שמירת ה-ID של הרופא
                            Dock = DockStyle.Right // עגינה לימין
                        };
                        editButton.Click += EditButton_Click; // קישור לאירוע לחיצה


                        // הוספת הפקדים לפאנל השורה (בסדר הפוך בגלל Dock Right/Left)
                        rowPanel.Controls.Add(deleteButton);
                        rowPanel.Controls.Add(editButton);
                        rowPanel.Controls.Add(surgeryLabel);
                        rowPanel.Controls.Add(conditionLabel);
                        rowPanel.Controls.Add(nameLabel); // התווית השמאלית ביותר נוספת אחרונה


                        // הוספת פאנל השורה ל-FlowLayoutPanel הראשי
                        flowPanel.Controls.Add(rowPanel);
                    }
                }


                flowPanel.ResumeLayout(); // המשך ציור
            }
            else
            {
                MessageBox.Show("Error: FlowLayoutPanel 'doctorsFlowPanel' not found inside 'panel2'. Please add it in the designer.", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateNavigationButtons(); // עדכון מצב כפתורי הניווט
        }


        // עדכון מצב (מופעל/מכובה) של כפתורי הניווט
        private void UpdateNavigationButtons()
        {
            // הפעלת כפתור "הקודם" רק אם אנחנו לא בעמוד הראשון
            GoLeft.Enabled = currentPageIndex > 0;

            // הפעלת כפתור "הבא" רק אם יש עוד עמודים להציג
            int totalPages = (int)Math.Ceiling((double)(allDoctors?.Count ?? 0) / ItemsPerPage);
            GoRight.Enabled = currentPageIndex < totalPages - 1;
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void DoctorRow_Paint(object sender, PaintEventArgs e)
        {

        }

        private void TableHeader_Paint(object sender, PaintEventArgs e)
        {

        }

        private void GoLeft_Click(object sender, EventArgs e)
        {
            if (currentPageIndex > 0)
            {
                currentPageIndex--;
                DisplayCurrentPage();
            }

        }

        private void GoRight_Click(object sender, EventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)(allDoctors?.Count ?? 0) / ItemsPerPage);
            if (currentPageIndex < totalPages - 1)
            {
                currentPageIndex++;
                DisplayCurrentPage();
            }
        }

        private void doctorsFlowPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void EditButton_Click(object sender, EventArgs e)
        {

        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {

        }

        private void Doctors_Load_1(object sender, EventArgs e)
        {

        }

        private void c_Click(object sender, EventArgs e)
        {

        }
    }
}
