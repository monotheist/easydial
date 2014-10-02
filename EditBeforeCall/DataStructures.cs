using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
namespace EasyDial
{

    public class CustomContact
    {
        Contact contact;
        public CustomContact(Contact c)
        {
            contact = c;
            DisplayName = c.DisplayName;

            DefaultNumber = string.Empty;
            foreach (var PhoneNumber in c.PhoneNumbers)
            {
                if (PhoneNumber.Kind == PhoneNumberKind.Mobile)
                {
                    DefaultNumber = PhoneNumber.PhoneNumber;
                    break;
                }
            }

            if (DefaultNumber == string.Empty && c.PhoneNumbers.Count()>0)
            {
                DefaultNumber = c.PhoneNumbers.FirstOrDefault().PhoneNumber;
            }
        }

        public CustomContact(Contact c, ContactPhoneNumber Phonenumber)
        {
            contact = c;
            DisplayName = c.DisplayName;
            this.DefaultNumber = Phonenumber.PhoneNumber;
            this.ContactType = Phonenumber.Kind.ToString();


          
        }

       

        public string Image { get; set; }
        public string DisplayName { get; set; }

        public string DefaultNumber { get; set; }

        public string ContactType { get; set; }
    }
    
    [Table]
    public class history : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Define item name: private field, public property and database column.
        private int _callserialno;
        private string _dialledNo;
        internal string _contactName;
        private string _callDetails;
        private DateTime _callTime;
        private bool _isComplete;
       

        public string Image
        {
            get;
            set;
        }

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Callserialno
        {
            get { return _callserialno; }
            set
            {
                if (_callserialno != value)
                {
                    NotifyPropertyChanging("Callserialno");
                    _callserialno = value;
                    NotifyPropertyChanged("Callserialno");
                }
            }
        }

        [Column]
        public string DialledNo
        {
            get
            {
                return _dialledNo;
            }
            set
            {
                if (_dialledNo != value)
                {
                    NotifyPropertyChanging("DialledNo");
                    _dialledNo = value;
                    NotifyPropertyChanged("DialledNo");
                }
            }
        }

        [Column]
        public string ContactName
        {
            get
            {

                if (string.IsNullOrEmpty(_contactName))
                    return _dialledNo;
                else
                    return _contactName;

            }
            set
            {
                if (_contactName != value)
                {
                    NotifyPropertyChanging("DialledContact");
                    _contactName = value;
                    NotifyPropertyChanged("DialledContact");
                }
            }
        }

        [Column]
        public string CallDetails
        {
            get { return _callDetails; }
            set
            {
                if (_callDetails != value)
                {
                    NotifyPropertyChanging("DialledContact");
                    _callDetails = value;
                    NotifyPropertyChanged("DialledContact");
                }
            }
        }

        [Column]
        public bool IsComplete
        {
            get
            {
                return _isComplete;
            }
            set
            {
                if (_isComplete != value)
                {
                    NotifyPropertyChanging("IsComplete");
                    _isComplete = value;
                    NotifyPropertyChanged("IsComplete");
                }
            }

        }

        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary _version;

        [Column]
        public DateTime CallTime
        {
            get
            {
                return _callTime;
            }
            set
            {
                if (_callTime != value)
                {
                    NotifyPropertyChanging("CallTime");
                    _callTime = value;
                    NotifyPropertyChanged("CallTime");
                }
            }
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion


    }

    public static class SearchStringManager
    {
        public static string SearchString;

        public static List<PhoneBook> CacheList; 
        static SearchStringManager()
        {
            SearchString = string.Empty;
            CacheList = null;
        }
    }

    [Table]
    public class PhoneBook : INotifyPropertyChanged, INotifyPropertyChanging
    {



        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int SerialNo
        {
            get { return _SerialNo; }
            set
            {
                if (_SerialNo != value)
                {
                    NotifyPropertyChanging("SerialNo");
                    _SerialNo = value;
                    NotifyPropertyChanged("SerialNo");
                }
            }
        }

        [Column(CanBeNull = false)]
        public string DisplayName
        {
            get { return _DisplayName; }
            set
            {
                if (_DisplayName != value)
                {
                    NotifyPropertyChanging("DisplayName");
                    _DisplayName = value;
                    NotifyPropertyChanged("DisplayName");
                }
            }
        }

