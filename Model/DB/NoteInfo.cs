using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model
{
    public class NoteInfo
    {
        public int node_id { get; set; }
        public string node_pid { get; set; }
        public int order_no { get; set; }
        public string node_path { get; set; }
        public string node_name { get; set; }
        public string map_key { get; set; }
        public string obj_table { get; set; }
    }
}
