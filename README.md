MedScheduler: Medical Appointment Scheduling Optimization via Genetic Algorithm

## Introduction

This project is a C# application designed to tackle the complex challenge of scheduling medical appointments efficiently and effectively. The system leverages a genetic algorithm (GA) to find optimal schedules that balance multiple real-world constraints and preferences.

**This project was developed as a final project for the Software Engineering program at Wan Tac Beit Berle College.**

## Problem Description

Scheduling patient appointments in hospitals is a complex optimization problem. Schedulers must consider:

- Matching patient needs (required specialization, urgency, complexity) with doctor qualifications (specialization, experience).
- Doctor availability and workload limits.
- Individual doctor preferences (e.g., avoiding certain case types).
- The need for continuity of care (meeting with the same doctor).
- Fair workload distribution among doctors.

Finding a schedule that satisfies all these often-conflicting requirements manually is a difficult and time-consuming task.

## Solution: Genetic Algorithm Approach

This project uses a genetic algorithm to explore a vast solution space of potential schedules and evolve towards near-optimal solutions. The algorithm evaluates each schedule using a configurable, multi-objective fitness function that considers:

- **Specialization Match:** Prioritizing assignment to doctors with the correct specialization.
- **Urgency & Complexity:** Assigning higher weight to urgent cases and appropriately matching doctor experience.
- **Workload Balance:** Penalizing schedules with uneven workload distribution among doctors.
- **Continuity of Care:** Rewarding schedules where patients see doctors they have seen before.
- **Doctor Preferences:** Respecting preferences defined by doctors, including avoidances.
- **Experience Alignment:** Ensuring doctor experience meets the demands of patient urgency/complexity.

## Technologies

- **Language:** C#
- **Environment:** .NET Framework 4.7.2
- **User Interface:** Windows Forms (WinForms)

## Project Context

This system was developed by **Stav Granek** as a final project for the Software Engineering degree at **Wan Tac Beit Berle** during the **2024-2025** academic year.

---
