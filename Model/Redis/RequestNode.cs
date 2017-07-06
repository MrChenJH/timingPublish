using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Redis
{
    public class RequestNode
    {
        public RequestNode()
        {
            Model = new List<NodeStruct>();
        }

        public int isSec { get; set; }

        public List<NodeStruct> Model { get; set; }
    }
}
