using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbucksCardTest
{
    class Program
    {
        static void Main(string[] args)
        {

            var card = new StarbucksCard.StarbucksCard("username", "password");
            card.Update();

            var diff = DateTime.Now - card.CardholderSince;

            decimal daysPerStar = diff.Days / card.LifetimeStars;

            Console.WriteLine("I have averaged a Starbucks run every {0} {1}.", Math.Round(daysPerStar, 2), (daysPerStar==1)?"day":"days");

            Console.ReadKey();

        }
    }
}
