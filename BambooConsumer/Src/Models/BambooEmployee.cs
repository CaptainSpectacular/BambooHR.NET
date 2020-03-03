using System.Linq;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BambooConsumer.Models
{
    public class BambooEmployee
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string DisplayName { get; set; }
        public string JobTitle { get; set; }
        public string State { get; set; }
        public string Supervisor { get; set; }
        public string WorkPhone { get; set; }
        public string MobilePhone { get; set; }
        public string WorkEmail { get; set; }
        public string Status { get; set; }
        public string CustomLoginName { get; set; }
        public string AnticipatedEmail
        {
            get
            {
                return GetEmail(FirstName, LastName);
            }
        }
        public string AnticipatedSAM
        {
            get
            {
                return GetSAM(FirstName, LastName);
            }
        }

        public string AnticipatedManagerSAM
        {
            get
            {
                if (Supervisor != null && Supervisor != "")
                {
                    return GetSAM(ParseSupervisorName(Supervisor));
                }

                return null;
            }
        }

        private string GetEmail(string firstName, string lastName)
        {
            string fullName = SanitizeInput($"{firstName} {lastName}");
            string[] names = fullName.Split(' ');
            firstName = names.First();
            int lastNameIndex = AvoidSuffix(fullName, names.Length);
            lastName = names[lastNameIndex];

            return $"{firstName}.{lastName}@testdomain.com";
        }
        
        private string GetSAM(string firstName, string lastName = "")
        {
            string fullName = SanitizeInput($"{firstName} {lastName}", true);
            char firstInitial = fullName[0];
            string[] words = fullName.Split(' ');
            int lastNameIndex = AvoidSuffix(fullName, words.Length);
            lastName = words[lastNameIndex];

            return firstInitial + lastName;
        }

        private string SanitizeInput(string input, bool replaceHyphen = false)
        {
            input = input.Trim();
            string sanitized = string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == ' ') sanitized += c;
                else if (c == '-' && replaceHyphen) sanitized += ' ';
                else if (c == '-') sanitized += c;
                else { }
            }
            return sanitized;
        }

        private int AvoidSuffix(string name, int wordCount)
        {
            if (wordCount > 2)
            {
                if (name.Contains(" Sr") || name.Contains(" Jr") || 
                    name.Contains(" sr") || name.Contains(" jr") || 
                    name.Contains(" II"))
                    return wordCount - 2;
            }

            return wordCount - 1;
        }

        private string ParseSupervisorName(string rName)
        {
            // Parses format of "lastname, firstname"
            List<string> reversed = Regex.Split(rName, ", ")
                .Reverse()
                .ToList<string>();

            return reversed[0] + " " + reversed[1];
        }
    }
}
