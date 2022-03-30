using NUnit.Framework;
using System;
using ReversiRestApi.Model;
using ReversiRestApi.Controllers;

namespace ReversiRestApiTests
{
    [TestFixture]
    public class SpelControllerTest
    {
        [Test]
        public void GetSpelOmschrijvingenVanSpellenMetWachtendeSpeler_Return_Games()
        {
            SpelController controller = new SpelController(null, null);
        }
    }
}