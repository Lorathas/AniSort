using System.Collections.Generic;
using System.Threading.Tasks;

namespace AniSort.Core.Tasks
{
    public class Defragment
    {
        private readonly List<string> _directories = new List<string>();

        public Defragment(IEnumerable<string> directories)
        {
            _directories.AddRange(directories);
        }

        public async Task RunAsync()
        {
            
        }
    }
}