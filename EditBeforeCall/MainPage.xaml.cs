using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.UserData;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using NavigationListControl;
using System.Windows.Data;

namespace EasyDial
{
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        // Constructor
        PhoneNumberChooserTask phoneNumberChooserTask;
        protected string CurrentTaskDisplayName;
        protected string CurrentTaskDisplayNumber;
        protected string phoneNumber;
        protected string displayName;
        protected string dialledNo;
        private bool isDialled = false;
        // Data context for the local database
        private HistoryDataContext HistoryDB;
        private UserData userdata;

        private byte diallerState = 0;
        private PhoneCallTask pct = null;
        CancellationTokenSource cts;
        Task<List<PhoneBook>> task;

        //History Page Variables
        private ReadHistoryDataContext readHistoryDB;
        private const int maxHistoryItems = 500;
        public bool Image
        {
            get
            {
                return (Visibility)Resources["PhoneDarkThemeVisibility"] == Visibility.Visible;
            }
        }

        // Define an observable collection property that controls can bind to.
        private ObservableCollection<history> _historyItems;
        public ObservableCollection<history> HistoryItems
        {
            get
            {
                return _historyItems;
            }
            set
            {
                if (_historyItems != value)
                {
                    _historyItems = value;
                    NotifyPropertyChanged("HistoryItems");
                }
            }
        }

        private ICommand removeCommand = null;

        public ICommand RemoveCommand
        {
            get
            {
                return this.removeCommand;
            }
        }
        
        internal void UpdateHistory()
        {
            var callHistoryItemsInDB = (from history historyItem in readHistoryDB.HistoryItems
                                        orderby historyItem.Callserialno descending
                                        select historyItem);
            HistoryItems = new ObservableCollection<history>(callHistoryItemsInDB);
            if (HistoryItems.Count > maxHistoryItems)
            {
                var lastItem = HistoryItems[maxHistoryItems];
                var toDelete = callHistoryItemsInDB.Where(a => a.Callserialno < lastItem.Callserialno);
                foreach (history item in toDelete)
                {
                    readHistoryDB.HistoryItems.DeleteOnSubmit(item);
                }
                readHistoryDB.SubmitChanges();
            }
            lstCalls.ItemsSource = HistoryItems;

        }


        // Define an observable collection property that controls can bind to.
        private ObservableCollection<PhoneBook> _contactItems;
        public ObservableCollection<PhoneBook> ContactItems
        {
            get
            {
                return _contactItems;
            }
            set
            {
                if (_contactItems != value)
                {
                    _contactItems = value;
                    NotifyPropertyChanged("ContactItems");
                }
            }
        }
        
        
        public MainPage()
        {
            InitializeComponent();
            //showContact = String.Empty;
            dialledNo = String.Empty;

            // Connect to the database and instantiate data context.
            HistoryDB = new HistoryDataContext(HistoryDataContext.DBConnectionString);
            
            // Data context and observable collection are children of the main page.
            this.DataContext = this;
            
            phoneNumberChooserTask = new PhoneNumberChooserTask();
            phoneNumberChooserTask.Completed += new EventHandler<PhoneNumberResult>(phoneNumberChooserTask_Completed);
            CurrentTaskDisplayName = String.Empty;
            (Application.Current as App).RootFrame.Obscured += OnObscured;
            (Application.Current as App).RootFrame.Unobscured += OnUnobscured;

            diallerState = 0;
            isDialled = false;
            SearchStringManager.SearchString = string.Empty;
            textBox1.Focus();
            //History Page Block
            readHistoryDB = new ReadHistoryDataContext(ReadHistoryDataContext.DBReadConnectionString);
            UpdateHistory();

        }

 
        void OnObscured(object sender, ObscuredEventArgs e)
        {
            if (diallerState == 3 && isDialled)
            {
                DateTime callTime = DateTime.Now;
                history newCall = new history
                {
                    DialledNo = pct.PhoneNumber
                ,
                    ContactName = pct.DisplayName
                ,
                    CallTime = callTime
                };


                // Add a history item to the local database.
                HistoryDB.HistoryItems.InsertOnSubmit(newCall);

                // Save changes to the database.
                HistoryDB.SubmitChanges();
                diallerState = 0;
                isDialled = false;
                UpdateHistory();
            }
            else
                if(isDialled) diallerState++;
        }
 
