using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TourGuideFunction.Excursions
{
    public record Excursion(string Name, DateTimeOffset DateFrom, DateTimeOffset DateTo, string OriginCity, int Price);
}
