using System;
using System.Collections.Generic;
using System.Linq;
using API.Models;
using System.Threading.Tasks;

namespace API.Helpers
{
    public class Storage
    {
        private static Storage _instance = null;

        public static Storage Instance
        {
            get
            {
                if (_instance == null) _instance = new Storage();
                return _instance;
            }
        }
        public List<LZW> HistoryList = new List<LZW>();
    }
}
