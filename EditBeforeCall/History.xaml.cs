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
using System.Data.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Phone;
using System.Windows.Data;
using System.Globalization;

namespace EasyDial
{
    
    public partial class History : PhoneApplicationPage, INotifyPropertyChanged
    {
        // Constructor
        PhoneNumberChooserTask phoneNumberChooserTask;
        private ReadHistoryDataContext readHistoryDB;
        private byte diallerState = 0;
        private PhoneCallTask pct = null;
        private bool isDialled = false;
        private const int maxHistoryItems = 500;


        public bool Image
        {
            get
            {
                return (Visibility)Resources["PhoneDarkThemeVisibility"]== Visibility.Visible;
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

       
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

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


        public History()
        {
            InitializeComponent();
            readHistoryDB = new ReadHistoryDataContext(ReadHistoryDataContext.DBReadConnectionString);
            // Define the query to gather all of the history items.

            
            // Execute the query and place the results into a collection.
            
            lstCalls.Width = Application.Current.RootVisual.RenderSize.Width - 10;
            lstCalls.Height = Application.Current.RootVisual.RenderSize.Height - 300;

            phoneNumberChooserTask = new PhoneNumberChooserTask();
            phoneNumberChooserTask.Completed += new EventHandler<PhoneNumberResult>(phoneNumberChooserTask_Completed);
            (Application.Current as App).RootFrame.Obscured += OnObscured;
            (Application.Current as App).RootFrame.Unobscured += OnUnobscured;
            UpdateHistory();
            
        }

        private void keypadButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        private void peopleButton_Click(object sender, EventArgs e)
        {
            phoneNumberChooserTask.Show();
        }

        private void searchButton_Click(object sender, EventArgs e)
        {

        }

        private void call_Click(object sender, EventArgs e)
        {
            history item = (history)((Button)sender).DataContext;
            string dialledno = item.DialledNo;
            string contactName = item._contactName;
            diallerState = 1;
            isDialled = true;

            pct = new PhoneCallTask();
            pct.PhoneNumber = dialledno;
            pct.DisplayName = (String.IsNullOrEmpty(contactName)?"":contactName);
            pct.Show();
          
        }

        void phoneNumberChooserTask_Completed(object sender, PhoneNumberResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                this.Dispatcher.BeginInvoke(new Action(
            () =>
            {
                this.NavigationService.Navigate(new Uri(String.Format("/MainPage.xaml?phoneNumber={0}&displayName={1}", e.PhoneNumber, e.DisplayName), UriKind.Relative));
            }));

                
            }
        }

        private void History_Loaded(object sender, RoutedEventArgs e)
        {          

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Call the base method.
            UpdateHistory();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Call the base method.
            base.OnNavigatedFrom(e);
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

        void OnObscured(object sender, ObscuredEventArgs e)
        {
            if (diallerState == 3 && isDialled)
            {
                history newCall = new history
                {
                    DialledNo = pct.PhoneNumber,
                    ContactName = pct.DisplayName,
                    CallTime = DateTime.Now
                };

                // Add a history item to the observable collection.
                //HistoryItems.Insert(0,newCall);
                //NotifyPropertyChanged("HistoryItems");

                // Add a history item to the local database.
                readHistoryDB.HistoryItems.InsertOnSubmit(newCall);

                // Save changes to the database.
                readHistoryDB.SubmitChanges();
                UpdateHistory();

                diallerState = 0;
                isDialled = false;
            }
            else
                if (isDialled) diallerState++;
        }

        void OnUnobscured(object sender, EventArgs e)
        {
            if (isDialled) diallerState++;
        }

        private void reviewButton_Click(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        private void people_Click(object sender, RoutedEventArgs e)
        {
            history item = (history)((Button)sender).DataContext;
            string dialledno = item.DialledNo;
            string contactName = item._contactName;

            this.NavigationService.Navigate(new Uri(String.Format("/MainPage.xaml?phoneNumber={0}&displayName={1}", HttpUtility.UrlEncode(dialledno), HttpUtility.UrlEncode(contactName)), UriKind.Relative));
        }

    }

  //  [ValueConversion(typeof(string), typeof(String))]
    public class PhoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = (string)value;
            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

 public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return ((SolidColorBrush)parameter).Color == Color.FromArgb(255, 0, 0, 0) ? "call.png" : "call_lightTheme.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ReadHistoryDataContext : DataContext
    {
        // Specify the connection string as a static, used in main page and app.xaml.
        public static string DBReadConnectionString = "Data Source=isostore:/history.sdf";

        // Pass the connection string to the base class.
        public ReadHistoryDataContext(string connectionString)
            : base(connectionString)
        { }

        // Specify a single table for the history items.
        public Table<history> HistoryItems;
    }

    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {            
            DateTime date = (DateTime)value;
            DateTime now = DateTime.Now;
            if ((int)now.Subtract(date).TotalDays >= 7)
                return ", " + String.Format("{0:MM/dd/yyyy hh:mm}", date);
            else
                return ", " + date.ToString("ddd") + " " + date.ToString("hh:mm");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }
}