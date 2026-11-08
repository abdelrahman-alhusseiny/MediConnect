using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MedicalSystem.Utilities.Utilities
{
    public static class SD
    {
        public const string Role_Patient = "Patient";
        public const string Role_Doctor = "Doctor";
        public const string Role_Admin = "Admin";

        public const string Status_Pending = "Pending";
        public const string Status_Confirmed = "Confirmed";
        public const string Status_Completed = "Completed";
        public const string Status_Cancelled = "Cancelled";

        public const string Status_Failed = "Failed";

        public const string PaymentMethod_Online = "Online";
        public const string PaymentMethod_Cash = "Cash";
        public const string PaymentMethod_Card = "Card";
        public const string PaymentMethod_PayPal = "PayPal";

        public const string Status_Readed = "Readed";
        public const string Status_Unreaded = "Unreaded";

        public const string Type_Appointment = "Appointment";
        public const string Type_Payment = "Payment";
        public const string Type_System = "System";
        public const string Type_Health = "Health";
        public const string Type_Account = "Account";
        public const string Type_Review = "Review";

        public const string PaymentStatus_Completed = "Completed";
        public const string PaymentStatus_NotPaid = "NotPaid";
        public const string PaymentStatus_Failed = "Failed";
    }
}
