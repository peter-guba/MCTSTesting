using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;

namespace CMS.ActionGenerators
{
    public class ActionStatePair
    {
        public List<ActionCms> PlayerAction { get; set; }
        public GameEnvironment Environment { get; set; }
    }
}