        public string DisplayNameHighlighted
        {
            get;
            set;
            //get { return _DisplayName + "&#160<Run Foreground=\"Red\">Hello</Run>&#160"; }
        }


        [Column(CanBeNull = false)]
        public string PhoneNumber
        {
            get { return _PhoneNumber; }
            set
            {
                if (_PhoneNumber != value)
                {
                    NotifyPropertyChanging("PhoneNumber");
                    _PhoneNumber = value;
                    NotifyPropertyChanged("PhoneNumber");
                }
            }
        }

        [Column]
        public string NumberType
        {
            get { return _NumberType; }
            set
            {
                if (_NumberType != value)
                {
                    NotifyPropertyChanging("NumberType");
                    _NumberType = value;
                    NotifyPropertyChanged("NumberType");
                }
            }
        }

        public string DisplayPrefix
        {
            get
            {
                try
                {
                    if (!isNumberSearch)
                        return _DisplayName.Substring(0, _DisplayName.IndexOf(matchedName,StringComparison.OrdinalIgnoreCase)); //_DisplayName.Substring(SearchStringManager.SearchString.Length);
                    else return _DisplayName;
                   // return matchedName;
                }
                catch (Exception e)
                {
                    return "";
                }
            }
        }

        public string Highlighted
        {
            get
            {
                try
                {
                    if (!isNumberSearch)
                        return _DisplayName.Substring(_DisplayName.IndexOf(matchedName,StringComparison.OrdinalIgnoreCase), matchedName.Length); //matchedName;// _DisplayName.Substring(0, SearchStringManager.SearchString.Length);
                    else
                        return string.Empty;
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }
            }
        }

       

        public string DisplaySuffix
        {
            get {
                try
                {
                    if (!isNumberSearch)
                        return _DisplayName.Substring(_DisplayName.IndexOf(matchedName,StringComparison.OrdinalIgnoreCase) + matchedName.Length); //_DisplayName.Substring(SearchStringManager.SearchString.Length);
                    else return string.Empty;
                }
                catch (Exception e)
                {
                    return string.Empty;
                }
            }
        }


        public string DisplayNumberPrefix
        {
            get {
                try
                {
                    if (isNumberSearch)
                        return _PhoneNumber.Substring(0, _PhoneNumber.IndexOf(matchedNumber));
                    else return _PhoneNumber;
                }
                catch (Exception e)
                {
                    return string.Empty;
                }
            }
        }

        public string DisplayNumberHighlighted
        {
            get
            {
                if (isNumberSearch)
                    return matchedNumber;
                else
                    return string.Empty;
                }
        }

        public string DisplayNumberSuffix
        {
            get {
                try
                {
                    if (isNumberSearch)
                        return _PhoneNumber.Substring(_PhoneNumber.IndexOf(matchedNumber) + matchedNumber.Length);
                    else
                        return string.Empty;
                }
                catch (Exception ex)
                {
                    return string.Empty;
                }
            }
        }




        public string Image { get; set; }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;
        private string _PhoneNumber;
        private string _NumberType;
        private string _DisplayName;
        private int _SerialNo;
        public bool isNumberSearch = false;
        public string matchedNumber = string.Empty;
        public string matchedName = string.Empty;

        public PhoneBook(string _PhoneNumber, string _DisplayName, string _NumberType )
        {
            // TODO: Complete member initialization
            this._PhoneNumber = _PhoneNumber;
            this._DisplayName = _DisplayName;
            this._NumberType = _NumberType;
        }

        public PhoneBook()
        {
        }

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        #endregion
    }

    public class HistoryDataContext : DataContext
    {
        // Specify the connection string as a static, used in main page and app.xaml.
        public static string DBConnectionString = "Data Source=isostore:/history.sdf";

        // Pass the connection string to the base class.
        public HistoryDataContext(string connectionString)
            : base(connectionString)
        { }

        // Specify a single table for the history items.
        public Table<history> HistoryItems;

        public Table<PhoneBook> PhoneBookItems;
    }


}
