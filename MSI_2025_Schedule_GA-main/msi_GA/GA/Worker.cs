using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace msi_GA.GA
{

    
    public class Worker
    {


        // 0 - intern , 1 - junior, 2 - mid, 3 - senior

        [JsonPropertyName("occupation")]
        private string _occupation;

        [JsonPropertyName("experience")]
        private int _experience;

        [JsonPropertyName("preferences")]
        int[] _preferences;
        [JsonConstructor]
        public Worker(string occupation, int experience, int[] preferences)
        {
            _occupation = occupation;
            _experience = experience;
            _preferences = preferences;

        }

        public int Experience => _experience;
        public string Occupation => _occupation;

        public int[] Preferences => _preferences;

        

    }
}
