using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System;
using System.Collections.Generic;

namespace EXPEDIT.Flow.ViewModels
{
    public class UserProfileViewModel
    {
        [HiddenInput, Required, DisplayName("Contact ID:")]
        public Guid? ContactID { get; set; }
        public string ContactName { get; set; }
        public string Title { get; set; }
        public string Surname { get; set; }
        public string Firstname { get; set; }
        public string Username { get; set; }
        public string Hash { get; set; }
        [DisplayName("Default Email")]
        public string DefaultEmail { get; set; }
        public DateTime? DefaultEmailValidated { get; set; }
        [DisplayName("Default Mobile")]
        public string DefaultMobile { get; set; }
        public DateTime? DefaultMobileValidated { get; set; }
        public string MiddleNames { get; set; }
        public string Initials { get; set; }
        public DateTime? DOB { get; set; }
        public string BirthCountryID { get; set; }
        public string BirthCity { get; set; }
        public Guid? AspNetUserID { get; set; }
        public Guid? XafUserID { get; set; }
        public string OAuthID { get; set; }
        public byte[] Photo { get; set; }
        public string ShortBiography { get; set; }
        //public Guid? ContactAddressID { get; set; }
        public Guid? AddressID { get; set; }
        public Guid? AddressTypeID { get; set; }
        [DisplayName("Company")]
        public string AddressName { get; set; }
        public int Sequence { get; set; }
        public string Street { get; set; }
        [DisplayName("Extended Address")]
        public string Extended { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public bool? IsHQ { get; set; }
        public bool? IsPostBox { get; set; }
        public bool? IsBusiness { get; set; }
        public bool? IsHome { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public Guid? LocationID { get; set; }
    }
 
}