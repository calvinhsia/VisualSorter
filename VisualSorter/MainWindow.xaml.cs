using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        public static string[] _SortTypes =
        {
            "Bubble",
            "Unknown",
            "Stupid",
            "Insertion",
            "Selection",
            "OddEven",
            "Cycle",
            "Merge",
            "MergeIP",
            "MergeIP2",
            "Quick",
            "Shell",
            "Heap",
            "Super",
        };
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.Title = "Visual Sorter";
            this.Loaded += MainWindow_Loaded;
        }
        public static CancellationTokenSource _cancelTokSrc;
        public static bool _ShowSort = true;
        internal Canvas _canvas = new Canvas();
        public static int _spControlsHeight = 60;
        public int _nRows;

        public class SortBox : Label, IComparable
        {
            public struct Stats
            {
                public DateTime startTime;
                public int numItems;
                public long numCompares;
                public long numReads;
                public long numWrites;
                public int MaxDepth;
                public override string ToString()
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var strDepth = string.Empty;
                    if (MaxDepth >= 0)
                    {
                        strDepth = $" MaxDepth= {MaxDepth}";
                    }
                    return $"Secs= {elapsed,9:n3} Items= {numItems,8} Compares= {numCompares,13:n0} Reads= {numReads,13:n0} Writes= {numWrites,13:n0}{strDepth}";
                }
            }
            public static Stats stats;
            internal static void InitStats(int nTotal)
            {
                stats = new Stats()
                {
                    numItems = nTotal,
                    startTime = DateTime.Now,
                    MaxDepth = -1
                };
            }

            public SortBox()
            {
                //                this.FontSize = 8;
            }
            public void Swap(SortBox other)
            {
                if (this != other)
                {
                    var temp = this.Data;
                    this.Data = other.Data;
                    other.Data = temp;
                }
            }
            private string _data;
            public string Data
            {
                get
                {
                    stats.numReads++;
                    return _data;
                }
                set
                {
                    _data = value;
                    stats.numWrites++;
                    this.Update();
                }
            }
            public static bool operator <(SortBox a, SortBox b)
            {
                stats.numCompares++;
                if (string.CompareOrdinal(a.Data, b.Data) < 0)
                {
                    return true;
                }
                return false;
            }
            public static bool operator >(SortBox a, SortBox b)
            {
                stats.numCompares++;
                if (string.CompareOrdinal(a.Data, b.Data) > 0)
                {
                    return true;
                }
                return false;
            }
            public void Update()
            {
                if (_ShowSort)
                {
                    bool fCancel = false;
                    Dispatcher.Invoke(() =>
                    {
                        // set the content on the UI thread
                        this.Content = this.Data;
                        if (_cancelTokSrc != null)
                        {
                            // check input queue for messages
                            var stat = GetQueueStatus(4);
                            if (stat != 0)
                            {
                                //need to throw on right thread
                                fCancel = true;
                            }
                        }
                    });
                    if (fCancel)
                    {
                        //need to throw on right thread
                        _cancelTokSrc.Cancel();
                        throw new TaskCanceledException("User cancelled");
                    }
                }
            }
            public override string ToString()
            {
                return $"{this.Data}";
            }

            public int CompareTo(object obj)
            {
                stats.numCompares++;
                var other = obj as SortBox;
                if (other != null)
                {
                    return string.CompareOrdinal(this.Data, other.Data);
                }
                var otherAsString = obj as string;
                return string.CompareOrdinal(this.Data, otherAsString);
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _nRows = ((int)this.ActualHeight - _spControlsHeight - 50) / 10;
                this.Content = _canvas;
                var spControls = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    MaxHeight = _spControlsHeight
                };
                _canvas.Children.Add(spControls);
                var btnSort = new Button()
                {
                    Content = "Do_Sort",
                    ToolTip = "Will generate data and sort. Click to cancel",
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Top
                };
                spControls.Children.Add(btnSort);
                var cboSortType = new ComboBox()
                {
                    ItemsSource = _SortTypes,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 150,
                };
                cboSortType.SelectedIndex = 8;
                spControls.Children.Add(cboSortType);
                var txtNumItems = new TextBox()
                {
                    Text = "4000",
                    ToolTip = "Max Number of items to sort. (limited by display)",
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 100
                };
                spControls.Children.Add(txtNumItems);
                var spVertControls = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                spControls.Children.Add(spVertControls);
                var chkLettersOnly = new CheckBox()
                {
                    Content = "_Letters only",
                    ToolTip = "Include just letters or other characters too",
                    IsChecked = true
                };
                spVertControls.Children.Add(chkLettersOnly);
                var chkShowSort = new CheckBox()
                {
                    Content = "Show ",
                    ToolTip = "Update display during sort. Turn this off to see performance without updaating",
                    IsChecked = true
                };
                chkShowSort.Checked += (os, es) => _ShowSort = true;
                chkShowSort.Unchecked += (os, es) => _ShowSort = false;
                spVertControls.Children.Add(chkShowSort);
                var txtMaxDataLength = new TextBox()
                {
                    Text = "0",
                    Width = 40,
                    ToolTip = "Max # of random chars per datum. 0 means use a dictionary of real words"
                };
                spVertControls.Children.Add(txtMaxDataLength);
                var txtStatus = new TextBox()
                {
                    Margin = new Thickness(10, 0, 0, 0),
                    Width = 900,
                    Height = _spControlsHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Action<string> addStatusMsg = (str) =>
                {
                    Dispatcher.BeginInvoke(new Action(
                        () =>
                        {
                            txtStatus.AppendText($"{str}\r\n");
                            txtStatus.ScrollToEnd();
                        }
                        ));
                };
                spControls.Children.Add(txtStatus);
                List<SortBox> arrData = null;
                int nTotal = 0;
                btnSort.Click += (ob, eb) =>
                {
                    _cancelTokSrc = new CancellationTokenSource();
                    btnSort.IsEnabled = false;
                    var sortType = (string)cboSortType.SelectedValue;
                    if (arrData == null ||
                        !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift)
                    )
                    {
                        // lets create controls on main thread
                        _canvas.Children.Clear();
                        _canvas.Children.Add(spControls);
                        nTotal = int.Parse(txtNumItems.Text);
                        arrData = new List<SortBox>();
                        var maxDatalength = int.Parse(txtMaxDataLength.Text);
                        arrData = InitData(ref nTotal, maxDatalength, chkLettersOnly.IsChecked.Value);
                        addStatusMsg($"Starting {sortType} with {nTotal} items. Click anywhare to stop");
                    }
                    else
                    {// user left shift-click: continue sorting with a possible different algorithm
                        // note: cancellation can result in slight data errors because exception thrown
                        for (int i = 1; i < nTotal; i++)
                        {
                            if (arrData[i] < arrData[i - 1])
                            {
                                arrData[i].FontWeight = FontWeights.Normal;
                            }
                        }
                        addStatusMsg($"Continuing with  {sortType}   {nTotal} items. Click anywhare to stop");
                    }
                    var tsk = Task.Run(() =>
                    {
                        // do the sorting on a background thread
                        try
                        {
                            DoTheSorting(arrData, sortType, nTotal);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            addStatusMsg($"Exception {ex.ToString()}");
                        }

                        txtStatus.Dispatcher.BeginInvoke(new Action(
                            () =>
                            {
                                // now that we're done, let's verify
                                var stats = SortBox.stats;
                                var hasError = ValidateSorted(arrData, nTotal);
                                if (!_ShowSort)
                                {
                                    // show sorted results
                                    for (int i = 0; i < nTotal; i++)
                                    {
                                        arrData[i].Content = arrData[i].Data;
                                    }
                                }
                                string donemsg = _cancelTokSrc.IsCancellationRequested ? "Aborted" : "Done   ";
                                addStatusMsg($"{sortType,10} {donemsg} {stats} {hasError}");
                                btnSort.IsEnabled = true;
                            }));
                    });
                };
                btnSort.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, this));
            }
            catch (Exception ex)
            {
                this.Content = ex.ToString();
            }
        }

        public string ValidateSorted(List<SortBox> arrData, int nTotal)
        {
            var hasError = string.Empty;

            int nErrors = 0;
            for (int i = 1; i < nTotal; i++)
            {
                if (arrData[i] < arrData[i - 1])
                {
                    nErrors++;
                    arrData[i].FontWeight = FontWeights.ExtraBold;
                }
            }
            if (nErrors > 0)
            {
                hasError = $"Error! {nErrors} not sorted";
            }
            return hasError;
        }

        public List<SortBox> InitData(ref int nTotal, int maxDatalength, bool ltrsOnly)
        {
            var arrData = new List<SortBox>();
            var rand = new Random(1);
            /* to use without the dictionary (using random letters) comment the current line and the new Dictionary line below
             
            dynamic dict = null;
            maxDatalength = 5;

            /*/
            // get the dictionary from my OneDrive:
            // https://onedrive.live.com/redir?resid=D69F3552CEFC21!99083&authkey=!AFjyjUlZpH5sQy0&ithint=file%2cdll 
            // then run the command RegSvr32 dictionary.dll
            // then add a reference to COM ->Dictionary 1.0 Type Library
            Dictionary.CDict dict = null;
            //*/
            int colWidth;
            // we will try to fill the screen with sort data. 
            // However, there are times when a particular # of items is desired,
            // such as debugging a list of 5 items.
            // so we limit by nTotal or screen capacity 
            if (maxDatalength == 0)
            {
                dict = new Dictionary.CDict(); // comment out this line if no dictionary
                dict.DictNum = 2;
                maxDatalength = 11;
                colWidth = 8 * (maxDatalength - 1);
            }
            else
            {
                colWidth = 20 + 8 * (maxDatalength - 1);
            }
            int nCols = (int)this.ActualWidth / colWidth;
            if (nCols == 0) //tests
            {
                nCols = 80;
            }
            for (int i = 0; i < _nRows; i++)
            {
                for (int j = 0; j < nCols; j++)
                {
                    if (arrData.Count < nTotal)
                    {
                        string dat = string.Empty;
                        switch (arrData.Count)
                        {
                            // set the first few items to const so easier to debug algorithms
                            case 0:
                                dat = "zero";
                                break;
                            case 1:
                                dat = "one";
                                break;
                            case 2:
                                dat = "two";
                                break;
                            case 3:
                                dat = "three";
                                break;
                            case 4:
                                dat = "four";
                                break;
                            case 5:
                                dat = "five";
                                break;
                            default:
                                var len = 1 + rand.Next(maxDatalength);
                                var datarray = new char[len];
                                for (int k = 0; k < len; k++)
                                {
                                    // "A" is 65,"!" is 33
                                    datarray[k] = (char)(ltrsOnly == true ?
                                        65 + rand.Next(26) :
                                        33 + rand.Next(90));
                                }
                                dat = new string(datarray);
                                break;
                        }
                        var box = new SortBox();
                        if (dict == null)
                        {
                            box.Data = dat.Substring(0, Math.Min(maxDatalength, dat.Length));
                        }
                        else
                        {
                            box.Data = dict.RandWord(0);
                        }
                        box.Content = box.Data;
                        arrData.Add(box);
                        if (_ShowSort)
                        {
                            Canvas.SetTop(box, 3 + _spControlsHeight + i * 10);
                            Canvas.SetLeft(box, j * colWidth);
                            _canvas.Children.Add(box);
                        }
                    }
                }
            }
            nTotal = arrData.Count; // could be less
            if (!_ShowSort)
            {
                // show initial values
                for (int i = 0; i < nTotal; i++)
                {
                    arrData[i].Content = arrData[i].Data;
                }
            }
            SortBox.InitStats(nTotal);
            return arrData;
        }

        public void DoTheSorting(List<SortBox> arrData, string sortType, int nTotal)
        {
            switch (sortType)
            {
                case "Bubble":
                    var nEnd = nTotal;
                    var newEnd = 0;
                    do
                    {
                        newEnd = 0;
                        for (int i = 1; i < nEnd; i++)
                        {
                            if (arrData[i - 1] > arrData[i])
                            {
                                arrData[i - 1].Swap(arrData[i]);
                                newEnd = i;
                            }
                        }
                        nEnd = newEnd;
                    } while (newEnd != 0);
                    break;
                case "Unknown":
                    for (int i = 1; i < nTotal; i++)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (arrData[i] < arrData[j])
                            {
                                arrData[i].Swap(arrData[j]);
                            }
                        }
                    }
                    break;
                case "Stupid": // gnomeSort
                    for (int i = 0; i < nTotal;)
                    {
                        if (i == 0 || !(arrData[i - 1] > arrData[i]))
                        {
                            i++;
                        }
                        else
                        {
                            arrData[i - 1].Swap(arrData[i]);
                            i--;
                        }
                    }
                    break;
                case "Insertion":
                    for (int i = 1; i < nTotal; i++)
                    {
                        var t = arrData[i].Data;
                        int j = i - 1;
                        for (; j >= 0 && arrData[j].CompareTo(t) > 0; j--)
                        {
                            arrData[j + 1].Data = arrData[j].Data;
                        }
                        //int j = i - 1;
                        //while (j >= 0 && arrData[j].CompareTo(t) > 0)
                        //{
                        //    arrData[j + 1].Data = arrData[j].Data;
                        //    j--;
                        //}
                        arrData[j + 1].Data = t;
                    }
                    break;
                case "Selection":
                    // scan entire array for minimum, swap with 1st, then repeat for rest
                    for (int i = 0; i < nTotal - 1; i++)
                    {
                        int minimum = i;
                        for (int j = i + 1; j < nTotal; j++)
                        {
                            if (arrData[j] < arrData[minimum])
                            {
                                minimum = j;
                            }
                        }
                        if (minimum != i)
                        {
                            arrData[i].Swap(arrData[minimum]);
                        }
                    }
                    break;
                case "OddEven":
                    var sorted = false;
                    while (!sorted)
                    {
                        sorted = true;
                        for (int i = 1; i < nTotal - 1; i += 2)
                        {
                            if (arrData[i + 1] < arrData[i])
                            {
                                arrData[i + 1].Swap(arrData[i]);
                                sorted = false;
                            }
                        }
                        for (int i = 0; i < nTotal - 1; i += 2)
                        {
                            if (arrData[i + 1] < arrData[i])
                            {
                                arrData[i + 1].Swap(arrData[i]);
                                sorted = false;
                            }
                        }
                    }
                    break;
                case "Cycle":
                    // deceptively fast because fewer updates, 
                    // which are expensive in this code because of thread
                    // context switches and drawing updated data
                    // note the # of updates <= ntotal
                    for (int cycleStart = 0; cycleStart < nTotal; cycleStart++)
                    {
                        // the item to place
                        var item = arrData[cycleStart].Data;
                        int pos = cycleStart;
                        do
                        {
                            int nextPos = 0;
                            // find it's position # in entire array
                            for (int i = 0; i < nTotal; i++)
                            {
                                if (i != cycleStart && arrData[i].CompareTo(item) < 0)
                                {
                                    nextPos++;
                                }
                            }
                            if (pos != nextPos)
                            {
                                // move past duplicates
                                while (pos != nextPos && arrData[nextPos].CompareTo(item) == 0)
                                {
                                    nextPos++;
                                }
                                // save the cur value at nextpos
                                var temp = arrData[nextPos].Data;
                                // set the value at nextpos to the item to place
                                arrData[nextPos].Data = item;
                                // new value for which to seek position
                                item = temp;
                                pos = nextPos;
                            }
                        } while (pos != cycleStart);
                    }
                    break;
                case "Merge":
                    // not in place: uses additional storage
                    // lets make a recursive lambda
                    Action<int, int, int> MergeSort = null;
                    MergeSort = (left, right, depth) =>
                    {
                        SortBox.stats.MaxDepth = Math.Max(depth, SortBox.stats.MaxDepth);
                        if (right > left)
                        {
                            int mid = (right + left) / 2;
                            MergeSort(left, mid, depth + 1);
                            mid++;
                            MergeSort(mid, right, depth + 1);
                            // now we merge 2 sections that are already sorted
                            int leftNdx = left;
                            // use extra storage
                            var temp = new string[right - left + 1];
                            int tmpIndex = 0;
                            int pivot = mid;
                            while (leftNdx < pivot && mid <= right)
                            {
                                // fill temp from left or right
                                if (arrData[mid] < arrData[leftNdx])
                                {
                                    temp[tmpIndex++] = arrData[mid++].Data;
                                }
                                else
                                {
                                    temp[tmpIndex++] = arrData[leftNdx++].Data;
                                }
                            }
                            // deal with leftovers on left or right
                            while (leftNdx < pivot)
                            {
                                temp[tmpIndex++] = arrData[leftNdx++].Data;
                            }
                            while (mid <= right)
                            {
                                temp[tmpIndex++] = arrData[mid++].Data;
                            }
                            // fill the elements with the sorted list
                            for (int i = 0; i < tmpIndex; i++)
                            {
                                arrData[left + i].Data = temp[i];
                            }
                        }
                    };
                    MergeSort(0, nTotal - 1, 0);
                    break;
                case "MergeIP":
                    Action<int, int, int> MergeSortIp = null;
                    MergeSortIp = (left, right, depth) =>
                    {
                        SortBox.stats.MaxDepth = Math.Max(depth, SortBox.stats.MaxDepth);
                        if (left >= right)
                        {
                            return;
                        }
                        int mid = (left + right) / 2;
                        MergeSortIp(left, mid, depth + 1);
                        mid++;
                        MergeSortIp(mid, right, depth + 1);
                        int leftNdx = left;
                        int pivot = mid;
                        int leftEnd = pivot - 1;
                        while (leftNdx <= leftEnd && mid <= right)
                        {
                            if (arrData[leftNdx] < arrData[mid])
                            {
                                // left already in place
                                leftNdx++;
                            }
                            else
                            {
                                // take from right: shift everyone over to make room
                                var temp = arrData[mid].Data;
                                for (int j = mid - 1; j >= leftNdx; j--)
                                {
                                    arrData[j + 1].Data = arrData[j].Data;
                                }
                                arrData[leftNdx].Data = temp;
                                leftNdx++;
                                leftEnd++;
                                mid++;
                            }
                        }
                    };
                    MergeSortIp(0, nTotal - 1, 0);
                    break;
                case "MergeIP2":
                    //http://stackoverflow.com/questions/2571049/how-to-sort-in-place-using-the-merge-sort-algorithm/22839426#22839426
                    Action<int, int> reverse = (a, b) =>
                    {
                        for (--b; a < b; a++, b--)
                        {
                            arrData[a].Swap(arrData[b]);
                        }
                    };
                    Func<int, int, int, int> rotate = (a, b, c) =>
                    {
                        //* swap the sequence [a,b) with [b,c). 
                        if (a != b && b != c)
                        {
                            reverse(a, b);
                            reverse(b, c);
                            reverse(a, c);
                        }
                        return a + c - b;
                    };
                    Func<int, int, SortBox, int> lower_bound = (a, b, key) =>
                    {
                        //* find first element not less than @p key in sorted sequence or end of
                        // * sequence (@p b) if not found. 
                        for (int i = b - a; i != 0; i /= 2)
                        {
                            int mid = a + i / 2;
                            if (arrData[mid] < key)
                            {
                                a = mid + 1;
                                i--;
                            }
                        }
                        return a;
                    };
                    Func<int, int, SortBox, int> upper_bound = (a, b, key) =>
                    {
                        ///* find first element greater than @p key in sorted sequence or end of
                        //* sequence (@p b) if not found. 
                        for (int i = b - a; i != 0; i /= 2)
                        {
                            int mid = a + i / 2;
                            if (arrData[mid].CompareTo(key) <= 0)
                            {
                                a = mid + 1;
                                i--;
                            }
                        }
                        return a;
                    };
                    Action<int, int, int, int> mergeInPlace = null;
                    mergeInPlace = (left, mid, right, depth) =>
                    {
                        SortBox.stats.MaxDepth = Math.Max(depth, SortBox.stats.MaxDepth);
                        int n1 = mid - left;
                        int n2 = right - mid;
                        if (n1 == 0 || n2 == 0)
                        {
                            return;
                        }
                        if (n1 == 1 && n2 == 1)
                        {
                            if (arrData[mid] < arrData[left])
                            {
                                arrData[mid].Swap(arrData[left]);
                            }
                        }
                        else
                        {
                            int p, q;
                            if (n1 <= n2)
                            {
                                q = mid + n2 / 2;
                                p = upper_bound(left, mid, arrData[q]);
                            }
                            else
                            {
                                p = left + n1 / 2;
                                q = lower_bound(mid, right, arrData[p]);
                            }
                            mid = rotate(p, mid, q);

                            mergeInPlace(left, p, mid, depth + 1);
                            mergeInPlace(mid, q, right, depth + 1);
                        }
                    };
                    Action<int, int, int> inPlaceMergeSort = null;
                    inPlaceMergeSort = (left, nElem, depth) =>
                    {
                        if (nElem > 1)
                        {
                            int mid = nElem / 2;
                            inPlaceMergeSort(left, mid, depth + 1);
                            inPlaceMergeSort(left + mid, nElem - mid, depth + 1);
                            mergeInPlace(left, left + mid, left + nElem, depth + 1);
                        }
                    };
                    inPlaceMergeSort(0, nTotal, 0);
                    break;
                case "Quick":
                    Action<int, int, int> quickSort = null;
                    quickSort = (left, right, depth) =>
                    {
                        SortBox.stats.MaxDepth = Math.Max(depth, SortBox.stats.MaxDepth);
                        if (left < right)
                        {
                            var pivot = arrData[left];
                            int i = left;
                            int j = right;
                            while (i < j)
                            {
                                // find the leftmost one that should be on the right
                                while (i < right && !(arrData[i] > pivot))
                                {
                                    i++;
                                }
                                // set j to the rightmost one that should be on the left
                                while (arrData[j] > pivot)
                                {
                                    j--;
                                }
                                if (i < j)
                                {
                                    arrData[i].Swap(arrData[j]);
                                }
                            }
                            // now put pivot into place
                            pivot.Swap(arrData[j]);
                            // now recur to sort left, then right sides 
                            quickSort(left, j - 1, depth + 1);
                            quickSort(j + 1, right, depth + 1);
                        }
                    };
                    // now do the actual sort
                    quickSort(0, nTotal - 1, 0);
                    break;
                case "Shell":
                    for (int g = nTotal / 2; g > 0; g /= 2)
                    {
                        for (int i = g; i < nTotal; i++)
                        {
                            for (int j = i - g; j >= 0 && arrData[j] > arrData[j + g]; j -= g)
                            {
                                arrData[j].Swap(arrData[j + g]);
                            }
                        }
                    }
                    break;
                case "Heap":
                    // https://simpledevcode.wordpress.com/2014/11/25/heapsort-c-tutorial/
                    int heapSize = 0;
                    Action<int, int> Heapify = null;
                    Heapify = (index, depth) =>
                    {
                        SortBox.stats.MaxDepth = Math.Max(SortBox.stats.MaxDepth, depth);
                        int left = 2 * index;
                        int right = 2 * index + 1;
                        int largest = index;

                        if (left <= heapSize && arrData[left] > arrData[index])
                        {
                            largest = left;
                        }
                        if (right <= heapSize && arrData[right] > arrData[largest])
                        {
                            largest = right;
                        }

                        if (largest != index)
                        {
                            arrData[index].Swap(arrData[largest]);
                            Heapify(largest, depth + 1);
                        }
                    };
                    heapSize = nTotal - 1;
                    for (int i = nTotal / 2; i >= 0; i--)
                    {
                        Heapify(i, 0);
                    }
                    for (int i = nTotal - 1; i >= 0; i--)
                    {
                        arrData[0].Swap(arrData[i]);
                        heapSize--;
                        Heapify(0, 0);
                    }
                    break;
                case "Super":
                    var data = (from dat in arrData
                                orderby dat
                                select dat.Data).ToArray();
                    for (int i = 0; i < nTotal; i++)
                    {
                        arrData[i].Data = data[i];
                    }
                    break;
            }
        }

        [DllImport("user32.dll")]
        static extern uint GetQueueStatus(uint flags);
    }
}
