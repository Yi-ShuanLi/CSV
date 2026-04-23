using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV
{
    internal class Student
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public Student(string id, string name, string email)
        {
            Id = id;
            Name = name;
            Email = email;

        }
        public Student() { }
    }

}
