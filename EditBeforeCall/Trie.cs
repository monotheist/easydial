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
using System.Collections.Generic;
using Microsoft.Phone.UserData;
using System.Threading;

namespace EasyDial
{
    public class Trie
    {
        Node Root;

        public Trie()
        {
            Root = new Node(' ');
        }


        public void InsertWord(PhoneBook con)
        {
            string word = CleanWord(con.DisplayName);
            
            Node current = Root;
            foreach (char c in word)
            {
                current = current.Insert(c);
            }
            current.MarkFinal(con);

            char[] splits = { ' ', '-', ',' };
            string[] words = word.Split(splits);
            if (words.Length > 1)
            {
                for (int i = 1; i < words.Length; i++)
                {
                    if (words[i].Length > 2)
                    {
                        Node cur = Root;
                        foreach (char c in words[i])
                        {
                            cur = cur.Insert(c);
                        }
                        cur.MarkFinal(con);
                    }
                }
            }

        }

        /*public List<PhoneBook> FindWords(string prefix)
        {
            prefix = CleanWord(prefix);
            Node current = Root;
            foreach (char c in prefix)
            {
                current = current.Look(c);
                if (current == null)
                    return null;
            }
            List<PhoneBook> list = new List<PhoneBook>();
            ReturnWords(current, list);

            return list;
        }*/

        public List<PhoneBook> FindWordsT9(string prefix, CancellationTokenSource cts)
        {
            List<Node> list = new List<Node>();
            Node.FindNodesT9(Root, prefix, list, 0, "");

            List<PhoneBook> contactList = new List<PhoneBook>();

            foreach(Node n in list)
            {
                cts.Token.ThrowIfCancellationRequested();
             //  ReturnWords(n, contactList);
                Node.FindWord(n, contactList, n.currentPath);
            }
            return contactList;
        }

        public static string CleanWord(string oldString)
        {
            return oldString.ToLower().Trim();
        }

        //private void ReturnWords(Node startNode, List<PhoneBook> list)
        //{
        //    Node.FindWord(startNode, list, startNode.currentPath);
        //    return;
        //}

        internal void InsertWord(string p)
        {
            throw new NotImplementedException();
        }
    }

    public static class Mapping
    {
        static Dictionary<char, string> mappings;


        public static string GetMapping(char i)
        {
            if (mappings == null)
            {
                mappings = new Dictionary<char, string>();

                mappings.Add('2', "abc");
                mappings.Add('3', "def");
                mappings.Add('4', "ghi");
                mappings.Add('5', "jkl");
                mappings.Add('6', "mno");
                mappings.Add('7', "pqrs");
                mappings.Add('8', "tuv");
                mappings.Add('9', "wxyz");
            }

            if (mappings.ContainsKey(i))
                return mappings[i];
            else
                return i.ToString().ToLower();
        }
    }

    public class Node
    {
        Dictionary<char, Node> Children;
        char c;
        bool final = false;
        List<PhoneBook> contacts;
        public string currentPath = "";

        public Node(char c)
        {
            this.c = c;
            Children = new Dictionary<char, Node>();
        }

        public void MarkFinal(PhoneBook _contact)
        {
            this.final = true;
            if (contacts == null)
                contacts = new List<PhoneBook>();

                contacts.Add(new PhoneBook(_contact.PhoneNumber,_contact.DisplayName,_contact.NumberType));
        }
        
        public static void FindWord(Node current,  List<PhoneBook> list, string CurrentPath)

        {
            //if (list.Count > 2) return;
            if (current.final == true)
            {
                //if(!string.IsNullOrEmpty(current.contact.DefaultNumber))
                foreach (PhoneBook p in current.contacts)
                    p.matchedName = CurrentPath;
                list.AddRange(current.contacts);
            }

            
            foreach(Node node in current.Children.Values)
            {
                FindWord(node,list, CurrentPath);
            }

            return;
        }

        internal static void FindNodesT9(Node current, string prefix, List<Node> list, int index, string CurrentSearch)
        {
            //if (list.Count > 2) return;
            if (prefix.Length==index)
            {
                current.currentPath = CurrentSearch;
                list.Add(current);
                
               // System.Diagnostics.Debug.WriteLine(CurrentSearch);
                return;
            }

            foreach (char c in Mapping.GetMapping(prefix[index]))
            {
                if(current.Children.ContainsKey(c))
                {
                    FindNodesT9(current.Children[c], prefix, list, index + 1, CurrentSearch + c);
                }
            }

            return;
        }


        public Node Look(char c)
        {
            if (Children.ContainsKey(c))
            {
                return Children[c];
            }
            else
                return null;
        }

        public Node Insert(char c)
        {
            if (Children.ContainsKey(c))
            {
                return Children[c];
            }
            else
            {
                Node newNode = new Node(c);
                Children.Add(c, newNode);
                return newNode;
            }
        }

       
    }
}