        void OnUnobscured(object sender, EventArgs e)
        {
            if (isDialled) diallerState++;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Call the base method.
            base.OnNavigatedTo(e);

            // Define the query to gather all of the history items.
            var callHistoryItemsInDB = from history historyItem in HistoryDB.HistoryItems
                                select historyItem;

            // Execute the query and place the results into a collection.
            //HistoryItems = new ObservableCollection<history>(callHistoryItemsInDB);

            // Fetch the parameters passed from querystring

            if (NavigationContext.QueryString.ContainsKey("phoneNumber"))
            {
                phoneNumber = NavigationContext.QueryString["phoneNumber"].ToString();
            }

            if (NavigationContext.QueryString.ContainsKey("displayName"))
            {
                displayName = NavigationContext.QueryString["displayName"].ToString();
            }
            isDialled = false;
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        private string currentTextData;
        private  Timer timer;

        // Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion


        private void UpdateTile()
        {
            // Application Tile is always the first Tile, even if it is not pinned to Start.
            ShellTile TileToFind = ShellTile.ActiveTiles.First();
            
            // Application should always be found
            if (TileToFind != null)
            {
                
                // Set the properties to update for the Application Tile.
                // Empty strings for the text values and URIs will result in the property being cleared.
                StandardTileData NewTileData = new StandardTileData
                {
                    Title = (DeviceNetworkInformation.IsNetworkAvailable || !String.IsNullOrEmpty(DeviceNetworkInformation.CellularMobileOperator)) ? DeviceNetworkInformation.CellularMobileOperator : "Easy Dial",
                };

                // Update the Application Tile
                TileToFind.Update(NewTileData);
            }

        }

        private void hyperlinkButton1_Click(object sender, RoutedEventArgs e)
        {

            //this.Content = new ContactsList();
            phoneNumberChooserTask.Show();
            
            //return;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            
            pct = new PhoneCallTask();
            pct.PhoneNumber = textBox1.Text;

            diallerState = 1;
            isDialled = true;
            if (textBox1.Text == CurrentTaskDisplayNumber)
                pct.DisplayName = CurrentTaskDisplayName;
            else
                pct.DisplayName = String.Empty;
            pct.Show();
            

            //Send the no to history
            // Create a new history item based on the text box.
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Call the base method.
            isDialled = false;
            base.OnNavigatedFrom(e);
        }  

        void phoneNumberChooserTask_Completed(object sender, PhoneNumberResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                CurrentTaskDisplayNumber = e.PhoneNumber;
                textBox1.Text = CurrentTaskDisplayNumber;
                CurrentTaskDisplayName = e.DisplayName;
            }
                
            textBox1.Focus();
        }

