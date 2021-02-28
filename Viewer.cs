using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace twitchBot
{
    [Serializable]
    public class Viewer
    {
        /// <summary>
        /// Viewer Points
        /// </summary>
        public int Points = 0;
        /// <summary>
        /// Viewer Name
        /// </summary>
        public string Name;
        public string Type;
        public int Answers = 0;

        public bool hasPermission = false;





    }
}
