using System;
using BambooConsumer.Models;
using System.Configuration;

using Xunit;

namespace BambooConsumerTests
{
    public class BambooEmployeeTests
    {
        [Theory]
        [InlineData("Kevin", "Jackson")]
        [InlineData(">?~Kevin", "Jackson")]
        [InlineData("Kevin", "Jackson jr")]
        [InlineData("Kevin", "Jackson, Jr")]
        [InlineData("Kevin", "John-Jackson")]
        [InlineData("Kevin", "Jackson, Jr.")]
        [InlineData("Kevin", "Jackson sr.")]
        [InlineData("Kevin", "John-Jackson Jr")]
        [InlineData("Kevin", "John-Jackson jr.")]
        [InlineData("Kevin", "John-Jackson, Jr")]
        [InlineData("Kevin", "John-Jackson, Jr.")]
        [InlineData("Kevin", "Jackson III")]
        [InlineData("Kevin", "John-Jackson III")]
        public void TestNameSanitizing(string firstName, string lastName)
        {
            BambooEmployee employee = new BambooEmployee
            {
                Id = "242",
                FirstName = firstName,
                LastName = lastName,
            };

            string expected = "KJackson";

            Assert.Equal(expected, employee.AnticipatedSAM);
        }

        [Theory]
        [InlineData("Kevin#%@@", "John-Jackson Jr")]
        [InlineData("Kevin", "John-Jackson jr.")]
        [InlineData("Kevin", "John-Jackson, Jr")]
        [InlineData("Kevin", "John-Jackson, Jr.")]
        [InlineData("Kevin", "John-Jackson III")]
        [InlineData("Kevin", "John-Jackson")]
        [InlineData("Kevin", "$#@John-Jackson")]
        [InlineData("Kevin@#!", ")*$John-Jackson")]
        public void TestEmailSanitizingWithHyphenated(string firstName, string lastName)
        {
            BambooEmployee employee = new BambooEmployee
            {
                Id = "242",
                FirstName = firstName,
                LastName = lastName
            };

            string expected = "Kevin.John-Jackson@testdomain.com";

            // Application config file does not work here.
            // string expected = ConfigurationManager.AppSettings["Domain"];
            Assert.Equal(expected, employee.AnticipatedEmail);
        }

        [Theory]
        [InlineData("Kevin", "Jackson III")]
        [InlineData("Kevin", "Jackson sr.")]
        [InlineData("Kevin", "Jackson")]
        [InlineData(">?~Kevin", "Jackson")]
        [InlineData("Kevin", "Jackson jr")]
        [InlineData("Kevin", "Jackson, Jr")]
        [InlineData("Kevin", "Jackson, Jr.")]
        public void TestEmailSanitizing(string firstName, string lastName)
        {
            BambooEmployee employee = new BambooEmployee
            {
                Id = "242",
                FirstName = firstName,
                LastName = lastName
            };

            string expected = "Kevin.Jackson@testdomain.com";

            Assert.Equal(expected, employee.AnticipatedEmail);
        }

        [Theory]
        [InlineData("Jackson, Kevin")]
        [InlineData("Jackson Sr., Kevin")]
        [InlineData("Jackson sr., Kevin")]
        [InlineData("Jackson sr, Kevin")]
        [InlineData("Jackson jr, Kevin")]
        [InlineData("Jackson Jr., Kevin")]
        [InlineData("Jackson jr., Kevin")]
        [InlineData("Jackson Jr, Kevin")]
        [InlineData("Jackson!, Kevin")]
        [InlineData("John-Jackson, Kevin")]
        [InlineData("J'ackson, Kevin")]
        public void TestParseRawName(string rawName)
        {
            BambooEmployee employee = new BambooEmployee
            {
                Id = "242",
                Supervisor = rawName
            };

            string expected = "KJackson";

            Assert.Equal(expected, employee.AnticipatedManagerSAM);
        }
    }
}
