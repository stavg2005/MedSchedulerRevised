using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
namespace SchedulingEngine
{
    public class Schedule
    {
       
        public Dictionary<DateTime, Dictionary<int, List<int>>> SurgerySchedule { get; set; } = new Dictionary<DateTime, Dictionary<int, List<int>>>(); // Date -> OperatingRoom -> List of PatientIDs


        // Core data structure: List of confirmed appointments
        public List<ScheduledAppointment> Appointments { get; private set; } = new List<ScheduledAppointment>();

        public bool TryAddAppointment(ScheduledAppointment appointment)
        {
            // Check for conflicts using the BookedTimeSlots lookup
            var timeSlotKey = (appointment.DoctorId, appointment.DayOfWeek, appointment.StartTime);
            if (BookedTimeSlots.Contains(timeSlotKey))
            {
                return false; // Slot already booked in this schedule
            }
            // Check if patient is already assigned
            if (PatientAssignmentLookup.ContainsKey(appointment.PatientId))
            {
                return false; // Patient already has an appointment
            }

            // Add the appointment
            Appointments.Add(appointment);
            PatientAssignmentLookup[appointment.PatientId] = appointment;
            if (!DoctorAssignmentLookup.ContainsKey(appointment.DoctorId))
            {
                DoctorAssignmentLookup[appointment.DoctorId] = new List<ScheduledAppointment>();
            }
            DoctorAssignmentLookup[appointment.DoctorId].Add(appointment);
            BookedTimeSlots.Add(timeSlotKey);

            FitnessScore = -1.0; // Invalidate cached fitness
            return true;
        }

        public bool RemoveAppointment(int patientId)
        {
            if (PatientAssignmentLookup.TryGetValue(patientId, out ScheduledAppointment appointment))
            {
                PatientAssignmentLookup.Remove(patientId);
                Appointments.Remove(appointment); // Assumes reference equality or implement IEquatable
                if (DoctorAssignmentLookup.TryGetValue(appointment.DoctorId, out var docAppointments))
                {
                    docAppointments.Remove(appointment);
                    if (!docAppointments.Any()) DoctorAssignmentLookup.Remove(appointment.DoctorId);
                }
                BookedTimeSlots.Remove((appointment.DoctorId, appointment.DayOfWeek, appointment.StartTime));

                FitnessScore = -1.0; // Invalidate cached fitness
                return true;
            }
            return false;
        }

        // Helper to check if a specific slot is free in this schedule
        public bool IsSlotFree(int doctorId, DayOfWeek day, TimeSpan startTime)
        {
            return !BookedTimeSlots.Contains((doctorId, day, startTime));
        }

        // --- Lookups for efficiency (managed by Add/Remove methods) ---

        // Quickly find a patient's assignment
        public Dictionary<int, ScheduledAppointment> PatientAssignmentLookup { get; private set; } = new Dictionary<int, ScheduledAppointment>();

        // Quickly find all appointments for a doctor
        public Dictionary<int, List<ScheduledAppointment>> DoctorAssignmentLookup { get; private set; } = new Dictionary<int, List<ScheduledAppointment>>();

        // Quickly check if a specific Doctor+Time is booked in THIS schedule
        // Key: Tuple of (DoctorId, DayOfWeek, StartTime)
        public HashSet<(int, DayOfWeek, TimeSpan)> BookedTimeSlots { get; private set; } = new HashSet<(int, DayOfWeek, TimeSpan)>();

        public double FitnessScore { get; set; } = -1; // Initialize to indicate not calculated
        // Get all patients assigned to a specific doctor
        public List<int> GetPatientsForDoctor(int doctorId)
        {
            
                if (DoctorAssignmentLookup.TryGetValue(doctorId, out var appointments))
                {
                    
                    return appointments.Select(appt => appt.PatientId).ToList();
                }
               
                return new List<int>(); 

        }

        // Get the doctor assigned to a specific patient
        public int? GetDoctorForPatient(int patientId)
        {
            if (PatientAssignmentLookup.TryGetValue(patientId, out var appointment))
            {
                return appointment.DoctorId;
            }
            
            return null;
        }



       
    }
}
