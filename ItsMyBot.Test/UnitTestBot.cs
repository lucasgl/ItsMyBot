using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ItsJanaBot.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRandomGenerator()
        {
            Task.Run(async () =>
            {              
                Random rdm = new Random(DateTime.Now.Millisecond);
                //for (int i = 0; i < 10; i++)
                //{
                //    Trace.WriteLine("Old:" + rdm.Next(0,2));
                //    Trace.WriteLine("New:" + cmd.RandomGenerator(DateTime.Now.Millisecond, 2, 10));
                //}

                var random = new Random(DateTime.Now.Millisecond);
                int winnerPosition = random.Next(0, 2);

                await Task.Delay(1000);
                for (int i = 6 - 1; i >= 1; i--)
                {
                    winnerPosition = random.Next(0, 2);
                    Trace.WriteLine($"New: {i} ({winnerPosition})!");
                    await Task.Delay(1000);
                }
            }).Wait();
        }
    }
}