        private void sms_Click(object sender, RoutedEventArgs e)
        {
            SmsComposeTask sct = new SmsComposeTask();
            sct.To = textBox1.Text;
            sct.Show();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTile();

            //history
            //lstCalls.Width = Application.Current.RootVisual.RenderSize.Width - 10;
            //lstCalls.Height = Application.Current.RootVisual.RenderSize.Height - 300;

            if (!String.IsNullOrEmpty(dialledNo))
                CurrentTaskDisplayNumber = dialledNo;
            else if (!String.IsNullOrEmpty(phoneNumber))
                CurrentTaskDisplayNumber = phoneNumber;
            if (!String.IsNullOrEmpty(displayName))
                CurrentTaskDisplayName = displayName;

            textBox1.Text = (String.IsNullOrEmpty(CurrentTaskDisplayNumber)?"":CurrentTaskDisplayNumber) ;
            textBox1.Focus();

            timer = new Timer(new TimerCallback((x) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    this.TimerFun(x);
                });
            }), this, Timeout.Infinite, Timeout.Infinite);


            userdata = new UserData();

            userdata.OnLoaded += new UserData.UserDataLoadedHandler(() =>
            {
                InvokeTrieSearch();
            });
            userdata.LoadDatabase();
            cts = new CancellationTokenSource();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            SmsComposeTask sct = new SmsComposeTask();
            string DisplayName = string.Empty;
            if (textBox1.Text == CurrentTaskDisplayNumber)
                DisplayName = CurrentTaskDisplayName +": ";
            

            sct.Body = DisplayName + ((textBox1.Text.Trim() == string.Empty) ? CurrentTaskDisplayNumber : textBox1.Text.Trim());
            sct.Show();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveContactTask saveContactTask;
            saveContactTask = new SaveContactTask();
            saveContactTask.Completed += new EventHandler<SaveContactResult>(saveContactTask_Completed);
            saveContactTask.MobilePhone = textBox1.Text.Trim();
            saveContactTask.Show();
        }

        private void reviewButton_Click(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        void saveContactTask_Completed(object sender, SaveContactResult e)
        {
        }

        void textbox1_LostFocus(object s, EventArgs e)
        {
            if (SearchStringManager.CacheList != null && ContactItems.Count < SearchStringManager.CacheList.Count)
            {
                Timer t = new Timer(new TimerCallback(x =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        ContactItems = new ObservableCollection<PhoneBook>(SearchStringManager.CacheList);
                    });
                }));
                t.Change(500, Timeout.Infinite);
            }
            this.ApplicationBar.Mode = ApplicationBarMode.Default;
        }

        void textbox1_GotFocus(object s, EventArgs e)
        {
            this.ApplicationBar.Mode = ApplicationBarMode.Minimized;
        }

        private void historyButton_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            //this.NavigationService.Navigate(new Uri("/History.xaml", UriKind.Relative));
        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {
            
            if (textBox1.Text.Length == 0)
            {
                lstContact.Visibility = System.Windows.Visibility.Collapsed;
                lstCalls.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                SearchStringManager.SearchString = textBox1.Text;
                InvokeTrieSearch();
            }
        }
        
        private void peopleButton_Click(object sender, EventArgs e)
        {
            phoneNumberChooserTask.Show();
        }

        private void panelCall_Hold(object sender, EventArgs e)
        {
            history deleteItem = (history)((MenuItem)sender).DataContext;
            HistoryItems.Remove(deleteItem);
            var isAttached = readHistoryDB.HistoryItems.GetOriginalEntityState(deleteItem);

            if (isAttached == null)
                readHistoryDB.HistoryItems.Attach(deleteItem);

            // Delete the selected history item from the local database. 
            readHistoryDB.HistoryItems.DeleteOnSubmit(deleteItem);

            // Save changes to the database. 
            readHistoryDB.SubmitChanges();

        }

        private void InvokeTrieSearch()
        {
            if (timer != null)
                timer.Change(250, Timeout.Infinite);
        }

        public void TimerFun(object c)
        {
            string text = ((MainPage)c).textBox1.Text;
            if (userdata.DataReady)
            {
                cts.Cancel();
                cts = new CancellationTokenSource();
                currentTextData = text;
                List<PhoneBook> list = userdata.FetchData(currentTextData, cts);
                Dispatcher.BeginInvoke(() =>
                {
                    SearchStringManager.CacheList = list;
                    ContactItems = new ObservableCollection<PhoneBook>(list.Take(6));
                    if (textBox1.Text.Length != 0)
                    {
                        lstCalls.Visibility = System.Windows.Visibility.Collapsed;
                        lstContact.Visibility = System.Windows.Visibility.Visible;
                    }
                });
            }
        }
        
        private void name_Click(object sender, EventArgs e)
        {
            PhoneBook item = (PhoneBook)((Button)sender).DataContext;
            CurrentTaskDisplayName = item.DisplayName;
            textBox1.Text = CurrentTaskDisplayNumber = item.PhoneNumber;
        }

        private void call_Click(object sender, EventArgs e)
        {
            PhoneBook item = (PhoneBook)((Button)sender).DataContext;
            string dialledno = item.PhoneNumber;
            string contactName = item.DisplayName;
            diallerState = 1;
            isDialled = true;

            pct = new PhoneCallTask();
            pct.PhoneNumber = dialledno;
            pct.DisplayName = (String.IsNullOrEmpty(contactName) ? "" : contactName);
            pct.Show();
        }

        private void callHistoryBtn_Click(object sender, EventArgs e)
        {
            history item = (history)((Button)sender).DataContext;
            string dialledno = item.DialledNo;
            string contactName = item._contactName;
            diallerState = 1;
            isDialled = true;

            pct = new PhoneCallTask();
            pct.PhoneNumber = dialledno;
            pct.DisplayName = (String.IsNullOrEmpty(contactName) ? "" : contactName);
            pct.Show();
        }


        private void people_Click(object sender, RoutedEventArgs e)
        {
            history item = (history)((Button)sender).DataContext;
            CurrentTaskDisplayName = item._contactName;
            textBox1.Text = CurrentTaskDisplayNumber = item.DialledNo;
        }

    }  
}