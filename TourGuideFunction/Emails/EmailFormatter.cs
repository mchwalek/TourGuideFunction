using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourGuideFunction.Excursions;

namespace TourGuideFunction.Emails
{
    public static class EmailFormatter
    {
        public static string Format(IEnumerable<Excursion> excursions)
        {
            const string dateFormat = "yyyy-MM-dd";

            var stringBuilder = new StringBuilder();

            foreach(var excursion in excursions)
            {
                var line = $"{excursion.Name} | {excursion.DateFrom.ToString(dateFormat)} - {excursion.DateTo.ToString(dateFormat)} | Wylot: {excursion.OriginCity} | {excursion.Price} zł";
                stringBuilder.AppendLine(line);
            }

            return stringBuilder.ToString();
        }
    }
}
