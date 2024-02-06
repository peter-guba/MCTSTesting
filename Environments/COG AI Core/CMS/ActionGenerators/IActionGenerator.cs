using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMS.Actions;

namespace CMS.ActionGenerators
{
    public interface IActionGenerator
    {
        List<ActionStatePair> GenerateActions(GameEnvironment environment);
        IEnumerable<ActionStatePair> EnumerateActions(GameEnvironment environment);

        int GetActionCount(GameEnvironment environment);
    }
}
