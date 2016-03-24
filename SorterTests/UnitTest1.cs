using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApplication1;
using System.Linq;

namespace SorterTests
{
    [TestClass]
    public class UnitTest1
    {
        public TestContext TestContext { get; set; }
        [TestMethod]
        public void TestMethod1()
        {
            Action<string> addstatusmsg = (s) => { };
            MainWindow._ShowSort = false;
            TestContext.WriteLine("Start");
            foreach (var sortType in MainWindow._SortTypes.Where(
                /*
                t => true
                /*/
                t => t != "Cycle" && t != "Bubble" && t != "Unknown" && t != "Stupid"
                //*/
                ))
            {
                for (int pow = 0, nTotal = 1; pow < 7; pow++)
                {
                    var mwindow = new MainWindow();
                    mwindow._nRows = 10000; // don't limit data by # rows
                    var arrData = mwindow.InitData(ref nTotal, maxDatalength: 4, ltrsOnly: false);
                    mwindow.DoTheSorting(arrData, sortType, nTotal, addstatusmsg);
                    var errs = mwindow.ValidateSorted(arrData, nTotal);
                    Assert.IsTrue(string.IsNullOrEmpty(errs), $"{sortType}  {errs}");
                    TestContext.WriteLine($"{sortType,10} {MainWindow.SortBox.stats}");
                    nTotal *= 10;
                }
            }
        }
    }
}
