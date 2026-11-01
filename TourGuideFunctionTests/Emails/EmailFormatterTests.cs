using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TourGuideFunction.Emails;
using TourGuideFunction.Excursions;

namespace TourGuideFunctionTests.Emails
{
    public class EmailFormatterTests
    {
        [Fact]
        public void Format_ReturnsTextRepresenationOfExcursions()
        {
            var excursions = new List<Excursion>()
            {
                new("Exc1", new DateOnly(2023, 9, 1), new DateOnly(2023, 9, 2), "Katowice", 123),
                new("Exc2", new DateOnly(2023, 10, 1), new DateOnly(2023, 10, 2), "Warszawa", 456),
            };

            var result = EmailFormatter.Format(excursions);

            var expectedResult =
                "Exc1 | 2023-09-01 - 2023-09-02 | Wylot: Katowice | 123 zł\r\n" +
                "Exc2 | 2023-10-01 - 2023-10-02 | Wylot: Warszawa | 456 zł\r\n";
            result.Should().Be(expectedResult);
        }
    }
}
