using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using DataAccessLayer;

namespace MedSchedulerFinal.Utilitys
{
    public class SharedState
    {
        // Use BindingList for automatic UI updates when list changes
        public BindingList<Doctor> Doctors { get; set; }
        public BindingList<Patient> Patients { get; set; }

        // Constructor - initialize the lists (e.g., load from DB)
        public SharedState()
        {
            // Ideally, load data here or have methods to load it
            Doctors = new BindingList<Doctor>(); // Load actual data later
            Patients = new BindingList<Patient>(); // Load actual data later

        }

        // Optional: Add methods to load/reload data
        public async Task LoadData()
        {
            Doctors.Clear();
            string connectionString = "server=localhost;port=3306;database=medscheduler;user=root;password=LuffyDono2005;";
            DoctorRepository doc = new DoctorRepository(connectionString);
            IEnumerable<Doctor> doctors = await doc.GetAllWithDetailsAsync();
            foreach (var dc in doctors) Doctors.Add(dc);
            PatientRepository petRep = new PatientRepository(connectionString);
            List<Patient> pets = await petRep.GetAllAsync();
            Patients.Clear();
            foreach (var pat in pets) Patients.Add(pat);

            // Important if using BindingList with existing controls:
            // ResetBindings might be needed after bulk loading if controls are already bound
        }
    }
}
