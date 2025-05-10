using System;
using System.ComponentModel.DataAnnotations;

namespace Health_Care_MIS.Models
{
    public class RegistrationViewModel
    {
        public int PatientID { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstnameString { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string Lastname { get; set; }

        [Required]
        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime Date_of_birth { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        [Display(Name = "Contact Information")]
        public string ContactInfo { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Emergency Contact")]
        public string EmergencyContact { get; set; }

        [Display(Name = "Blood Type")]
        public string BloodType { get; set; }

        [Display(Name = "Insurance Information")]
        public string InsuranceInfo { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string email { get; set; }
    }
}
