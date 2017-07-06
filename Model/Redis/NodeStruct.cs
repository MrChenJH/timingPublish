using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Redis
{
    public class NodeStruct
    {
        public int AutoNo { get; set; }

        public string NodeName { get; set; }

        public int NodeId { get; set; }

        public int[] ChildNodeIds { get; set; }
    }
}
