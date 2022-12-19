using System;

namespace TodoListAPI.Models
{
    public class PasswordProfile
    {
        public string password { get; set; }
        public bool forceChangePasswordNextSignIn { get; set; }
    }

    public class AdUser
    {
        public Guid? id { get; set; }
        public bool accountEnabled { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string department { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string jobTitle { get; set; }
        public string mailNickname { get; set; }
        public string passwordPolicies { get; set; }
        public PasswordProfile passwordProfile { get; set; }
        public string officeLocation { get; set; }
        public string postalCode { get; set; }
        public string preferredLanguage { get; set; }
        public string state { get; set; }
        public string streetAddress { get; set; }
        public string surname { get; set; }
        public string mobilePhone { get; set; }
        public string usageLocation { get; set; }
        public string userPrincipalName { get; set; }
    }

}
