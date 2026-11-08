# MediConnect

MediConnect is a healthcare management platform designed to improve communication and service delivery between patients and healthcare providers.

---

## Overview

MediConnect provides a centralized medical platform that allows patients to manage appointments and access healthcare services while enabling doctors and administrators to manage patient information and healthcare operations efficiently.

---

## Features

### Authentication & Authorization

- User Registration

- User Login

- Role-Based Authorization

- Secure Authentication

### Patient Features

- View Personal Dashboard

- Book Appointments

- Manage Appointments

- View Medical Information

- Update Profile Information

### Doctor Features

- Manage Appointments

- View Patient Information

- Manage Medical Records

- Update Availability Schedule

### Admin Features

- Manage Doctors

- Manage Patients

- Manage Appointments

- Manage System Data

### General Features

- Responsive User Interface

- Notifications System

- Profile Management

- Data Validation

---

## Technologies Used

### Backend

- ASP.NET Core MVC

- C#

- Entity Framework Core

- LINQ

### Frontend

- HTML5

- CSS3

- Bootstrap 5

- JavaScript

- jQuery

### Database

- SQL Server

---

## Project Structure


MediConnect

│

├── Controllers

├── Models

├── Views

├── Data

├── Services

├── ViewModels

├── wwwroot

├── Migrations

└── Program.cs

---

## Installation

### Clone Repository

git clone https://github.com/your-username/MediConnect.git
### Navigate to Project

cd MediConnect
### Restore Packages

dotnet restore
### Apply Migrations

dotnet ef database update
### Run Application

dotnet run
---

## Database Configuration

Configure:

appsettings.json
{

&#x20; "ConnectionStrings": {

&#x20;   "DefaultConnection": "YOUR\_CONNECTION\_STRING"

&#x20; }

}
---

## Future Improvements

- Online Consultations

- Electronic Prescriptions

- Medical Reports Export

- Mobile Application

- Real-Time Notifications

---

## License

This project was developed for educational purposes.
