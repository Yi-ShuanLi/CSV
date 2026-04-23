using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV
{
    internal class Teacher
    {
        public string Name { get; set; }
        public string Id { get; set; }
        //public string Email { get; set; }


        public Teacher(string id, string name)
        {
            Id = id;
            Name = name;
            //Email = email;

        }
        public Teacher() { }
    }
}
