using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Redis
{
    public class RequestScript
    {
        public RequestScript()
        {
            Model = new List<Manuscript>();
        }

        public int isSec { get; set; }

        public List<Manuscript> Model { get; set; }
    }
}
