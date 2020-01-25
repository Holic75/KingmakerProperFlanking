using CallOfTheWild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProperFlanking20
{
    class Compatibility
    {
        static internal void load()
        {
            //no cover provided by those under effect of arrowsongm isntrel bardic performance
            CallOfTheWild.Archetypes.ArrowsongMinstrel.precise_minstrel.AddComponent(CallOfTheWild.Helpers.Create<CoverSpecial.NoCoverToCasterIfCoverProviderHasBuff>(n => n.buff = CallOfTheWild.Archetypes.ArrowsongMinstrel.precise_minstrel_no_cover_buff));
        }
    }
}
