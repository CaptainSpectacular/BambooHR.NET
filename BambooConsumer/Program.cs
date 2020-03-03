using BambooConsumer.Models;
using BambooConsumer.Services;
using System.Collections.Generic;
using System.Configuration;

namespace BambooConsumer
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = ConfigurationManager.AppSettings["FilePath"];
            var updates = new List<BambooEmployee>();
            var employeeService = new EmployeeService();
            var changeService = new ChangeService();
            ChangeContainer changes = changeService.GetChangedEmployees();

            if (changes != null && changes.Employees != null)
            {
                foreach (KeyValuePair<string, ChangeEmployee> employee in changes.Employees)
                {
                    BambooEmployee bambooEmployee = employeeService.GetEmployee(employee.Value.Id);
                    updates.Add(bambooEmployee);
                }

                var fileWriter = new CsvUpdates(filePath);
                fileWriter.WriteToFile(updates, "BambooEmployeeUpdates.csv");
            }
        }
    }
}
