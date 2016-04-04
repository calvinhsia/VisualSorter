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
            MainWindow._ShowSort = false;
            TestContext.WriteLine("Start");
            foreach (var sortType in MainWindow._SortTypes.Where(
                /*
                t => true
                /*/
                t => 
                t != "Cycle" 
                && t != "Bubble" 
                && t != "Unknown"
                && t != "Stupid"
                && t != "Selection"
                && t != "Insertion"
                && t != "MergeIP"
                && t != "OddEven"

                //*/
                ))
            {
                for (int pow = 0, nTotal = 1; pow < 7; pow++)
                {
                    var mwindow = new MainWindow();
                    mwindow._nRows = 1000000; // don't limit data by # rows
                    var arrData = mwindow.InitData(ref nTotal, maxDatalength: 4, ltrsOnly: false);
                    mwindow.DoTheSorting(arrData, sortType, nTotal);
                    var errs = mwindow.ValidateSorted(arrData, nTotal);
                    Assert.IsTrue(string.IsNullOrEmpty(errs), $"{sortType}  {errs}");
                    TestContext.WriteLine($"{sortType,10} {MainWindow.SortBox.stats}");
                    nTotal *= 10;
                }
            }
        }
        /*
    ActiveSheet.PasteSpecial Format:="Unicode Text", Link:=False, _
        DisplayAsIcon:=False
    Range("A1:B5").Select
    Selection.EntireRow.Delete
    Columns("B:P").Select
    Columns("B:P").EntireColumn.AutoFit
    Range("C:C,E:E,G:G,I:I,K:K,M:M,O:O").Select
    Range("O1").Activate
    Selection.EntireColumn.Hidden = True
    Range("B1").Select
    Selection.EntireRow.Insert , CopyOrigin:=xlFormatFromLeftOrAbove
    ActiveCell.FormulaR1C1 = "Sort"
    Range("D1").Select
    ActiveCell.FormulaR1C1 = "Secs"
    Range("F1").Select
    ActiveCell.FormulaR1C1 = "Items"
    Range("H1").Select
    ActiveCell.FormulaR1C1 = "Compares"
    Range("J1").Select
    ActiveCell.FormulaR1C1 = "Updates"
    Range("L1").Select
    ActiveCell.FormulaR1C1 = "Reads"
    Range("N1").Select
    ActiveCell.FormulaR1C1 = "Writes"
    Range("P1").Select
    ActiveCell.FormulaR1C1 = "Depth"
    Range("H10").Select
    ActiveSheet.ListObjects.Add(xlSrcRange, Range("$B$1:$P$50"), , xlYes).Name = _
        "Table1"
    Range("Table1[#All]").Select
    Sheets.Add
    ActiveWorkbook.PivotCaches.Create(SourceType:=xlDatabase, SourceData:= _
        "Table1", Version:=6).CreatePivotTable TableDestination:="Sheet2!R3C1", _
        TableName:="PivotTable1", DefaultVersion:=6
    Sheets("Sheet2").Select
    Cells(3, 1).Select
    With ActiveSheet.PivotTables("PivotTable1").PivotFields("Sort")
        .Orientation = xlColumnField
        .Position = 1
    End With
    With ActiveSheet.PivotTables("PivotTable1").PivotFields("Items")
        .Orientation = xlRowField
        .Position = 1
    End With
    ActiveSheet.PivotTables("PivotTable1").AddDataField ActiveSheet.PivotTables( _
        "PivotTable1").PivotFields("Secs"), "Sum of Secs", xlSum
    ActiveChart.ClearToMatchStyle
    ActiveSheet.Shapes.AddChart2(227, xlLine).Select
    ActiveChart.SetSourceData Source:=Range("Sheet2!$A$3:$I$12")
    Application.Goto Reference:="Macro1"
        */
    }
}
