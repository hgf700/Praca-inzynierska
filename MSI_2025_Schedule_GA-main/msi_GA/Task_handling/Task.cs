using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msi_GA.Task_handling
{
    public class Task
    {
        private int _experience;
        private int _skill;
        private int _day;




        public List<string> Skills = new List<string>
{
    // Electrical
    "High-Voltage",
    "Low-Voltage",
    "SCADA",
    "PLC",
    "Grid-Stability",

    // Mechanical
    "Turbine-Repair",
    "Pump-Maintenance",
    "Valve-Systems",
    "Welding",
    "Hydraulics",

};

        public Task(int experience, int skill, int day)
        {
            _skill = skill;
            _experience = experience;
            _day = day;


        }


        public int Skill
        {
            get { return _skill; }

        }

        public int Experience
        {
            get { return _experience; }
        }
        public int Day
        {
            get { return _day; }
        }



    }
}
