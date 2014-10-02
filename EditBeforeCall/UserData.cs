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
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
namespace EasyDial
{
   

    public class UserData
    {
        Contacts contacts;
        Trie trie;
        bool isDataReady = false;
        List<PhoneBook> ContactsCache;
        
        public bool DataReady
        {
            get { return isDataReady; }
        }

        public delegate void UserDataLoadedHandler();
        public event UserDataLoadedHandler OnLoaded;
        HistoryDataContext HistoryDB;
        private bool isDataLoaded; 
        public UserData()
        {

            isDataReady = false;
            isDataLoaded = false;
            contacts = new Contacts();
            //Identify the method that runs after the asynchronous search completes.
            contacts.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(Contacts_SearchCompleted);

            //Start the asynchronous search.
            contacts.SearchAsync(string.Empty,FilterKind.None, "Contacts Fetch");
            HistoryDB = new HistoryDataContext(HistoryDataContext.DBConnectionString);

        }


        public void LoadDatabase()
        {
            var PhoneBookItems = (from PhoneBook PhoneBookItem in HistoryDB.PhoneBookItems
                                        orderby PhoneBookItem.DisplayName
                                        select PhoneBookItem);
            ContactsCache = new List<PhoneBook>(PhoneBookItems);
            trie = new Trie();
            foreach (PhoneBook item in PhoneBookItems)
            {
                //PhoneBook item1 = new PhoneBook("12345", "Abhijeet Gokhs", "Home");
                trie.InsertWord(item);

                //PhoneBook item2 = new PhoneBook("12345", "Sadaf Khan", "Home");
                //trie.InsertWord(item2);
            }

            isDataReady = true;
            if(PhoneBookItems.Any())
            {
                isDataLoaded = true;
                if (this.OnLoaded != null)
                    this.OnLoaded();
            }
        }


        public List<PhoneBook> FetchData(string prefix, CancellationTokenSource cts)
        {
            List<PhoneBook> list = (prefix.Length==0)?ContactsCache:trie.FindWordsT9(prefix, cts);

            if (prefix.Length > 1)
            {
                foreach (PhoneBook c in ContactsCache)
                {
                    c.isNumberSearch = false;

                    Match match = Regex.Match(c.PhoneNumber,GetRegex(prefix));
                    
                    if(match.Success)
                    {
                        list.Add(c);
                        c.isNumberSearch = true;
                        c.matchedNumber = match.Value;
                    }
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
            return list;
        }

        private string GetRegex(string prefix)
        {
            StringBuilder str = new StringBuilder();
            foreach (char c in prefix)
            {
                str.Append(Regex.Escape(c.ToString()));
                str.Append(@"[\(\)\-\+\sXx]*");
            }

            return str.ToString();
        }

        void Contacts_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            Timer t = new Timer(new TimerCallback(UpdateDB), e.Results, 0, Timeout.Infinite);
        }

        void UpdateDB(object o)
        {
            if (isDataReady == false)
                return;
            
           HistoryDB.PhoneBookItems.DeleteAllOnSubmit(ContactsCache);
           HistoryDB.SubmitChanges();

            IEnumerable<Contact> Results = ((IEnumerable<Contact>)o);

            foreach (Contact con in Results)
            {
                foreach (ContactPhoneNumber phonenumber in con.PhoneNumbers)
                {
                    HistoryDB.PhoneBookItems.InsertOnSubmit(new PhoneBook(phonenumber.PhoneNumber, con.DisplayName, phonenumber.Kind.ToString()));
                }
            }
            HistoryDB.SubmitChanges();

            if (!isDataLoaded)
            {
                LoadDatabase();
            }
        }
    }
}
