using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server.Accounting
{
    public class Accounts
    {
        private static Dictionary<string, IAccount> m_Accounts = new Dictionary<string, IAccount>();
		private static readonly string AccountDirectory = Config.Get("Server.AccountPath", "Saves/Accounts");


		public static void Configure()
        {
            EventSink.WorldLoad += Load;
            EventSink.WorldSave += Save;
        }

        static Accounts()
        {
        }

        public static int Count => m_Accounts.Count;

        public static ICollection<IAccount> GetAccounts()
        {
            return m_Accounts.Values;
        }

        public static IAccount GetAccount(string username)
        {
            IAccount a;

            m_Accounts.TryGetValue(username, out a);

            return a;
        }

        public static void Add(IAccount a)
        {
            m_Accounts[a.Username] = a;
        }

        public static void Remove(string username)
        {
            m_Accounts.Remove(username);
        }

        public static void Load()
        {
            m_Accounts = new Dictionary<string, IAccount>(32, StringComparer.OrdinalIgnoreCase);

			var accounts = GetAccountNode();

			if (accounts != null)
			{
				foreach (XmlElement account in accounts)
				{
					try
					{
						Account acct = new Account(account);
					}
					catch (Exception e)
					{
						Console.WriteLine("Warning: Account instance load failed");
						Diagnostics.ExceptionLogging.LogException(e);
					}
				}
			}			
        }

		public static List<AccountRecord> LoadAccountRecords()
		{
			var accountRecords = new List<AccountRecord>();

			var accounts = GetAccountNode();

			if (accounts != null)
			{
				foreach (XmlElement account in accounts)
				{
					try
					{
						var username = Utility.GetText(account["username"], "empty");
						var characters = Account.LoadCharacterRecords(account);

						if (!string.IsNullOrWhiteSpace(username))
						{
							accountRecords.Add(new AccountRecord { Username = username, Characters = characters });
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Warning: Account instance load failed");
						Diagnostics.ExceptionLogging.LogException(e);
					}
				}				
			}

			return accountRecords;
		}

		private static XmlNodeList GetAccountNode()
		{
			string filePath = Path.Combine(AccountDirectory, "accounts.xml");

			if (!File.Exists(filePath))
				return null;

			XmlDocument doc = new XmlDocument();
			doc.Load(filePath);

			XmlElement root = doc["accounts"];

			return root.GetElementsByTagName("account");
		}

		public static void Save(WorldSaveEventArgs e)
        {
			try
			{
				var accountRecords = LoadAccountRecords();

				if (!Directory.Exists(AccountDirectory))
					Directory.CreateDirectory(AccountDirectory);

				string filePath = Path.Combine(AccountDirectory, "accounts.xml");

				using (StreamWriter op = new StreamWriter(filePath))
				{
					XmlTextWriter xml = new XmlTextWriter(op)
					{
						Formatting = Formatting.Indented,
						IndentChar = '\t',
						Indentation = 1
					};

					xml.WriteStartDocument(true);

					xml.WriteStartElement("accounts");

					xml.WriteAttributeString("count", m_Accounts.Count.ToString());

					foreach (Account a in GetAccounts())
					{
						var characters = accountRecords.FirstOrDefault(account => a.Username.Equals(account.Username, StringComparison.InvariantCultureIgnoreCase))
							?.Characters ?? new List<CharacterRecord>();

						a.Save(xml, characters);
					}

					xml.WriteEndElement();

					xml.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Warning: Account file failed to save.");
				Diagnostics.ExceptionLogging.LogException(ex);
			}			
        }
    }
}
