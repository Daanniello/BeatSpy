using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace BeatSpy
{
    public class SaveDataLocal
    {

        /// <summary>
        /// Saves All data as json on the local machine
        /// </summary>
        public static void Save(object data)
        {
            string jsonString = JsonSerializer.Serialize(data);
        }
    }
}
