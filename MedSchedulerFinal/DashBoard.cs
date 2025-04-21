using MedSchedulerFinal.Panels;
using MedSchedulerFinal.Utilitys;
using Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MedSchedulerFinal
{
    public partial class DashBoard: Form
    {

        private DashBoardPanel _dashboardControl;
        private Doctors _doctorsControl;
        private Patients _patientsControl;
        private Algorithm _algorithmControl;

        public static SharedState _sharedState = new SharedState();
        private UserControl _currentControl = null;

        public  DashBoard()
        {
            InitializeComponent();
            InitializeUserControls();

            
        }

        private async void DashBoard_Load(object sender, EventArgs e)
        {
            try
            {
                // אופציונלי: הצג חיווי טעינה
                this.Cursor = Cursors.WaitCursor;
                // statusLabel.Text = "טוען נתונים..."; // אם יש לך תווית סטטוס

                // קרא והמתן לסיום טעינת הנתונים
                await _sharedState.LoadData();

                // --- העבר את הנתונים לפקד Doctors ---
                if (_doctorsControl != null)
                {
                    // קרא למתודה הציבורית שיצרנו ב-Doctors
                    _doctorsControl.SetDoctorData(_sharedState.Doctors);
                    _algorithmControl.SetGa(_sharedState.Doctors.ToList(), _sharedState.Patients.ToList());
                }
                else
                {
                    MessageBox.Show("שגיאה: פקד הרופאים לא אותחל כראוי.", "שגיאה", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                // העבר נתונים לפקדים אחרים אם צריך
                // if (_patientsControl != null) _patientsControl.SetPatientData(_sharedState.Patients);

                // statusLabel.Text = "הנתונים נטענו.";
            }
            catch (Exception ex)
            {
                // טפל בשגיאות טעינה
                MessageBox.Show($"שגיאה בטעינת הנתונים: {ex.Message}", "שגיאת טעינה", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // statusLabel.Text = "שגיאה בטעינת נתונים.";

                // אפשרות: להעביר רשימה ריקה במקרה של שגיאה
                if (_doctorsControl != null)
                {
                    _doctorsControl.SetDoctorData(new BindingList<Doctor>());
                }
            }
            finally
            {
                // החזר את הסמן למצב רגיל
                this.Cursor = Cursors.Default;
            }
        }


        private void InitializeUserControls()
        {
            _dashboardControl = new DashBoardPanel();
            _doctorsControl = new Doctors();
            _patientsControl = new Patients();
            _algorithmControl = new Algorithm();

            _dashboardControl.Dock = DockStyle.Fill;
            _doctorsControl.Dock = DockStyle.Fill;
            _patientsControl.Dock = DockStyle.Fill;
            _algorithmControl.Dock = DockStyle.Fill;

            panelContentContainer.Controls.Add(_dashboardControl);
            panelContentContainer.Controls.Add(_doctorsControl);
            panelContentContainer.Controls.Add(_patientsControl);
            panelContentContainer.Controls.Add(_algorithmControl);

            _dashboardControl.Visible = false;
            _doctorsControl.Visible = false;
            _patientsControl.Visible = false;
            _algorithmControl.Visible = false;
            

        }

        private void ShowUserControl(UserControl controlToShow)
        {
            if (controlToShow == null) return;

           
            if (_currentControl == controlToShow) return;

            
            if (_currentControl != null)
            {
                _currentControl.Visible = false;
            }

            controlToShow.Visible = true;
           
            _currentControl = controlToShow;
        }

        private void labelDashBoard_Click(object sender, EventArgs e)
        {
            ShowUserControl(_dashboardControl);
        }

        private void labelDoctors_Click(object sender, EventArgs e)
        {
            ShowUserControl(_doctorsControl);
        }

        private void Surgeries_Click(object sender, EventArgs e)
        {
            ShowUserControl(_patientsControl);
        }

        private void labelAlgorithm_Click(object sender, EventArgs e)
        {
            ShowUserControl(_algorithmControl);
        }

        private void TopBar_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panelContentContainer_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
