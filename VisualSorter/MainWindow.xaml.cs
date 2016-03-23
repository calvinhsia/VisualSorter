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
            "Cycle",
            "Merge",
            "MergeIP",
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
        public int _spControlsHeight = 60;
        public int _nRows = 80;

        public class SortBox : Label, IComparable
        {
            public struct Stats
            {
                public DateTime startTime;
                public int numItems;
                public uint numCompares;
                public uint numUpdates;
                public uint numReads;
                public uint numWrites;
                public int MaxDepth;
                public override string ToString()
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    var strDepth = string.Empty;
                    if (MaxDepth >= 0)
                    {
                        strDepth = $" MaxDepth= {MaxDepth}";
                    }
                    return $"Secs= {elapsed,9:n2} Items= {numItems,6} Compares= {numCompares,11:n0} Updates= {numUpdates,11:n0} Reads= {numReads,11:n0} Writes= {numWrites,11:n0}{strDepth}";
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
            public static void Swap(SortBox a, SortBox b)
            {
                if (a != b)
                {
                    var temp = a.data;
                    a.data = b.data;
                    b.data = temp;
                    a.Update();
                    b.Update();
                }
            }
            private string _data;
            public string data
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
                }
            }
            public void SetData(string newvalue)
            {
                if (this.data != newvalue)
                {
                    this.data = newvalue;
                    this.Update();
                }
            }
            public static bool operator <(SortBox a, SortBox b)
            {
                stats.numCompares++;
                if (string.CompareOrdinal(a.data, b.data) < 0)
                {
                    return true;
                }
                return false;
            }
            public static bool operator >(SortBox a, SortBox b)
            {
                stats.numCompares++;
                if (string.CompareOrdinal(a.data, b.data) > 0)
                {
                    return true;
                }
                return false;
            }
            public void Update()
            {
                stats.numUpdates++;
                if (_ShowSort)
                {
                    bool fCancel = false;
                    Dispatcher.Invoke(() =>
                    {
                        // set the content on the UI thread
                        this.Content = this.data;
                        // check input queue for messages
                        var stat = GetQueueStatus(4);
                        if (stat != 0)
                        {
                            //need to throw on right thread
                            fCancel = true;
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
                return $"{this.data}";
            }

            public int CompareTo(object obj)
            {
                stats.numCompares++;
                var other = obj as SortBox;
                if (other != null)
                {
                    return string.CompareOrdinal(this.data, other.data);
                }
                var otherAsString = obj as string;
                return string.CompareOrdinal(this.data, otherAsString);
            }
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
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
                    Height = _spControlsHeight
                };
                spControls.Children.Add(btnSort);
                var cboSortType = new ComboBox()
                {
                    ItemsSource = _SortTypes,
                    Width = 150,
                };
                cboSortType.SelectedIndex = 6;
                spControls.Children.Add(cboSortType);
                var txtNumItems = new TextBox()
                {
                    Text = "4000",
                    ToolTip = "Max Number of items to sort. (limited by display)",
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
                var txtDataLength = new TextBox()
                {
                    Text = "5",
                    Width = 40,
                    ToolTip = "Max # of chars per datum"
                };
                spVertControls.Children.Add(txtDataLength);
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

                btnSort.Click += (ob, eb) =>
                {
                    _cancelTokSrc = new CancellationTokenSource();
                    var sortType = (string)cboSortType.SelectedValue;
                    // lets create controls on main thread
                    _canvas.Children.Clear();
                    _canvas.Children.Add(spControls);
                    int nTotal = int.Parse(txtNumItems.Text);
                    var arrData = new List<SortBox>();
                    var datalength = int.Parse(txtDataLength.Text);
                    arrData = InitData(ref nTotal, datalength, chkLettersOnly.IsChecked.Value);

                    var tsk = Task.Run(() =>
                    {
                        // do the sorting on a background thread
                        DoTheSorting(arrData, sortType, nTotal, addStatusMsg);
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
                                        arrData[i].Content = arrData[i].data;
                                    }
                                }
                                string donemsg = _cancelTokSrc.IsCancellationRequested ? "Aborted" : "Done   ";
                                addStatusMsg($"{sortType} {donemsg} {stats} {hasError}");
                            }));
                    });
                };
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

        public List<SortBox> InitData(ref int nTotal, int datalength, bool ltrsOnly)
        {
            var arrData = new List<SortBox>();
            var rand = new Random(1);
            int colWidth = 20 + 8 * (datalength - 1);
            int nCols = 60 - (datalength - 1) * 8;

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
                                dat = "zer".Substring(0, Math.Min(datalength, 3));
                                break;
                            case 1:
                                dat = "one".Substring(0, Math.Min(datalength, 3));
                                break;
                            case 2:
                                dat = "two".Substring(0, Math.Min(datalength, 3));
                                break;
                            case 3:
                                dat = "thr".Substring(0, Math.Min(datalength, 3));
                                break;
                            case 4:
                                dat = "fou".Substring(0, Math.Min(datalength, 3));
                                break;
                            default:
                                var len = 1 + rand.Next(datalength);
                                var datarray = new char[len];
                                for (int k = 0; k < len; k++)
                                {
                                    datarray[k] = (char)(ltrsOnly == true ?
                                        65 + rand.Next(26) :
                                        33 + rand.Next(90));
                                }
                                dat = new string(datarray);
                                break;
                        }
                        var box = new SortBox();
                        box.data = dat;
                        box.Content = box.data;
                        arrData.Add(box);
                        Canvas.SetTop(box, 10 + _spControlsHeight + i * 10);
                        Canvas.SetLeft(box, j * colWidth);
                        _canvas.Children.Add(box);
                    }
                }
            }
            nTotal = arrData.Count; // could be less
            if (!_ShowSort)
            {
                // show initial values
                for (int i = 0; i < nTotal; i++)
                {
                    arrData[i].Content = arrData[i].data;
                }
            }

            SortBox.InitStats(nTotal);
            return arrData;
        }

        public void DoTheSorting(List<SortBox> arrData, string sortType, int nTotal, Action<string> addStatusMsg)
        {
            try
            {
                addStatusMsg($"Starting {sortType} with {nTotal} items. Click anywhare to stop");
                switch (sortType)
                {
                    case "Bubble":
                        bool didSwap = false;
                        do
                        {
                            didSwap = false;
                            for (int i = 1; i < nTotal; i++)
                            {
                                if (arrData[i - 1] > arrData[i])
                                {
                                    didSwap = true;
                                    SortBox.Swap(arrData[i - 1], arrData[i]);
                                }
                            }
                        } while (didSwap);
                        break;
                    case "Unknown":
                        for (int i = 1; i < nTotal; i++)
                        {
                            for (int j = 0; j < i; j++)
                            {
                                if (arrData[i] < arrData[j])
                                {
                                    SortBox.Swap(arrData[i], arrData[j]);
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
                                SortBox.Swap(arrData[i - 1], arrData[i]);
                                i--;
                            }
                        }
                        break;
                    case "Insertion":
                        for (int i = 1; i < nTotal; i++)
                        {
                            var t = arrData[i].data;
                            int j = i - 1;
                            while (j >= 0 && arrData[j].CompareTo(t) > 0)
                            {
                                arrData[j + 1].SetData(arrData[j].data);
                                j--;
                            }
                            arrData[j + 1].SetData(t);
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
                                SortBox.Swap(arrData[i], arrData[minimum]);
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
                            var item = arrData[cycleStart].data;
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
                                    // save the cur value at pos
                                    var temp = arrData[nextPos].data;
                                    // set the value at pos to the item to place
                                    arrData[nextPos].SetData(item);
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
                            if (depth > SortBox.stats.MaxDepth)
                            {
                                SortBox.stats.MaxDepth = depth;
                            }
                            if (right > left)
                            {
                                int mid = (right + left) / 2;
                                MergeSort(left, mid, depth + 1);
                                mid++;
                                MergeSort(mid, right, depth + 1);
                                // now we merge 2 sections that are already sorted
                                int leftNdx = left;
                                var temp = new string[right - left + 1];
                                int tmpIndex = 0;
                                int pivot = mid;
                                while (leftNdx < pivot && mid <= right)
                                {
                                    // fill temp from left or right
                                    if (arrData[leftNdx].CompareTo(arrData[mid]) > 0)
                                    {
                                        temp[tmpIndex++] = arrData[mid++].data;
                                    }
                                    else
                                    {
                                        temp[tmpIndex++] = arrData[leftNdx++].data;
                                    }
                                }
                                // deal with leftovers on left or right
                                while (leftNdx < pivot)
                                {
                                    temp[tmpIndex++] = arrData[leftNdx++].data;
                                }
                                while (mid <= right)
                                {
                                    temp[tmpIndex++] = arrData[mid++].data;
                                }
                                // fill the elements with the sorted list
                                for (int i = 0; i < tmpIndex; i++)
                                {
                                    arrData[left + i].SetData(temp[i]);
                                }
                            }
                        };
                        MergeSort(0, nTotal - 1, 0);
                        break;
                    case "MergeIP":
                        //http://stackoverflow.com/questions/2571049/how-to-sort-in-place-using-the-merge-sort-algorithm/22839426#22839426
                        Action<int, int> reverse = (a, b) =>
                        {
                            for (--b; a < b; a++, b--)
                            {
                                SortBox.Swap(arrData[a], arrData[b]);
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
                            int i;
                            for (i = b - a; i != 0; i /= 2)
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

                            int i;
                            for (i = b - a; i != 0; i /= 2)
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
                            if (depth > SortBox.stats.MaxDepth)
                            {
                                SortBox.stats.MaxDepth = depth;
                            }
                            int n1 = mid - left;
                            int n2 = right - mid;

                            if (n1 == 0 || n2 == 0)
                                return;
                            if (n1 == 1 && n2 == 1)
                            {
                                if (arrData[mid] < arrData[left])
                                {
                                    SortBox.Swap(arrData[mid], arrData[left]);
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
                        // lets make a recursive lambda
                        Action<int, int, int> quickSort = null;
                        quickSort = (left, right, depth) =>
                        {
                            if (depth > SortBox.stats.MaxDepth)
                            {
                                SortBox.stats.MaxDepth = depth;
                            }
                            if (left < right)
                            {
                                var pivot = arrData[left];
                                // i will move in from the left, 
                                //  j from the right
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
                                        SortBox.Swap(arrData[i], arrData[j]);
                                    }
                                }
                                // now put pivot into place
                                SortBox.Swap(pivot, arrData[j]);

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
                                    SortBox.Swap(arrData[j], arrData[j + g]);
                                }
                            }
                        }
                        break;
                    case "Heap":
                        // https://simpledevcode.wordpress.com/2014/11/25/heapsort-c-tutorial/
                        int heapSize = 0;
                        Action<int> Heapify = null;
                        Heapify = (index) =>
                        {
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
                                SortBox.Swap(arrData[index], arrData[largest]);
                                Heapify(largest);
                            }
                        };
                        heapSize = nTotal - 1;
                        for (int i = nTotal / 2; i >= 0; i--)
                        {
                            Heapify(i);
                        }
                        for (int i = nTotal - 1; i >= 0; i--)
                        {
                            SortBox.Swap(arrData[0], arrData[i]);
                            heapSize--;
                            Heapify(0);
                        }
                        break;
                    case "Super":

                        var data = (from dat in arrData
                                    orderby dat
                                    select dat.data).ToArray();
                        for (int i = 0; i < nTotal; i++)
                        {
                            arrData[i].SetData(data[i]);
                        }
                        break;
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                addStatusMsg($"Exception {ex.ToString()}");
            }
        }

        [DllImport("user32.dll")]
        static extern uint GetQueueStatus(uint flags);
    }
}
